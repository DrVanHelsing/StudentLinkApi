using System.ComponentModel.DataAnnotations;

namespace StudentLinkApi.Models;

public class CV
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string FileUrl { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string FileType { get; set; } = string.Empty; // pdf, docx, etc.
    
    public long FileSize { get; set; } // in bytes
    
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public User? User { get; set; }
    public ICollection<CVFeedback> Feedbacks { get; set; } = new List<CVFeedback>();
    public ICollection<CVAnalysisResult> AnalysisResults { get; set; } = new List<CVAnalysisResult>();
}