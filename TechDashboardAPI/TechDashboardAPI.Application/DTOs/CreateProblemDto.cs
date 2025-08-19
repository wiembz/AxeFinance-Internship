using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using TechDashboardAPI.Domain.Enums;

namespace TechDashboardAPI.Application.DTOs;

/// <summary>
/// Data Transfer Object for creating new problems in the system.
/// Contains all the necessary information to create a problem that needs solving.
/// Includes validation attributes to ensure data quality and consistency.
/// </summary>
public class CreateProblemDto
{
    /// <summary>
    /// Gets or sets the title of the problem.
    /// Should be concise but descriptive enough to understand the core issue.
    /// This will be displayed in problem lists and summaries.
    /// </summary>
    /// <example>User authentication fails after password reset</example>
    [Required(ErrorMessage = "Problem title is required and cannot be empty.")]
    [StringLength(200, MinimumLength = 10, ErrorMessage = "Title must be between 10 and 200 characters.")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the detailed description of the problem.
    /// Should include comprehensive information about the issue including:
    /// - What is happening vs what should happen
    /// - Steps to reproduce the problem
    /// - Any error messages or symptoms observed
    /// - Impact on users or business operations
    /// </summary>
    /// <example>After users reset their passwords through the forgot password feature, they are unable to log in with their new password. The login form shows 'Invalid credentials' error even when the correct password is entered. This affects approximately 15% of our users daily and prevents them from accessing their accounts.</example>
    [Required(ErrorMessage = "Problem description is required and cannot be empty.")]
    [StringLength(2000, MinimumLength = 20, ErrorMessage = "Description must be between 20 and 2000 characters.")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID of the project this problem belongs to.
    /// The problem will be categorized under this project for organization and tracking purposes.
    /// Must reference an existing, active project.
    /// </summary>
    /// <example>42</example>
    [Required(ErrorMessage = "Project ID is required. Please select a project for this problem.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid project.")]
    public int ProjectId { get; set; }

    /// <summary>
    /// Gets or sets optional file attachments that support the problem description.
    /// Can include screenshots, error logs, configuration files, or any other relevant documentation.
    /// Maximum file size and allowed types are enforced by the system configuration.
    /// </summary>
    /// <example>screenshot.png, error-log.txt</example>
    public List<IFormFile>? Attachments { get; set; }

    /// <summary>
    /// Gets or sets the expected outcome or resolution for the problem.
    /// Describes what should happen when the problem is successfully resolved.
    /// Helps solution providers understand the desired end state.
    /// </summary>
    /// <example>Users should be able to log in immediately after resetting their password, without any additional steps or delays.</example>
    [StringLength(500, ErrorMessage = "Expected outcome cannot exceed 500 characters.")]
    public string? ExpectedOutcome { get; set; }

    /// <summary>
    /// Gets or sets tags for categorizing and searching the problem.
    /// Should use relevant keywords that help with discoverability and organization.
    /// Common tags include technology stack, problem type, affected systems, etc.
    /// </summary>
    /// <example>["authentication", "password-reset", "login", "security", "bug"]</example>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Gets or sets any additional context or environment information.
    /// Can include browser versions, operating systems, specific configurations, or timing details.
    /// </summary>
    /// <example>Occurs primarily on Chrome browser, Windows 10, during peak hours (9-11 AM)</example>
    [StringLength(1000, ErrorMessage = "Additional context cannot exceed 1000 characters.")]
    public string? AdditionalContext { get; set; }

    /// <summary>
    /// Gets or sets the business impact level of the problem.
    /// Helps stakeholders understand the urgency and resource allocation needed.
    /// </summary>
    /// <example>Prevents users from accessing their accounts, leading to support tickets and potential revenue loss</example>
    [StringLength(300, ErrorMessage = "Business impact description cannot exceed 300 characters.")]
    public string? BusinessImpact { get; set; }

    // Business Logic Methods

    /// <summary>
    /// Validates that the problem creation request contains all required information.
    /// </summary>
    /// <returns>True if all validation passes, false otherwise.</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Title) &&
               !string.IsNullOrWhiteSpace(Description) &&
               ProjectId > 0 &&
               Title.Length >= 10 &&
               Description.Length >= 20;
    }

    /// <summary>
    /// Gets the tags as a formatted string for storage.
    /// </summary>
    /// <returns>Comma-separated tag string.</returns>
    public string GetTagsAsString()
    {
        if (Tags == null || !Tags.Any())
            return string.Empty;

        return string.Join(",", Tags.Where(tag => !string.IsNullOrWhiteSpace(tag))
                                   .Select(tag => tag.Trim()));
    }

    /// <summary>
    /// Determines if the problem has any supporting attachments.
    /// </summary>
    /// <returns>True if attachments are provided, false otherwise.</returns>
    public bool HasAttachments()
    {
        return Attachments != null && Attachments.Any();
    }

    /// <summary>
    /// Gets the total size of all attachments in bytes.
    /// </summary>
    /// <returns>Total size of attachments, or 0 if no attachments.</returns>
    public long GetTotalAttachmentSize()
    {
        if (Attachments == null || !Attachments.Any())
            return 0;

        return Attachments.Sum(file => file.Length);
    }

    /// <summary>
    /// Validates that attachment sizes are within acceptable limits.
    /// </summary>
    /// <param name="maxSizePerFile">Maximum size per file in bytes.</param>
    /// <param name="maxTotalSize">Maximum total size for all files in bytes.</param>
    /// <returns>True if sizes are acceptable, false otherwise.</returns>
    public bool AreAttachmentSizesValid(long maxSizePerFile = 10 * 1024 * 1024, long maxTotalSize = 50 * 1024 * 1024)
    {
        if (Attachments == null || !Attachments.Any())
            return true;

        return Attachments.All(file => file.Length <= maxSizePerFile) &&
               GetTotalAttachmentSize() <= maxTotalSize;
    }
}

/// <summary>
/// Data Transfer Object for updating existing problems in the system.
/// Allows modification of problem details while preserving audit trail and history.
/// Includes validation attributes to maintain data quality during updates.
/// </summary>
public class UpdateProblemDto
{
    /// <summary>
    /// Gets or sets the updated title of the problem.
    /// Must remain descriptive and accurate to the current state of the problem.
    /// </summary>
    /// <example>User authentication intermittently fails after password reset</example>
    [Required(ErrorMessage = "Problem title is required and cannot be empty.")]
    [StringLength(200, MinimumLength = 10, ErrorMessage = "Title must be between 10 and 200 characters.")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the updated detailed description of the problem.
    /// Should reflect any new information discovered or changes in the problem state.
    /// </summary>
    /// <example>Investigation shows that authentication failures occur primarily during high-traffic periods. Updated reproduction steps and additional error details have been identified.</example>
    [Required(ErrorMessage = "Problem description is required and cannot be empty.")]
    [StringLength(2000, MinimumLength = 20, ErrorMessage = "Description must be between 20 and 2000 characters.")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the updated status of the problem.
    /// Reflects the current state in the problem resolution lifecycle.
    /// Status changes should be accompanied by appropriate comments or justification.
    /// </summary>
    /// <example>InProgress</example>
    public ProblemStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the updated expected outcome or resolution for the problem.
    /// May be refined as more information becomes available about the solution approach.
    /// </summary>
    /// <example>Authentication system should handle high-traffic scenarios gracefully with sub-2-second response times and 99.9% success rate.</example>
    [StringLength(500, ErrorMessage = "Expected outcome cannot exceed 500 characters.")]
    public string? ExpectedOutcome { get; set; }

    /// <summary>
    /// Gets or sets updated tags for the problem.
    /// May include new tags discovered during investigation or remove irrelevant ones.
    /// </summary>
    /// <example>["authentication", "performance", "scalability", "critical-bug"]</example>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Gets or sets new attachments to add to the problem.
    /// Additional supporting materials discovered during problem analysis.
    /// </summary>
    /// <example>updated-logs.txt, performance-metrics.png</example>
    public List<IFormFile>? NewAttachments { get; set; }

    /// <summary>
    /// Gets or sets the list of existing attachment filenames to remove.
    /// Allows cleanup of outdated or irrelevant supporting materials.
    /// </summary>
    /// <example>["old-screenshot.png", "obsolete-log.txt"]</example>
    public List<string>? AttachmentsToRemove { get; set; }

    /// <summary>
    /// Gets or sets additional notes about the update.
    /// Provides context for why the problem information was changed.
    /// </summary>
    /// <example>Updated priority to Critical based on customer escalation and business impact assessment.</example>
    [StringLength(1000, ErrorMessage = "Update notes cannot exceed 1000 characters.")]
    public string? UpdateNotes { get; set; }

    /// <summary>
    /// Gets or sets updated business impact information.
    /// Reflects current understanding of how the problem affects operations.
    /// </summary>
    /// <example>Now affects 30% of users during peak hours, causing significant support load and potential revenue impact.</example>
    [StringLength(300, ErrorMessage = "Business impact description cannot exceed 300 characters.")]
    public string? BusinessImpact { get; set; }

    // Business Logic Methods

    /// <summary>
    /// Validates that the problem update request contains all required information.
    /// </summary>
    /// <returns>True if all validation passes, false otherwise.</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Title) &&
               !string.IsNullOrWhiteSpace(Description) &&
               Title.Length >= 10 &&
               Description.Length >= 20;
    }

    /// <summary>
    /// Gets the tags as a formatted string for storage.
    /// </summary>
    /// <returns>Comma-separated tag string.</returns>
    public string GetTagsAsString()
    {
        if (Tags == null || !Tags.Any())
            return string.Empty;

        return string.Join(",", Tags.Where(tag => !string.IsNullOrWhiteSpace(tag))
                                   .Select(tag => tag.Trim()));
    }

    /// <summary>
    /// Determines if the status is changing to a resolved state.
    /// </summary>
    /// <returns>True if status is Resolved or Closed, false otherwise.</returns>
    public bool IsResolvingProblem()
    {
        return Status == ProblemStatus.Resolved || Status == ProblemStatus.Closed;
    }

    /// <summary>
    /// Determines if new attachments are being added.
    /// </summary>
    /// <returns>True if new attachments are provided, false otherwise.</returns>
    public bool HasNewAttachments()
    {
        return NewAttachments != null && NewAttachments.Any();
    }

    /// <summary>
    /// Determines if existing attachments are being removed.
    /// </summary>
    /// <returns>True if attachments are marked for removal, false otherwise.</returns>
    public bool HasAttachmentsToRemove()
    {
        return AttachmentsToRemove != null && AttachmentsToRemove.Any();
    }

    /// <summary>
    /// Gets a summary of the changes being made in this update.
    /// </summary>
    /// <returns>A descriptive summary of the update actions.</returns>
    public string GetUpdateSummary()
    {
        var actions = new List<string>();
        
        if (HasNewAttachments())
            actions.Add($"{NewAttachments!.Count} new attachments");
            
        if (HasAttachmentsToRemove())
            actions.Add($"{AttachmentsToRemove!.Count} attachments removed");
            
        if (!string.IsNullOrWhiteSpace(UpdateNotes))
            actions.Add("notes updated");

        return actions.Any() 
            ? $"Problem updated: {string.Join(", ", actions)}"
            : "Problem information updated";
    }
}
