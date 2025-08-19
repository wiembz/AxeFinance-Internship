using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using TechDashboardAPI.Application.DTOs;
using TechDashboardAPI.Application.Interfaces;
using TechDashboardAPI.Application.Responses;
using TechDashboardAPI.Domain.Entities;
using TechDashboardAPI.Domain.Enums;
using TechDashboardAPI.Infrastructure.Auth;
using TechDashboardAPI.Infrastructure.Data;

namespace TechDashboardAPI.Infrastructure.Services;

public class AuthService : IAuthService
{
    #region Fields and Dependencies
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IFakeAdService _fakeAdService;
    private readonly ILogger<AuthService> _logger;
    private readonly ITokenService _tokenService;

    private const string PasswordHashVersion = "PBKDF2-SHA256";
    private const int Pbkdf2Iterations = 100_000;
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;
    #endregion

    #region Constructor and Initialization
    public AuthService(
        ApplicationDbContext context,
        IConfiguration configuration,
        IFakeAdService fakeAdService,
        ILogger<AuthService> logger,
        ITokenService tokenService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _fakeAdService = fakeAdService ?? throw new ArgumentNullException(nameof(fakeAdService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
    }
    #endregion

    #region Public API Methods - User Registration and Authentication
    public async Task<ApiResponse<UserResponseDto>> RegisterAsync(RegisterDto registerDto)
    {
        if (registerDto == null)
        {
            _logger.LogWarning("Registration attempted with null data");
            return ApiResponse<UserResponseDto>.ErrorResponse("Registration data is required.");
        }
        return await RegisterAsync(registerDto, null);
    }

    public async Task<ApiResponse<UserResponseDto>> RegisterAsync(RegisterDto registerDto, int? currentUserId)
    {
        if (registerDto == null)
        {
            _logger.LogWarning("Registration attempted with null data");
            return ApiResponse<UserResponseDto>.ErrorResponse("Registration data is required.");
        }

        try
        {
            var validationResult = await ValidateRegistrationDataAsync(registerDto);
            if (!validationResult.Success)
                return validationResult;

            var userCount = await _context.Users.CountAsync();
            _logger.LogInformation("User registration attempt, Current user count: {UserCount}", userCount);

            if (userCount == 0)
            {
                return await CreateFirstUserAsync(registerDto);
            }
            else
            {
                return await CreateRegularUserAsync(registerDto, currentUserId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed");
            return ApiResponse<UserResponseDto>.ErrorResponse($"Registration failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<string>> LoginAsync(LoginDto loginDto)
    {
        if (loginDto == null)
        {
            _logger.LogWarning("Login attempted with null credentials");
            return ApiResponse<string>.ErrorResponse("Login credentials are required.");
        }

        try
        {
            _logger.LogInformation("Login attempt received");

            var user = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.Project)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == loginDto.Email.ToLower());

            if (user == null)
            {
                _logger.LogWarning("Login failed - invalid credentials");
                return ApiResponse<string>.ErrorResponse("Invalid email or password");
            }

            var (verified, isLegacy) = VerifyPassword(loginDto.Password, user.PasswordHash);
            if (!verified)
            {
                _logger.LogWarning("Login failed - invalid credentials");
                return ApiResponse<string>.ErrorResponse("Invalid email or password");
            }

            if (isLegacy)
            {
                try
                {
                    user.PasswordHash = HashPassword(loginDto.Password);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Upgraded legacy password hash to PBKDF2 for user: {UserId}", user.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to upgrade legacy password hash for user: {UserId}", user.Id);
                }
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Login failed - inactive account");
                return ApiResponse<string>.ErrorResponse("User account is inactive");
            }

            user.LastLoginDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = _tokenService.GenerateJwtToken(user);
            _logger.LogInformation("Successful login for userId: {UserId}", user.Id);
            
            return ApiResponse<string>.SuccessResponse(token, "Login successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for email: {Email}", loginDto.Email);
            return ApiResponse<string>.ErrorResponse($"Login failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<UserResponseDto>> GetCurrentUserAsync(int userId)
    {
        try
        {
            _logger.LogInformation("Retrieving user information for ID: {UserId}", userId);

            var user = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.Project)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning("User not found for ID: {UserId}", userId);
                return ApiResponse<UserResponseDto>.ErrorResponse("User not found");
            }

            var userResponse = new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role.ToString(),
                DepartmentName = user.Department?.Name,
                IsActive = user.IsActive,
                CreatedDate = user.CreatedDate,
                LastLoginDate = user.LastLoginDate
            };

            _logger.LogInformation("Successfully retrieved user information for ID: {UserId}", userId);
            return ApiResponse<UserResponseDto>.SuccessResponse(userResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve user information for ID: {UserId}", userId);
            return ApiResponse<UserResponseDto>.ErrorResponse($"Failed to get user: {ex.Message}");
        }
    }
    #endregion

    #region Token Management and Validation
    public Task<bool> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Token validation attempted with null or empty token");
            return Task.FromResult(false);
        }

        var principal = _tokenService.ValidateToken(token);
        var isValid = principal != null;
        if (isValid)
        {
            _logger.LogDebug("Token validated successfully");
        }
        else
        {
            _logger.LogWarning("Token validation failed");
        }
        return Task.FromResult(isValid);
    }

    public Task<int?> GetUserIdFromTokenAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("User ID extraction attempted with null or empty token");
            return Task.FromResult<int?>(null);
        }

        var userId = _tokenService.GetUserIdFromToken(token);
        if (userId.HasValue)
        {
            _logger.LogDebug("Successfully extracted user ID from token: {UserId}", userId);
            return Task.FromResult<int?>(userId);
        }

        _logger.LogWarning("Failed to extract valid user ID from token");
        return Task.FromResult<int?>(null);
    }

    public async Task<ApiResponse<string>> RefreshTokenAsync(string expiredToken)
    {
        if (string.IsNullOrEmpty(expiredToken))
        {
            _logger.LogWarning("Token refresh attempted with null or empty token");
            return ApiResponse<string>.ErrorResponse("Token is required for refresh");
        }

        try
        {
            var userId = _tokenService.GetUserIdFromTokenUnsafe(expiredToken);
            if (userId is null)
            {
                _logger.LogWarning("Invalid token structure during refresh attempt");
                return ApiResponse<string>.ErrorResponse("Invalid token structure");
            }

            var user = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.Project)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Token refresh failed - user not found or inactive: {UserId}", userId);
                return ApiResponse<string>.ErrorResponse("User not found or account is inactive");
            }

            var newToken = _tokenService.GenerateJwtToken(user);
            _logger.LogInformation("Successfully refreshed token for user: {UserId}", userId);
            
            return ApiResponse<string>.SuccessResponse(newToken, "Token refreshed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed");
            return ApiResponse<string>.ErrorResponse($"Token refresh failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
        {
            _logger.LogWarning("Password change attempted with missing credentials for user: {UserId}", userId);
            return ApiResponse<bool>.ErrorResponse("Current and new passwords are required.");
        }

        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Password change attempted for non-existent user: {UserId}", userId);
                return ApiResponse<bool>.ErrorResponse("User not found.");
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Password change attempted for inactive user: {UserId}", userId);
                return ApiResponse<bool>.ErrorResponse("Cannot change password for inactive account.");
            }

            var (verifiedCurrent, _) = VerifyPassword(currentPassword, user.PasswordHash);
            if (!verifiedCurrent)
            {
                _logger.LogWarning("Password change failed - incorrect current password for user: {UserId}", userId);
                return ApiResponse<bool>.ErrorResponse("Current password is incorrect.");
            }

            var passwordValidation = ValidatePasswordStrength(newPassword);
            if (!passwordValidation.IsValid)
            {
                _logger.LogWarning("Password change failed - weak password for user: {UserId}", userId);
                return ApiResponse<bool>.ErrorResponse(passwordValidation.ErrorMessage);
            }

            user.PasswordHash = HashPassword(newPassword);
            user.LastModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Password successfully changed for user: {UserId}", userId);
            return ApiResponse<bool>.SuccessResponse(true, "Password changed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password change failed for user: {UserId}", userId);
            return ApiResponse<bool>.ErrorResponse($"Password change failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<UserResponseDto>> UpdateUserProfileAsync(int userId, UpdateUserDto updateDto)
    {
        if (updateDto == null)
        {
            _logger.LogWarning("Profile update attempted with null data for user: {UserId}", userId);
            return ApiResponse<UserResponseDto>.ErrorResponse("Update data is required.");
        }

        try
        {
            var user = await _context.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning("Profile update attempted for non-existent user: {UserId}", userId);
                return ApiResponse<UserResponseDto>.ErrorResponse("User not found.");
            }

            if (!user.Email.Equals(updateDto.Email, StringComparison.OrdinalIgnoreCase))
            {
                var existingUser = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == updateDto.Email.ToLower() && u.Id != userId);
                
                if (existingUser != null)
                {
                    _logger.LogWarning("Profile update failed - email already exists: {Email}", updateDto.Email);
                    return ApiResponse<UserResponseDto>.ErrorResponse("Email address is already in use.");
                }
            }

            user.Email = updateDto.Email;
            user.Role = updateDto.Role;
            user.DepartmentId = updateDto.DepartmentId;
            user.LastModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            
            await _context.Entry(user).Reference(u => u.Department).LoadAsync();

            var userResponse = new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role.ToString(),
                DepartmentName = user.Department?.Name,
                IsActive = user.IsActive,
                CreatedDate = user.CreatedDate,
                LastLoginDate = user.LastLoginDate
            };

            _logger.LogInformation("Profile successfully updated for user: {UserId}", userId);
            return ApiResponse<UserResponseDto>.SuccessResponse(userResponse, "Profile updated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Profile update failed for user: {UserId}", userId);
            return ApiResponse<UserResponseDto>.ErrorResponse($"Profile update failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> LogoutAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Logout attempted with null or empty token");
            return ApiResponse<bool>.ErrorResponse("Token is required for logout.");
        }

