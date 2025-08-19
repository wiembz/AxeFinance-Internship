using System.ComponentModel.DataAnnotations;
using TechDashboardAPI.Domain.Enums;

namespace TechDashboardAPI.Domain.Entities;

/// <summary>
/// Represents a user in the tech dashboard system.
/// Users can have different roles and belong to departments and projects.
/// </summary>
public class User
{
    /// <summary>
    /// Gets or sets the unique identifier for the user.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the username for authentication and display purposes.
    /// This should be unique and human-readable.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address for the user.
    /// This is used for authentication and communication.
    /// </summary>
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the hashed password for authentication.
    /// Never store plain text passwords.
    /// </summary>
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's role, which determines their permissions in the system.
    /// </summary>
    public UserRole Role { get; set; } = UserRole.Viewer;

    /// <summary>
    /// Gets or sets the ID of the department this user belongs to.
    /// Can be null if the user is not assigned to a specific department.
    /// </summary>
    public int? DepartmentId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the project this user is working on.
    /// Can be null if the user is not assigned to a specific project.
    /// </summary>
    public int? ProjectId { get; set; }

    /// <summary>
    /// Gets or sets when the user account was created.
    /// Automatically set to UTC time when the user is created.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether the user account is active.
    /// Inactive users cannot log in or perform actions in the system.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets when the user last logged in.
    /// Useful for tracking user activity and security purposes.
    /// </summary>
    public DateTime? LastLoginDate { get; set; }

    /// <summary>
    /// Gets or sets when the user account was last modified.
    /// Updated automatically when any user property changes.
    /// </summary>
    public DateTime? LastModifiedDate { get; set; }

    // Navigation Properties
    // These represent the relationships between entities and are used by Entity Framework

    /// <summary>
    /// Gets or sets the department this user belongs to.
    /// Navigation property for the Department relationship.
    /// </summary>
    public virtual Department? Department { get; set; }

    /// <summary>
    /// Gets or sets the project this user is working on.
    /// Navigation property for the Project relationship.
    /// </summary>
    public virtual Project? Project { get; set; }

    /// <summary>
    /// Gets or sets the collection of problems created by this user.
    /// One user can create many problems.
    /// </summary>
    public virtual ICollection<Problem> Problems { get; set; } = new List<Problem>();

    /// <summary>
    /// Gets or sets the collection of solutions provided by this user.
    /// One user can provide many solutions to different problems.
    /// </summary>
    public virtual ICollection<Solution> Solutions { get; set; } = new List<Solution>();

    /// <summary>
    /// Gets or sets the collection of problem likes given by this user.
    /// One user can like many problems.
    /// </summary>
    public virtual ICollection<ProblemLike> ProblemLikes { get; set; } = new List<ProblemLike>();

    // Business Logic Methods

    /// <summary>
    /// Determines if the user has administrative privileges.
    /// </summary>
    /// <returns>True if the user is an Admin or SuperAdmin, false otherwise.</returns>
    public bool IsAdministrator()
    {
        return Role == UserRole.Admin || Role == UserRole.SuperAdmin;
    }

    /// <summary>
    /// Determines if the user can contribute to the system (create problems, solutions, etc.).
    /// </summary>
    /// <returns>True if the user has Contributor role or higher, false otherwise.</returns>
    public bool CanContribute()
    {
        return Role >= UserRole.Contributor;
    }

    /// <summary>
    /// Determines if the user is a SuperAdmin with full system access.
    /// </summary>
    /// <returns>True if the user is a SuperAdmin, false otherwise.</returns>
    public bool IsSuperAdmin()
    {
        return Role == UserRole.SuperAdmin;
    }

    /// <summary>
    /// Updates the last login timestamp to the current UTC time.
    /// Call this method when the user successfully authenticates.
    /// </summary>
    public void UpdateLastLogin()
    {
        LastLoginDate = DateTime.UtcNow;
        LastModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the user account.
    /// Deactivated users cannot log in or perform actions.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        LastModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Reactivates the user account.
    /// Reactivated users can log in and perform actions based on their role.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        LastModifiedDate = DateTime.UtcNow;
    }
}
