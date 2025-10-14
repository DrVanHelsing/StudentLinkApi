namespace StudentLinkApi.DTOs;

// Registration DTO
public class RegisterDto
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string Role { get; set; } = "Student"; // Student, Recruiter, Admin
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
}

// Login DTO
public class LoginDto
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}

// Login Response DTO
public class LoginResponseDto
{
    public required string Token { get; set; }
    public required UserDto User { get; set; }
    public DateTime ExpiresAt { get; set; }
}

// User Response DTO
public class UserDto
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string Role { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Update Profile DTO
public class UpdateProfileDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
}