        try
        {
            var userId = await GetUserIdFromTokenAsync(token);
            if (userId.HasValue)
            {
                _logger.LogInformation("User successfully logged out: {UserId}", userId.Value);
            }

            return ApiResponse<bool>.SuccessResponse(true, "Successfully logged out.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout failed");
            return ApiResponse<bool>.ErrorResponse($"Logout failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> InitiatePasswordResetAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("Password reset initiated with null or empty email");
            return ApiResponse<bool>.ErrorResponse("Email address is required.");
        }

        try
        {
            _logger.LogInformation("Password reset initiated for email: {Email}", email);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            
            if (user != null && user.IsActive)
            {
                _logger.LogInformation("Password reset token would be generated and emailed for user: {UserId}", user.Id);
                _logger.LogInformation("Demo - Password reset initiated for user: {Email}", email);
            }

            return ApiResponse<bool>.SuccessResponse(true, "If the email exists, a password reset link has been sent.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password reset initiation failed for email: {Email}", email);
            return ApiResponse<bool>.ErrorResponse("Password reset request could not be processed.");
        }
    }

    public Task<ApiResponse<bool>> ResetPasswordAsync(string resetToken, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(resetToken) || string.IsNullOrWhiteSpace(newPassword))
        {
            _logger.LogWarning("Password reset attempted with missing token or password");
            return Task.FromResult(ApiResponse<bool>.ErrorResponse("Reset token and new password are required."));
        }

