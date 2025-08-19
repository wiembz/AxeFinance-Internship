using System.ComponentModel.DataAnnotations;
using TechDashboardAPI.Domain.Enums;

namespace TechDashboardAPI.Domain.Entities;

/// <summary>
/// Represents a solution proposed to solve a specific problem.
/// Solutions can be approved, rejected, or remain pending review.
/// </summary>
public class Solution
{
    /// <summary>
    /// Gets or sets the unique identifier for the solution.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the problem this solution addresses.
    /// Every solution must be associated with a specific problem.
    /// </summary>
    public int ProblemId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who proposed this solution.
    /// Used for tracking authorship and giving credit.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the detailed content of the solution.
    /// Should include step-by-step instructions, code samples, or methodology.
    /// </summary>
    [Required]
    [MaxLength(5000)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file path for any attachments (screenshots, files, etc.).
    /// Can be null if no supporting materials are needed.
    /// </summary>
    [MaxLength(500)]
    public string? AttachmentPath { get; set; }

    /// <summary>
    /// Gets or sets the current status of the solution.
    /// Tracks whether the solution is pending review, approved, or rejected.
    /// </summary>
    public SolutionStatus Status { get; set; } = SolutionStatus.Pending;

    /// <summary>
    /// Gets or sets the Azure DevOps work item or pull request link.
    /// Connects the solution to actual implementation work.
    /// </summary>
    [MaxLength(500)]
    public string? AzureDevOpsLink { get; set; }

    /// <summary>
    /// Gets or sets when the solution was created.
    /// Automatically set to UTC time when the solution is proposed.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the solution was approved.
    /// Set only when the status changes to Approved.
    /// </summary>
    public DateTime? ApprovedDate { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who approved this solution.
    /// Typically a project manager, lead developer, or administrator.
    /// </summary>
    public int? ApprovedBy { get; set; }

    /// <summary>
    /// Gets or sets when the solution was last modified.
    /// Updated when content, status, or other properties change.
    /// </summary>
    public DateTime? LastModifiedDate { get; set; }

    /// <summary>
    /// Gets or sets any feedback or comments about the solution.
    /// Used by reviewers to provide feedback during the review process.
    /// </summary>
    [MaxLength(1000)]
    public string? ReviewComments { get; set; }

    /// <summary>
    /// Gets or sets whether the solution is currently active.
    /// Inactive solutions are hidden but preserved for historical purposes.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    // These represent the relationships between entities

    /// <summary>
    /// Gets or sets the problem this solution addresses.
    /// Navigation property for the Problem relationship.
    /// </summary>
    public virtual Problem Problem { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user who proposed this solution.
    /// Navigation property for the User relationship.
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user who approved this solution.
    /// Navigation property for the approving User relationship.
    /// </summary>
    public virtual User? ApprovedByUser { get; set; }

    // Business Logic Methods

    /// <summary>
    /// Determines if the solution is currently pending review.
    /// </summary>
    /// <returns>True if the solution status is Pending, false otherwise.</returns>
    public bool IsPending()
    {
        return Status == SolutionStatus.Pending && IsActive;
    }

    /// <summary>
    /// Determines if the solution has been approved for implementation.
    /// </summary>
    /// <returns>True if the solution status is Approved, false otherwise.</returns>
    public bool IsApproved()
    {
        return Status == SolutionStatus.Approved;
    }

    /// <summary>
    /// Determines if the solution has been rejected.
    /// </summary>
    /// <returns>True if the solution status is Rejected, false otherwise.</returns>
    public bool IsRejected()
    {
        return Status == SolutionStatus.Rejected;
    }

    /// <summary>
    /// Gets the age of the solution in days since it was created.
    /// </summary>
    /// <returns>The number of days since the solution was proposed.</returns>
    public int GetAgeInDays()
    {
        return (DateTime.UtcNow - CreatedDate).Days;
    }

    /// <summary>
    /// Approves the solution and sets the approval date and approver.
    /// </summary>
    /// <param name="approvedByUserId">The ID of the user approving the solution.</param>
    /// <param name="comments">Optional comments about the approval.</param>
    public void Approve(int approvedByUserId, string? comments = null)
    {
        Status = SolutionStatus.Approved;
        ApprovedDate = DateTime.UtcNow;
        ApprovedBy = approvedByUserId;
        LastModifiedDate = DateTime.UtcNow;
        
        if (!string.IsNullOrWhiteSpace(comments))
        {
            ReviewComments = comments;
        }
    }

    /// <summary>
    /// Rejects the solution with optional feedback comments.
    /// </summary>
    /// <param name="reviewerUserId">The ID of the user rejecting the solution.</param>
    /// <param name="rejectionReason">The reason for rejection.</param>
    public void Reject(int reviewerUserId, string rejectionReason)
    {
        Status = SolutionStatus.Rejected;
        ReviewComments = rejectionReason;
        LastModifiedDate = DateTime.UtcNow;
        
        // Clear approval data if it was previously approved
        ApprovedDate = null;
        ApprovedBy = null;
    }

    /// <summary>
    /// Updates the solution content and marks it as modified.
    /// </summary>
    /// <param name="newContent">The updated solution content.</param>
    public void UpdateContent(string newContent)
    {
        Content = newContent;
        LastModifiedDate = DateTime.UtcNow;
        
        // Reset status to pending if it was previously rejected
        if (Status == SolutionStatus.Rejected)
        {
            Status = SolutionStatus.Pending;
            ReviewComments = null;
        }
    }

    /// <summary>
    /// Links the solution to an Azure DevOps work item or pull request.
    /// </summary>
    /// <param name="azureDevOpsUrl">The URL to the Azure DevOps item.</param>
    public void LinkToAzureDevOps(string azureDevOpsUrl)
    {
        AzureDevOpsLink = azureDevOpsUrl;
        LastModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Determines if the solution has implementation tracking via Azure DevOps.
    /// </summary>
    /// <returns>True if an Azure DevOps link is provided, false otherwise.</returns>
    public bool HasImplementationTracking()
    {
        return !string.IsNullOrWhiteSpace(AzureDevOpsLink);
    }

    /// <summary>
    /// Determines if the solution has supporting attachments.
    /// </summary>
    /// <returns>True if attachment path is provided, false otherwise.</returns>
    public bool HasAttachments()
    {
        return !string.IsNullOrWhiteSpace(AttachmentPath);
    }
}
