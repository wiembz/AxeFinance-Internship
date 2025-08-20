using System.ComponentModel.DataAnnotations;

namespace TechDashboardAPI.Application.DTOs;

/// <summary>
/// Data Transfer Object for user registration requests.
/// Contains all the necessary information to create a new user account in the system.
/// </summary>
public class RegisterDto
{
    /// <summary>
    /// Gets or sets the username for the new user account.
    /// Must be unique across the system and will be used for identification.
    /// </summary>
    /// <example>john.doe</example>
    [Required(ErrorMessage = "Username is required and cannot be empty.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "Username can only contain letters, numbers, dots, underscores, and hyphens.")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address for the new user account.
    /// Must be a valid email format and unique across the system.
    /// Used for authentication and system communications.
    /// </summary>
    /// <example>john.doe@company.com</example>
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
    [StringLength(100, ErrorMessage = "Email address cannot exceed 100 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$", 
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character.")]
    public string Password { get; set; } = string.Empty;

    public int? DepartmentId { get; set; }

    public int? ProjectId { get; set; }
 public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Username) &&
               !string.IsNullOrWhiteSpace(Email) &&
               !string.IsNullOrWhiteSpace(Password) &&
               Email.Contains("@") &&
               Username.Length >= 3 &&
               Password.Length >= 8;
    }
}


public class LoginDto
{

    [Required(ErrorMessage = "Email address is required for login.")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required for login.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Password cannot be empty.")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Validates that the login credentials are provided and formatted correctly.
    /// </summary>
    /// <returns>True if basic validation passes, false otherwise.</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Email) &&
               !string.IsNullOrWhiteSpace(Password) &&
               Email.Contains("@");
    }

    /// <summary>
    /// Sanitizes the email by trimming whitespace and converting to lowercase.
    /// </summary>
    /// <returns>The cleaned email address.</returns>
    public string GetSanitizedEmail()
    {
        return Email.Trim().ToLowerInvariant();
    }
}

/// <summary>
/// Data Transfer Object for user information responses.
/// Contains user details returned after authentication or user queries.
/// Excludes sensitive information like passwords for security.
/// </summary>
public class UserResponseDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the user.
    /// Used for referencing the user in other operations.
    /// </summary>
    /// <example>123</example>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the username of the user.
    /// Display name used throughout the application interface.
    /// </summary>
    /// <example>john.doe</example>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address of the user.
    /// Primary contact method and unique identifier.
    /// </summary>
    /// <example>john.doe@company.com</example>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role of the user in the system.
    /// Determines the user's permissions and access levels.
    /// </summary>
    /// <example>Administrator</example>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the department the user belongs to.
    /// Can be null if the user is not associated with a specific department.
    /// </summary>
    /// <example>Information Technology</example>
    public string? DepartmentName { get; set; }

    /// <summary>
    /// Gets or sets when the user account was created.
    /// Useful for account management and auditing purposes.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Gets or sets when the user last logged into the system.
    /// Helps track user activity and account usage.
    /// </summary>
    /// <example>2024-08-09T14:22:00Z</example>
    public DateTime? LastLoginDate { get; set; }

    /// <summary>
    /// Gets or sets whether the user account is currently active.
    /// Inactive users cannot log in or perform actions in the system.
    /// </summary>
    /// <example>true</example>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Determines if the user has administrative privileges.
    /// </summary>
    /// <returns>True if the user is an administrator, false otherwise.</returns>
    public bool IsAdministrator()
    {
        return Role.Equals("Administrator", StringComparison.OrdinalIgnoreCase) ||
               Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets a formatted display name combining username and department.
    /// </summary>
    /// <returns>A user-friendly display string.</returns>
    public string GetDisplayName()
    {
        return string.IsNullOrWhiteSpace(DepartmentName) 
            ? Username 
            : $"{Username} ({DepartmentName})";
    }

    /// <summary>
    /// Determines if the user account is considered recently active.
    /// </summary>
    /// <param name="daysThreshold">Number of days to consider as recent (default 30).</param>
    /// <returns>True if the user has logged in within the threshold, false otherwise.</returns>
    public bool IsRecentlyActive(int daysThreshold = 30)
    {
        if (!LastLoginDate.HasValue) return false;
        return (DateTime.UtcNow - LastLoginDate.Value).Days <= daysThreshold;
    }
}
