using TechDashboardAPI.Application.DTOs.Problem;
using TechDashboardAPI.Application.DTOs;
using TechDashboardAPI.Application.Responses;
using TechDashboardAPI.Domain.Entities;

namespace TechDashboardAPI.Application.Services;

public interface IUserService
{
    Task<ApiResponse<User>> GetUserByIdAsync(int id);
    Task<ApiResponse<User>> GetUserByEmailAsync(string email);
    Task<ApiResponse<PaginatedResponse<User>>> GetUsersAsync(int page, int pageSize);
    Task<ApiResponse<User>> CreateUserAsync(CreateUserDto dto);
    Task<ApiResponse<User>> UpdateUserAsync(int id, UpdateUserDto dto);
    Task<ApiResponse<bool>> DeactivateUserAsync(int id);
    Task<ApiResponse<bool>> ActivateUserAsync(int id);
    Task<ApiResponse<bool>> UpdateUserRoleAsync(int id, string role);
}

public interface IProblemService
{
    Task<ApiResponse<Problem>> GetProblemByIdAsync(int id);
    Task<ApiResponse<PaginatedResponse<Problem>>> GetProblemsAsync(int page, int pageSize, string? search = null, int? projectId = null);
    Task<ApiResponse<Problem>> CreateProblemAsync(CreateProblemDto dto, int userId);
    Task<ApiResponse<Problem>> UpdateProblemAsync(int id, UpdateProblemDto dto, int userId);
    Task<ApiResponse<bool>> DeleteProblemAsync(int id, int userId);
    Task<ApiResponse<bool>> LikeProblemAsync(int problemId, int userId);
}

public interface ISolutionService
{
    Task<ApiResponse<Solution>> GetSolutionByIdAsync(int id);
    Task<ApiResponse<PaginatedResponse<Solution>>> GetSolutionsByProblemAsync(int problemId, int page, int pageSize);
    Task<ApiResponse<Solution>> CreateSolutionAsync(TechDashboardAPI.Application.DTOs.SolutionDto.CreateSolutionDto dto, int userId);
    Task<ApiResponse<Solution>> UpdateSolutionAsync(int id, TechDashboardAPI.Application.DTOs.SolutionDto.UpdateSolutionDto dto, int userId);
    Task<ApiResponse<bool>> DeleteSolutionAsync(int id, int userId);
    Task<ApiResponse<bool>> ApproveSolutionAsync(int id, bool isApproved, int adminId);
}
