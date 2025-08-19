using TechDashboardAPI.Application.DTOs;
using TechDashboardAPI.Application.Responses;
using TechDashboardAPI.Domain.Enums;

namespace TechDashboardAPI.Application.Interfaces;

/// <summary>
/// Defines the contract for problem management services.
/// Provides methods for creating, reading, updating, and deleting problems,
/// as well as managing problem interactions like likes and tag filtering.
/// </summary>
public interface IProblemService
{
    /// <summary>
    /// Retrieves a paginated list of problems for a specific project.
    /// Supports filtering by tags and includes user-specific information like like status.
    /// Results are ordered by creation date (newest first) and include problem metrics.
    /// </summary>
    /// <param name="projectId">The ID of the project whose problems should be retrieved.</param>
    /// <param name="pageNumber">The page number for pagination (1-based).</param>
    /// <param name="pageSize">The number of problems to return per page (maximum 100).</param>
    /// <param name="tags">Optional comma-separated list of tags to filter problems (case-insensitive).</param>
    /// <param name="status">Optional problem status filter (Open, InProgress, Resolved, etc.).</param>
    /// <param name="priority">Optional priority level filter (Low, Medium, High, Critical).</param>
    /// <param name="createdBy">Optional user ID to filter problems created by a specific user.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains a paginated response with problem summaries and metadata.
    /// </returns>
    /// <example>
    /// var result = await problemService.GetProblemsByProjectAsync(
    ///     projectId: 42,
    ///     pageNumber: 1,
    ///     pageSize: 20,
    ///     tags: "authentication,bug",
    ///     status: ProblemStatus.Open
    /// );
    /// </example>
    Task<ApiResponse<PaginatedResponse<ProblemResponseDto>>> GetProblemsByProjectAsync(
        int projectId, 
        int pageNumber, 
        int pageSize, 
        string? tags = null, 
        ProblemStatus? status = null, 
        string? priority = null, 
        int? createdBy = null);

    /// <summary>
    /// Retrieves detailed information about a specific problem including all associated solutions.
    /// Includes user-specific context such as whether the current user has liked the problem.
    /// Returns comprehensive problem details suitable for problem detail views.
    /// </summary>
    /// <param name="id">The unique identifier of the problem to retrieve.</param>
    /// <param name="userId">The ID of the current user for context-specific information.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains detailed problem information including solutions, likes, and user context.
    /// </returns>
    /// <example>
    /// var result = await problemService.GetProblemByIdAsync(123, currentUserId);
    /// if (result.IsSuccess)
    /// {
    ///     var problem = result.Data;
    ///     Console.WriteLine($"Problem: {problem.Title}");
    ///     Console.WriteLine($"Solutions: {problem.Solutions.Count}");
    /// }
    /// </example>
    Task<ApiResponse<ProblemDetailDto>> GetProblemByIdAsync(int id, int userId);

    /// <summary>
    /// Creates a new problem in the specified project.
    /// Validates the problem data, processes any file attachments, and sets the creation metadata.
    /// Automatically sets the problem status to Open and assigns the creating user.
    /// </summary>
    /// <param name="createDto">The problem creation data including title, description, and attachments.</param>
    /// <param name="userId">The ID of the user creating the problem.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains the created problem information or validation error details.
    /// </returns>
    /// <example>
    /// var problemData = new CreateProblemDto
    /// {
    ///     Title = "Login page not responsive on mobile",
    ///     Description = "Detailed description of the issue...",
    ///     ProjectId = 42,
    ///     Tags = new List<string> { "mobile", "ui", "bug" }
    /// };
    /// var result = await problemService.CreateProblemAsync(problemData, currentUserId);
    /// </example>
    Task<ApiResponse<ProblemResponseDto>> CreateProblemAsync(CreateProblemDto createDto, int userId);

