using TechDashboardAPI.Application.DTOs;
using TechDashboardAPI.Application.Responses;

namespace TechDashboardAPI.Application.Interfaces;

/// <summary>
/// Defines the contract for authentication and authorization services.
/// Provides methods for user registration, login, token validation, and user management.
/// This service handles all security-related operations in the application.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new user account in the system.
    /// Creates a user with the provided credentials and associates them with the specified department/project.
    /// Validates that username and email are unique before creating the account.
    /// </summary>
    /// <param name="registerDto">The registration information including username, email, password, and optional department/project assignments.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains an API response with the created user details (excluding sensitive information) or error information.
    /// </returns>
    /// <example>
    /// var registerData = new RegisterDto 
    /// {
    ///     Username = "john.doe",
    ///     Email = "john.doe@company.com",
    ///     Password = "SecurePass123!",
    ///     DepartmentId = 1
    /// };
    /// var result = await authService.RegisterAsync(registerData);
    /// </example>
    Task<ApiResponse<UserResponseDto>> RegisterAsync(RegisterDto registerDto);

    /// <summary>
    /// Authenticates a user with their email and password credentials.
    /// Validates the provided credentials against stored user data and generates a JWT token for successful authentication.
    /// Updates the user's last login timestamp upon successful authentication.
    /// </summary>
    /// <param name="loginDto">The login credentials including email and password.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains an API response with a JWT token for successful authentication or error information for failed attempts.
    /// </returns>
    /// <example>
    /// var loginData = new LoginDto 
    /// {
    ///     Email = "john.doe@company.com",
    ///     Password = "SecurePass123!"
    /// };
    /// var result = await authService.LoginAsync(loginData);
    /// if (result.IsSuccess)
    /// {
    ///     var jwtToken = result.Data; // Use this token for subsequent API calls
    /// }
    /// </example>
    Task<ApiResponse<string>> LoginAsync(LoginDto loginDto);

    /// <summary>
    /// Retrieves the current user's information based on their user ID.
    /// Returns comprehensive user details excluding sensitive information like passwords.
    /// Used to get updated user information for display and authorization purposes.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose information is being requested.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains an API response with the user's current information or error information if the user is not found.
    /// </returns>
    /// <example>
    /// var result = await authService.GetCurrentUserAsync(123);
    /// if (result.IsSuccess)
    /// {
    ///     var userInfo = result.Data;
    ///     Console.WriteLine($"User: {userInfo.Username}, Role: {userInfo.Role}");
    /// }
    /// </example>
    Task<ApiResponse<UserResponseDto>> GetCurrentUserAsync(int userId);

    /// <summary>
    /// Validates whether a JWT token is still valid and has not expired.
    /// Checks token signature, expiration date, and ensures the token hasn't been revoked.
    /// Used by middleware and authorization filters to verify request authenticity.
    /// </summary>
    /// <param name="token">The JWT token to validate (without "Bearer " prefix).</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result is true if the token is valid and not expired, false otherwise.
    /// </returns>
    /// <example>
    /// var isValid = await authService.ValidateTokenAsync(jwtToken);
    /// if (isValid)
    /// {
    ///     // Proceed with the authenticated request
    /// }
    /// else
    /// {
    ///     // Return 401 Unauthorized
    /// }
    /// </example>
    Task<bool> ValidateTokenAsync(string token);

    /// <summary>
    /// Extracts and returns the user ID from a valid JWT token.
    /// Parses the token claims to retrieve the user identifier without requiring database lookup.
    /// Used for quickly identifying the authenticated user from their token.
    /// </summary>
    /// <param name="token">The JWT token to parse (without "Bearer " prefix).</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains the user ID if the token is valid and contains user information, null otherwise.
    /// </returns>
    /// <example>
    /// var userId = await authService.GetUserIdFromTokenAsync(jwtToken);
    /// if (userId.HasValue)
    /// {
    ///     // Use the user ID for further operations
    ///     var userSpecificData = await GetUserData(userId.Value);
    /// }
    /// </example>
    Task<int?> GetUserIdFromTokenAsync(string token);

    /// <summary>
    /// Changes a user's password after validating their current password.
    /// Ensures password strength requirements are met and updates the password hash securely.
    /// Invalidates existing tokens to force re-authentication with the new password.
    /// </summary>
    /// <param name="userId">The ID of the user requesting the password change.</param>
    /// <param name="currentPassword">The user's current password for verification.</param>
    /// <param name="newPassword">The new password that meets security requirements.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains an API response indicating success or failure with appropriate error messages.
    /// </returns>
    Task<ApiResponse<bool>> ChangePasswordAsync(int userId, string currentPassword, string newPassword);

    /// <summary>
    /// Updates a user's profile information such as username, email, and department associations.
    /// Validates that any new email or username values are unique before applying changes.
    /// Maintains audit trail of changes for security and compliance purposes.
    /// </summary>
    /// <param name="userId">The ID of the user whose profile is being updated.</param>
    /// <param name="updateDto">The updated profile information.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains an API response with the updated user information or error details.
    /// </returns>
    Task<ApiResponse<UserResponseDto>> UpdateUserProfileAsync(int userId, UpdateUserDto updateDto);

    /// <summary>
    /// Logs out a user by invalidating their current authentication token.
    /// Adds the token to a blacklist to prevent further use even if it hasn't expired.
    /// Updates the user's last activity timestamp for audit purposes.
    /// </summary>
    /// <param name="token">The JWT token to invalidate.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains an API response indicating whether the logout was successful.
    /// </returns>
    Task<ApiResponse<bool>> LogoutAsync(string token);

    /// <summary>
    /// Initiates a password reset process by generating a secure reset token and sending it to the user's email.
    /// The reset token has a limited lifetime and can only be used once.
    /// Does not reveal whether the email exists in the system for security reasons.
    /// </summary>
    /// <param name="email">The email address associated with the user account.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains an API response indicating that the reset process has been initiated.
    /// </returns>
    Task<ApiResponse<bool>> InitiatePasswordResetAsync(string email);

    /// <summary>
    /// Completes the password reset process using a valid reset token.
    /// Validates the reset token, checks expiration, and updates the user's password.
    /// Invalidates the reset token after use to prevent replay attacks.
    /// </summary>
    /// <param name="resetToken">The password reset token received via email.</param>
    /// <param name="newPassword">The new password that meets security requirements.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains an API response indicating success or failure of the password reset.
    /// </returns>
    Task<ApiResponse<bool>> ResetPasswordAsync(string resetToken, string newPassword);

    /// <summary>
    /// Refreshes an expired or soon-to-expire JWT token with a new one.
    /// Validates the existing token and issues a new token with updated expiration.
    /// Helps maintain user sessions without requiring frequent re-authentication.
    /// </summary>
    /// <param name="expiredToken">The expired or soon-to-expire JWT token.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains an API response with a new JWT token or error information.
    /// </returns>
    Task<ApiResponse<string>> RefreshTokenAsync(string expiredToken);
}
