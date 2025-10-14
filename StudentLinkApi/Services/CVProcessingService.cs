using Azure.Messaging.ServiceBus;
using StudentLinkApi.Data;
using StudentLinkApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace StudentLinkApi.Services;

public interface ICVProcessingService
{
    Task QueueCVForProcessingAsync(Guid cvId);
    Task ProcessCVAsync(Guid cvId);
}

public class CVProcessingService : ICVProcessingService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CVProcessingService> _logger;
    private readonly ServiceBusClient? _serviceBusClient;
    private readonly ServiceBusSender? _sender;
    private readonly bool _useServiceBus;

    public CVProcessingService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<CVProcessingService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        // Optional: Use Service Bus for async processing
        var serviceBusConnection = configuration["Azure:ServiceBus:ConnectionString"];
        var queueName = configuration["Azure:ServiceBus:QueueName"] ?? "cv-processing-queue";
        
        if (!string.IsNullOrEmpty(serviceBusConnection))
        {
            try
            {
                _serviceBusClient = new ServiceBusClient(serviceBusConnection);
                _sender = _serviceBusClient.CreateSender(queueName);
                _useServiceBus = true;
                _logger.LogInformation("Service Bus configured with queue: {QueueName}", queueName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Service Bus configuration failed. Will still process CVs immediately.");
                _useServiceBus = false;
            }
        }
        else
        {
            _logger.LogInformation("Service Bus not configured. Will process CVs immediately.");
            _useServiceBus = false;
        }
    }

    // Enqueue AND process immediately for responsive UX and background redundancy
    public async Task QueueCVForProcessingAsync(Guid cvId)
    {
        // Best-effort enqueue
        if (_useServiceBus && _sender != null)
        {
            try
            {
                var message = new ServiceBusMessage(JsonSerializer.Serialize(new { CvId = cvId }));
                await _sender.SendMessageAsync(message);
                _logger.LogInformation("CV {CvId} enqueued to Service Bus for background processing", cvId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to enqueue CV {CvId}. Proceeding with immediate processing.", cvId);
            }
        }
        else
        {
            _logger.LogInformation("Service Bus unavailable. Proceeding with immediate processing for CV {CvId}", cvId);
        }

        // Always process immediately
        await ProcessCVAsync(cvId);
    }

    public async Task ProcessCVAsync(Guid cvId)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var documentService = scope.ServiceProvider.GetRequiredService<IDocumentAnalysisService>();
        var openAIService = scope.ServiceProvider.GetRequiredService<IAzureOpenAIService>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();

        try
        {
            _logger.LogInformation("Starting CV processing for {CvId}", cvId);

            var cv = await context.CVs
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == cvId);

            if (cv == null)
            {
                _logger.LogWarning("CV {CvId} not found", cvId);
                return;
            }

            // Idempotency: if already processed after this upload, skip duplicate work
            var alreadyProcessedAt = await context.CVAnalysisResults
                .Where(a => a.CVId == cvId && a.ProcessingStatus == "Completed")
                .OrderByDescending(a => a.ProcessedAt)
                .Select(a => a.ProcessedAt)
                .FirstOrDefaultAsync();

            if (alreadyProcessedAt != default && alreadyProcessedAt >= cv.UploadedAt)
            {
                _logger.LogInformation("CV {CvId} already processed at {ProcessedAt}. Skipping duplicate run.", cvId, alreadyProcessedAt);
                return;
            }

            // Step 1: Download CV file and copy to seekable MemoryStream
            using var originalStream = await fileStorage.DownloadFileAsync(cv.FileUrl);
            using var seekableStream = new MemoryStream();
            await originalStream.CopyToAsync(seekableStream);
            seekableStream.Position = 0; // Reset position to beginning

            // Step 2: Extract text using Form Recognizer
            var extractedData = await documentService.ExtractStructuredDataAsync(seekableStream);

            // Step 3: Get previous CV text for comparison (if exists)
            var previousCV = await context.CVs
                .Where(c => c.UserId == cv.UserId && c.Id != cvId && c.UploadedAt < cv.UploadedAt)
                .OrderByDescending(c => c.UploadedAt)
                .FirstOrDefaultAsync();

            string? previousCVText = null;
            if (previousCV != null)
            {
                var previousAnalysis = await context.CVAnalysisResults
                    .Where(a => a.CVId == previousCV.Id)
                    .OrderByDescending(a => a.ProcessedAt)
                    .FirstOrDefaultAsync();
                previousCVText = previousAnalysis?.ExtractedText;
            }

            // Step 4: Run basic quality analysis first (gives us a reliable baseline)
            var qualityAnalysis = await openAIService.AnalyzeCVQualityAsync(extractedData.FullText);

            // Step 5: Run interactive analysis (section-by-section). If parsing fails, we'll fallback to quality.
            var interactiveAnalysis = await openAIService.AnalyzeCVInteractiveAsync(extractedData.FullText, previousCVText);

            bool interactiveLooksEmpty = interactiveAnalysis == null ||
                (interactiveAnalysis.OverallScore == 0 &&
                 string.IsNullOrWhiteSpace(interactiveAnalysis.ContactSectionFeedback) &&
                 string.IsNullOrWhiteSpace(interactiveAnalysis.SummarySectionFeedback) &&
                 string.IsNullOrWhiteSpace(interactiveAnalysis.ExperienceSectionFeedback) &&
                 string.IsNullOrWhiteSpace(interactiveAnalysis.EducationSectionFeedback) &&
                 string.IsNullOrWhiteSpace(interactiveAnalysis.SkillsSectionFeedback) &&
                 (interactiveAnalysis.ImprovementPriorities?.Count ?? 0) == 0);

            if (interactiveLooksEmpty)
            {
                _logger.LogWarning("Interactive analysis returned empty result. Falling back to quality analysis for CV {CvId}", cvId);
                interactiveAnalysis = new InteractiveCVAnalysis
                {
                    OverallScore = qualityAnalysis.QualityScore,
                    IsApproved = qualityAnalysis.IsApproved,
                    ContactSectionFeedback = string.IsNullOrWhiteSpace(qualityAnalysis.MissingFields) ? "Provide clear contact details (email, phone)." : qualityAnalysis.MissingFields,
                    ContactSectionScore = qualityAnalysis.QualityScore,
                    SummarySectionFeedback = string.IsNullOrWhiteSpace(qualityAnalysis.OverallFeedback) ? "Add a concise professional summary with your value proposition." : qualityAnalysis.OverallFeedback,
                    SummarySectionScore = qualityAnalysis.QualityScore,
                    ExperienceSectionFeedback = string.IsNullOrWhiteSpace(qualityAnalysis.StructureIssues) ? "Use bullet points with measurable achievements." : qualityAnalysis.StructureIssues,
                    ExperienceSectionScore = qualityAnalysis.QualityScore,
                    EducationSectionFeedback = string.IsNullOrWhiteSpace(qualityAnalysis.Recommendations) ? "List relevant courses, GPA if strong, and certifications." : qualityAnalysis.Recommendations,
                    EducationSectionScore = qualityAnalysis.QualityScore,
                    SkillsSectionFeedback = string.IsNullOrWhiteSpace(qualityAnalysis.GrammarIssues) ? "Group technical and soft skills; keep to the most relevant." : qualityAnalysis.GrammarIssues,
                    SkillsSectionScore = qualityAnalysis.QualityScore,
                    ImprovementPriorities = new List<ImprovementActionItem>
                    {
                        new() { Section = "Summary", Priority = "High", Action = "Write a 2-3 sentence summary emphasizing outcomes", Reason = "Improves first impression", IsCompleted = false },
                        new() { Section = "Experience", Priority = "High", Action = "Add 2-3 measurable achievements per role", Reason = "Shows impact", IsCompleted = false },
                        new() { Section = "Skills", Priority = "Medium", Action = "Group and prioritize skills relevant to target roles", Reason = "Improves readability", IsCompleted = false }
                    },
                    NextSteps = string.IsNullOrWhiteSpace(qualityAnalysis.Recommendations) ? "Implement the top 2 actions, then upload an improved version." : qualityAnalysis.Recommendations,
                    ImprovementFromPrevious = null
                };
            }

            // Step 6: Extract skills
            var skills = await openAIService.ExtractSkillsAsync(extractedData.FullText);

            // Step 7: Save analysis results
            var analysisResult = new CVAnalysisResult
            {
                CVId = cvId,
                ExtractedText = extractedData.FullText,
                ExtractedSkills = string.Join(", ", skills),
                ExtractedExperience = extractedData.Experience,
                ExtractedEducation = extractedData.Education,
                ExtractedContact = $"{extractedData.Email ?? ""} {extractedData.Phone ?? ""}".Trim(),
                AIConfidenceScore = (decimal)(interactiveAnalysis?.OverallScore ?? qualityAnalysis.QualityScore),
                ProcessingStatus = "Completed",
                ProcessedAt = DateTime.UtcNow
            };

            context.CVAnalysisResults.Add(analysisResult);

            // Step 8: Save basic feedback (for backward compatibility)
            var feedback = new CVFeedback
            {
                CVId = cvId,
                UserId = cv.UserId,
                FeedbackText = qualityAnalysis.OverallFeedback,
                QualityScore = (decimal)qualityAnalysis.QualityScore,
                StructureIssues = qualityAnalysis.StructureIssues,
                GrammarIssues = qualityAnalysis.GrammarIssues,
                MissingFields = qualityAnalysis.MissingFields,
                Recommendations = qualityAnalysis.Recommendations,
                IsApproved = interactiveAnalysis?.IsApproved ?? qualityAnalysis.IsApproved
            };

            context.CVFeedbacks.Add(feedback);

            // Step 9: Save interactive feedback
            var interactiveFeedback = new CVInteractiveFeedback
            {
                CVId = cvId,
                UserId = cv.UserId,
                OverallScore = (decimal)(interactiveAnalysis?.OverallScore ?? qualityAnalysis.QualityScore),
                IsApproved = interactiveAnalysis?.IsApproved ?? qualityAnalysis.IsApproved,
                ContactSectionFeedback = interactiveAnalysis?.ContactSectionFeedback,
                ContactSectionScore = (decimal)(interactiveAnalysis?.ContactSectionScore ?? qualityAnalysis.QualityScore),
                SummarySectionFeedback = interactiveAnalysis?.SummarySectionFeedback,
                SummarySectionScore = (decimal)(interactiveAnalysis?.SummarySectionScore ?? qualityAnalysis.QualityScore),
                ExperienceSectionFeedback = interactiveAnalysis?.ExperienceSectionFeedback,
                ExperienceSectionScore = (decimal)(interactiveAnalysis?.ExperienceSectionScore ?? qualityAnalysis.QualityScore),
                EducationSectionFeedback = interactiveAnalysis?.EducationSectionFeedback,
                EducationSectionScore = (decimal)(interactiveAnalysis?.EducationSectionScore ?? qualityAnalysis.QualityScore),
                SkillsSectionFeedback = interactiveAnalysis?.SkillsSectionFeedback,
                SkillsSectionScore = (decimal)(interactiveAnalysis?.SkillsSectionScore ?? qualityAnalysis.QualityScore),
                ImprovementPriorities = JsonSerializer.Serialize(interactiveAnalysis?.ImprovementPriorities ?? new List<ImprovementActionItem>()),
                NextSteps = interactiveAnalysis?.NextSteps ?? qualityAnalysis.Recommendations,
                ImprovementFromPrevious = interactiveAnalysis?.ImprovementFromPrevious
            };

            context.CVInteractiveFeedbacks.Add(interactiveFeedback);

            // Step 10: Update or create improvement progress
            var progress = await context.CVImprovementProgresses
                .FirstOrDefaultAsync(p => p.UserId == cv.UserId);

            if (progress == null)
            {
                progress = new CVImprovementProgress
                {
                    UserId = cv.UserId,
                    TotalUploads = 1,
                    InitialScore = (decimal)(interactiveAnalysis?.OverallScore ?? qualityAnalysis.QualityScore),
                    CurrentScore = (decimal)(interactiveAnalysis?.OverallScore ?? qualityAnalysis.QualityScore),
                    ImprovementPercentage = 0,
                    CompletedActions = 0,
                    TotalActions = interactiveAnalysis?.ImprovementPriorities?.Count ?? 0,
                    FirstUploadDate = DateTime.UtcNow,
                    LastUpdateDate = DateTime.UtcNow
                };
                context.CVImprovementProgresses.Add(progress);
            }
            else
            {
                progress.TotalUploads++;
                progress.CurrentScore = (decimal)(interactiveAnalysis?.OverallScore ?? qualityAnalysis.QualityScore);
                progress.ImprovementPercentage = progress.InitialScore > 0 
                    ? ((progress.CurrentScore - progress.InitialScore) / progress.InitialScore) * 100 
                    : 0;
                progress.TotalActions = interactiveAnalysis?.ImprovementPriorities?.Count ?? 0;
                progress.LastUpdateDate = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();

            _logger.LogInformation("CV processing completed for {CvId} with interactive score {Score}", 
                cvId, interactiveAnalysis?.OverallScore ?? qualityAnalysis.QualityScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CV {CvId}", cvId);
            
            // Mark as failed and add minimal placeholders
            var failedResult = new CVAnalysisResult
            {
                CVId = cvId,
                ProcessingStatus = "Failed",
                ProcessedAt = DateTime.UtcNow
            };
            
            using var scope2 = _serviceProvider.CreateScope();
            var context2 = scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Try to fetch CV to get UserId for fallback feedback
            var cvEntity = await context2.CVs.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cvId);
            if (cvEntity != null)
            {
                // Create a minimal basic feedback so UI fallback can render
                var placeholder = new CVFeedback
                {
                    CVId = cvId,
                    UserId = cvEntity.UserId,
                    FeedbackText = "We couldn't complete AI analysis at this time. Try re-uploading later.",
                    QualityScore = 0.5m,
                    Recommendations = "Ensure the CV is clear and concise. Include key projects and achievements.",
                    IsApproved = false,
                    CreatedAt = DateTime.UtcNow
                };
                context2.CVFeedbacks.Add(placeholder);
            }

            context2.CVAnalysisResults.Add(failedResult);
            await context2.SaveChangesAsync();
        }
    }
}