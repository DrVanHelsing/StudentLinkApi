using System.ComponentModel.DataAnnotations;

namespace StudentLinkApi.Models;

// Interactive feedback with detailed section analysis
public class CVInteractiveFeedback
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid CVId { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    // Overall metrics
    public decimal OverallScore { get; set; } // 0.0 - 1.0
    public bool IsApproved { get; set; }
    
    // Section-specific feedback
    public string? ContactSectionFeedback { get; set; }
    public decimal ContactSectionScore { get; set; }
    
    public string? SummarySectionFeedback { get; set; }
    public decimal SummarySectionScore { get; set; }
    
    public string? ExperienceSectionFeedback { get; set; }
    public decimal ExperienceSectionScore { get; set; }
    
    public string? EducationSectionFeedback { get; set; }
    public decimal EducationSectionScore { get; set; }
    
    public string? SkillsSectionFeedback { get; set; }
    public decimal SkillsSectionScore { get; set; }
    
    // Improvement priorities (JSON array of action items)
    public string? ImprovementPriorities { get; set; }
    
    // Next steps for user
    public string? NextSteps { get; set; }
    
    // Comparison with previous version (if exists)
    public string? ImprovementFromPrevious { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public CV? CV { get; set; }
    public User? User { get; set; }
}

// Model for improvement action items
public class ImprovementAction
{
    public string Section { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium"; // High, Medium, Low
    public string Action { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}

// Progress tracking for CV improvement
public class CVImprovementProgress
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserId { get; set; }
    
    public int TotalUploads { get; set; }
    public decimal InitialScore { get; set; }
    public decimal CurrentScore { get; set; }
    public decimal ImprovementPercentage { get; set; }
    
    public int CompletedActions { get; set; }
    public int TotalActions { get; set; }
    
    public DateTime FirstUploadDate { get; set; }
    public DateTime LastUpdateDate { get; set; }
    
    // Navigation
    public User? User { get; set; }
}