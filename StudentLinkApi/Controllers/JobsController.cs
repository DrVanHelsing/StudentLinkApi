using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentLinkApi.Data;
using StudentLinkApi.Models;
using System.Security.Claims;

namespace StudentLinkApi.Controllers;

[ApiController]
[Route("jobs")]
public class JobsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<JobsController> _logger;

    public JobsController(ApplicationDbContext context, ILogger<JobsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Public job listing with filters
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetJobs([FromQuery] string? q, [FromQuery] string? location, [FromQuery] string? jobType)
    {
        var query = _context.Jobs.AsNoTracking().Where(j => j.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(j => j.Title.Contains(q) || j.Description.Contains(q) || (j.RequiredSkills ?? "").Contains(q));
        }
        if (!string.IsNullOrWhiteSpace(location))
        {
            query = query.Where(j => j.Location == location);
        }
        if (!string.IsNullOrWhiteSpace(jobType))
        {
            query = query.Where(j => j.JobType == jobType);
        }

        var jobs = await query
            .OrderByDescending(j => j.CreatedAt)
            .Take(100)
            .ToListAsync();

        return Ok(jobs);
    }

    // Recruiter: my jobs (all, including inactive)
    [HttpGet("mine")]
    [Authorize(Roles = "Recruiter,Admin")]
    public async Task<IActionResult> GetMyJobs()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var uid = Guid.Parse(userId);

        var jobs = await _context.Jobs.AsNoTracking()
            .Where(j => j.RecruiterId == uid)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();

        return Ok(jobs);
    }

    // Get a job
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetJob(Guid id)
    {
        var job = await _context.Jobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == id);
        if (job == null) return NotFound();
        return Ok(job);
    }

    // Recruiter: create job
    [HttpPost]
    [Authorize(Roles = "Recruiter,Admin")]
    public async Task<IActionResult> CreateJob([FromBody] Job job)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        job.RecruiterId = Guid.Parse(userId);
        job.IsActive = true;
        job.CreatedAt = DateTime.UtcNow;

        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetJob), new { id = job.Id }, job);
    }

    // Recruiter/Admin: update job
    [HttpPut("{id}")]
    [Authorize(Roles = "Recruiter,Admin")]
    public async Task<IActionResult> UpdateJob(Guid id, [FromBody] Job update)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var job = await _context.Jobs.FindAsync(id);
        if (job == null) return NotFound();

        if (job.RecruiterId != Guid.Parse(userId) && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        job.Title = update.Title;
        job.Description = update.Description;
        job.RequiredSkills = update.RequiredSkills;
        job.Location = update.Location;
        job.JobType = update.JobType;
        job.SalaryMin = update.SalaryMin;
        job.SalaryMax = update.SalaryMax;
        job.ExperienceYears = update.ExperienceYears;
        job.EducationLevel = update.EducationLevel;
        job.IsActive = update.IsActive;
        job.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(job);
    }

    // Recruiter/Admin: delete job
    [HttpDelete("{id}")]
    [Authorize(Roles = "Recruiter,Admin")]
    public async Task<IActionResult> DeleteJob(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var job = await _context.Jobs.FindAsync(id);
        if (job == null) return NotFound();

        if (job.RecruiterId != Guid.Parse(userId) && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        _context.Jobs.Remove(job);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Student: apply to job
    [HttpPost("{id}/apply")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Apply(Guid id, [FromBody] string? notes)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        if (!await _context.Jobs.AnyAsync(j => j.Id == id && j.IsActive))
            return NotFound(new { error = "Job not found" });

        var app = new JobApplication
        {
            JobId = id,
            UserId = Guid.Parse(userId),
            Status = "Applied",
            Notes = notes,
            AppliedAt = DateTime.UtcNow
        };

        _context.JobApplications.Add(app);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { error = "You already applied to this job" });
        }
        return Ok(app);
    }

    // Recruiter/Admin: list applications per job
    [HttpGet("{id}/applications")]
    [Authorize(Roles = "Recruiter,Admin")]
    public async Task<IActionResult> GetJobApplications(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var job = await _context.Jobs.FindAsync(id);
        if (job == null) return NotFound();
        if (job.RecruiterId != Guid.Parse(userId) && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        var apps = await _context.JobApplications
            .Where(a => a.JobId == id)
            .OrderByDescending(a => a.AppliedAt)
            .Select(a => new {
                a.Id,
                a.JobId,
                a.UserId,
                a.Status,
                a.Notes,
                a.AppliedAt,
                a.UpdatedAt
            })
            .ToListAsync();

        return Ok(apps);
    }

    // Recruiter/Admin: update application status
    [HttpPut("applications/{appId}/status")]
    [Authorize(Roles = "Recruiter,Admin")]
    public async Task<IActionResult> UpdateApplicationStatus(Guid appId, [FromBody] string status)
    {
        var app = await _context.JobApplications.FindAsync(appId);
        if (app == null) return NotFound();

        app.Status = status;
        app.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(app);
    }

    // Student: my applications (project with job info)
    [HttpGet("applications/me")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> MyApplications()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var uid = Guid.Parse(userId);

        var apps = await _context.JobApplications
            .Where(a => a.UserId == uid)
            .OrderByDescending(a => a.AppliedAt)
            .Join(_context.Jobs,
                a => a.JobId,
                j => j.Id,
                (a, j) => new {
                    a.Id,
                    a.JobId,
                    a.Status,
                    a.Notes,
                    a.AppliedAt,
                    a.UpdatedAt,
                    jobTitle = j.Title,
                    jobLocation = j.Location,
                    jobType = j.JobType
                })
            .ToListAsync();
        return Ok(apps);
    }
}