        try
        {
            _logger.LogWarning("Password reset attempted with token - feature requires token storage implementation");
            return Task.FromResult(ApiResponse<bool>.ErrorResponse("Password reset feature requires additional implementation for token storage."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password reset failed");
            return Task.FromResult(ApiResponse<bool>.ErrorResponse($"Password reset failed: {ex.Message}"));
        }
    }
    #endregion

    #region Private Helper Methods
    private async Task<ApiResponse<UserResponseDto>> ValidateRegistrationDataAsync(RegisterDto registerDto)
    {
        var existingUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username.ToLower() == registerDto.Username.ToLower() || 
                                      u.Email.ToLower() == registerDto.Email.ToLower());
        
        if (existingUser != null)
        {
            _logger.LogWarning("Registration validation failed - duplicate username or email: {Username}, {Email}", 
                registerDto.Username, registerDto.Email);
            return ApiResponse<UserResponseDto>.ErrorResponse("Username or email already exists.");
        }

        if (!_fakeAdService.ValidateUser(registerDto.Email))
        {
            _logger.LogWarning("Registration validation failed - user not found in AD: {Email}", registerDto.Email);
            return ApiResponse<UserResponseDto>.ErrorResponse("User not found in Active Directory.");
        }

        var passwordValidation = ValidatePasswordStrength(registerDto.Password);
        if (!passwordValidation.IsValid)
        {
            _logger.LogWarning("Registration validation failed - weak password for user: {Email}", registerDto.Email);
            return ApiResponse<UserResponseDto>.ErrorResponse(passwordValidation.ErrorMessage);
        }

        return new ApiResponse<UserResponseDto> { Success = true, Message = "Validation passed" };
    }

