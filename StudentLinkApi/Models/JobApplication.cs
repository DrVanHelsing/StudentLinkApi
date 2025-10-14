using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentLinkApi.Models;

public class JobApplication
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid JobId { get; set; }

    [Required]
    public Guid UserId { get; set; } // Student applying

    [MaxLength(30)]
    public string Status { get; set; } = "Applied"; // Applied, Reviewed, Interview, Offer, Rejected, Hired

    public string? Notes { get; set; }

    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Job? Job { get; set; }
    // Do not include navigation back to User to avoid cascade path conflicts
    // public User? User { get; set; }
}
