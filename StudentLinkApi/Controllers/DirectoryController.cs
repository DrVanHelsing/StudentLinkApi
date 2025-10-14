using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentLinkApi.Data;

namespace StudentLinkApi.Controllers;

[ApiController]
[Route("directory")]
[Authorize(Roles = "Admin,Recruiter")]
public class DirectoryController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DirectoryController> _logger;

    public DirectoryController(ApplicationDbContext context, ILogger<DirectoryController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Searchable students directory for Admin and Recruiter
    [HttpGet("students")]
    public async Task<IActionResult> GetStudents([FromQuery] string? q, [FromQuery] string? skill)
    {
        q = q?.Trim();
        skill = skill?.Trim();

        var students = await _context.Users
            .Where(u => u.Role == "Student")
            .Where(u => string.IsNullOrEmpty(q) || u.Email.Contains(q) || ((u.FirstName + " " + u.LastName).Contains(q)))
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                profile = u.Profile == null ? null : new {
                    u.Profile.Summary,
                    u.Profile.Skills
                },
                latestCV = _context.CVs
                    .Where(c => c.UserId == u.Id)
                    .OrderByDescending(c => c.UploadedAt)
                    .Select(c => new
                    {
                        c.Id,
                        c.FileName,
                        c.UploadedAt,
                        basicFeedback = _context.CVFeedbacks
                            .Where(f => f.CVId == c.Id)
                            .OrderByDescending(f => f.CreatedAt)
                            .Select(f => new { f.QualityScore, f.IsApproved })
                            .FirstOrDefault(),
                        interactive = _context.CVInteractiveFeedbacks
                            .Where(f => f.CVId == c.Id)
                            .OrderByDescending(f => f.CreatedAt)
                            .Select(f => new { f.OverallScore, f.IsApproved })
                            .FirstOrDefault()
                    })
                    .FirstOrDefault()
            })
            .ToListAsync();

        if (!string.IsNullOrEmpty(skill))
        {
            var s = skill.ToLowerInvariant();
            students = students
                .Where(u => (u.profile?.Skills ?? string.Empty).ToLowerInvariant().Contains(s))
                .ToList();
        }

        return Ok(students.Take(300));
    }

    // CVs for a student (no download in this controller)
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
}
