using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentLinkApi.Data;
using StudentLinkApi.Services;
using System.Security.Claims;
using System.Text.Json;

namespace StudentLinkApi.Controllers;

[ApiController]
[Route("cv/interactive")]
[Authorize]
public class InteractiveFeedbackController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InteractiveFeedbackController> _logger;
    private readonly ICVProcessingService _cvProcessing;

    public InteractiveFeedbackController(
        ApplicationDbContext context,
        ILogger<InteractiveFeedbackController> logger,
        ICVProcessingService cvProcessing)
    {
        _context = context;
        _logger = logger;
        _cvProcessing = cvProcessing;
    }

    // GET /cv/interactive/{cvId}/feedback - Get detailed interactive feedback
    [HttpGet("{cvId}/feedback")]
    public async Task<IActionResult> GetInteractiveFeedback(Guid cvId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "Invalid token" });
            }
            var userGuid = Guid.Parse(userId);

            var feedback = await _context.CVInteractiveFeedbacks
                .Where(f => f.CVId == cvId && f.UserId == userGuid)
                .OrderByDescending(f => f.CreatedAt)
                .FirstOrDefaultAsync();

            if (feedback == null)
            {
                // Check analysis status to avoid 404 while processing
                var analysis = await _context.CVAnalysisResults
                    .Where(a => a.CVId == cvId)
                    .OrderByDescending(a => a.CreatedAt)
                    .FirstOrDefaultAsync();

                if (analysis != null && (analysis.ProcessingStatus == "Processing" || analysis.ProcessingStatus == "Pending"))
                {
                    return Ok(new
                    {
                        status = "Processing",
                        message = "Your CV is being analyzed. Please wait a moment...",
                        overallScore = 0,
                        isApproved = false,
                        sections = new
                        {
                            contact = new { feedback = string.Empty, score = 0 },
                            summary = new { feedback = string.Empty, score = 0 },
                            experience = new { feedback = string.Empty, score = 0 },
                            education = new { feedback = string.Empty, score = 0 },
                            skills = new { feedback = string.Empty, score = 0 }
                        },
                        improvementPriorities = Array.Empty<object>(),
                        nextSteps = "We will notify you when analysis completes.",
                        improvementFromPrevious = (string?)null,
                        createdAt = DateTime.UtcNow
                    });
                }

                // Fallback: build a minimal interactive response from basic CVFeedback if available
                var basic = await _context.CVFeedbacks
                    .Where(f => f.CVId == cvId && f.UserId == userGuid)
                    .OrderByDescending(f => f.CreatedAt)
                    .FirstOrDefaultAsync();

                if (basic != null)
                {
                    var overall = basic.QualityScore;
                    return Ok(new
                    {
                        overallScore = overall,
                        isApproved = basic.IsApproved,
                        sections = new
                        {
                            contact = new { feedback = basic.MissingFields ?? string.Empty, score = overall },
                            summary = new { feedback = basic.FeedbackText, score = overall },
                            experience = new { feedback = basic.StructureIssues ?? string.Empty, score = overall },
                            education = new { feedback = basic.Recommendations ?? string.Empty, score = overall },
                            skills = new { feedback = basic.GrammarIssues ?? string.Empty, score = overall }
                        },
                        improvementPriorities = Array.Empty<object>(),
                        nextSteps = basic.Recommendations,
                        improvementFromPrevious = (string?)null,
                        createdAt = basic.CreatedAt
                    });
                }

                // If no records exist at all but the CV belongs to the user, trigger processing now
                var cv = await _context.CVs.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cvId && c.UserId == userGuid);
                if (cv != null)
                {
                    await _cvProcessing.QueueCVForProcessingAsync(cvId);
                    return Ok(new
                    {
                        status = "Processing",
                        message = "We started analyzing your CV. Please refresh in a few seconds.",
                        overallScore = 0,
                        isApproved = false,
                        sections = new
                        {
                            contact = new { feedback = string.Empty, score = 0 },
                            summary = new { feedback = string.Empty, score = 0 },
                            experience = new { feedback = string.Empty, score = 0 },
                            education = new { feedback = string.Empty, score = 0 },
                            skills = new { feedback = string.Empty, score = 0 }
                        },
                        improvementPriorities = Array.Empty<object>(),
                        nextSteps = "We will notify you when analysis completes.",
                        improvementFromPrevious = (string?)null,
                        createdAt = DateTime.UtcNow
                    });
                }

                return NotFound(new { error = "Interactive feedback not available yet" });
            }

            var improvementPriorities = string.IsNullOrEmpty(feedback.ImprovementPriorities)
                ? new List<object>()
                : JsonSerializer.Deserialize<List<object>>(feedback.ImprovementPriorities);

            return Ok(new
            {
                overallScore = feedback.OverallScore,
                isApproved = feedback.IsApproved,
                sections = new
                {
                    contact = new
                    {
                        feedback = feedback.ContactSectionFeedback,
                        score = feedback.ContactSectionScore
                    },
                    summary = new
                    {
                        feedback = feedback.SummarySectionFeedback,
                        score = feedback.SummarySectionScore
                    },
                    experience = new
                    {
                        feedback = feedback.ExperienceSectionFeedback,
                        score = feedback.ExperienceSectionScore
                    },
                    education = new
                    {
                        feedback = feedback.EducationSectionFeedback,
                        score = feedback.EducationSectionScore
                    },
                    skills = new
                    {
                        feedback = feedback.SkillsSectionFeedback,
                        score = feedback.SkillsSectionScore
                    }
                },
                improvementPriorities,
                nextSteps = feedback.NextSteps,
                improvementFromPrevious = feedback.ImprovementFromPrevious,
                createdAt = feedback.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting interactive feedback for CV {CvId}", cvId);
            return StatusCode(500, new { error = "Failed to get interactive feedback" });
        }
    }

    // GET /cv/interactive/progress - Get improvement progress
    [HttpGet("progress")]
    public async Task<IActionResult> GetProgress()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            var progress = await _context.CVImprovementProgresses
                .FirstOrDefaultAsync(p => p.UserId == Guid.Parse(userId));

            if (progress == null)
            {
                // Fallback: compute minimal progress from CVs/feedbacks
                var uploads = await _context.CVs.CountAsync(c => c.UserId == Guid.Parse(userId));
                var latestFeedback = await _context.CVFeedbacks
                    .Where(f => f.UserId == Guid.Parse(userId))
                    .OrderByDescending(f => f.CreatedAt)
                    .FirstOrDefaultAsync();

                var initialFeedback = await _context.CVFeedbacks
                    .Where(f => f.UserId == Guid.Parse(userId))
                    .OrderBy(f => f.CreatedAt)
                    .FirstOrDefaultAsync();

                if (uploads == 0 || latestFeedback == null || initialFeedback == null)
                {
                    return NotFound(new { error = "No progress data available. Upload a CV to start tracking!" });
                }

                var improvement = initialFeedback.QualityScore > 0
                    ? ((latestFeedback.QualityScore - initialFeedback.QualityScore) / initialFeedback.QualityScore) * 100
                    : 0;

                return Ok(new
                {
                    totalUploads = uploads,
                    initialScore = initialFeedback.QualityScore,
                    currentScore = latestFeedback.QualityScore,
                    improvementPercentage = improvement,
                    completedActions = 0,
                    totalActions = 0,
                    progressPercentage = 0,
                    firstUploadDate = initialFeedback.CreatedAt,
                    lastUpdateDate = latestFeedback.CreatedAt,
                    daysInProgress = (DateTime.UtcNow - initialFeedback.CreatedAt).Days
                });
            }

            return Ok(new
            {
                totalUploads = progress.TotalUploads,
                initialScore = progress.InitialScore,
                currentScore = progress.CurrentScore,
                improvementPercentage = progress.ImprovementPercentage,
                completedActions = progress.CompletedActions,
                totalActions = progress.TotalActions,
                progressPercentage = progress.TotalActions > 0 
                    ? (decimal)progress.CompletedActions / progress.TotalActions * 100 
                    : 0,
                firstUploadDate = progress.FirstUploadDate,
                lastUpdateDate = progress.LastUpdateDate,
                daysInProgress = (DateTime.UtcNow - progress.FirstUploadDate).Days
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting progress");
            return StatusCode(500, new { error = "Failed to get progress" });
        }
    }

    // POST /cv/interactive/{cvId}/action/{actionIndex}/complete - Mark action as completed
    [HttpPost("{cvId}/action/{actionIndex}/complete")]
    public async Task<IActionResult> MarkActionCompleted(Guid cvId, int actionIndex)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            var feedback = await _context.CVInteractiveFeedbacks
                .Where(f => f.CVId == cvId && f.UserId == Guid.Parse(userId))
                .OrderByDescending(f => f.CreatedAt)
                .FirstOrDefaultAsync();

            if (feedback == null)
            {
                return NotFound(new { error = "Feedback not found" });
            }

            if (string.IsNullOrEmpty(feedback.ImprovementPriorities))
            {
                return BadRequest(new { error = "No improvement priorities found" });
            }

            var priorities = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(feedback.ImprovementPriorities);
            if (priorities == null || actionIndex < 0 || actionIndex >= priorities.Count)
            {
                return BadRequest(new { error = "Invalid action index" });
            }

            priorities[actionIndex]["isCompleted"] = true;
            feedback.ImprovementPriorities = JsonSerializer.Serialize(priorities);

            // Update progress
            var progress = await _context.CVImprovementProgresses
                .FirstOrDefaultAsync(p => p.UserId == Guid.Parse(userId));

            if (progress != null)
            {
                progress.CompletedActions++;
                progress.LastUpdateDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Action marked as completed",
                completedActions = progress?.CompletedActions ?? 0,
                totalActions = progress?.TotalActions ?? 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking action as completed");
            return StatusCode(500, new { error = "Failed to mark action as completed" });
        }
    }

    // GET /cv/interactive/dashboard - Get complete dashboard data
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            var userGuid = Guid.Parse(userId);

            // Get current CV
            var currentCV = await _context.CVs
                .Where(c => c.UserId == userGuid && c.IsActive)
                .OrderByDescending(c => c.UploadedAt)
                .FirstOrDefaultAsync();

            if (currentCV == null)
            {
                return Ok(new
                {
                    hasCV = false,
                    message = "No CV uploaded yet. Upload your first CV to get started!"
                });
            }

            // Get latest interactive feedback
            var latestInteractive = await _context.CVInteractiveFeedbacks
                .Where(f => f.CVId == currentCV.Id)
                .OrderByDescending(f => f.CreatedAt)
                .FirstOrDefaultAsync();

            // Fallback to basic feedback if interactive missing
            var latestBasic = await _context.CVFeedbacks
                .Where(f => f.CVId == currentCV.Id)
                .OrderByDescending(f => f.CreatedAt)
                .FirstOrDefaultAsync();

            // Get progress
            var progress = await _context.CVImprovementProgresses
                .FirstOrDefaultAsync(p => p.UserId == userGuid);

            // Get all CVs with scores
            var cvHistory = await _context.CVs
                .Where(c => c.UserId == userGuid)
                .OrderByDescending(c => c.UploadedAt)
                .Select(c => new
                {
                    id = c.Id,
                    fileName = c.FileName,
                    uploadedAt = c.UploadedAt,
                    isActive = c.IsActive,
                    feedback = _context.CVInteractiveFeedbacks
                        .Where(f => f.CVId == c.Id)
                        .OrderByDescending(f => f.CreatedAt)
                        .Select(f => new
                        {
                            overallScore = f.OverallScore,
                            isApproved = f.IsApproved
                        })
                        .FirstOrDefault() ?? _context.CVFeedbacks
                        .Where(f => f.CVId == c.Id)
                        .OrderByDescending(f => f.CreatedAt)
                        .Select(f => new
                        {
                            overallScore = f.QualityScore,
                            isApproved = f.IsApproved
                        })
                        .FirstOrDefault()
                })
                .Take(5)
                .ToListAsync();

            return Ok(new
            {
                hasCV = true,
                currentCV = new
                {
                    id = currentCV.Id,
                    fileName = currentCV.FileName,
                    uploadedAt = currentCV.UploadedAt,
                    feedback = latestInteractive != null ? new
                    {
                        overallScore = latestInteractive.OverallScore,
                        isApproved = latestInteractive.IsApproved,
                        nextSteps = latestInteractive.NextSteps
                    } : latestBasic != null ? new
                    {
                        overallScore = latestBasic.QualityScore,
                        isApproved = latestBasic.IsApproved,
                        nextSteps = latestBasic.Recommendations
                    } : null
                },
                progress = progress != null ? new
                {
                    totalUploads = progress.TotalUploads,
                    improvementPercentage = progress.ImprovementPercentage,
                    completedActions = progress.CompletedActions,
                    totalActions = progress.TotalActions
                } : null,
                recentCVs = cvHistory
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard");
            return StatusCode(500, new { error = "Failed to get dashboard data" });
        }
    }
}