
using TechDashboardAPI.Application.DTOs.Problem;

using TechDashboardAPI.Application.Responses;

namespace TechDashboardAPI.Application.Interfaces;

public interface ISolutionService
{
    Task<ApiResponse<List<TechDashboardAPI.Application.DTOs.SolutionDto.SolutionResponseDto>>> GetSolutionsByProblemAsync(int problemId);
    Task<ApiResponse<List<TechDashboardAPI.Application.DTOs.SolutionDto.SolutionResponseDto>>> GetPendingSolutionsAsync();
    Task<ApiResponse<TechDashboardAPI.Application.DTOs.SolutionDto.SolutionResponseDto>> CreateSolutionAsync(TechDashboardAPI.Application.DTOs.SolutionDto.CreateSolutionDto createDto, int userId);
    Task<ApiResponse<TechDashboardAPI.Application.DTOs.SolutionDto.SolutionResponseDto>> UpdateSolutionAsync(int id, TechDashboardAPI.Application.DTOs.SolutionDto.UpdateSolutionDto updateDto, int userId);
    Task<ApiResponse<bool>> DeleteSolutionAsync(int id, int userId);
    Task<ApiResponse<bool>> ApproveSolutionAsync(TechDashboardAPI.Application.DTOs.SolutionDto.ApproveSolutionDto approveDto, int adminId);
}
