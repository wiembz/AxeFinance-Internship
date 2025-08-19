using TechDashboardAPI.Application.DTOs;
using TechDashboardAPI.Application.Responses;

namespace TechDashboardAPI.Application.Interfaces;

public interface IDepartmentService
{
    Task<ApiResponse<PaginatedDepartmentsResponse>> GetAllDepartmentsAsync(
        int page = 1, 
        int pageSize = 20, 
        string? searchTerm = null,
        string sortBy = "name",
        string sortOrder = "asc");
    
    Task<ApiResponse<DepartmentResponseDto>> GetDepartmentByIdAsync(int id);
    Task<ApiResponse<DepartmentResponseDto>> CreateDepartmentAsync(CreateDepartmentDto createDto, int userId);
    Task<ApiResponse<DepartmentResponseDto>> UpdateDepartmentAsync(int id, UpdateDepartmentDto updateDto, int userId);
    Task<ApiResponse<bool>> DeleteDepartmentAsync(int id, int userId);

}
