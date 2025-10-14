using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentLinkApi.Data;
using StudentLinkApi.Models;
using StudentLinkApi.Services;

namespace StudentLinkApi.Controllers;

[ApiController]
[Route("admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminController> _logger;
    private readonly IFileStorageService _fileStorage;

    public AdminController(ApplicationDbContext context, ILogger<AdminController> logger, IFileStorageService fileStorage)
    {
        _context = context;
        _logger = logger;
        _fileStorage = fileStorage;
    }

    // Users list with basic metrics
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _context.Users
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.Role,
                u.FirstName,
                u.LastName,
                u.IsActive,
                u.CreatedAt
            })
            .ToListAsync();
        return Ok(users);
    }

    // Activate/Deactivate user
    [HttpPut("users/{id}/status")]
    public async Task<IActionResult> SetUserStatus(Guid id, [FromBody] bool isActive)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();
        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(new { message = "User status updated" });
    }

    // Promote/demote role
    [HttpPut("users/{id}/role")]
    public async Task<IActionResult> SetUserRole(Guid id, [FromBody] string role)
    {
        var validRoles = new[] { "Student", "Recruiter", "Admin" };
        if (!validRoles.Contains(role)) return BadRequest(new { error = "Invalid role" });
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();
        user.Role = role;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(new { message = "User role updated" });
    }

    // Stats summary
    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        var totalUsers = await _context.Users.CountAsync();
        var totalJobs = await _context.Jobs.CountAsync();
        var totalApplications = await _context.JobApplications.CountAsync();
        var totalCVs = await _context.CVs.CountAsync();

        return Ok(new
        {
            totalUsers,
            totalJobs,
            totalApplications,
            totalCVs
        });
    }

    // Admin: list students with their latest CV + feedback (searchable)
    [HttpGet("students")]
    public async Task<IActionResult> GetStudents([FromQuery] string? q)
    {
        q = q?.Trim();
        var students = await _context.Users
            .Where(u => u.Role == "Student")
            .Where(u => string.IsNullOrEmpty(q) ||
                        (u.Email.Contains(q) || (u.FirstName + " " + u.LastName).Contains(q)))
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.IsActive,
                profile = u.Profile,
                latestCV = _context.CVs
                    .Where(c => c.UserId == u.Id)
                    .OrderByDescending(c => c.UploadedAt)
                    .Select(c => new
                    {
                        c.Id,
                        c.FileName,
                        c.FileType,
                        c.FileSize,
                        c.UploadedAt,
                        feedback = _context.CVFeedbacks
                            .Where(f => f.CVId == c.Id)
                            .OrderByDescending(f => f.CreatedAt)
                            .Select(f => new { f.QualityScore, f.IsApproved, f.CreatedAt })
                            .FirstOrDefault()
                    })
                    .FirstOrDefault()
            })
            .OrderBy(u => u.FirstName)
            .Take(200)
            .ToListAsync();

        return Ok(students);
    }

    // Admin: get all CVs for a student with latest feedback
    [HttpGet("students/{id}/cvs")]
    public async Task<IActionResult> GetStudentCvs(Guid id)
    {
        var exists = await _context.Users.AnyAsync(u => u.Id == id && u.Role == "Student");
        if (!exists) return NotFound();

        var cvs = await _context.CVs
            .Where(c => c.UserId == id)
            .OrderByDescending(c => c.UploadedAt)
            .Select(c => new
            {
                c.Id,
                c.FileName,
                c.FileType,
                c.FileSize,
                c.UploadedAt,
                c.IsActive,
                basicFeedback = _context.CVFeedbacks
                    .Where(f => f.CVId == c.Id)
                    .OrderByDescending(f => f.CreatedAt)
                    .Select(f => new { f.QualityScore, f.IsApproved, f.FeedbackText, f.CreatedAt })
                    .FirstOrDefault(),
                interactive = _context.CVInteractiveFeedbacks
                    .Where(f => f.CVId == c.Id)
                    .OrderByDescending(f => f.CreatedAt)
                    .Select(f => new { f.OverallScore, f.IsApproved, f.NextSteps, f.CreatedAt })
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(cvs);
    }

    // Admin: download any CV
    [HttpGet("cv/{cvId}/download")]
    public async Task<IActionResult> DownloadAnyCv(Guid cvId)
    {
        var cv = await _context.CVs.FindAsync(cvId);
        if (cv == null) return NotFound();
        var stream = await _fileStorage.DownloadFileAsync(cv.FileUrl);
        var contentType = cv.FileType switch
        {
            "pdf" => "application/pdf",
            "doc" => "application/msword",
            "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };
        return File(stream, contentType, cv.FileName);
    }

    // Admin: list recruiters (searchable)
    [HttpGet("recruiters")]
    public async Task<IActionResult> GetRecruiters([FromQuery] string? q)
    {
        q = q?.Trim();
        var recruiters = await _context.Users
            .Where(u => u.Role == "Recruiter")
            .Where(u => string.IsNullOrEmpty(q) ||
                        (u.Email.Contains(q) || (u.FirstName + " " + u.LastName).Contains(q)))
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                jobsCount = _context.Jobs.Count(j => j.RecruiterId == u.Id),
                recentJob = _context.Jobs
                    .Where(j => j.RecruiterId == u.Id)
                    .OrderByDescending(j => j.CreatedAt)
                    .Select(j => new { j.Id, j.Title, j.IsActive, j.CreatedAt })
                    .FirstOrDefault()
            })
            .OrderBy(u => u.FirstName)
            .Take(200)
            .ToListAsync();
        return Ok(recruiters);
    }

    // Admin: list all jobs (searchable)
    [HttpGet("jobs")]
    public async Task<IActionResult> GetAllJobs([FromQuery] string? q)
    {
        q = q?.Trim();
        var jobs = await _context.Jobs
            .Where(j => string.IsNullOrEmpty(q) ||
                        j.Title.Contains(q) || j.Description.Contains(q) || (j.RequiredSkills ?? "").Contains(q))
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => new
            {
                j.Id,
                j.Title,
                j.Location,
                j.JobType,
                j.IsActive,
                j.CreatedAt,
                recruiter = _context.Users.Where(u => u.Id == j.RecruiterId).Select(u => new { u.Id, u.Email }).FirstOrDefault()
            })
            .Take(500)
            .ToListAsync();
        return Ok(jobs);
    }

    // Admin: recruiter jobs (searchable)
    [HttpGet("recruiters/{id}/jobs")]
    public async Task<IActionResult> GetRecruiterJobs(Guid id, [FromQuery] string? q)
    {
        var exists = await _context.Users.AnyAsync(u => u.Id == id && u.Role == "Recruiter");
        if (!exists) return NotFound();
        q = q?.Trim();
        var jobs = await _context.Jobs
            .Where(j => j.RecruiterId == id)
            .Where(j => string.IsNullOrEmpty(q) ||
                        j.Title.Contains(q) || j.Description.Contains(q) || (j.RequiredSkills ?? "").Contains(q))
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();
        return Ok(jobs);
    }
}
