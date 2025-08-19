using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using TechDashboardAPI.Application.DTOs;

namespace TechDashboardAPI.Application.DTOs;

/// <summary>
/// Data Transfer Object for problem responses.
/// Contains all the essential information about a problem for display and interaction.
/// Used in list views and summary displays throughout the application.
/// </summary>
public class ProblemResponseDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the problem.
    /// Used for referencing and linking to the specific problem.
    /// </summary>
    /// <example>42</example>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the title of the problem.
    /// A concise but descriptive summary of what the problem is about.
    /// </summary>
    /// <example>Login page not responding on mobile devices</example>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the detailed description of the problem.
    /// Contains comprehensive information about the issue, including steps to reproduce,
    /// expected vs actual behavior, and any relevant context.
    /// </summary>
    /// <example>When users try to log in using mobile browsers, the login button becomes unresponsive after entering credentials...</example>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the comma-separated tags associated with the problem.
    /// Used for categorization, filtering, and search functionality.
    /// </summary>
    /// <example>mobile,login,ui,bug</example>
    public string Tags { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current status of the problem.
    /// Indicates whether the problem is open, in progress, resolved, etc.
    /// </summary>
    /// <example>Open</example>
    public string Status { get; set; } = "Open";

    /// <summary>
    /// Gets or sets when the problem was created.
    /// Used for tracking problem age and response times.
    /// </summary>
    /// <example>2024-08-09T10:30:00Z</example>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Gets or sets the name of the user who created the problem.
    /// Displayed for attribution and follow-up purposes.
    /// </summary>
    /// <example>John Doe</example>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier of the project this problem belongs to.
    /// Used for organizing problems within project contexts.
    /// </summary>
    /// <example>15</example>
    public int ProjectId { get; set; }

    /// <summary>
    /// Gets or sets the name of the project this problem belongs to.
    /// Provides context about which project area the problem affects.
    /// </summary>
    /// <example>Customer Portal Redesign</example>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the department that owns the project.
    /// Helps in routing and escalation decisions.
    /// </summary>
    /// <example>Information Technology</example>
    public string DepartmentName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file path to any attached files or screenshots.
    /// Can be null if no supporting materials were provided.
    /// </summary>
    /// <example>/uploads/problems/42/screenshot.png</example>
    public string? AttachmentPath { get; set; }

    /// <summary>
    /// Gets or sets the number of likes this problem has received.
    /// Indicates community interest and priority from a user perspective.
    /// </summary>
    /// <example>12</example>
    public int LikeCount { get; set; }

    /// <summary>
    /// Gets or sets whether the current user has liked this problem.
    /// Used for UI state management to show liked/not liked status.
    /// </summary>
    /// <example>true</example>
    public bool IsLikedByCurrentUser { get; set; }

    /// <summary>
    /// Gets or sets the number of solutions proposed for this problem.
    /// Indicates the level of engagement and potential resolution options.
    /// </summary>
    /// <example>3</example>
    public int SolutionsCount { get; set; }

    /// <summary>
    /// Gets or sets whether the problem is currently active and visible.
    /// Inactive problems are archived but preserved for historical purposes.
    /// </summary>
    /// <example>true</example>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets when the problem was last updated.
    /// Useful for tracking recent activity and changes.
    /// </summary>
    /// <example>2024-08-09T14:15:00Z</example>
    public DateTime? LastUpdatedDate { get; set; }

    /// <summary>
    /// Gets or sets when the problem was resolved, if applicable.
    /// Used to calculate resolution times and track completion.
    /// </summary>
    /// <example>2024-08-10T16:30:00Z</example>
    public DateTime? ResolvedDate { get; set; }

    // Business Logic Methods

    /// <summary>
    /// Gets the age of the problem in days since it was created.
    /// </summary>
    /// <returns>The number of days since problem creation.</returns>
    public int GetAgeInDays()
    {
        return (DateTime.UtcNow - CreatedDate).Days;
    }

    /// <summary>
    /// Determines if the problem is considered high priority.
    /// Since all problems are critical, this always returns true.
    /// </summary>
    /// <returns>Always returns true as all problems are critical.</returns>
    public bool IsHighPriority()
    {
        return true; // All problems are critical
    }

    /// <summary>
    /// Determines if the problem is currently open and accepting solutions.
    /// </summary>
    /// <returns>True if status is Open and problem is active, false otherwise.</returns>
    public bool IsOpen()
    {
        return Status.Equals("Open", StringComparison.OrdinalIgnoreCase) && IsActive;
    }

    /// <summary>
    /// Determines if the problem has been resolved.
    /// </summary>
    /// <returns>True if status is Resolved, false otherwise.</returns>
    public bool IsResolved()
    {
        return Status.Equals("Resolved", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the individual tags as a list for easier processing.
    /// </summary>
    /// <returns>A list of tag strings.</returns>
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
    /// Determines if the problem has community engagement (likes or solutions).
    /// </summary>
    /// <returns>True if the problem has likes or solutions, false otherwise.</returns>
    public bool HasCommunityEngagement()
    {
        return LikeCount > 0 || SolutionsCount > 0;
    }

    /// <summary>
    /// Determines if the problem has supporting attachments.
    /// </summary>
    /// <returns>True if attachment path is provided, false otherwise.</returns>
    public bool HasAttachments()
    {
        return !string.IsNullOrWhiteSpace(AttachmentPath);
    }

    /// <summary>
    /// Gets a formatted summary string for the problem.
    /// </summary>
    /// <returns>A concise summary including key metrics.</returns>
    public string GetSummary()
    {
        return $"{Title} | {Status} | Critical Priority | {LikeCount} likes | {SolutionsCount} solutions | Age: {GetAgeInDays()} days";
    }
}

/// <summary>
/// Data Transfer Object for detailed problem responses.
/// Extends ProblemResponseDto with additional detailed information including solutions.
/// Used for problem detail views where comprehensive information is needed.
/// </summary>
public class ProblemDetailDto : ProblemResponseDto
{
    /// <summary>
    /// Gets or sets the collection of solutions proposed for this problem.
    /// Contains detailed information about each solution including status and content.
    /// </summary>
    public List<SolutionDto.SolutionResponseDto> Solutions { get; set; } = new List<SolutionDto.SolutionResponseDto>();

    /// <summary>
    /// Gets or sets the collection of custom form field values for this problem.
    /// Contains any additional structured data captured during problem submission.
    /// </summary>
    public List<FormFieldValueDto> FieldValues { get; set; } = new List<FormFieldValueDto>();

    /// <summary>
    /// Gets or sets the full details of the user who created the problem.
    /// Provides complete user context for detailed displays.
    /// </summary>
    public UserResponseDto? CreatedByUser { get; set; }

    /// <summary>
    /// Gets the number of approved solutions for this problem.
    /// </summary>
    /// <returns>Count of solutions with Approved status.</returns>
    public int GetApprovedSolutionsCount()
    {
        return Solutions.Count(s => s.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the number of pending solutions awaiting review.
    /// </summary>
    /// <returns>Count of solutions with Pending status.</returns>
    public int GetPendingSolutionsCount()
    {
        return Solutions.Count(s => s.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Determines if the problem has any approved solutions.
    /// </summary>
    /// <returns>True if at least one solution is approved, false otherwise.</returns>
    public bool HasApprovedSolutions()
    {
        return GetApprovedSolutionsCount() > 0;
    }

    /// <summary>
    /// Gets the most recently created solution for this problem.
    /// </summary>
    /// <returns>The latest solution, or null if no solutions exist.</returns>
    public SolutionDto.SolutionResponseDto? GetLatestSolution()
    {
        return Solutions.OrderByDescending(s => s.CreatedDate).FirstOrDefault();
    }
}

/// <summary>
/// Data Transfer Object for form field values associated with problems.
/// Contains dynamic field data captured through custom problem forms.
/// </summary>
public class FormFieldValueDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the form field.
    /// </summary>
    public int FieldId { get; set; }

    /// <summary>
    /// Gets or sets the name of the form field.
    /// </summary>
    /// <example>Affected System</example>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value entered for this field.
    /// </summary>
    /// <example>Payment Processing Module</example>
    public string FieldValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the form field.
    /// </summary>
    /// <example>Text</example>
    public string FieldType { get; set; } = string.Empty;
}
