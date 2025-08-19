using TechDashboardAPI.Application.DTOs;
using TechDashboardAPI.Application.Responses;

namespace TechDashboardAPI.Application.Interfaces;

public interface IProblemFormService
{
    Task<ApiResponse<List<ProblemFormResponseDto>>> GetFormsByProjectAsync(int projectId);
    Task<ApiResponse<ProblemFormResponseDto>> GetFormByIdAsync(int id);
    Task<ApiResponse<ProblemFormResponseDto>> CreateFormAsync(CreateProblemFormDto createDto, int userId);
    Task<ApiResponse<ProblemFormResponseDto>> UpdateFormAsync(int id, UpdateProblemFormDto updateDto, int userId);
    Task<ApiResponse<bool>> DeleteFormAsync(int id, int userId);
}
