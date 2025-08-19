using TechDashboardAPI.Application.DTOs;
using TechDashboardAPI.Application.Responses;

namespace TechDashboardAPI.Application.Interfaces;

public interface ISolutionService
{
    Task<ApiResponse<List<SolutionDto.SolutionResponseDto>>> GetSolutionsByProblemAsync(int problemId);
    Task<ApiResponse<List<SolutionDto.SolutionResponseDto>>> GetPendingSolutionsAsync();
    Task<ApiResponse<SolutionDto.SolutionResponseDto>> CreateSolutionAsync(CreateSolutionDto createDto, int userId);
    Task<ApiResponse<SolutionDto.SolutionResponseDto>> UpdateSolutionAsync(int id, UpdateSolutionDto updateDto, int userId);
    Task<ApiResponse<bool>> DeleteSolutionAsync(int id, int userId);
    Task<ApiResponse<bool>> ApproveSolutionAsync(SolutionDto.ApproveSolutionDto approveDto, int adminId);
}
