using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TechDashboardAPI.Application.DTOs;
using TechDashboardAPI.Application.Interfaces;
using TechDashboardAPI.Application.Responses;
using TechDashboardAPI.Domain.Enums;
using TechDashboardAPI.Infrastructure.Auth;
using TechDashboardAPI.Infrastructure.Data;

namespace TechDashboardAPI.API.Controllers;
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Authentication")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ApplicationDbContext _context;
    private readonly IFakeAdService _fakeAdService;
    private readonly ILogger<AuthController> _logger;
    public AuthController(
        IAuthService authService, 
        ApplicationDbContext context, 
        IFakeAdService fakeAdService,
        ILogger<AuthController> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _fakeAdService = fakeAdService ?? throw new ArgumentNullException(nameof(fakeAdService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    [HttpPost("register")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register([FromBody] RegisterDto request)
    {
        try
        {
            _logger.LogInformation("User registration attempt received");
            if (request == null)
            {
                _logger.LogWarning("Registration attempt with null request data");
                return BadRequest(new 
                {
                    Success = false,
                    Message = "Registration data is required",
                    Errors = new List<string> { "Request body cannot be empty" }
                });
            }
            var result = await _authService.RegisterAsync(request);
            if (!result.Success)
            {
                _logger.LogWarning("Registration failed: {Message}", result.Message);
                var statusCode = result.Message.Contains("already exists") 
                    ? StatusCodes.Status409Conflict 
                    : StatusCodes.Status400BadRequest;
                return StatusCode(statusCode, new 
                {
                    Success = false,
                    Message = result.Message,
                    Errors = result.Errors ?? new List<string>()
                });
            }
            _logger.LogInformation("User registration successful");
            return Ok(new 
            {
                Success = true,
                Message = result.Message,
                Data = result.Data
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during user registration");
            return StatusCode(StatusCodes.Status500InternalServerError, new 
            {
                Success = false,
                Message = "An unexpected error occurred during registration. Please try again.",
                Errors = new List<string> { "Internal server error" }
            });
        }
    }
    [HttpPost("login")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginDto request)
    {
        try
        {
            _logger.LogInformation("Login attempt received");
            if (request == null)
            {
                _logger.LogWarning("Login attempt with null request data");
                return BadRequest(new 
                {
                    success = false,
                    message = "Login credentials are required",
                    errors = new List<string> { "Request body cannot be empty" }
                });
            }
            var result = await _authService.LoginAsync(request);
            if (!result.Success)
            {
                _logger.LogWarning("Login failed: {Message}", result.Message);
                return BadRequest(new 
                {
                    success = false,
                    message = result.Message,
                    errors = result.Errors ?? new List<string>()
                });
            }
            _logger.LogInformation("Login successful");
            var user = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.Project)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());
            if (user == null)
            {
                _logger.LogError("User not found after successful authentication");
                return StatusCode(StatusCodes.Status500InternalServerError, new 
                {
                    success = false,
                    message = "Authentication succeeded but user data could not be retrieved",
                    errors = new List<string> { "Internal server error" }
                });
            }
            // Set JWT as HttpOnly cookie (preferred storage); keep JSON body for SPA compatibility
            var expiresAt = DateTimeOffset.UtcNow.AddHours(24);
            Response.Cookies.Append("td_auth", result.Data!, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = expiresAt
            });

            return Ok(new 
            {
                success = true,
                data = new 
                {
                    user = new 
                    {
                        id = user.Id,
                        firstName = user.Username?.Split(' ').FirstOrDefault() ?? "User",
                        lastName = user.Username?.Split(' ').LastOrDefault() ?? "",
                        email = user.Email,
                        role = user.Role.ToString(),
                        departmentId = user.DepartmentId,
                        departmentName = user.Department?.Name,
                        isActive = user.IsActive,
                        createdAt = user.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        updatedAt = user.LastModifiedDate?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") ?? user.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                    },
                    token = result.Data,
                    expiresAt = expiresAt.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                },
                message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login");
            return StatusCode(StatusCodes.Status500InternalServerError, new 
            {
                success = false,
                message = "An unexpected error occurred during authentication. Please try again.",
                errors = new List<string> { "Internal server error" }
            });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        // Clear auth cookie; also allow client to clear localStorage token.
        var opts = new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(-1)
        };
        Response.Cookies.Delete("td_auth", opts);
        return Ok(new { success = true, message = "Logged out" });
    }
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            // Be tolerant to different subject claim types (legacy tokens may use "sub" or "nameid")
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? User.FindFirst("nameid")?.Value
                               ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                var allClaims = string.Join(", ", User?.Claims?.Select(c => $"{c.Type}={c.Value}") ?? Array.Empty<string>());
                _logger.LogWarning("Invalid or missing user ID claim in JWT token. Claims: {Claims}", allClaims);
                return Unauthorized(new 
                {
                    Success = false,
                    Message = "Invalid authentication token - user ID claim missing or invalid",
                    Errors = new List<string> { "Token validation failed" }
                });
            }
            _logger.LogDebug("Retrieving profile for user ID: {UserId}", userId);
            var result = await _authService.GetCurrentUserAsync(userId);
            if (!result.Success)
            {
                if (result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("User profile not found for user ID: {UserId}", userId);
                    return NotFound(new 
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors ?? new List<string>()
                    });
                }
                _logger.LogWarning("Failed to retrieve profile for user ID {UserId}: {Message}", userId, result.Message);
                return BadRequest(new 
                {
                    Success = false,
                    Message = result.Message,
                    Errors = result.Errors ?? new List<string>()
                });
            }
            _logger.LogDebug("Profile retrieved successfully for user ID: {UserId}", userId);
            return Ok(new 
            {
                Success = true,
                Data = result.Data,
                Message = "Profile retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving current user profile");
            return StatusCode(StatusCodes.Status500InternalServerError, new 
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving your profile. Please try again.",
                Errors = new List<string> { "Internal server error" }
            });
        }
    }
    [HttpGet("superadmin-exists")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SuperAdminExists()
    {
        try
        {
            _logger.LogDebug("Checking SuperAdmin existence status");
            var exists = await _context.Users
                .AnyAsync(u => u.Role == UserRole.SuperAdmin);
            _logger.LogInformation("SuperAdmin existence check completed: {Exists}", exists);
            return Ok(new 
            {
                Success = true,
                Exists = exists,
                Message = exists 
                    ? "SuperAdmin account exists - system is initialized" 
                    : "No SuperAdmin account found - system requires initial setup"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking SuperAdmin existence status");
            return StatusCode(StatusCodes.Status500InternalServerError, new 
            {
                Success = false,
                Message = "Unable to check system status. Please try again.",
                Errors = new List<string> { "Database query error" }
            });
        }
    }
}
