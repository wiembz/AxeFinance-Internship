using TechDashboardAPI.Application.DTOs.Problem;
using TechDashboardAPI.Application.Responses;
using TechDashboardAPI.Domain.Enums;

namespace TechDashboardAPI.Application.Interfaces
{
    public interface IProblemService
    {
        Task<ApiResponse<PaginatedResponse<ProblemResponseDto>>> GetProblemsByProjectAsync(
            int projectId,
            int pageNumber,
            int pageSize,
            string? tags = null,
            ProblemStatus? status = null,
            int? createdBy = null,
            int? assignedTo = null,
            string? search = null);

        Task<ApiResponse<ProblemDetailDto>> GetProblemByIdAsync(int id, int userId);

    Task<ApiResponse<ProblemResponseDto>> CreateProblemAsync(TechDashboardAPI.Application.DTOs.Problem.CreateProblemDto createDto, int userId);

        Task<ApiResponse<ProblemResponseDto>> UpdateProblemAsync(int id, UpdateProblemDto updateDto, int userId);

        Task<ApiResponse<bool>> DeleteProblemAsync(int id, int userId); // soft delete recommended

        Task<ApiResponse<bool>> LikeProblemAsync(int problemId, int userId);
        Task<ApiResponse<bool>> UnlikeProblemAsync(int problemId, int userId);

        Task<ApiResponse<List<string>>> GetAllTagsAsync();

        Task<ApiResponse<PaginatedResponse<ProblemResponseDto>>> GetUserAccessibleProblemsAsync(
            int userId,
            int pageNumber,
            int pageSize,
            string? searchTerm = null);

        Task<ApiResponse<PaginatedResponse<ProblemResponseDto>>> GetProblemsByUserAsync(
            int ownerUserId,
            int requestingUserId,
            int pageNumber,
            int pageSize);

        Task<ApiResponse<ProblemStatisticsDto>> GetProblemStatisticsAsync(int projectId, int userId);

        Task<ApiResponse<bool>> ChangeStatusAsync(
            int problemId,
            ProblemStatus newStatus,
            int userId,
            string? statusChangeComments = null);
    }
}
