using System.ComponentModel.DataAnnotations;
using TechDashboardAPI.Domain.Enums;

namespace TechDashboardAPI.Domain.Entities;

/// <summary>
/// Represents a problem that needs to be solved within a project.
/// Problems can have multiple solutions and can be liked by users.
/// </summary>
public class Problem
{
    /// <summary>
    /// Gets or sets the unique identifier for the problem.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the title of the problem.
    /// Should be concise but descriptive enough to understand the issue.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the detailed description of the problem.
    /// Should include all relevant information for understanding and solving the problem.
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets comma-separated tags for categorizing and searching problems.
    /// Examples: "bug,frontend,critical" or "feature,api,enhancement"
    /// </summary>
    [MaxLength(500)]
    public string Tags { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current status of the problem.
    /// Tracks the lifecycle from open to resolved.
    /// </summary>
    public ProblemStatus Status { get; set; } = ProblemStatus.Open;

    /// <summary>
    /// Gets or sets when the problem was created.
    /// Automatically set to UTC time when the problem is created.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the problem was last updated.
    /// Updated when status, solutions, or other properties change.
    /// </summary>
    public DateTime? LastUpdatedDate { get; set; }

    /// <summary>
    /// Gets or sets when the problem was resolved.
    /// Set when the status changes to Resolved.
    /// </summary>
    public DateTime? ResolvedDate { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who created this problem.
    /// Required for tracking and permissions.
    /// </summary>
    public int CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the ID of the project this problem belongs to.
    /// Problems must be associated with a project for organization.
    /// </summary>
    public int ProjectId { get; set; }

    /// <summary>
    /// Gets or sets the file path for any attached files or screenshots.
    /// Can be null if no attachments are provided.
    /// </summary>
    [MaxLength(500)]
    public string? AttachmentPath { get; set; }

    /// <summary>
    /// Gets or sets the number of likes this problem has received.
    /// Automatically calculated from the ProblemLikes collection.
    /// </summary>
    public int LikeCount { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether the problem is active and visible.
    /// Inactive problems are hidden from normal views but preserved for history.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    // These represent the relationships between entities

    /// <summary>
    /// Gets or sets the user who created this problem.
    /// Navigation property for the User relationship.
    /// </summary>
    public virtual User CreatedByUser { get; set; } = null!;

    /// <summary>
    /// Gets or sets the project this problem belongs to.
    /// Navigation property for the Project relationship.
    /// </summary>
    public virtual Project Project { get; set; } = null!;

    /// <summary>
    /// Gets or sets the collection of solutions proposed for this problem.
    /// One problem can have many solutions.
    /// </summary>
    public virtual ICollection<Solution> Solutions { get; set; } = new List<Solution>();

    /// <summary>
    /// Gets or sets the collection of likes this problem has received.
    /// Used to track user engagement and problem importance.
    /// </summary>
    public virtual ICollection<ProblemLike> ProblemLikes { get; set; } = new List<ProblemLike>();

    /// <summary>
    /// Gets or sets the collection of custom field values for this problem.
    /// Allows for dynamic form fields to be associated with problems.
    /// </summary>
    public virtual ICollection<ProblemFieldValue> FieldValues { get; set; } = new List<ProblemFieldValue>();

    // Business Logic Methods

    /// <summary>
    /// Determines if the problem is currently open and accepting solutions.
    /// </summary>
    /// <returns>True if the problem status is Open, false otherwise.</returns>
    public bool IsOpen()
    {
        return Status == ProblemStatus.Open && IsActive;
    }

    /// <summary>
    /// Determines if the problem has been resolved.
    /// </summary>
    /// <returns>True if the problem status is Resolved, false otherwise.</returns>
    public bool IsResolved()
    {
        return Status == ProblemStatus.Resolved;
    }

    /// <summary>
    /// Determines if the problem is critical and needs immediate attention.
    /// Since all problems are considered critical, this always returns true.
    /// </summary>
    /// <returns>Always returns true as all problems are critical.</returns>
    public bool IsHighPriority()
    {
        return true; // All problems are critical
    }

    /// <summary>
    /// Gets the age of the problem in days since it was created.
    /// </summary>
    /// <returns>The number of days since the problem was created.</returns>
    public int GetAgeInDays()
    {
        return (DateTime.UtcNow - CreatedDate).Days;
    }

    /// <summary>
    /// Adds a like to the problem and updates the like count.
    /// </summary>
    public void AddLike()
    {
        LikeCount++;
        LastUpdatedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Removes a like from the problem and updates the like count.
    /// </summary>
    public void RemoveLike()
    {
        if (LikeCount > 0)
        {
            LikeCount--;
            LastUpdatedDate = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Marks the problem as resolved and sets the resolution date.
    /// </summary>
    public void MarkAsResolved()
    {
        Status = ProblemStatus.Resolved;
        ResolvedDate = DateTime.UtcNow;
        LastUpdatedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Reopens a previously resolved problem.
    /// </summary>
    public void Reopen()
    {
        Status = ProblemStatus.Open;
        ResolvedDate = null;
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
}
