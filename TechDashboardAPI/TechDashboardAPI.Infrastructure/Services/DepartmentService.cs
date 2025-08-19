using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TechDashboardAPI.Application.DTOs;
using TechDashboardAPI.Application.Interfaces;
using TechDashboardAPI.Application.Responses;
using TechDashboardAPI.Domain.Entities;
using TechDashboardAPI.Infrastructure.Data;

namespace TechDashboardAPI.Infrastructure.Services;

public class DepartmentService : IDepartmentService
{
    private readonly ApplicationDbContext _context;

    public DepartmentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<PaginatedDepartmentsResponse>> GetAllDepartmentsAsync(
        int page = 1, 
        int pageSize = 20, 
        string? searchTerm = null,
        string sortBy = "name",
        string sortOrder = "asc")
    {
        try
        {
            var query = _context.Departments
                .Include(d => d.CreatedByUser)
                .Include(d => d.Projects)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var search = searchTerm.Trim().ToLower();
                query = query.Where(d => 
                    d.Name.ToLower().Contains(search) ||
                    d.Description.ToLower().Contains(search));
            }

            // Apply sorting
            query = sortBy.ToLowerInvariant() switch
            {
                "created" => sortOrder.ToLowerInvariant() == "desc" 
                    ? query.OrderByDescending(d => d.CreatedDate).ThenBy(d => d.Name)
                    : query.OrderBy(d => d.CreatedDate).ThenBy(d => d.Name),
                "projects" => sortOrder.ToLowerInvariant() == "desc" 
                    ? query.OrderByDescending(d => d.Projects.Count).ThenBy(d => d.Name)
                    : query.OrderBy(d => d.Projects.Count).ThenBy(d => d.Name),
                _ => sortOrder.ToLowerInvariant() == "desc" 
                    ? query.OrderByDescending(d => d.Name)
                    : query.OrderBy(d => d.Name)
            };

            var totalCount = await query.CountAsync();
            var departments = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new DepartmentSummaryDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Description = d.Description,
                    CreatedDate = d.CreatedDate,
                    CreatedBy = d.CreatedByUser.Username,
                    ProjectsCount = d.Projects.Count,
                    UsersCount = 0,
                    CanEdit = true,
                    CanDelete = d.Projects.Count == 0
                })
                .ToListAsync();

            var response = new PaginatedDepartmentsResponse
            {
                Departments = departments,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                HasNextPage = page * pageSize < totalCount,
                HasPreviousPage = page > 1,
                AppliedFilters = new Dictionary<string, object>
                {
                    ["searchTerm"] = searchTerm ?? string.Empty,
                    ["sortBy"] = sortBy,
                    ["sortOrder"] = sortOrder
                }
            };

            return ApiResponse<PaginatedDepartmentsResponse>.SuccessResponse(response, "Departments retrieved successfully.");
        }
        catch (Exception ex)
        {
            return ApiResponse<PaginatedDepartmentsResponse>.ErrorResponse($"Error retrieving departments: {ex.Message}");
        }
    }

    public async Task<ApiResponse<DepartmentResponseDto>> GetDepartmentByIdAsync(int id)
    {
        try
        {
            var department = await _context.Departments
                .Include(d => d.CreatedByUser)
                .Include(d => d.Projects)
                .FirstOrDefaultAsync(d => d.Id == id);
                
            if (department == null)
                return ApiResponse<DepartmentResponseDto>.ErrorResponse("Department not found.");
                
            var dto = new DepartmentResponseDto
            {
                Id = department.Id,
                Name = department.Name,
                Description = department.Description,
                CreatedDate = department.CreatedDate,
                CreatedBy = department.CreatedByUser?.Username ?? "Unknown",
                ProjectsCount = department.Projects?.Count ?? 0
            };
            
            return ApiResponse<DepartmentResponseDto>.SuccessResponse(dto);
        }
        catch (Exception ex)
        {
            return ApiResponse<DepartmentResponseDto>.ErrorResponse($"Error retrieving department: {ex.Message}");
        }
    }

    public async Task<ApiResponse<DepartmentResponseDto>> CreateDepartmentAsync(CreateDepartmentDto createDto, int userId)
    {
        try
        {
            // Check if department name already exists among active departments
            var existingDepartment = await _context.Departments
                .FirstOrDefaultAsync(d => d.Name.ToLower() == createDto.Name.ToLower());
            
            if (existingDepartment != null)
                return ApiResponse<DepartmentResponseDto>.ErrorResponse("A department with this name already exists.");

            // Validate user existence
            var createdByUser = await _context.Users.FindAsync(userId);
            if (createdByUser == null)
                return ApiResponse<DepartmentResponseDto>.ErrorResponse($"User with ID {userId} does not exist. Cannot create department.");

            var department = new Department
            {
                Name = createDto.Name.Trim(),
                Description = createDto.Description?.Trim() ?? string.Empty,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = userId
            };

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();

            var dto = new DepartmentResponseDto
            {
                Id = department.Id,
                Name = department.Name,
                Description = department.Description,
                CreatedDate = department.CreatedDate,
                CreatedBy = createdByUser.Username,
                ProjectsCount = 0
            };

            return ApiResponse<DepartmentResponseDto>.SuccessResponse(dto, "Department created successfully.");
        }
        catch (Exception ex)
        {
            return ApiResponse<DepartmentResponseDto>.ErrorResponse($"Error creating department: {ex.Message}");
        }
    }

    public async Task<ApiResponse<DepartmentResponseDto>> UpdateDepartmentAsync(int id, UpdateDepartmentDto updateDto, int userId)
    {
        try
        {
            var department = await _context.Departments
                .Include(d => d.CreatedByUser)
                .Include(d => d.Projects)
                .FirstOrDefaultAsync(d => d.Id == id);
                
            if (department == null)
                return ApiResponse<DepartmentResponseDto>.ErrorResponse("Department not found.");

            // Check if new name already exists (excluding current department)
            var existingDepartment = await _context.Departments
                .FirstOrDefaultAsync(d => d.Name.ToLower() == updateDto.Name.ToLower() && d.Id != id);
            
            if (existingDepartment != null)
                return ApiResponse<DepartmentResponseDto>.ErrorResponse("A department with this name already exists.");

            department.Name = updateDto.Name.Trim();
            department.Description = updateDto.Description?.Trim() ?? string.Empty;
            
            await _context.SaveChangesAsync();
            
            var dto = new DepartmentResponseDto
            {
                Id = department.Id,
                Name = department.Name,
                Description = department.Description,
                CreatedDate = department.CreatedDate,
                CreatedBy = department.CreatedByUser?.Username ?? "Unknown",
                ProjectsCount = department.Projects?.Count ?? 0
            };
            
            return ApiResponse<DepartmentResponseDto>.SuccessResponse(dto, "Department updated successfully.");
        }
        catch (Exception ex)
        {
            return ApiResponse<DepartmentResponseDto>.ErrorResponse($"Error updating department: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> DeleteDepartmentAsync(int id, int userId)
    {
        try
        {
            var department = await _context.Departments
                .Include(d => d.Projects)
                .FirstOrDefaultAsync(d => d.Id == id);
                
            if (department == null)
                return ApiResponse<bool>.ErrorResponse("Department not found.");

            // Check if department has projects
            if (department.Projects != null && department.Projects.Any())
                return ApiResponse<bool>.ErrorResponse("Cannot delete department with projects. Please remove or move projects first.");

            // Check if department has users
            var hasUsers = await _context.Users.AnyAsync(u => u.DepartmentId == id);
            if (hasUsers)
                return ApiResponse<bool>.ErrorResponse("Cannot delete department with users. Please reassign users first.");

            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();
            
            return ApiResponse<bool>.SuccessResponse(true, "Department deleted successfully.");
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.ErrorResponse($"Error deleting department: {ex.Message}");
        }
    }
    
}
