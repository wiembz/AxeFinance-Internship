using Microsoft.EntityFrameworkCore;
using TechDashboardAPI.Application.DTOs.Problem;
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
        int projectId,
        int pageNumber,
        int pageSize,
        string? tags = null,
        ProblemStatus? status = null,
        int? createdBy = null,
        int? assignedTo = null,
        string? search = null)
    {
        var query = _context.Problems
            .Include(p => p.Project).ThenInclude(pr => pr.Department)
            .Include(p => p.Solutions)
            .AsQueryable()
            .Where(p => p.ProjectId == projectId && p.IsActive);

        if (!string.IsNullOrWhiteSpace(tags))
            query = query.Where(p => p.Tags.Contains(tags));
        if (status.HasValue)
            query = query.Where(p => p.Status == status);
        if (createdBy.HasValue)
            query = query.Where(p => p.CreatedBy == createdBy);
        if (assignedTo.HasValue)
            query = query.Where(p => p.AssignedToUserId == assignedTo);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Title.Contains(search) || p.Description.Contains(search));

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(p => p.CreatedDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProblemResponseDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                Tags = p.Tags,
                AzureLink = p.AzureLink,
                AssignedToUserId = p.AssignedToUserId,
                Status = p.Status.ToString(),
                CreatedDate = p.CreatedDate,
                CreatedBy = p.CreatedBy.ToString(),
                ProjectId = p.ProjectId,
                ProjectName = p.Project != null ? p.Project.Name : string.Empty,
                DepartmentName = p.Project != null && p.Project.Department != null ? p.Project.Department.Name : string.Empty,
                AttachmentPath = p.AttachmentPath,
                LikeCount = p.LikeCount,
                IsLikedByCurrentUser = false, // TODO: Implement based on userId
                SolutionsCount = p.Solutions.Count,
                IsActive = p.IsActive,
                LastUpdatedDate = p.LastUpdatedDate,
                ResolvedDate = p.ResolvedDate
            }).ToListAsync();

        var paged = new PaginatedResponse<ProblemResponseDto>(items, total, pageNumber, pageSize);

        return ApiResponse<PaginatedResponse<ProblemResponseDto>>.SuccessResponse(paged);
    }

    public async Task<ApiResponse<ProblemDetailDto>> GetProblemByIdAsync(int id, int userId)
    {
        var problem = await _context.Problems
            .Include(p => p.Solutions)
            .Include(p => p.Project).ThenInclude(pr => pr.Department)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

        if (problem == null)
            return ApiResponse<ProblemDetailDto>.ErrorResponse("Problem not found");

        var dto = new ProblemDetailDto
        {
            Id = problem.Id,
            Title = problem.Title,
            Description = problem.Description,
            Tags = problem.Tags,
            AzureLink = problem.AzureLink,
            AssignedToUserId = problem.AssignedToUserId,
            IsLikedByCurrentUser = false, // TODO: implement like tracking
            CreatedDate = problem.CreatedDate,
            CreatedBy = problem.CreatedBy.ToString(),
            ProjectId = problem.ProjectId,
            ProjectName = problem.Project?.Name ?? string.Empty,
            DepartmentName = problem.Project?.Department?.Name ?? string.Empty,
            AttachmentPath = problem.AttachmentPath,
            LikeCount = problem.LikeCount,
            SolutionsCount = problem.Solutions.Count,
            IsActive = problem.IsActive,
            LastUpdatedDate = problem.LastUpdatedDate,
            ResolvedDate = problem.ResolvedDate,
            Solutions = problem.Solutions.Select(s => new SolutionDto
            {
                Id = s.Id,
                Content = s.Content,
                AttachmentPath = s.AttachmentPath,
                AzureDevOpsLink = s.AzureDevOpsLink,
                Status = s.Status.ToString(),
                CreatedDate = s.CreatedDate,
                ApprovedDate = s.ApprovedDate,
                CreatedBy = s.User != null ? s.User.Username : string.Empty,
                ApprovedBy = s.ApprovedByUser != null ? s.ApprovedByUser.Username : null,
                ProblemTitle = problem.Title,
                HasAttachment = !string.IsNullOrEmpty(s.AttachmentPath),
                CanEdit = s.UserId == userId && s.Status == SolutionStatus.Pending,
                CanDelete = s.UserId == userId && s.Status == SolutionStatus.Pending
            }).ToList()
        };

        return ApiResponse<ProblemDetailDto>.SuccessResponse(dto);
    }

    public async Task<ApiResponse<ProblemResponseDto>> CreateProblemAsync(CreateProblemDto createDto, int userId)
    {
        if (string.IsNullOrWhiteSpace(createDto.Title) || string.IsNullOrWhiteSpace(createDto.Description))
            return ApiResponse<ProblemResponseDto>.ErrorResponse("Title and Description are required.");

        var problem = new Problem
        {
            Title = createDto.Title.Trim(),
            Description = createDto.Description.Trim(),
            Tags = createDto.GetTagsAsString(),
            AzureLink = createDto.AzureLink,
            AssignedToUserId = createDto.AssignedToUserId,
            Status = ProblemStatus.Requested,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = userId,
            ProjectId = createDto.ProjectId,
            DepartmentId = createDto.DepartmentId,
            AttachmentPath = createDto.AttachmentPath,
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
            AzureLink = problem.AzureLink,
            AssignedToUserId = problem.AssignedToUserId,
            Status = problem.Status.ToString(),
            CreatedDate = problem.CreatedDate,
            CreatedBy = problem.CreatedBy.ToString(),
            ProjectId = problem.ProjectId,
            ProjectName = string.Empty,
            DepartmentName = string.Empty,
            AttachmentPath = problem.AttachmentPath,
            LikeCount = 0,
            IsLikedByCurrentUser = false,
            SolutionsCount = 0,
            IsActive = true
        };

        return ApiResponse<ProblemResponseDto>.SuccessResponse(dto);
    }

    public async Task<ApiResponse<ProblemResponseDto>> UpdateProblemAsync(int id, UpdateProblemDto updateDto, int userId)
    {
        var problem = await _context.Problems
            .Include(p => p.Solutions)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (problem == null || !problem.IsActive)
            return ApiResponse<ProblemResponseDto>.ErrorResponse("Problem not found");

        problem.Title = updateDto.Title.Trim();
        problem.Description = updateDto.Description.Trim();
        problem.Tags = updateDto.Tags != null ? string.Join(",", updateDto.Tags.Select(t => t.Trim())) : string.Empty;
        problem.AzureLink = updateDto.AzureLink;
        problem.AssignedToUserId = updateDto.AssignedToUserId;
        problem.AttachmentPath = updateDto.AttachmentPath;
        problem.LastUpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var dto = new ProblemResponseDto
        {
            Id = problem.Id,
            Title = problem.Title,
            Description = problem.Description,
            Tags = problem.Tags,
            AzureLink = problem.AzureLink,
            AssignedToUserId = problem.AssignedToUserId,
            Status = problem.Status.ToString(),
            CreatedDate = problem.CreatedDate,
            CreatedBy = problem.CreatedBy.ToString(),
            ProjectId = problem.ProjectId,
            ProjectName = string.Empty,
            DepartmentName = string.Empty,
            AttachmentPath = problem.AttachmentPath,
            LikeCount = problem.LikeCount,
            IsLikedByCurrentUser = false,
            SolutionsCount = problem.Solutions?.Count ?? 0,
            LastUpdatedDate = problem.LastUpdatedDate,
            ResolvedDate = problem.ResolvedDate
        };

        return ApiResponse<ProblemResponseDto>.SuccessResponse(dto);
    }

    public async Task<ApiResponse<bool>> DeleteProblemAsync(int id, int userId)
    {
        var problem = await _context.Problems.FindAsync(id);
        if (problem == null)
            return ApiResponse<bool>.ErrorResponse("Problem not found");

        // TODO: allow admin override for delete
        if (problem.CreatedBy != userId)
            return ApiResponse<bool>.ErrorResponse("Not authorized");

        problem.IsActive = false;
        await _context.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(true);
    }

    public Task<ApiResponse<bool>> LikeProblemAsync(int problemId, int userId)
    {
    // Like tracking not implemented yet
    return Task.FromResult(ApiResponse<bool>.SuccessResponse(true));
    }

    public Task<ApiResponse<bool>> UnlikeProblemAsync(int problemId, int userId)
    {
    // Like tracking not implemented yet
    return Task.FromResult(ApiResponse<bool>.SuccessResponse(true));
    }

    public async Task<ApiResponse<List<string>>> GetAllTagsAsync()
    {
        var tags = await _context.Problems
            .Where(p => !string.IsNullOrEmpty(p.Tags))
            .SelectMany(p => p.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrEmpty(t))
            .Distinct()
            .ToListAsync();

        return ApiResponse<List<string>>.SuccessResponse(tags);
    }

    public Task<ApiResponse<PaginatedResponse<ProblemResponseDto>>> GetUserAccessibleProblemsAsync(int userId, int pageNumber, int pageSize, string? searchTerm = null)
    {
    // Permission logic not implemented yet
    return Task.FromResult(ApiResponse<PaginatedResponse<ProblemResponseDto>>.SuccessResponse(new PaginatedResponse<ProblemResponseDto>()));
    }

    public Task<ApiResponse<PaginatedResponse<ProblemResponseDto>>> GetProblemsByUserAsync(int ownerUserId, int requestingUserId, int pageNumber, int pageSize)
    {
    // Ownership/permissions logic not implemented yet
    return Task.FromResult(ApiResponse<PaginatedResponse<ProblemResponseDto>>.SuccessResponse(new PaginatedResponse<ProblemResponseDto>()));
    }

    public Task<ApiResponse<ProblemStatisticsDto>> GetProblemStatisticsAsync(int projectId, int userId)
    {
    // Statistics aggregation not implemented yet
    return Task.FromResult(ApiResponse<ProblemStatisticsDto>.SuccessResponse(new ProblemStatisticsDto()));
    }

    public async Task<ApiResponse<bool>> ChangeStatusAsync(int problemId, ProblemStatus newStatus, int userId, string? statusChangeComments = null)
    {
        var problem = await _context.Problems.FindAsync(problemId);
        if (problem == null)
            return ApiResponse<bool>.ErrorResponse("Problem not found");

        switch (newStatus)
        {
            case ProblemStatus.Resolved:
                if (problem.AssignedToUserId != userId /* && !IsAdmin(userId) */)
                    return ApiResponse<bool>.ErrorResponse("Not authorized to resolve");
                problem.Status = ProblemStatus.Resolved;
                problem.ResolvedDate = DateTime.UtcNow;
                break;
            case ProblemStatus.Closed:
                problem.Status = ProblemStatus.Closed;
                break;
            case ProblemStatus.Rejected:
                problem.Status = ProblemStatus.Rejected;
                break;
            default:
                return ApiResponse<bool>.ErrorResponse("Invalid status change");
        }

        problem.LastUpdatedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(true);
    }
}
