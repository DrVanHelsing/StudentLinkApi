using System.ComponentModel.DataAnnotations;

namespace StudentLinkApi.Models;

/// <summary>
/// Represents a user in the system (Student, Recruiter, or Admin)
/// </summary>
public class User
{
  [Key]
  public Guid Id { get; set; } = Guid.NewGuid();

  [Required]
  [MaxLength(256)]
  public string Email { get; set; } = string.Empty;

  [Required]
  [MaxLength(256)]
  public string PasswordHash { get; set; } = string.Empty;

  [Required]
  [MaxLength(50)]
  public string Role { get; set; } = "Student"; // Student, Recruiter, Admin

  [MaxLength(100)]
  public string? FirstName { get; set; }

  [MaxLength(100)]
  public string? LastName { get; set; }

  [MaxLength(20)]
  public string? PhoneNumber { get; set; }

  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  public DateTime? UpdatedAt { get; set; }

  public bool IsActive { get; set; } = true;

  // Navigation properties
  public Profile? Profile { get; set; }
  public ICollection<CV> CVs { get; set; } = new List<CV>();
}
