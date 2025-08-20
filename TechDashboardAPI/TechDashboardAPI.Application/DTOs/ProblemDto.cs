using System.ComponentModel.DataAnnotations;

namespace TechDashboardAPI.Application.DTOs.Problem
{
    // ✅ DTO for creating a problem
    public class CreateProblemDto
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(5000)]
        public string Description { get; set; } = string.Empty;

        // UI multi-select → stored as CSV in DB
        public List<string>? Tags { get; set; }

        [Url]
        public string? AzureLink { get; set; }

        public int? AssignedToUserId { get; set; }

        [Required]
        public int DepartmentId { get; set; }

        [Required]
        public int ProjectId { get; set; }

        public string? AttachmentPath { get; set; }

        // Utility: convert tags list to CSV
        public string GetTagsAsString() =>
            Tags == null || Tags.Count == 0
                ? string.Empty
                : string.Join(',', Tags.Distinct().Select(t => t.Trim()));
    }

    // ✅ DTO for updating a problem
    public class UpdateProblemDto
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(5000)]
        public string Description { get; set; } = string.Empty;

        public List<string>? Tags { get; set; }

        [Url]
        public string? AzureLink { get; set; }

        public int? AssignedToUserId { get; set; }

        public string? AttachmentPath { get; set; }
    }

    // ✅ Lightweight response DTO for list/detail
    public class ProblemResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public string? AzureLink { get; set; }
        public int? AssignedToUserId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string? AttachmentPath { get; set; }
        public int LikeCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public int SolutionsCount { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public DateTime? ResolvedDate { get; set; }
    }

    // ✅ Extended DTO for detailed view with solutions
    public class ProblemDetailDto : ProblemResponseDto
    {
        public List<SolutionDto> Solutions { get; set; } = new();
    }

    // ✅ DTO for nested solutions inside ProblemDetailDto
    public class SolutionDto
    {
        public int Id { get; set; }
        public string Summary { get; set; } = string.Empty;
        public string? Details { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsAccepted { get; set; }
    }

    // ✅ For reporting/statistics API
    public class ProblemStatisticsDto
    {
        public int TotalProblems { get; set; }
        public int OpenProblems { get; set; }
        public int InProgressProblems { get; set; }
        public int ResolvedProblems { get; set; }
        public double ResolutionRate { get; set; }
        public double AverageResolutionTimeInDays { get; set; }

        public Dictionary<string, int> TopTags { get; set; } = new();
        public Dictionary<string, int> MonthlyTrends { get; set; } = new();
    }
}