    private async Task<ApiResponse<UserResponseDto>> CreateFirstUserAsync(RegisterDto registerDto)
    {
        _logger.LogInformation("Creating first user (SuperAdmin)");

        var user = new User
        {
            Username = registerDto.Username,
            Email = registerDto.Email,
            PasswordHash = HashPassword(registerDto.Password),
            Role = UserRole.SuperAdmin,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var userResponse = new UserResponseDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            CreatedDate = user.CreatedDate
        };

        _logger.LogInformation("First user (SuperAdmin) created successfully: {UserId}", user.Id);
        return ApiResponse<UserResponseDto>.SuccessResponse(userResponse, "First user registered as SuperAdmin");
    }

    private async Task<ApiResponse<UserResponseDto>> CreateRegularUserAsync(RegisterDto registerDto, int? currentUserId)
    {
        if (currentUserId == null)
        {
            _logger.LogWarning("Regular user registration attempted without SuperAdmin context");
            return ApiResponse<UserResponseDto>.ErrorResponse("Only SuperAdmin can register new users.");
        }

        var currentUser = await _context.Users.FindAsync(currentUserId);
        if (currentUser == null || currentUser.Role != UserRole.SuperAdmin)
        {
            _logger.LogWarning("Regular user registration attempted by non-SuperAdmin: {CurrentUserId}", currentUserId);
            return ApiResponse<UserResponseDto>.ErrorResponse("Only SuperAdmin can register new users.");
        }

        Department? department = null;
        Project? project = null;

        if (registerDto.DepartmentId.HasValue)
        {
            department = await _context.Departments
                .Include(d => d.Projects)
                .FirstOrDefaultAsync(d => d.Id == registerDto.DepartmentId.Value);
            
            if (department == null)
            {
                _logger.LogWarning("Registration failed - invalid department: {DepartmentId}", registerDto.DepartmentId);
                return ApiResponse<UserResponseDto>.ErrorResponse("Invalid department selected.");
            }

            if (registerDto.ProjectId.HasValue)
            {
                project = await _context.Projects
                    .FirstOrDefaultAsync(p => p.Id == registerDto.ProjectId.Value && 
                                              p.DepartmentId == department.Id);
                
                if (project == null)
                {
                    _logger.LogWarning("Registration failed - invalid project: {ProjectId}", registerDto.ProjectId);
                    return ApiResponse<UserResponseDto>.ErrorResponse("Invalid project or project doesn't belong to the selected department.");
                }
            }
        }

        var user = new User
        {
            Username = registerDto.Username,
            Email = registerDto.Email,
            PasswordHash = HashPassword(registerDto.Password),
            Role = UserRole.Viewer,
            DepartmentId = department?.Id,
            ProjectId = project?.Id,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var userResponse = new UserResponseDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role.ToString(),
            DepartmentName = department?.Name,
            IsActive = user.IsActive,
            CreatedDate = user.CreatedDate
        };

        _logger.LogInformation("Regular user created successfully: {UserId} by SuperAdmin: {SuperAdminId}", 
            user.Id, currentUserId);
        return ApiResponse<UserResponseDto>.SuccessResponse(userResponse, "User registered successfully");
    }

