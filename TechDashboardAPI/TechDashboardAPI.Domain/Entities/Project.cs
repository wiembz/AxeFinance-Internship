using System.ComponentModel.DataAnnotations;

namespace TechDashboardAPI.Domain.Entities;

/// <summary>
/// Represents a project within a department that can contain problems and solutions.
/// Projects are organizational units for managing related problems and their solutions.
/// </summary>
public class Project
{
    /// <summary>
    /// Gets or sets the unique identifier for the project.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the project.
    /// Should be descriptive and unique within the department.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the detailed description of the project.
    /// Should explain the project's purpose, scope, and objectives.
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID of the department this project belongs to.
    /// Projects must be associated with a department for organization.
    /// </summary>
    public int DepartmentId { get; set; }

    /// <summary>
    /// Gets or sets when the project was created.
    /// Automatically set to UTC time when the project is created.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the ID of the user who created this project.
    /// Used for tracking and permissions management.
    /// </summary>
    public int CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets whether the project is active and accepting new problems.
    /// Inactive projects are archived but preserved for historical reference.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the planned start date for the project.
    /// Used for project planning and timeline management.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Gets or sets the planned end date for the project.
    /// Used for project planning and deadline tracking.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Gets or sets the last time the project was updated.
    /// Updated when problems are added, status changes, or other modifications occur.
    /// </summary>
    public DateTime? LastUpdatedDate { get; set; }

    /// <summary>
    /// Gets or sets comma-separated tags for categorizing and searching projects.
    /// Examples: "web,frontend,react" or "api,backend,critical"
    /// </summary>
    [MaxLength(500)]
    public string Tags { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the project manager or lead responsible for this project.
    /// Used for escalation and decision-making authority.
    /// </summary>
    public int? ProjectManagerId { get; set; }

    // Navigation Properties
    // These represent the relationships between entities

    /// <summary>
    /// Gets or sets the department this project belongs to.
    /// Navigation property for the Department relationship.
    /// </summary>
    public virtual Department Department { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user who created this project.
    /// Navigation property for the User relationship.
    /// </summary>
    public virtual User CreatedByUser { get; set; } = null!;

    /// <summary>
    /// Gets or sets the project manager for this project.
    /// Navigation property for the project manager User relationship.
    /// </summary>
    public virtual User? ProjectManager { get; set; }

    /// <summary>
    /// Gets or sets the collection of problems associated with this project.
    /// One project can have many problems that need solving.
    /// </summary>
    public virtual ICollection<Problem> Problems { get; set; } = new List<Problem>();

    /// <summary>
    /// Gets or sets the collection of problem forms configured for this project.
    /// Defines the structure for problem submission in this project.
    /// </summary>
    public virtual ICollection<ProblemForm> ProblemForms { get; set; } = new List<ProblemForm>();

    /// <summary>
    /// Gets or sets the collection of form fields available for this project.
    /// Used to customize problem submission forms.
    /// </summary>
    public virtual ICollection<FormField> FormFields { get; set; } = new List<FormField>();

    // Business Logic Methods

    /// <summary>
    /// Determines if the project is currently active and accepting new problems.
    /// </summary>
    /// <returns>True if the project is active, false otherwise.</returns>
    public bool IsActiveProject()
    {
        return IsActive;
    }

    /// <summary>
    /// Gets the total number of problems in this project.
    /// </summary>
    /// <returns>The count of all problems associated with this project.</returns>
    public int GetTotalProblemsCount()
    {
        return Problems.Count;
    }

    /// <summary>
    /// Gets the number of open problems in this project.
    /// </summary>
    /// <returns>The count of problems with Open status.</returns>
    public int GetOpenProblemsCount()
    {
        return Problems.Count(p => p.IsOpen());
    }

    /// <summary>
    /// Gets the number of resolved problems in this project.
    /// </summary>
    /// <returns>The count of problems with Resolved status.</returns>
    public int GetResolvedProblemsCount()
    {
        return Problems.Count(p => p.IsResolved());
    }

    /// <summary>
    /// Calculates the problem resolution rate for this project.
    /// </summary>
    /// <returns>The percentage of problems that have been resolved (0-100).</returns>
    public double GetResolutionRate()
    {
        var totalProblems = GetTotalProblemsCount();
        if (totalProblems == 0) return 0;

        var resolvedProblems = GetResolvedProblemsCount();
        return (double)resolvedProblems / totalProblems * 100;
    }

    /// <summary>
    /// Gets the age of the project in days since it was created.
    /// </summary>
    /// <returns>The number of days since the project was created.</returns>
    public int GetAgeInDays()
    {
        return (DateTime.UtcNow - CreatedDate).Days;
    }

    /// <summary>
    /// Determines if the project is overdue based on its end date.
    /// </summary>
    /// <returns>True if the project has an end date and it has passed, false otherwise.</returns>
    public bool IsOverdue()
    {
        return EndDate.HasValue && EndDate.Value < DateTime.UtcNow && IsActive;
    }

    /// <summary>
    /// Gets the number of days remaining until the project deadline.
    /// </summary>
    /// <returns>Number of days remaining, or null if no end date is set.</returns>
    public int? GetDaysUntilDeadline()
    {
        if (!EndDate.HasValue) return null;

        var daysRemaining = (EndDate.Value - DateTime.UtcNow).Days;
        return daysRemaining;
    }

    /// <summary>
    /// Archives the project by setting it as inactive.
    /// </summary>
    public void Archive()
    {
        IsActive = false;
        LastUpdatedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Reactivates an archived project.
    /// </summary>
    public void Reactivate()
    {
        IsActive = true;
        LastUpdatedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the project timeline with new start and end dates.
    /// </summary>
    /// <param name="startDate">The new start date.</param>
    /// <param name="endDate">The new end date.</param>
    public void UpdateTimeline(DateTime? startDate, DateTime? endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
        LastUpdatedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the tags as a list of strings for easier processing.
    /// </summary>
    /// <returns>A list of individual tags.</returns>
    public List<string> GetTagsList()
    {
        if (string.IsNullOrWhiteSpace(Tags))
            return new List<string>();

        return Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                  .Select(tag => tag.Trim())
                  .Where(tag => !string.IsNullOrEmpty(tag))
                  .ToList();
    }

    /// <summary>
    /// Sets the tags from a list of strings.
    /// </summary>
    /// <param name="tagsList">The list of tags to set.</param>
    public void SetTags(IEnumerable<string> tagsList)
    {
        Tags = string.Join(",", tagsList.Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag.Trim()));
        LastUpdatedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Assigns a project manager to the project.
    /// </summary>
    /// <param name="projectManagerUserId">The ID of the user to assign as project manager.</param>
    public void AssignProjectManager(int projectManagerUserId)
    {
        ProjectManagerId = projectManagerUserId;
        LastUpdatedDate = DateTime.UtcNow;
    }
}
