using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace StudentLinkApi.Models;

/// <summary>
/// Student profile created from parsed CV data
/// </summary>
[Table("Profiles")]
public class Profile
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [MaxLength(500)]
    public string? Summary { get; set; }

    /// <summary>
    /// JSON array of skills (e.g., ["C#", "Azure", "SQL"])
    /// </summary>
    public string? Skills { get; set; }

    /// <summary>
    /// JSON array of education history
    /// </summary>
    public string? Education { get; set; }

    /// <summary>
    /// JSON array of work experience
    /// </summary>
    public string? Experience { get; set; }

    [MaxLength(500)]
    public string? CvUrl { get; set; }

    [MaxLength(100)]
    public string? LinkedInUrl { get; set; }

    [MaxLength(100)]
    public string? GitHubUrl { get; set; }

    [MaxLength(100)]
    public string? PortfolioUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation (ignored to prevent circular JSON serialization causing /auth/me 500)
    [ForeignKey(nameof(UserId))]
    [JsonIgnore]
    public User User { get; set; } = null!;
}
