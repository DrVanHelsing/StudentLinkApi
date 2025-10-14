using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentLinkApi.Data;
using StudentLinkApi.DTOs;
using StudentLinkApi.Models;
using StudentLinkApi.Services;
using System.Security.Claims;

namespace StudentLinkApi.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ApplicationDbContext context,
        IJwtService jwtService,
        ILogger<AuthController> logger)
    {
        _context = context;
        _jwtService = jwtService;
        _logger = logger;
    }

    // Public health check endpoint
    [HttpGet("ping")]
    [AllowAnonymous]
    public IActionResult Ping() => Ok(new { status = "ok", time = DateTime.UtcNow });

    // Register new user
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        try
        {
            // Validate role
            var validRoles = new[] { "Student", "Recruiter", "Admin" };
            if (!validRoles.Contains(dto.Role)) return BadRequest(new { error = "Invalid role." });

            // Check if user already exists
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest(new { error = "User with this email already exists" });

            // Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // Create user
            var user = new User
            {
                Email = dto.Email,
                PasswordHash = passwordHash,
                Role = dto.Role,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                PhoneNumber = dto.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Generate token
            var token = _jwtService.GenerateToken(user);
            return Ok(BuildLoginResponse(token, user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Register error {Email}", dto.Email);
            return StatusCode(500, new { error = "Registration failed" });
        }
    }

    // Login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        try
        {
            // Find user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email && u.IsActive);
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new { error = "Invalid email or password" });

            // Generate token
            var token = _jwtService.GenerateToken(user);
            return Ok(BuildLoginResponse(token, user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error {Email}", dto.Email);
            return StatusCode(500, new { error = "Login failed" });
        }
    }

    // Get current user info
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var uid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(uid)) return Unauthorized(new { error = "Invalid token claims" });
            if (!Guid.TryParse(uid, out var userGuid)) return Unauthorized(new { error = "Invalid user id format" });

            var user = await _context.Users
                .Include(u => u.Profile)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userGuid);
            if (user == null) return NotFound(new { error = "User not found" });

            return Ok(new
            {
                id = user.Id,
                email = user.Email,
                role = user.Role,
                firstName = user.FirstName,
                lastName = user.LastName,
                phoneNumber = user.PhoneNumber,
                createdAt = user.CreatedAt,
                profile = user.Profile == null ? null : new
                {
                    user.Profile.Id,
                    user.Profile.Summary,
                    user.Profile.Skills,
                    user.Profile.Education,
                    user.Profile.Experience,
                    user.Profile.LinkedInUrl,
                    user.Profile.GitHubUrl,
                    user.Profile.PortfolioUrl,
                    user.Profile.UpdatedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetCurrentUser error");
            return StatusCode(500, new { error = "Failed to get user info" });
        }
    }

    private static LoginResponseDto BuildLoginResponse(string token, User user) => new()
    {
        Token = token,
        User = new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Role = user.Role,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            CreatedAt = user.CreatedAt
        },
        ExpiresAt = DateTime.UtcNow.AddMinutes(60)
    };
}
