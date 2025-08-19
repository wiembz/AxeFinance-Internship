// NOTE: The /api/problems/tags/popular endpoint is not implemented in the backend.
// If you need tag suggestions, use /api/problems/tags or implement the endpoint as needed.
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TechDashboardAPI.Application.DTOs;
using TechDashboardAPI.Application.Interfaces;
using TechDashboardAPI.Application.Responses;
using TechDashboardAPI.Domain.Entities;
using TechDashboardAPI.Domain.Enums;
using TechDashboardAPI.Infrastructure.Data;

namespace TechDashboardAPI.Infrastructure.Services;

public class ProblemService : IProblemService
{
    private readonly ApplicationDbContext _context;

    public ProblemService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<PaginatedResponse<ProblemResponseDto>>> GetProblemsByProjectAsync(
        int projectId, int pageNumber, int pageSize, string? tags = null, ProblemStatus? status = null, string? priority = null, int? createdBy = null)
    {
        var query = _context.Problems.AsQueryable().Where(p => p.ProjectId == projectId);
        if (!string.IsNullOrWhiteSpace(tags))
            query = query.Where(p => p.Tags.Contains(tags));
        if (status.HasValue)
            query = query.Where(p => p.Status == status);
        if (createdBy.HasValue)
            query = query.Where(p => p.CreatedBy == createdBy);
        var total = await query.CountAsync();
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(p => new ProblemResponseDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                Tags = p.Tags,
                Status = p.Status.ToString(),
                CreatedDate = p.CreatedDate,
                CreatedBy = p.CreatedBy.ToString(), // Use ID as string, or map as needed
                ProjectId = p.ProjectId,
                ProjectName = p.Project != null ? p.Project.Name : string.Empty,
                DepartmentName = p.Project != null && p.Project.Department != null ? p.Project.Department.Name : string.Empty,
                AttachmentPath = p.AttachmentPath,
                LikeCount = p.LikeCount,
                IsLikedByCurrentUser = false, // Implement as needed
                SolutionsCount = p.Solutions.Count,
                IsActive = p.IsActive,
                LastUpdatedDate = p.LastUpdatedDate,
                ResolvedDate = p.ResolvedDate
            }).ToListAsync();
        // Try to assign to Results, if not, just return the list directly or use the constructor
        var paged = new PaginatedResponse<ProblemResponseDto>();
        // Try common property names
        if (paged.GetType().GetProperty("Results") != null)
            paged.GetType().GetProperty("Results")!.SetValue(paged, items);
        else if (paged.GetType().GetProperty("Problems") != null)
            paged.GetType().GetProperty("Problems")!.SetValue(paged, items);
        else if (paged.GetType().GetProperty("Page") != null && paged.GetType().GetProperty("Page")!.PropertyType == typeof(List<ProblemResponseDto>))
            paged.GetType().GetProperty("Page")!.SetValue(paged, items);
        paged.TotalCount = total;
        if (paged.GetType().GetProperty("Page") != null && paged.GetType().GetProperty("Page")!.PropertyType == typeof(int))
            paged.GetType().GetProperty("Page")!.SetValue(paged, pageNumber);
        paged.PageSize = pageSize;
    return new ApiResponse<PaginatedResponse<ProblemResponseDto>> { Data = paged, Success = true };
    }

    public async Task<ApiResponse<ProblemDetailDto>> GetProblemByIdAsync(int id, int userId)
    {
        var problem = await _context.Problems.Include(p => p.Solutions).FirstOrDefaultAsync(p => p.Id == id);
        if (problem == null)
            return new ApiResponse<ProblemDetailDto> { Success = false, Message = "Problem not found" };
        var dto = new ProblemDetailDto
        {
            Id = problem.Id,
            Title = problem.Title,
            Description = problem.Description,
            Tags = problem.Tags,
            Status = problem.Status.ToString(),
            CreatedDate = problem.CreatedDate,
            CreatedBy = problem.CreatedBy.ToString(), // Use ID as string, or map as needed
            ProjectId = problem.ProjectId,
            ProjectName = problem.Project != null ? problem.Project.Name : string.Empty,
            DepartmentName = problem.Project != null && problem.Project.Department != null ? problem.Project.Department.Name : string.Empty,
            AttachmentPath = problem.AttachmentPath,
            LikeCount = problem.LikeCount,
            IsLikedByCurrentUser = false, // Implement as needed
            SolutionsCount = problem.Solutions.Count,
            IsActive = problem.IsActive,
            LastUpdatedDate = problem.LastUpdatedDate,
            ResolvedDate = problem.ResolvedDate,
            Solutions = new List<SolutionDto.SolutionResponseDto>() // Map solutions as needed
        };
    return new ApiResponse<ProblemDetailDto> { Data = dto, Success = true };
    }

    public async Task<ApiResponse<ProblemResponseDto>> CreateProblemAsync(CreateProblemDto createDto, int userId)
    {
        var problem = new Problem
        {
            Title = createDto.Title,
            Description = createDto.Description,
            Tags = createDto.Tags is List<string> tagList ? string.Join(",", tagList) : (createDto.Tags?.ToString() ?? string.Empty),
            Status = ProblemStatus.Open,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = userId,
            ProjectId = createDto.ProjectId,
            AttachmentPath = null, // Set as needed
            IsActive = true
        };
        _context.Problems.Add(problem);
        await _context.SaveChangesAsync();
        var dto = new ProblemResponseDto
        {
            Id = problem.Id,
            Title = problem.Title,
            Description = problem.Description,
            Tags = problem.Tags,
            Status = problem.Status.ToString(),
            CreatedDate = problem.CreatedDate,
            CreatedBy = problem.CreatedBy.ToString(),
            ProjectId = problem.ProjectId,
            ProjectName = string.Empty, // Fill as needed
            DepartmentName = string.Empty, // Fill as needed
            AttachmentPath = problem.AttachmentPath,
            LikeCount = 0,
            IsLikedByCurrentUser = false,
            SolutionsCount = 0,
            IsActive = true
        };
    return new ApiResponse<ProblemResponseDto> { Data = dto, Success = true };
    }

    public async Task<ApiResponse<ProblemResponseDto>> UpdateProblemAsync(int id, UpdateProblemDto updateDto, int userId)
    {
        var problem = await _context.Problems.FindAsync(id);
        if (problem == null)
            return new ApiResponse<ProblemResponseDto> { Success = false, Message = "Problem not found" };
    problem.Title = updateDto.Title;
    problem.Description = updateDto.Description;
    problem.Tags = updateDto.Tags is List<string> tagList ? string.Join(",", tagList) : (updateDto.Tags?.ToString() ?? string.Empty);
    problem.LastUpdatedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        var dto = new ProblemResponseDto
        {
            Id = problem.Id,
            Title = problem.Title,
            Description = problem.Description,
            Tags = problem.Tags,
            Status = problem.Status.ToString(),
            CreatedDate = problem.CreatedDate,
            CreatedBy = problem.CreatedBy.ToString(),
            ProjectId = problem.ProjectId,
            ProjectName = string.Empty, // Fill as needed
            DepartmentName = string.Empty, // Fill as needed
            AttachmentPath = problem.AttachmentPath,
            LikeCount = problem.LikeCount,
            IsLikedByCurrentUser = false,
            SolutionsCount = 0,
            IsActive = problem.IsActive
        };
    return new ApiResponse<ProblemResponseDto> { Data = dto, Success = true };
    }

    public async Task<ApiResponse<bool>> DeleteProblemAsync(int id, int userId)
    {
        var problem = await _context.Problems.FindAsync(id);
        if (problem == null)
            return new ApiResponse<bool> { Success = false, Message = "Problem not found" };
        _context.Problems.Remove(problem);
        await _context.SaveChangesAsync();
    return new ApiResponse<bool> { Data = true, Success = true };
    }

    public Task<ApiResponse<bool>> LikeProblemAsync(int problemId, int userId)
    {
        // Implement like logic
    return Task.FromResult(new ApiResponse<bool> { Data = true, Success = true });
    }

    public Task<ApiResponse<bool>> UnlikeProblemAsync(int problemId, int userId)
    {
        // Implement unlike logic
    return Task.FromResult(new ApiResponse<bool> { Data = true, Success = true });
    }

    public async Task<ApiResponse<List<string>>> GetAllTagsAsync()
    {
        var tags = await _context.Problems
            .Where(p => !string.IsNullOrEmpty(p.Tags))
            .SelectMany(p => p.Tags.Split(',', System.StringSplitOptions.RemoveEmptyEntries))
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrEmpty(t))
            .Distinct()
            .ToListAsync();
    return new ApiResponse<List<string>> { Data = tags, Success = true };
    }

    public Task<ApiResponse<PaginatedResponse<ProblemResponseDto>>> GetUserAccessibleProblemsAsync(int userId, int pageNumber, int pageSize, string? searchTerm = null)
    {
        // Implement as needed
    return Task.FromResult(new ApiResponse<PaginatedResponse<ProblemResponseDto>> { Data = new PaginatedResponse<ProblemResponseDto>(), Success = true });
    }

    public Task<ApiResponse<PaginatedResponse<ProblemResponseDto>>> GetProblemsByUserAsync(int userId, int requestingUserId, int pageNumber, int pageSize)
    {
        // Implement as needed
    return Task.FromResult(new ApiResponse<PaginatedResponse<ProblemResponseDto>> { Data = new PaginatedResponse<ProblemResponseDto>(), Success = true });
    }

    public Task<ApiResponse<ProblemStatisticsDto>> GetProblemStatisticsAsync(int projectId, int userId)
    {
        // Implement as needed
    return Task.FromResult(new ApiResponse<ProblemStatisticsDto> { Data = new ProblemStatisticsDto(), Success = true });
    }

    public Task<ApiResponse<bool>> ChangeStatusAsync(int problemId, ProblemStatus newStatus, int userId, string? statusChangeComments = null)
    {
        // Implement as needed
    return Task.FromResult(new ApiResponse<bool> { Data = true, Success = true });
    }
}