    /// <summary>
    /// Updates an existing problem with new information.
    /// Validates user permissions to ensure only authorized users can modify the problem.
    /// Maintains audit trail of changes and updates the last modified timestamp.
    /// </summary>
    /// <param name="id">The ID of the problem to update.</param>
    /// <param name="updateDto">The updated problem information.</param>
    /// <param name="userId">The ID of the user performing the update.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains the updated problem information or error details for unauthorized access.
    /// </returns>
    /// <example>
    /// var updateData = new UpdateProblemDto
    /// {
    ///     Title = "Updated problem title",
    ///     Description = "Updated description with new findings...",
    ///     Status = ProblemStatus.InProgress
    /// };
    /// var result = await problemService.UpdateProblemAsync(123, updateData, currentUserId);
    /// </example>
    Task<ApiResponse<ProblemResponseDto>> UpdateProblemAsync(int id, UpdateProblemDto updateDto, int userId);

    /// <summary>
    /// Soft deletes a problem by marking it as inactive rather than removing it from the database.
    /// Validates user permissions and maintains referential integrity with solutions and likes.
    /// Preserves problem history for audit and reporting purposes.
    /// </summary>
    /// <param name="id">The ID of the problem to delete.</param>
    /// <param name="userId">The ID of the user requesting the deletion.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result indicates whether the deletion was successful or if access was denied.
    /// </returns>
    /// <example>
    /// var result = await problemService.DeleteProblemAsync(123, currentUserId);
    /// if (result.IsSuccess)
    /// {
    ///     Console.WriteLine("Problem successfully archived.");
    /// }
    /// </example>
    Task<ApiResponse<bool>> DeleteProblemAsync(int id, int userId);

    /// <summary>
    /// Adds a like to a problem from the specified user.
    /// Prevents duplicate likes from the same user and updates the problem's like count.
    /// Tracks user engagement metrics for problem prioritization and analytics.
    /// </summary>
    /// <param name="problemId">The ID of the problem to like.</param>
    /// <param name="userId">The ID of the user adding the like.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result indicates whether the like was successfully added.
    /// </returns>
    /// <example>
    /// var result = await problemService.LikeProblemAsync(123, currentUserId);
    /// if (result.IsSuccess)
    /// {
    ///     Console.WriteLine("Problem liked successfully.");
    /// }
    /// </example>
    Task<ApiResponse<bool>> LikeProblemAsync(int problemId, int userId);

    /// <summary>
    /// Removes a like from a problem for the specified user.
    /// Updates the problem's like count and removes the user's like record.
    /// Allows users to change their engagement level with problems.
    /// </summary>
    /// <param name="problemId">The ID of the problem to unlike.</param>
    /// <param name="userId">The ID of the user removing their like.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result indicates whether the like was successfully removed.
    /// </returns>
    /// <example>
    /// var result = await problemService.UnlikeProblemAsync(123, currentUserId);
    /// if (result.IsSuccess)
    /// {
    ///     Console.WriteLine("Problem unliked successfully.");
    /// }
    /// </example>
    Task<ApiResponse<bool>> UnlikeProblemAsync(int problemId, int userId);

    /// <summary>
    /// Retrieves all unique tags used across all problems in the system.
    /// Returns tags sorted by frequency of use (most common first) for better user experience.
    /// Used for tag suggestion and filtering capabilities in the user interface.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains a list of all unique tags used in problems.
    /// </returns>
    /// <example>
    /// var result = await problemService.GetAllTagsAsync();
    /// if (result.IsSuccess)
    /// {
    ///     var tags = result.Data; // ["authentication", "ui", "bug", "performance", ...]
    /// }
    /// </example>
    Task<ApiResponse<List<string>>> GetAllTagsAsync();

