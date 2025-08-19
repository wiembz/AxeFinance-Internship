using TechDashboardAPI.Application.DTOs;
using TechDashboardAPI.Application.Responses;

namespace TechDashboardAPI.Application.Interfaces;

public interface IProjectService
{
    Task<ApiResponse<List<ProjectResponseDto>>> GetProjectsByDepartmentAsync(int departmentId);
    Task<ApiResponse<ProjectResponseDto>> GetProjectByIdAsync(int id);
    Task<ApiResponse<ProjectResponseDto>> CreateProjectAsync(CreateProjectDto createDto, int userId);
    Task<ApiResponse<ProjectResponseDto>> UpdateProjectAsync(int id, UpdateProjectDto updateDto, int userId);
    Task<ApiResponse<bool>> DeleteProjectAsync(int id, int userId);
}