    private (bool IsValid, string ErrorMessage) ValidatePasswordStrength(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return (false, "Password is required.");

        if (password.Length < 8)
            return (false, "Password must be at least 8 characters long.");

        if (password.Length > 128)
            return (false, "Password cannot exceed 128 characters.");

        if (!Regex.IsMatch(password, @"[a-z]"))
            return (false, "Password must contain at least one lowercase letter.");

        if (!Regex.IsMatch(password, @"[A-Z]"))
            return (false, "Password must contain at least one uppercase letter.");

        if (!Regex.IsMatch(password, @"\d"))
            return (false, "Password must contain at least one number.");

        if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-\=\[\]{};':""\\|,.<>\/?]"))
            return (false, "Password must contain at least one special character.");

        var weakPatterns = new[] { "12345", "password", "qwerty", "admin", "letmein" };
        if (weakPatterns.Any(pattern => password.ToLower().Contains(pattern)))
            return (false, "Password contains common weak patterns.");

        return (true, string.Empty);
    }

    private string GeneratePasswordResetToken()
    {
        const int tokenLength = 32;
        using var rng = RandomNumberGenerator.Create();
        var tokenBytes = new byte[tokenLength];
        rng.GetBytes(tokenBytes);
        return Convert.ToBase64String(tokenBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private string HashPasswordResetToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token + "ResetTokenSalt"));
        return Convert.ToBase64String(hashedBytes);
    }

    private string HashPassword(string password)
    {
        Span<byte> salt = stackalloc byte[SaltSizeBytes];
        RandomNumberGenerator.Fill(salt);

        Span<byte> derived = stackalloc byte[HashSizeBytes];
        Rfc2898DeriveBytes.Pbkdf2(
            password: password,
            salt: salt,
            iterations: Pbkdf2Iterations,
            hashAlgorithm: HashAlgorithmName.SHA256,
            destination: derived);

        var saltB64 = Convert.ToBase64String(salt);
        var hashB64 = Convert.ToBase64String(derived);

        return $"{PasswordHashVersion}${Pbkdf2Iterations}${saltB64}${hashB64}";
    }

    private (bool Verified, bool IsLegacy) VerifyPassword(string password, string stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
            return (false, false);

        if (stored.StartsWith(PasswordHashVersion + "$", StringComparison.Ordinal))
        {
            var parts = stored.Split('$');
            if (parts.Length != 4)
                return (false, false);

            if (!int.TryParse(parts[1], out var iterations) || iterations <= 0)
                return (false, false);

            try
            {
                var salt = Convert.FromBase64String(parts[2]);
                var expected = Convert.FromBase64String(parts[3]);

                Span<byte> derived = stackalloc byte[expected.Length];
                Rfc2898DeriveBytes.Pbkdf2(
                    password: password,
                    salt: salt,
                    iterations: iterations,
                    hashAlgorithm: HashAlgorithmName.SHA256,
                    destination: derived);

                var verified = CryptographicOperations.FixedTimeEquals(derived, expected);
                return (verified, false);
            }
            catch
            {
                return (false, false);
            }
        }

        try
        {
            using var sha256 = SHA256.Create();
            var computed = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            var storedBytes = Convert.FromBase64String(stored);
            var isMatch = CryptographicOperations.FixedTimeEquals(computed, storedBytes);
            return (isMatch, isMatch);
        }
        catch
        {
            return (false, false);
        }
    }
    #endregion
}