    /// <summary>
    /// Retrieves problems across all projects that the user has access to.
    /// Filters based on user permissions and department/project associations.
    /// Useful for dashboard views and cross-project problem tracking.
    /// </summary>
    /// <param name="userId">The ID of the user requesting the problems.</param>
    /// <param name="pageNumber">The page number for pagination (1-based).</param>
    /// <param name="pageSize">The number of problems to return per page.</param>
    /// <param name="searchTerm">Optional search term to filter problems by title or description.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains a paginated list of problems accessible to the user.
    /// </returns>
    Task<ApiResponse<PaginatedResponse<ProblemResponseDto>>> GetUserAccessibleProblemsAsync(
        int userId, 
        int pageNumber, 
        int pageSize, 
        string? searchTerm = null);

    /// <summary>
    /// Retrieves problems created by a specific user across all projects.
    /// Includes problems from projects the user has access to, with creation date ordering.
    /// Useful for user profile pages and personal problem tracking.
    /// </summary>
    /// <param name="userId">The ID of the user whose problems should be retrieved.</param>
    /// <param name="requestingUserId">The ID of the user making the request (for permission validation).</param>
    /// <param name="pageNumber">The page number for pagination (1-based).</param>
    /// <param name="pageSize">The number of problems to return per page.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains a paginated list of problems created by the specified user.
    /// </returns>
    Task<ApiResponse<PaginatedResponse<ProblemResponseDto>>> GetProblemsByUserAsync(
        int userId, 
        int requestingUserId, 
        int pageNumber, 
        int pageSize);

    /// <summary>
    /// Retrieves statistical information about problems within a project.
    /// Includes metrics like total problems, resolution rates, average resolution time, and tag usage.
    /// Used for project management dashboards and performance analytics.
    /// </summary>
    /// <param name="projectId">The ID of the project for which statistics are requested.</param>
    /// <param name="userId">The ID of the user requesting the statistics (for permission validation).</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains comprehensive problem statistics for the project.
    /// </returns>
    Task<ApiResponse<ProblemStatisticsDto>> GetProblemStatisticsAsync(int projectId, int userId);

    /// <summary>
    /// Changes the status of a problem with optional status change comments.
    /// Validates that the status transition is valid and that the user has appropriate permissions.
    /// Maintains audit trail of all status changes for compliance and tracking.
    /// </summary>
    /// <param name="problemId">The ID of the problem whose status is being changed.</param>
    /// <param name="newStatus">The new status to set for the problem.</param>
    /// <param name="userId">The ID of the user making the status change.</param>
    /// <param name="statusChangeComments">Optional comments explaining the reason for the status change.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result indicates whether the status change was successful.
    /// </returns>
    Task<ApiResponse<bool>> ChangeStatusAsync(
        int problemId, 
        ProblemStatus newStatus, 
        int userId, 
        string? statusChangeComments = null);
}

/// <summary>
/// Data Transfer Object containing statistical information about problems within a project or system.
/// Used for dashboard displays and performance metrics reporting.
/// </summary>
public class ProblemStatisticsDto
{
    /// <summary>
    /// Gets or sets the total number of problems in the scope.
    /// </summary>
    public int TotalProblems { get; set; }

    /// <summary>
    /// Gets or sets the number of open problems.
    /// </summary>
    public int OpenProblems { get; set; }

    /// <summary>
    /// Gets or sets the number of problems currently in progress.
    /// </summary>
    public int InProgressProblems { get; set; }

    /// <summary>
    /// Gets or sets the number of resolved problems.
    /// </summary>
    public int ResolvedProblems { get; set; }

    /// <summary>
    /// Gets or sets the problem resolution rate as a percentage.
    /// </summary>
    public double ResolutionRate { get; set; }

    /// <summary>
    /// Gets or sets the average time to resolution in days.
    /// </summary>
    public double AverageResolutionTimeInDays { get; set; }

    /// <summary>
    /// Gets or sets the most frequently used tags with their usage counts.
    /// </summary>
    public Dictionary<string, int> TopTags { get; set; } = new();

    /// <summary>
    /// Gets or sets the monthly problem creation trend data.
    /// </summary>
    public Dictionary<string, int> MonthlyTrends { get; set; } = new();
}
