using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TechDashboardAPI.Application.DTOs;

public class SolutionDto
{

    public class CreateSolutionDto
    {

        [Required(ErrorMessage = "Problem ID is required. Please specify which problem this solution addresses.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid problem.")]
        public int ProblemId { get; set; }

 
        [Required(ErrorMessage = "Solution content is required and cannot be empty.")]
        [StringLength(5000, MinimumLength = 20, ErrorMessage = "Solution content must be between 20 and 5000 characters.")]
        public string Content { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Azure DevOps link cannot exceed 500 characters.")]
        [Url(ErrorMessage = "Please provide a valid URL format for the Azure DevOps link.")]
        public string? AzureDevOpsLink { get; set; }

        public IFormFile? Attachment { get; set; }

        [StringLength(200, ErrorMessage = "Estimated effort cannot exceed 200 characters.")]
        public string? EstimatedEffort { get; set; }

        [StringLength(500, ErrorMessage = "Prerequisites cannot exceed 500 characters.")]
        public string? Prerequisites { get; set; }


        public bool IsValid()
        {
            return ProblemId > 0 &&
                   !string.IsNullOrWhiteSpace(Content) &&
                   Content.Length >= 20;
        }

        public bool HasImplementationTracking()
        {
            return !string.IsNullOrWhiteSpace(AzureDevOpsLink);
        }


        public bool HasAttachment()
        {
            return Attachment != null;
        }

        public bool IsAttachmentSizeValid(long maxSizeInBytes = 10 * 1024 * 1024) // 10MB default
        {
            return Attachment == null || Attachment.Length <= maxSizeInBytes;
        }
    }

    public class UpdateSolutionDto
    {

        [Required(ErrorMessage = "Solution content is required and cannot be empty.")]
        [StringLength(5000, MinimumLength = 20, ErrorMessage = "Solution content must be between 20 and 5000 characters.")]
        public string Content { get; set; } = string.Empty;


        [StringLength(500, ErrorMessage = "Azure DevOps link cannot exceed 500 characters.")]
        [Url(ErrorMessage = "Please provide a valid URL format for the Azure DevOps link.")]
        public string? AzureDevOpsLink { get; set; }


        public IFormFile? Attachment { get; set; }

        [StringLength(200, ErrorMessage = "Estimated effort cannot exceed 200 characters.")]
        public string? EstimatedEffort { get; set; }


        [StringLength(500, ErrorMessage = "Prerequisites cannot exceed 500 characters.")]
        public string? Prerequisites { get; set; }

        [StringLength(1000, ErrorMessage = "Update notes cannot exceed 1000 characters.")]
        public string? UpdateNotes { get; set; }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Content) && Content.Length >= 20;
        }

        public bool HasUpdatedImplementationTracking()
        {
            return !string.IsNullOrWhiteSpace(AzureDevOpsLink);
        }


        public bool HasNewAttachment()
        {
            return Attachment != null;
        }
    }


    public class SolutionResponseDto
    {

        public int Id { get; set; }

        public int ProblemId { get; set; }


        public string ProblemTitle { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public string? AttachmentPath { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? AzureDevOpsLink { get; set; }


        public DateTime CreatedDate { get; set; }


        public string CreatedBy { get; set; } = string.Empty;


        public DateTime? ApprovedDate { get; set; }

        public string? ApprovedBy { get; set; }

        public string? ReviewComments { get; set; }

        public DateTime? LastModifiedDate { get; set; }

        public bool IsPending()
        {
            return Status.Equals("Pending", StringComparison.OrdinalIgnoreCase);
        }

        public bool IsApproved()
        {
            return Status.Equals("Approved", StringComparison.OrdinalIgnoreCase);
        }

        public bool IsRejected()
        {
            return Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase);
        }


        public int GetAgeInDays()
        {
            return (DateTime.UtcNow - CreatedDate).Days;
        }


        public bool HasImplementationTracking()
        {
            return !string.IsNullOrWhiteSpace(AzureDevOpsLink);
        }


        public bool HasAttachments()
        {
            return !string.IsNullOrWhiteSpace(AttachmentPath);
        }

        public TimeSpan? GetTimeSinceApproval()
        {
            return ApprovedDate.HasValue ? DateTime.UtcNow - ApprovedDate.Value : null;
        }


        public string GetSummary()
        {
            return $"Solution #{Id} | {Status} | Created by {CreatedBy} | Age: {GetAgeInDays()} days" +
                   (HasImplementationTracking() ? " | Tracked in Azure DevOps" : "");
        }
    }


    public class ApproveSolutionDto
    {

        [Required(ErrorMessage = "Solution ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid solution.")]
        public int SolutionId { get; set; }


        public bool IsApproved { get; set; }

        [StringLength(1000, ErrorMessage = "Rejection reason cannot exceed 1000 characters.")]
        public string? RejectionReason { get; set; }


        [StringLength(500, ErrorMessage = "Approval comments cannot exceed 500 characters.")]
        public string? ApprovalComments { get; set; }

        public bool IsValid()
        {
            // If rejecting, must provide a reason
            if (!IsApproved && string.IsNullOrWhiteSpace(RejectionReason))
                return false;

            // Basic validation passes
            return SolutionId > 0;
        }


        public string GetActionText()
        {
            return IsApproved ? "Approved" : "Rejected";
        }


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
