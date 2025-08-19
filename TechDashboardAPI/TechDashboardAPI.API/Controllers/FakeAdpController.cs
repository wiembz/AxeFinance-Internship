using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechDashboardAPI.Application.DTOs;
using TechDashboardAPI.Domain.Entities;
using TechDashboardAPI.Domain.Enums;
using TechDashboardAPI.Infrastructure.Auth;
using TechDashboardAPI.Infrastructure.Data;
using System.Security.Cryptography;
using System.Text;

namespace TechDashboardAPI.API.Controllers;

[ApiController]
[Route("api/auth/fakeadp")]
public class FakeAdpController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IFakeAdService _fakeAdService;
    private readonly ITokenService _tokenService;

    public FakeAdpController(
        ApplicationDbContext context,
        IFakeAdService fakeAdService,
        ITokenService tokenService)
    {
        _context = context;
        _fakeAdService = fakeAdService;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> FakeAdpLogin([FromBody] FakeAdpLoginDto request)
    {
        if (!_fakeAdService.IsEnabled)
        {
            return BadRequest(new { message = "Fake ADP authentication is disabled" });
        }

        try
        {
            // Authenticate with fake ADP service
            var adUser = await _fakeAdService.AuthenticateAsync(request.Email, request.Password);
            if (adUser == null)
            {
                return BadRequest(new { message = "Invalid credentials or user not found in ADP" });
            }

            // Check if user exists in database
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            User user;
            if (existingUser == null)
            {
                // Auto-provision user if not exists
                user = new User
                {
                    Username = adUser.Username,
                    Email = adUser.Email,
                    PasswordHash = HashPassword(request.Password), // Store hashed password
                    Role = UserRole.Contributor, // Default role for ADP users
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            else
            {
                user = existingUser;
                // Update user info from ADP if needed
                if (user.Username != adUser.Username)
                {
                    user.Username = adUser.Username;
                    await _context.SaveChangesAsync();
                }
            }

            // Generate JWT token
            var token = _tokenService.GenerateJwtToken(user);

            return Ok(new LoginResponseDto
            {
                Token = token,
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role.ToString(),
                    IsActive = user.IsActive
                },
                Message = existingUser == null ? "User auto-provisioned and logged in successfully" : "Login successful"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred during authentication", error = ex.Message });
        }
    }

    [HttpGet("users")]
    public IActionResult GetFakeAdpUsers()
    {
        if (!_fakeAdService.IsEnabled)
        {
            return BadRequest(new { message = "Fake ADP service is disabled" });
        }

        var users = new[]
        {
            new { Email = "superadmin@company.com", Name = "Super Admin", Department = "IT" },
            new { Email = "admin@company.com", Name = "Admin User", Department = "HR" },
            new { Email = "viewer@company.com", Name = "Viewer User", Department = "Finance" },
            new { Email = "contributor@company.com", Name = "Contributor User", Department = "IT" }
        };

        return Ok(users);
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
}
