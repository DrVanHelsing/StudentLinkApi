using System.ComponentModel.DataAnnotations;

namespace StudentLinkApi.Models;

public class CVFeedback
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid CVId { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    public string FeedbackText { get; set; } = string.Empty;
    
    public decimal QualityScore { get; set; } // 0.0 - 1.0
    
    public string? StructureIssues { get; set; }
    public string? GrammarIssues { get; set; }
    public string? MissingFields { get; set; }
    public string? Recommendations { get; set; }
    
    public bool IsApproved { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public CV? CV { get; set; }
    public User? User { get; set; }
}

public class CVAnalysisResult
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid CVId { get; set; }
    
    public string? ExtractedText { get; set; }
    public string? ExtractedSkills { get; set; }
    public string? ExtractedExperience { get; set; }
    public string? ExtractedEducation { get; set; }
    public string? ExtractedContact { get; set; }
    
    public decimal AIConfidenceScore { get; set; }
    
    public string? ProcessingStatus { get; set; } // Pending, Processing, Completed, Failed
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    
    // Navigation property
    public CV? CV { get; set; }
}

public class JobMatch
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    public Guid JobId { get; set; }
    
    public decimal MatchScore { get; set; } // 0.0 - 1.0
    
    public string? MatchReason { get; set; }
    
    public bool IsViewed { get; set; } = false;
    public bool IsApplied { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public User? User { get; set; }
    public Job? Job { get; set; }
}

public class Job
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid RecruiterId { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? RequiredSkills { get; set; }
    
    [MaxLength(200)]
    public string? Location { get; set; }
    
    [MaxLength(50)]
    public string? JobType { get; set; } // Full-time, Part-time, Contract, Internship
    
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    
    public int? ExperienceYears { get; set; }
    
    [MaxLength(100)]
    public string? EducationLevel { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    
    // Navigation property
    public User? Recruiter { get; set; }
}