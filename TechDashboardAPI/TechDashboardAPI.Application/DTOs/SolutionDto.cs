using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TechDashboardAPI.Application.DTOs;

/// <summary>
/// Container class for solution-related Data Transfer Objects.
/// Groups all solution DTOs for better organization and namespace management.
/// </summary>
public class SolutionDto
{
    /// <summary>
    /// Data Transfer Object for creating new solutions to problems.
    /// Contains all the necessary information to propose a solution for an existing problem.
    /// </summary>
    public class CreateSolutionDto
    {
        /// <summary>
        /// Gets or sets the ID of the problem this solution addresses.
        /// Must reference an existing, active problem in the system.
        /// </summary>
        /// <example>42</example>
        [Required(ErrorMessage = "Problem ID is required. Please specify which problem this solution addresses.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid problem.")]
        public int ProblemId { get; set; }

        /// <summary>
        /// Gets or sets the detailed content of the solution.
        /// Should include comprehensive step-by-step instructions, code samples, methodologies, or approaches.
        /// Should be clear enough for others to understand and implement.
        /// </summary>
        /// <example>To fix the authentication issue: 1) Update the password reset token expiration to 24 hours in appsettings.json. 2) Clear the authentication cache using the following command... 3) Restart the authentication service to apply changes.</example>
        [Required(ErrorMessage = "Solution content is required and cannot be empty.")]
        [StringLength(5000, MinimumLength = 20, ErrorMessage = "Solution content must be between 20 and 5000 characters.")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional Azure DevOps work item or pull request link.
        /// Connects the solution to actual implementation work and tracking.
        /// Helps maintain traceability between problems, solutions, and development work.
        /// </summary>
        /// <example>https://dev.azure.com/company/project/_workitems/edit/12345</example>
        [StringLength(500, ErrorMessage = "Azure DevOps link cannot exceed 500 characters.")]
        [Url(ErrorMessage = "Please provide a valid URL format for the Azure DevOps link.")]
        public string? AzureDevOpsLink { get; set; }

        /// <summary>
        /// Gets or sets an optional attachment supporting the solution.
        /// Can include code files, configuration samples, screenshots, or documentation.
        /// Maximum file size is enforced by system configuration.
        /// </summary>
        /// <example>solution-code.cs, configuration-sample.json</example>
        public IFormFile? Attachment { get; set; }

        /// <summary>
        /// Gets or sets the estimated effort required to implement this solution.
        /// Helps stakeholders understand the resource investment needed.
        /// </summary>
        /// <example>2-4 hours for implementation and testing</example>
        [StringLength(200, ErrorMessage = "Estimated effort cannot exceed 200 characters.")]
        public string? EstimatedEffort { get; set; }

        /// <summary>
        /// Gets or sets any prerequisites or dependencies for implementing the solution.
        /// Helps ensure proper planning and sequencing of implementation activities.
        /// </summary>
        /// <example>Requires database backup, maintenance window scheduled, and development team coordination</example>
        [StringLength(500, ErrorMessage = "Prerequisites cannot exceed 500 characters.")]
        public string? Prerequisites { get; set; }

        // Business Logic Methods

        /// <summary>
        /// Validates that the solution creation request contains all required information.
        /// </summary>
        /// <returns>True if all validation passes, false otherwise.</returns>
        public bool IsValid()
        {
            return ProblemId > 0 &&
                   !string.IsNullOrWhiteSpace(Content) &&
                   Content.Length >= 20;
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
        /// <returns>True if an attachment is provided, false otherwise.</returns>
        public bool HasAttachment()
        {
            return Attachment != null;
        }

        /// <summary>
        /// Validates that the attachment size is within acceptable limits.
        /// </summary>
        /// <param name="maxSizeInBytes">Maximum allowed file size in bytes.</param>
        /// <returns>True if attachment size is acceptable, false otherwise.</returns>
        public bool IsAttachmentSizeValid(long maxSizeInBytes = 10 * 1024 * 1024) // 10MB default
        {
            return Attachment == null || Attachment.Length <= maxSizeInBytes;
        }
    }

    /// <summary>
    /// Data Transfer Object for updating existing solutions.
    /// Allows modification of solution details while preserving audit trail.
    /// </summary>
    public class UpdateSolutionDto
    {
        /// <summary>
        /// Gets or sets the updated content of the solution.
        /// Should reflect improvements, clarifications, or corrections to the original solution.
        /// </summary>
        /// <example>Updated solution with improved error handling: 1) Add try-catch blocks around authentication calls... 2) Implement retry logic for transient failures... 3) Add comprehensive logging for troubleshooting.</example>
        [Required(ErrorMessage = "Solution content is required and cannot be empty.")]
        [StringLength(5000, MinimumLength = 20, ErrorMessage = "Solution content must be between 20 and 5000 characters.")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the updated Azure DevOps work item or pull request link.
        /// Can be updated to reflect new work items or pull requests related to the solution.
        /// </summary>
        /// <example>https://dev.azure.com/company/project/_git/repo/pullrequest/456</example>
        [StringLength(500, ErrorMessage = "Azure DevOps link cannot exceed 500 characters.")]
        [Url(ErrorMessage = "Please provide a valid URL format for the Azure DevOps link.")]
        public string? AzureDevOpsLink { get; set; }

        /// <summary>
        /// Gets or sets a new attachment to replace or supplement the existing one.
        /// Previous attachment will be preserved in history for audit purposes.
        /// </summary>
        /// <example>updated-solution-code.cs, revised-configuration.json</example>
        public IFormFile? Attachment { get; set; }

        /// <summary>
        /// Gets or sets updated estimated effort information.
        /// May be revised based on new understanding or changed requirements.
        /// </summary>
        /// <example>4-6 hours for implementation, testing, and documentation</example>
        [StringLength(200, ErrorMessage = "Estimated effort cannot exceed 200 characters.")]
        public string? EstimatedEffort { get; set; }

        /// <summary>
        /// Gets or sets updated prerequisites or dependencies.
        /// May be modified based on changing circumstances or new discoveries.
        /// </summary>
        /// <example>Additional requirement: coordination with security team for authentication changes</example>
        [StringLength(500, ErrorMessage = "Prerequisites cannot exceed 500 characters.")]
        public string? Prerequisites { get; set; }

        /// <summary>
        /// Gets or sets notes explaining the reasons for the update.
        /// Provides context and justification for the changes made.
        /// </summary>
        /// <example>Updated based on code review feedback and additional security considerations identified.</example>
        [StringLength(1000, ErrorMessage = "Update notes cannot exceed 1000 characters.")]
        public string? UpdateNotes { get; set; }

        // Business Logic Methods

        /// <summary>
        /// Validates that the solution update request contains all required information.
        /// </summary>
        /// <returns>True if all validation passes, false otherwise.</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Content) && Content.Length >= 20;
        }

        /// <summary>
        /// Determines if the update includes implementation tracking changes.
        /// </summary>
        /// <returns>True if Azure DevOps link is being updated, false otherwise.</returns>
        public bool HasUpdatedImplementationTracking()
        {
            return !string.IsNullOrWhiteSpace(AzureDevOpsLink);
        }

        /// <summary>
        /// Determines if a new attachment is being provided.
        /// </summary>
        /// <returns>True if a new attachment is included, false otherwise.</returns>
        public bool HasNewAttachment()
        {
            return Attachment != null;
        }
    }

    /// <summary>
    /// Data Transfer Object for solution response information.
    /// Contains comprehensive solution details for display and interaction purposes.
    /// </summary>
    public class SolutionResponseDto
    {
        /// <summary>
        /// Gets or sets the unique identifier of the solution.
        /// </summary>
        /// <example>123</example>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the ID of the problem this solution addresses.
        /// </summary>
        /// <example>42</example>
        public int ProblemId { get; set; }

        /// <summary>
        /// Gets or sets the title of the problem this solution addresses.
        /// Provides context without requiring additional lookups.
        /// </summary>
        /// <example>User authentication fails after password reset</example>
        public string ProblemTitle { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the content of the solution.
        /// Contains the step-by-step instructions or methodology to resolve the problem.
        /// </summary>
        /// <example>To fix the authentication issue: 1) Update the password reset token expiration...</example>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the file path to any attached supporting materials.
        /// Can be null if no attachments were provided with the solution.
        /// </summary>
        /// <example>/uploads/solutions/123/solution-code.cs</example>
        public string? AttachmentPath { get; set; }

        /// <summary>
        /// Gets or sets the current review status of the solution.
        /// Indicates whether the solution is pending, approved, or rejected.
        /// </summary>
        /// <example>Approved</example>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Azure DevOps work item or pull request link.
        /// Provides traceability to implementation work.
        /// </summary>
        /// <example>https://dev.azure.com/company/project/_workitems/edit/12345</example>
        public string? AzureDevOpsLink { get; set; }

        /// <summary>
        /// Gets or sets when the solution was created.
        /// </summary>
        /// <example>2024-08-09T10:30:00Z</example>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the name of the user who created the solution.
        /// </summary>
        /// <example>Jane Smith</example>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets when the solution was approved, if applicable.
        /// </summary>
        /// <example>2024-08-10T14:22:00Z</example>
        public DateTime? ApprovedDate { get; set; }

        /// <summary>
        /// Gets or sets the name of the user who approved the solution, if applicable.
        /// </summary>
        /// <example>John Manager</example>
        public string? ApprovedBy { get; set; }

        /// <summary>
        /// Gets or sets any review comments or feedback about the solution.
        /// </summary>
        /// <example>Excellent solution, well documented and addresses all aspects of the problem.</example>
        public string? ReviewComments { get; set; }

        /// <summary>
        /// Gets or sets when the solution was last modified.
        /// </summary>
        /// <example>2024-08-10T12:45:00Z</example>
        public DateTime? LastModifiedDate { get; set; }

        // Business Logic Methods

        /// <summary>
        /// Determines if the solution is currently pending review.
        /// </summary>
        /// <returns>True if the solution status is Pending, false otherwise.</returns>
        public bool IsPending()
        {
            return Status.Equals("Pending", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines if the solution has been approved for implementation.
        /// </summary>
        /// <returns>True if the solution status is Approved, false otherwise.</returns>
        public bool IsApproved()
        {
            return Status.Equals("Approved", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines if the solution has been rejected.
        /// </summary>
        /// <returns>True if the solution status is Rejected, false otherwise.</returns>
        public bool IsRejected()
        {
            return Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the age of the solution in days since it was created.
        /// </summary>
        /// <returns>The number of days since solution creation.</returns>
        public int GetAgeInDays()
        {
            return (DateTime.UtcNow - CreatedDate).Days;
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

        /// <summary>
        /// Gets the time elapsed since approval, if approved.
        /// </summary>
        /// <returns>TimeSpan since approval, or null if not approved.</returns>
        public TimeSpan? GetTimeSinceApproval()
        {
            return ApprovedDate.HasValue ? DateTime.UtcNow - ApprovedDate.Value : null;
        }

        /// <summary>
        /// Gets a formatted summary of the solution status and metrics.
        /// </summary>
        /// <returns>A concise summary string.</returns>
        public string GetSummary()
        {
            return $"Solution #{Id} | {Status} | Created by {CreatedBy} | Age: {GetAgeInDays()} days" +
                   (HasImplementationTracking() ? " | Tracked in Azure DevOps" : "");
        }
    }

    /// <summary>
    /// Data Transfer Object for solution approval or rejection actions.
    /// Contains the decision and any associated feedback for the review process.
    /// </summary>
    public class ApproveSolutionDto
    {
        /// <summary>
        /// Gets or sets the ID of the solution being reviewed.
        /// Must reference an existing solution in Pending status.
        /// </summary>
        /// <example>123</example>
        [Required(ErrorMessage = "Solution ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid solution.")]
        public int SolutionId { get; set; }

        /// <summary>
        /// Gets or sets whether the solution is being approved (true) or rejected (false).
        /// Determines the final status that will be set for the solution.
        /// </summary>
        /// <example>true</example>
        public bool IsApproved { get; set; }

        /// <summary>
        /// Gets or sets the reason for rejection if IsApproved is false.
        /// Required when rejecting a solution to provide feedback to the author.
        /// Should be constructive and specific about what needs improvement.
        /// </summary>
        /// <example>The solution lacks error handling for edge cases and doesn't address the root cause of the authentication timeout issue. Please revise to include comprehensive error handling and consider implementing connection pooling.</example>
        [StringLength(1000, ErrorMessage = "Rejection reason cannot exceed 1000 characters.")]
        public string? RejectionReason { get; set; }

        /// <summary>
        /// Gets or sets optional approval comments or feedback.
        /// Used to provide positive feedback, implementation notes, or additional guidance.
        /// </summary>
        /// <example>Excellent solution with clear steps and good documentation. Recommend implementing during next maintenance window.</example>
        [StringLength(500, ErrorMessage = "Approval comments cannot exceed 500 characters.")]
        public string? ApprovalComments { get; set; }

        // Business Logic Methods

        /// <summary>
        /// Validates the approval/rejection request based on the decision made.
        /// </summary>
        /// <returns>True if validation passes, false otherwise.</returns>
        public bool IsValid()
        {
            // If rejecting, must provide a reason
            if (!IsApproved && string.IsNullOrWhiteSpace(RejectionReason))
                return false;

            // Basic validation passes
            return SolutionId > 0;
        }

        /// <summary>
        /// Gets the action being performed as a string.
        /// </summary>
        /// <returns>"Approved" or "Rejected" based on the IsApproved flag.</returns>
        public string GetActionText()
        {
            return IsApproved ? "Approved" : "Rejected";
        }

        /// <summary>
        /// Gets the primary feedback message for the decision.
        /// </summary>
        /// <returns>The rejection reason if rejecting, approval comments if approving, or a default message.</returns>
        public string GetFeedbackMessage()
        {
            if (!IsApproved && !string.IsNullOrWhiteSpace(RejectionReason))
                return RejectionReason;

            if (IsApproved && !string.IsNullOrWhiteSpace(ApprovalComments))
                return ApprovalComments;

            return IsApproved ? "Solution approved for implementation." : "Solution rejected.";
        }
    }
}
