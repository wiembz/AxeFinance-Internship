namespace TechDashboardAPI.Domain.Enums;

/// <summary>
/// Defines the different user roles in the tech dashboard system.
/// Roles determine what actions a user can perform and what data they can access.
/// Higher numeric values generally indicate more privileges.
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Read-only access to view problems and solutions.
    /// Cannot create, edit, or delete content.
    /// Lowest privilege level.
    /// </summary>
    Viewer = 0,

    /// <summary>
    /// Can create problems and solutions, and interact with content.
    /// Can view and contribute to projects within their department.
    /// Standard user privilege level.
    /// </summary>
    Contributor = 1,

    /// <summary>
    /// Department-level administrator.
    /// Can manage users within their department, approve solutions, and manage projects.
    /// Has elevated privileges within their scope.
    /// </summary>
    Admin = 2,

    /// <summary>
    /// System-wide administrator with full access.
    /// Can manage all users, departments, projects, and system settings.
    /// Highest privilege level with complete system access.
    /// </summary>
    SuperAdmin = 99
}
