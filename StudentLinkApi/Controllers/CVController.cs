using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentLinkApi.Data;
using StudentLinkApi.Models;
using StudentLinkApi.Services;
using System.Security.Claims;

namespace StudentLinkApi.Controllers;

[ApiController]
[Route("cv")]
[Authorize]
public class CVController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorage;
    private readonly ICVProcessingService _cvProcessing;
    private readonly ILogger<CVController> _logger;
    private readonly long _maxFileSize = 5 * 1024 * 1024; // 5MB
    private readonly string[] _allowedExtensions = { ".pdf", ".doc", ".docx" };

    public CVController(
        ApplicationDbContext context,
        IFileStorageService fileStorage,
        ICVProcessingService cvProcessing,
        ILogger<CVController> logger)
    {
        _context = context;
        _fileStorage = fileStorage;
        _cvProcessing = cvProcessing;
        _logger = logger;
    }

    // Upload CV with AI processing
    [HttpPost("upload")]
    public async Task<IActionResult> UploadCV(IFormFile file)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            // Validate file
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file uploaded" });
            }

            if (file.Length > _maxFileSize)
            {
                return BadRequest(new { error = "File size exceeds 5MB limit" });
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(fileExtension))
            {
                return BadRequest(new { error = "Invalid file type. Only PDF, DOC, and DOCX are allowed" });
            }

            // Upload file
            using var stream = file.OpenReadStream();
            var fileUrl = await _fileStorage.UploadFileAsync(stream, file.FileName, file.ContentType);

            // Save CV metadata
            var cv = new CV
            {
                UserId = Guid.Parse(userId),
                FileName = file.FileName,
                FileUrl = fileUrl,
                FileType = fileExtension.TrimStart('.'),
                FileSize = file.Length,
                UploadedAt = DateTime.UtcNow,
                IsActive = true
            };

            // Deactivate previous CVs
            var previousCVs = await _context.CVs
                .Where(c => c.UserId == Guid.Parse(userId) && c.IsActive)
                .ToListAsync();

            foreach (var oldCV in previousCVs)
            {
                oldCV.IsActive = false;
                oldCV.UpdatedAt = DateTime.UtcNow;
            }

            _context.CVs.Add(cv);
            await _context.SaveChangesAsync();

            // Create an initial analysis record to indicate processing
            _context.CVAnalysisResults.Add(new CVAnalysisResult
            {
                CVId = cv.Id,
                ProcessingStatus = "Processing",
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            // Queue CV for AI processing
            await _cvProcessing.QueueCVForProcessingAsync(cv.Id);

            _logger.LogInformation("CV uploaded successfully for user {UserId}", userId);

            return Ok(new
            {
                message = "CV uploaded successfully and queued for AI analysis",
                cv = new
                {
                    id = cv.Id,
                    fileName = cv.FileName,
                    fileSize = cv.FileSize,
                    fileType = cv.FileType,
                    uploadedAt = cv.UploadedAt,
                    processingStatus = "Processing"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading CV");
            return StatusCode(500, new { error = "Failed to upload CV" });
        }
    }

    // Get CV feedback
    [HttpGet("{cvId}/feedback")]
    public async Task<IActionResult> GetCVFeedback(Guid cvId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            var feedback = await _context.CVFeedbacks
                .Where(f => f.CVId == cvId && f.UserId == Guid.Parse(userId))
                .OrderByDescending(f => f.CreatedAt)
                .FirstOrDefaultAsync();

            if (feedback == null)
            {
                // Check if CV is still processing
                var analysisResult = await _context.CVAnalysisResults
                    .Where(a => a.CVId == cvId)
                    .OrderByDescending(a => a.CreatedAt)
                    .FirstOrDefaultAsync();

                if (analysisResult != null && analysisResult.ProcessingStatus == "Processing")
                {
                    return Ok(new { status = "Processing", message = "Your CV is being analyzed. Please check back in a moment." });
                }

                return NotFound(new { error = "No feedback available yet" });
            }

            return Ok(new
            {
                id = feedback.Id,
                qualityScore = feedback.QualityScore,
                isApproved = feedback.IsApproved,
                overallFeedback = feedback.FeedbackText,
                structureIssues = feedback.StructureIssues,
                grammarIssues = feedback.GrammarIssues,
                missingFields = feedback.MissingFields,
                recommendations = feedback.Recommendations,
                createdAt = feedback.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting CV feedback");
            return StatusCode(500, new { error = "Failed to get feedback" });
        }
    }

    // Get CV analysis results
    [HttpGet("{cvId}/analysis")]
    public async Task<IActionResult> GetCVAnalysis(Guid cvId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            var cv = await _context.CVs.FindAsync(cvId);
            if (cv == null || cv.UserId != Guid.Parse(userId))
            {
                return NotFound(new { error = "CV not found" });
            }

            var analysis = await _context.CVAnalysisResults
                .Where(a => a.CVId == cvId)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (analysis == null)
            {
                return NotFound(new { error = "Analysis not available yet" });
            }

            return Ok(new
            {
                id = analysis.Id,
                extractedSkills = analysis.ExtractedSkills?.Split(',', StringSplitOptions.RemoveEmptyEntries),
                extractedExperience = analysis.ExtractedExperience,
                extractedEducation = analysis.ExtractedEducation,
                extractedContact = analysis.ExtractedContact,
                confidenceScore = analysis.AIConfidenceScore,
                processingStatus = analysis.ProcessingStatus,
                processedAt = analysis.ProcessedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting CV analysis");
            return StatusCode(500, new { error = "Failed to get analysis" });
        }
    }

    // Reprocess CV (for feedback loop)
    [HttpPost("{cvId}/reprocess")]
    public async Task<IActionResult> ReprocessCV(Guid cvId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            var cv = await _context.CVs.FindAsync(cvId);
            if (cv == null || cv.UserId != Guid.Parse(userId))
            {
                return NotFound(new { error = "CV not found" });
            }

            // Queue for reprocessing
            await _cvProcessing.QueueCVForProcessingAsync(cvId);

            return Ok(new { message = "CV queued for reprocessing" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reprocessing CV");
            return StatusCode(500, new { error = "Failed to reprocess CV" });
        }
    }

    // Get user's current CV
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentCV()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new { error = "Invalid token" });
            var guid = Guid.Parse(userId);

            var cv = await _context.CVs
                .Where(c => c.UserId == guid && c.IsActive)
                .OrderByDescending(c => c.UploadedAt)
                .FirstOrDefaultAsync();

            if (cv == null)
            {
                return Ok(new { hasCV = false, message = "No CV uploaded yet." });
            }

            var feedback = await _context.CVFeedbacks
                .Where(f => f.CVId == cv.Id)
                .OrderByDescending(f => f.CreatedAt)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                hasCV = true,
                id = cv.Id,
                fileName = cv.FileName,
                fileUrl = cv.FileUrl,
                fileSize = cv.FileSize,
                fileType = cv.FileType,
                uploadedAt = cv.UploadedAt,
                qualityScore = feedback?.QualityScore,
                isApproved = feedback?.IsApproved ?? false,
                hasFeedback = feedback != null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current CV");
            return StatusCode(500, new { error = "Failed to get CV" });
        }
    }

    // Get all user's CVs (history)
    [HttpGet("history")]
    public async Task<IActionResult> GetCVHistory()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            var cvs = await _context.CVs
                .Where(c => c.UserId == Guid.Parse(userId))
                .OrderByDescending(c => c.UploadedAt)
                .Select(c => new
                {
                    id = c.Id,
                    fileName = c.FileName,
                    fileSize = c.FileSize,
                    fileType = c.FileType,
                    uploadedAt = c.UploadedAt,
                    isActive = c.IsActive,
                    feedback = _context.CVFeedbacks
                        .Where(f => f.CVId == c.Id)
                        .OrderByDescending(f => f.CreatedAt)
                        .Select(f => new
                        {
                            qualityScore = f.QualityScore,
                            isApproved = f.IsApproved
                        })
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(cvs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting CV history");
            return StatusCode(500, new { error = "Failed to get CV history" });
        }
    }

    // Download CV
    [HttpGet("download/{id}")]
    public async Task<IActionResult> DownloadCV(Guid id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            var cv = await _context.CVs.FindAsync(id);

            if (cv == null || cv.UserId != Guid.Parse(userId))
            {
                return NotFound(new { error = "CV not found" });
            }

            var fileStream = await _fileStorage.DownloadFileAsync(cv.FileUrl);
            var contentType = cv.FileType switch
            {
                "pdf" => "application/pdf",
                "doc" => "application/msword",
                "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream"
            };

            return File(fileStream, contentType, cv.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading CV");
            return StatusCode(500, new { error = "Failed to download CV" });
        }
    }

    // Delete CV
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCV(Guid id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            var cv = await _context.CVs.FindAsync(id);

            if (cv == null || cv.UserId != Guid.Parse(userId))
            {
                return NotFound(new { error = "CV not found" });
            }

            // Delete file from storage
            await _fileStorage.DeleteFileAsync(cv.FileUrl);

            // Delete from database (cascade will delete feedback and analysis)
            _context.CVs.Remove(cv);
            await _context.SaveChangesAsync();

            _logger.LogInformation("CV deleted successfully: {CVId}", id);

            return Ok(new { message = "CV deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting CV");
            return StatusCode(500, new { error = "Failed to delete CV" });
        }
    }
}