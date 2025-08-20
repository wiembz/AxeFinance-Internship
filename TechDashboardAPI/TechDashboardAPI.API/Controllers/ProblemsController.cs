using TechDashboardAPI.Application.DTOs.Problem;
using TechDashboardAPI.Application.DTOs.Problem;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json.Serialization;
using TechDashboardAPI.Domain.Entities;
using TechDashboardAPI.Domain.Enums;
using TechDashboardAPI.Infrastructure.Data;
using TechDashboardAPI.Application.DTOs;
using UpdateProblemRequest = TechDashboardAPI.Application.DTOs.UpdateProblemRequest;
using TechDashboardAPI.Application.Responses;

namespace TechDashboardAPI.API.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class ProblemsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ProblemsController> _logger;

    public ProblemsController(
        ApplicationDbContext context, 
        IWebHostEnvironment environment,
        ILogger<ProblemsController> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedProblemsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetProblems(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 12,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int? projectId = null,
        [FromQuery] int? departmentId = null,
        [FromQuery] string? status = null,
        [FromQuery] bool? isActive = true,
        [FromQuery] string? tags = null,
        [FromQuery] string sortBy = "created",
        [FromQuery] string sortOrder = "desc")
    {
        
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Getting problems. CorrelationId: {CorrelationId}, Page: {Page}, PageSize: {PageSize}, SearchTerm: {SearchTerm}, ProjectId: {ProjectId}, DepartmentId: {DepartmentId}",
            correlationId, page, pageSize, searchTerm, projectId, departmentId);

        try
        {
            // Validate parameters
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 12;

            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                _logger.LogWarning("Unable to retrieve user ID from token. CorrelationId: {CorrelationId}", correlationId);
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Invalid authentication token. Please login again."));
            }

            var query = _context.Problems
                .Include(p => p.Project)
                    .ThenInclude(pr => pr.Department)
                .Include(p => p.CreatedByUser)
                .Include(p => p.Solutions.Where(s => s.Status == Domain.Enums.SolutionStatus.Approved))
                .Include(p => p.ProblemLikes)
                .AsQueryable();

            // Apply filters
            if (isActive.HasValue)
            {
                query = query.Where(p => p.IsActive == isActive.Value);
            }

            if (projectId.HasValue && projectId.Value > 0)
            {
                query = query.Where(p => p.ProjectId == projectId.Value);
            }

            if (departmentId.HasValue && departmentId.Value > 0)
            {
                query = query.Where(p => p.Project.DepartmentId == departmentId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerSearchTerm = searchTerm.ToLowerInvariant();
                query = query.Where(p => 
                    p.Title.ToLower().Contains(lowerSearchTerm) || 
                    p.Description.ToLower().Contains(lowerSearchTerm) ||
                    p.Project.Name.ToLower().Contains(lowerSearchTerm) ||
                    p.Project.Department.Name.ToLower().Contains(lowerSearchTerm));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (Enum.TryParse<ProblemStatus>(status, true, out var statusEnum))
                {
                    query = query.Where(p => p.Status == statusEnum);
                }
            }

            // Apply tag filtering if provided
            if (!string.IsNullOrWhiteSpace(tags))
            {
                var tagList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(t => t.Trim().ToLowerInvariant())
                                 .Where(t => !string.IsNullOrEmpty(t))
                                 .ToList();

                if (tagList.Any())
                {
                    query = query.Where(p => !string.IsNullOrEmpty(p.Tags) && 
                                           tagList.Any(tag => p.Tags.ToLower().Contains(tag)));
                }
            }

            // Apply sorting
            query = sortBy.ToLowerInvariant() switch
            {
                "likes" => sortOrder.ToLowerInvariant() == "asc" 
                    ? query.OrderBy(p => p.LikeCount).ThenByDescending(p => p.CreatedDate)
                    : query.OrderByDescending(p => p.LikeCount).ThenByDescending(p => p.CreatedDate),
                "title" => sortOrder.ToLowerInvariant() == "asc" 
                    ? query.OrderBy(p => p.Title).ThenByDescending(p => p.CreatedDate)
                    : query.OrderByDescending(p => p.Title).ThenByDescending(p => p.CreatedDate),
                "status" => sortOrder.ToLowerInvariant() == "asc" 
                    ? query.OrderBy(p => p.Status).ThenByDescending(p => p.CreatedDate)
                    : query.OrderByDescending(p => p.Status).ThenByDescending(p => p.CreatedDate),
                _ => sortOrder.ToLowerInvariant() == "asc" 
                    ? query.OrderBy(p => p.CreatedDate)
                    : query.OrderByDescending(p => p.CreatedDate)
            };

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var problems = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProblemSummaryDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Description.Length > 200 
                        ? p.Description.Substring(0, 200) + "..."
                        : p.Description,
                    Tags = p.Tags,
                    CreatedDate = p.CreatedDate,
                    CreatedBy = p.CreatedByUser.Username,
                    ProjectName = p.Project.Name,
                    DepartmentName = p.Project.Department.Name,
                    AttachmentPath = p.AttachmentPath,
                    LikeCount = p.LikeCount,
                    IsLikedByCurrentUser = p.ProblemLikes.Any(pl => pl.UserId == userId),
                    SolutionsCount = p.Solutions.Count,
                    CanEdit = p.CreatedBy == userId,
                    HasAttachment = !string.IsNullOrEmpty(p.AttachmentPath)
                })
                .ToListAsync();

            var response = new PaginatedProblemsResponse
            {
                Problems = problems,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1,
                AppliedFilters = new Dictionary<string, object>
                {
                    ["searchTerm"] = searchTerm ?? string.Empty,
                    ["projectId"] = projectId ?? 0,
                    ["departmentId"] = departmentId ?? 0,
                    ["status"] = status ?? string.Empty,
                    ["isActive"] = isActive ?? true,
                    ["tags"] = tags ?? string.Empty
                }
            };

            _logger.LogInformation("Successfully retrieved problems. CorrelationId: {CorrelationId}, Count: {Count}, TotalCount: {TotalCount}",
                correlationId, problems.Count, totalCount);

            return Ok(ApiResponse<PaginatedProblemsResponse>.SuccessResponse(response, "Problems retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving problems. CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse("An error occurred while retrieving problems"));
        }
    }

    [HttpGet("project/{projectId:int}")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedProblemsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProblemsByProject(
        int projectId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10, 
        [FromQuery] string? tags = null,
        [FromQuery] string sortBy = "created",
        [FromQuery] string sortOrder = "desc")
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Getting problems by project. CorrelationId: {CorrelationId}, ProjectId: {ProjectId}, Page: {Page}, PageSize: {PageSize}, Tags: {Tags}",
            correlationId, projectId, page, pageSize, tags);

        try
        {
            // Validate parameters
            if (projectId <= 0)
            {
                _logger.LogWarning("Invalid project ID provided. CorrelationId: {CorrelationId}, ProjectId: {ProjectId}", correlationId, projectId);
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid project ID. Project ID must be a positive integer."));
            }

            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 10;

            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                _logger.LogWarning("Unable to retrieve user ID from token. CorrelationId: {CorrelationId}", correlationId);
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Invalid authentication token. Please login again."));
            }

            // Verify project exists and user has access
            var projectExists = await _context.Projects
                .AnyAsync(p => p.Id == projectId && p.IsActive);

            if (!projectExists)
            {
                _logger.LogWarning("Project not found or inactive. CorrelationId: {CorrelationId}, ProjectId: {ProjectId}", correlationId, projectId);
                return NotFound(ApiResponse<object>.ErrorResponse(
                    $"Project with ID {projectId} not found or is inactive."));
            }

            var query = _context.Problems
                .Include(p => p.Project)
                    .ThenInclude(pr => pr.Department)
                .Include(p => p.CreatedByUser)
                .Include(p => p.Solutions.Where(s => s.Status == Domain.Enums.SolutionStatus.Approved))
                .Include(p => p.ProblemLikes)
                .Where(p => p.ProjectId == projectId && p.IsActive);

            // Apply tag filtering if provided
            if (!string.IsNullOrWhiteSpace(tags))
            {
                var tagList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(t => t.Trim().ToLowerInvariant())
                                 .Where(t => !string.IsNullOrEmpty(t))
                                 .ToList();

                if (tagList.Any())
                {
                    query = query.Where(p => !string.IsNullOrEmpty(p.Tags) && 
                                           tagList.Any(tag => p.Tags.ToLower().Contains(tag)));
                }
            }

            // Apply sorting
            query = sortBy.ToLowerInvariant() switch
            {
                "likes" => sortOrder.ToLowerInvariant() == "asc" 
                    ? query.OrderBy(p => p.LikeCount).ThenByDescending(p => p.CreatedDate)
                    : query.OrderByDescending(p => p.LikeCount).ThenByDescending(p => p.CreatedDate),
                "title" => sortOrder.ToLowerInvariant() == "asc" 
                    ? query.OrderBy(p => p.Title).ThenByDescending(p => p.CreatedDate)
                    : query.OrderByDescending(p => p.Title).ThenByDescending(p => p.CreatedDate),
                _ => sortOrder.ToLowerInvariant() == "asc" 
                    ? query.OrderBy(p => p.CreatedDate)
                    : query.OrderByDescending(p => p.CreatedDate)
            };

            var totalCount = await query.CountAsync();
            var problems = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProblemSummaryDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Description.Length > 200 ? p.Description.Substring(0, 200) + "..." : p.Description,
                    Tags = p.Tags ?? string.Empty,
                    CreatedDate = p.CreatedDate,
                    CreatedBy = p.CreatedByUser.Username ?? "Unknown",
                    ProjectName = p.Project.Name ?? "Unknown Project",
                    DepartmentName = p.Project.Department.Name ?? "Unknown Department",
                    AttachmentPath = p.AttachmentPath,
                    LikeCount = p.LikeCount,
                    IsLikedByCurrentUser = p.ProblemLikes.Any(pl => pl.UserId == userId),
                    SolutionsCount = p.Solutions.Count(s => s.Status == Domain.Enums.SolutionStatus.Approved),
                    CanEdit = p.CreatedBy == userId || User.IsInRole("Admin") || User.IsInRole("SuperAdmin"),
                    HasAttachment = !string.IsNullOrEmpty(p.AttachmentPath)
                })
                .ToListAsync();

            var response = new PaginatedProblemsResponse
            {
                Problems = problems,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                HasNextPage = page * pageSize < totalCount,
                HasPreviousPage = page > 1,
                AppliedFilters = new Dictionary<string, object>
                {
                    ["projectId"] = projectId,
                    ["tags"] = tags ?? string.Empty,
                    ["sortBy"] = sortBy,
                    ["sortOrder"] = sortOrder
                }
            };

            _logger.LogInformation("Successfully retrieved problems by project. CorrelationId: {CorrelationId}, ProjectId: {ProjectId}, TotalCount: {TotalCount}",
                correlationId, projectId, totalCount);

            return Ok(ApiResponse<PaginatedProblemsResponse>.SuccessResponse(
                response, 
                $"Successfully retrieved {problems.Count} problems for project {projectId}."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving problems by project. CorrelationId: {CorrelationId}, ProjectId: {ProjectId}",
                correlationId, projectId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                ApiResponse<object>.ErrorResponse(
                    "An unexpected error occurred while retrieving problems. Please try again later."));
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ProblemDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProblemById(int id)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Getting problem by ID. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}",
            correlationId, id);

        try
        {
            // Validate parameters
            if (id <= 0)
            {
                _logger.LogWarning("Invalid problem ID provided. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}", correlationId, id);
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid problem ID. Problem ID must be a positive integer."));
            }

            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                _logger.LogWarning("Unable to retrieve user ID from token. CorrelationId: {CorrelationId}", correlationId);
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Invalid authentication token. Please login again."));
            }

            var problem = await _context.Problems
                .Include(p => p.Project)
                    .ThenInclude(pr => pr.Department)
                .Include(p => p.CreatedByUser)
                .Include(p => p.Solutions.Where(s => s.Status == Domain.Enums.SolutionStatus.Approved))
                    .ThenInclude(s => s.User)
                .Include(p => p.ProblemLikes)
                .Where(p => p.Id == id && p.IsActive)
                .FirstOrDefaultAsync();

            if (problem == null)
            {
                _logger.LogWarning("Problem not found or inactive. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}", correlationId, id);
                return NotFound(ApiResponse<object>.ErrorResponse(
                    $"Problem with ID {id} not found or is inactive."));
            }

            var isAdmin = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
            var canEdit = problem.CreatedBy == userId || isAdmin;
            var canDelete = problem.CreatedBy == userId || isAdmin;

                    var problemDetail = new ProblemDetailDto
                    {
                        Id = problem.Id,
                        Title = problem.Title,
                        Description = problem.Description,
                        Tags = problem.Tags ?? string.Empty,
                        CreatedDate = problem.CreatedDate,
                        CreatedBy = problem.CreatedByUser?.Username ?? "Unknown",
                        ProjectName = problem.Project?.Name ?? "Unknown Project",
                        DepartmentName = problem.Project?.Department?.Name ?? "Unknown Department",
                        AttachmentPath = problem.AttachmentPath,
                        LikeCount = problem.LikeCount,
                        IsLikedByCurrentUser = problem.ProblemLikes.Any(pl => pl.UserId == userId),
                        SolutionsCount = problem.Solutions.Count(s => s.IsActive),
                        CanEdit = canEdit,
                        CanDelete = canDelete,
                        HasAttachment = !string.IsNullOrEmpty(problem.AttachmentPath),
                        Status = problem.Status.ToString(),
                        AzureLink = problem.AzureLink,
                        AssignedToUserId = problem.AssignedToUserId,
                        Solutions = problem.Solutions.Select(s => new TechDashboardAPI.Application.DTOs.Problem.SolutionDto
                        {
                            Id = s.Id,
                            Content = s.Content ?? string.Empty,
                            AttachmentPath = s.AttachmentPath,
                            AzureDevOpsLink = s.AzureDevOpsLink,
                            Status = s.Status.ToString(),
                            CreatedDate = s.CreatedDate,
                            ApprovedDate = s.ApprovedDate,
                            CreatedBy = s.User?.Username ?? "Unknown",
                            ApprovedBy = s.ApprovedByUser?.Username,
                            ProblemTitle = problem.Title,
                            HasAttachment = !string.IsNullOrEmpty(s.AttachmentPath),
                            CanEdit = s.UserId == userId && s.Status == SolutionStatus.Pending,
                            CanDelete = s.UserId == userId && s.Status == SolutionStatus.Pending
                        }).OrderByDescending(s => s.CreatedDate).ToList()
                    };

            _logger.LogInformation("Successfully retrieved problem details. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}, SolutionsCount: {SolutionsCount}",
                correlationId, id, problemDetail.Solutions.Count);

            return Ok(ApiResponse<ProblemDetailDto>.SuccessResponse(
                problemDetail, 
                "Problem details retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving problem by ID. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}",
                correlationId, id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                ApiResponse<object>.ErrorResponse(
                    "An unexpected error occurred while retrieving the problem. Please try again later."));
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProblemCreatedDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> CreateProblem([FromForm] CreateProblemRequest request)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Creating new problem. CorrelationId: {CorrelationId}, ProjectId: {ProjectId}, Title: {Title}",
            correlationId, request.ProjectId, request.Title);

        try
        {
            // Validate model state
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                             .Select(e => e.ErrorMessage)
                                             .ToList();
                _logger.LogWarning("Model validation failed. CorrelationId: {CorrelationId}, Errors: {Errors}",
                    correlationId, string.Join(", ", errors));
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid request data.", errors));
            }

            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                _logger.LogWarning("Unable to retrieve user ID from token. CorrelationId: {CorrelationId}", correlationId);
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Invalid authentication token. Please login again."));
            }

            // Verify project exists and user has access
            var projectExists = await _context.Projects
                .AnyAsync(p => p.Id == request.ProjectId && p.IsActive);

            if (!projectExists)
            {
                _logger.LogWarning("Project not found or inactive. CorrelationId: {CorrelationId}, ProjectId: {ProjectId}",
                    correlationId, request.ProjectId);
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    $"Project with ID {request.ProjectId} not found or is inactive."));
            }

            // Validate file if provided
            if (request.Attachment != null)
            {
                if (!IsValidFile(request.Attachment))
                {
                    _logger.LogWarning("Invalid file upload. CorrelationId: {CorrelationId}, FileName: {FileName}, FileSize: {FileSize}",
                        correlationId, request.Attachment.FileName, request.Attachment.Length);
                    return BadRequest(ApiResponse<object>.ErrorResponse(
                        "Invalid file. Please ensure the file is under 10MB and has a supported extension (.pdf, .doc, .docx, .txt, .jpg, .jpeg, .png, .gif, .zip, .rar)."));
                }
            }


            // Get the departmentId from the project
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == request.ProjectId && p.IsActive);
            if (project == null)
            {
                _logger.LogWarning("Project not found or inactive. CorrelationId: {CorrelationId}, ProjectId: {ProjectId}",
                    correlationId, request.ProjectId);
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    $"Project with ID {request.ProjectId} not found or is inactive."));
            }

            var problem = new Problem
            {
                Title = request.Title.Trim(),
                Description = request.Description.Trim(),
                Tags = NormalizeTags(request.Tags),
                ProjectId = request.ProjectId,
                DepartmentId = project.DepartmentId, // Set department from project
                CreatedBy = userId,
                CreatedDate = DateTime.UtcNow,
                IsActive = true,
                LikeCount = 0,
                AzureLink = request.AzureLink,
                AssignedToUserId = request.AssignedToUserId
            };

            // Handle file upload
            if (request.Attachment != null && request.Attachment.Length > 0)
            {
                try
                {
                    var attachmentPath = await SaveFileAsync(request.Attachment);
                    problem.AttachmentPath = attachmentPath;
                    _logger.LogInformation("File uploaded successfully. CorrelationId: {CorrelationId}, FilePath: {FilePath}",
                        correlationId, attachmentPath);
                }
                catch (Exception fileEx)
                {
                    _logger.LogError(fileEx, "Error uploading file. CorrelationId: {CorrelationId}, FileName: {FileName}",
                        correlationId, request.Attachment.FileName);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        ApiResponse<object>.ErrorResponse(
                            "Failed to upload file attachment. Please try again later."));
                }
            }

            _context.Problems.Add(problem);
            await _context.SaveChangesAsync();

            var createdProblem = new ProblemCreatedDto
            {
                Id = problem.Id,
                Title = problem.Title,
                Description = problem.Description,
                Tags = problem.Tags,
                ProjectId = problem.ProjectId,
                CreatedDate = problem.CreatedDate,
                HasAttachment = !string.IsNullOrEmpty(problem.AttachmentPath),
                AttachmentPath = problem.AttachmentPath
            };

            _logger.LogInformation("Problem created successfully. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}",
                correlationId, problem.Id);

            return CreatedAtAction(
                nameof(GetProblemById),
                new { id = problem.Id },
                ApiResponse<ProblemCreatedDto>.SuccessResponse(
                    createdProblem,
                    "Problem created successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating problem. CorrelationId: {CorrelationId}, ProjectId: {ProjectId}",
                correlationId, request.ProjectId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "An unexpected error occurred while creating the problem. Please try again later."));
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ProblemUpdatedDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProblem(int id, [FromForm] UpdateProblemRequest request)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Updating problem. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}, Title: {Title}",
            correlationId, id, request.Title);

        try
        {
            // Validate parameters
            if (id <= 0)
            {
                _logger.LogWarning("Invalid problem ID provided. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}", correlationId, id);
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid problem ID. Problem ID must be a positive integer."));
            }

            // Validate model state
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                             .Select(e => e.ErrorMessage)
                                             .ToList();
                _logger.LogWarning("Model validation failed. CorrelationId: {CorrelationId}, Errors: {Errors}",
                    correlationId, string.Join(", ", errors));
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid request data.", errors));
            }

            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                _logger.LogWarning("Unable to retrieve user ID from token. CorrelationId: {CorrelationId}", correlationId);
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Invalid authentication token. Please login again."));
            }

            var isAdmin = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

            var problem = await _context.Problems
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (problem == null)
            {
                _logger.LogWarning("Problem not found or inactive. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}", correlationId, id);
                return NotFound(ApiResponse<object>.ErrorResponse(
                    $"Problem with ID {id} not found or is inactive."));
            }

            // Check if user can edit (owner or admin)
            if (problem.CreatedBy != userId && !isAdmin)
            {
                _logger.LogWarning("User not authorized to update problem. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}, UserId: {UserId}",
                    correlationId, id, userId);
                return Forbid(ApiResponse<object>.ErrorResponse(
                    "You are not authorized to update this problem. Only the creator or administrators can edit problems.").Message);
            }

            // Validate file if provided
            if (request.Attachment != null)
            {
                if (!IsValidFile(request.Attachment))
                {
                    _logger.LogWarning("Invalid file upload. CorrelationId: {CorrelationId}, FileName: {FileName}, FileSize: {FileSize}",
                        correlationId, request.Attachment.FileName, request.Attachment.Length);
                    return BadRequest(ApiResponse<object>.ErrorResponse(
                        "Invalid file. Please ensure the file is under 10MB and has a supported extension (.pdf, .doc, .docx, .txt, .jpg, .jpeg, .png, .gif, .zip, .rar)."));
                }
            }

            // Store old values for logging
            var oldTitle = problem.Title;
            var oldAttachmentPath = problem.AttachmentPath;

            // Update problem properties
            problem.Title = request.Title.Trim();
            problem.Description = request.Description.Trim();
            problem.Tags = NormalizeTags(request.Tags);

            // Handle file operations
            if (request.RemoveExistingAttachment && !string.IsNullOrEmpty(problem.AttachmentPath))
            {
                // Remove existing attachment
                DeleteFileAsync(problem.AttachmentPath);
                problem.AttachmentPath = null;
                _logger.LogInformation("Existing attachment removed. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}, OldPath: {OldPath}",
                    correlationId, id, oldAttachmentPath);
            }

            if (request.Attachment != null && request.Attachment.Length > 0)
            {
                try
                {
                    // Delete old file if exists and we're replacing it
                    if (!string.IsNullOrEmpty(problem.AttachmentPath))
                    {
                        DeleteFileAsync(problem.AttachmentPath);
                    }

                    var attachmentPath = await SaveFileAsync(request.Attachment);
                    problem.AttachmentPath = attachmentPath;
                    _logger.LogInformation("New file uploaded successfully. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}, FilePath: {FilePath}",
                        correlationId, id, attachmentPath);
                }
                catch (Exception fileEx)
                {
                    _logger.LogError(fileEx, "Error uploading file during problem update. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}, FileName: {FileName}",
                        correlationId, id, request.Attachment.FileName);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        ApiResponse<object>.ErrorResponse(
                            "Failed to upload file attachment. Please try again later."));
                }
            }

            await _context.SaveChangesAsync();

            var updatedProblem = new ProblemUpdatedDto
            {
                Id = problem.Id,
                Title = problem.Title,
                Description = problem.Description,
                Tags = problem.Tags,
                HasAttachment = !string.IsNullOrEmpty(problem.AttachmentPath),
                AttachmentPath = problem.AttachmentPath,
                LastModified = DateTime.UtcNow
            };

            _logger.LogInformation("Problem updated successfully. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}, TitleChanged: {TitleChanged}",
                correlationId, id, oldTitle != problem.Title);

            return Ok(ApiResponse<ProblemUpdatedDto>.SuccessResponse(
                updatedProblem,
                "Problem updated successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating problem. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}",
                correlationId, id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "An unexpected error occurred while updating the problem. Please try again later."));
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ProblemDeletedDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProblem(int id)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Deleting problem. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}",
            correlationId, id);

        try
        {
            // Validate parameters
            if (id <= 0)
            {
                _logger.LogWarning("Invalid problem ID provided. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}", correlationId, id);
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid problem ID. Problem ID must be a positive integer."));
            }

            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                _logger.LogWarning("Unable to retrieve user ID from token. CorrelationId: {CorrelationId}", correlationId);
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Invalid authentication token. Please login again."));
            }

            var isAdmin = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

            var problem = await _context.Problems
                .Include(p => p.Solutions)
                .Include(p => p.ProblemLikes)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (problem == null)
            {
                _logger.LogWarning("Problem not found or inactive. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}", correlationId, id);
                return NotFound(ApiResponse<object>.ErrorResponse(
                    $"Problem with ID {id} not found or is inactive."));
            }

            // Check if user can delete (owner or admin)
            if (problem.CreatedBy != userId && !isAdmin)
            {
                _logger.LogWarning("User not authorized to delete problem. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}, UserId: {UserId}",
                    correlationId, id, userId);
                return Forbid(ApiResponse<object>.ErrorResponse(
                    "You are not authorized to delete this problem. Only the creator or administrators can delete problems.").Message);
            }

            // Store information for response
            var problemTitle = problem.Title;
            var solutionsCount = problem.Solutions.Count;
            var likesCount = problem.LikeCount;

            // Soft delete the problem
            problem.IsActive = false;
            
            // Also soft delete related solutions to maintain referential integrity
            foreach (var solution in problem.Solutions.Where(s => s.IsActive))
            {
                solution.IsActive = false;
            }

            await _context.SaveChangesAsync();

            var deletedProblem = new ProblemDeletedDto
            {
                Id = id,
                Title = problemTitle,
                DeletedDate = DateTime.UtcNow,
                SolutionsAffected = solutionsCount,
                LikesCount = likesCount
            };

            _logger.LogInformation("Problem deleted successfully. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}, SolutionsAffected: {SolutionsAffected}",
                correlationId, id, solutionsCount);

            return Ok(ApiResponse<ProblemDeletedDto>.SuccessResponse(
                deletedProblem,
                "Problem deleted successfully. All associated solutions have also been deactivated."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting problem. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}",
                correlationId, id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "An unexpected error occurred while deleting the problem. Please try again later."));
        }
    }

    [HttpPost("{id:int}/like")]
    [ProducesResponseType(typeof(ApiResponse<ProblemLikeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LikeProblem(int id)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Liking problem. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}",
            correlationId, id);

        try
        {
            // Validate parameters
            if (id <= 0)
            {
                _logger.LogWarning("Invalid problem ID provided. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}", correlationId, id);
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid problem ID. Problem ID must be a positive integer."));
            }

            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                _logger.LogWarning("Unable to retrieve user ID from token. CorrelationId: {CorrelationId}", correlationId);
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Invalid authentication token. Please login again."));
            }

            var problem = await _context.Problems
                .Include(p => p.ProblemLikes)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (problem == null)
            {
                _logger.LogWarning("Problem not found or inactive. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}", correlationId, id);
                return NotFound(ApiResponse<object>.ErrorResponse(
                    $"Problem with ID {id} not found or is inactive."));
            }

            var existingLike = problem.ProblemLikes.FirstOrDefault(pl => pl.UserId == userId);

            if (existingLike != null)
            {
                _logger.LogWarning("Problem already liked by user. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}, UserId: {UserId}",
                    correlationId, id, userId);
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "You have already liked this problem."));
            }

            var like = new ProblemLike
            {
                ProblemId = id,
                UserId = userId,
                CreatedDate = DateTime.UtcNow
            };

            _context.ProblemLikes.Add(like);
            problem.LikeCount++;
            await _context.SaveChangesAsync();

            var likeResponse = new ProblemLikeDto
            {
                ProblemId = id,
                UserId = userId,
                NewLikeCount = problem.LikeCount,
                IsLiked = true,
                LikedDate = like.CreatedDate
            };

            _logger.LogInformation("Problem liked successfully. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}, UserId: {UserId}, NewLikeCount: {NewLikeCount}",
                correlationId, id, userId, problem.LikeCount);

            return Ok(ApiResponse<ProblemLikeDto>.SuccessResponse(
                likeResponse,
                "Problem liked successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error liking problem. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}",
                correlationId, id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "An unexpected error occurred while liking the problem. Please try again later."));
        }
    }

    [HttpDelete("{id:int}/like")]
    [ProducesResponseType(typeof(ApiResponse<ProblemLikeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlikeProblem(int id)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Unliking problem. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}",
            correlationId, id);

        try
        {
            // Validate parameters
            if (id <= 0)
            {
                _logger.LogWarning("Invalid problem ID provided. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}", correlationId, id);
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid problem ID. Problem ID must be a positive integer."));
            }

            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                _logger.LogWarning("Unable to retrieve user ID from token. CorrelationId: {CorrelationId}", correlationId);
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Invalid authentication token. Please login again."));
            }

            var problem = await _context.Problems
                .Include(p => p.ProblemLikes)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (problem == null)
            {
                _logger.LogWarning("Problem not found or inactive. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}", correlationId, id);
                return NotFound(ApiResponse<object>.ErrorResponse(
                    $"Problem with ID {id} not found or is inactive."));
            }

            var existingLike = problem.ProblemLikes.FirstOrDefault(pl => pl.UserId == userId);

            if (existingLike == null)
            {
                _logger.LogWarning("Problem not currently liked by user. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}, UserId: {UserId}",
                    correlationId, id, userId);
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "You have not liked this problem yet."));
            }

            _context.ProblemLikes.Remove(existingLike);
            problem.LikeCount = Math.Max(0, problem.LikeCount - 1);
            await _context.SaveChangesAsync();

            var unlikeResponse = new ProblemLikeDto
            {
                ProblemId = id,
                UserId = userId,
                NewLikeCount = problem.LikeCount,
                IsLiked = false,
                LikedDate = DateTime.UtcNow // Date of unlike action
            };

            _logger.LogInformation("Problem unliked successfully. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}, UserId: {UserId}, NewLikeCount: {NewLikeCount}",
                correlationId, id, userId, problem.LikeCount);

            return Ok(ApiResponse<ProblemLikeDto>.SuccessResponse(
                unlikeResponse,
                "Problem unliked successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unliking problem. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}",
                correlationId, id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "An unexpected error occurred while unliking the problem. Please try again later."));
        }
    }

    [HttpGet("tags")]
    [ProducesResponseType(typeof(ApiResponse<TagsResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllTags([FromQuery] int minUsage = 1)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Getting all problem tags. CorrelationId: {CorrelationId}, MinUsage: {MinUsage}",
            correlationId, minUsage);

        try
        {
            // Ensure minUsage is at least 1
            if (minUsage < 1) minUsage = 1;

            var problemTags = await _context.Problems
                .Where(p => p.IsActive && !string.IsNullOrEmpty(p.Tags))
                .Select(p => p.Tags)
                .ToListAsync();

            // Parse and count tags
            var tagCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var tagString in problemTags)
            {
                var tags = tagString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                   .Select(t => t.Trim())
                                   .Where(t => !string.IsNullOrEmpty(t));

                foreach (var tag in tags)
                {
                    var normalizedTag = tag.Trim();
                    if (tagCounts.ContainsKey(normalizedTag))
                    {
                        tagCounts[normalizedTag]++;
                    }
                    else
                    {
                        tagCounts[normalizedTag] = 1;
                    }
                }
            }

            // Filter by minimum usage and prepare response
            var filteredTags = tagCounts
                .Where(kvp => kvp.Value >= minUsage)
                .Select(kvp => new TechDashboardAPI.Application.DTOs.Problem.TagInfoDto
                {
                    Name = kvp.Key,
                    UsageCount = kvp.Value
                })
                .OrderByDescending(t => t.UsageCount)
                .ThenBy(t => t.Name)
                .ToList();

            var response = new TechDashboardAPI.Application.DTOs.Problem.TagsResponseDto
            {
                Tags = filteredTags,
                TotalTags = filteredTags.Count,
                TotalProblemsWithTags = problemTags.Count,
                MinUsageFilter = minUsage,
                MostUsedTag = filteredTags.FirstOrDefault()?.Name ?? "None",
                AvgTagsPerProblem = problemTags.Count > 0 
                    ? Math.Round(problemTags.Sum(t => t.Split(',', StringSplitOptions.RemoveEmptyEntries).Length) / (double)problemTags.Count, 2)
                    : 0
            };

            _logger.LogInformation($"Successfully retrieved problem tags. CorrelationId: {correlationId}, TotalTags: {tagCounts.Count}, FilteredTags: {filteredTags.Count}");

            return Ok(ApiResponse<TechDashboardAPI.Application.DTOs.Problem.TagsResponseDto>.SuccessResponse(
                response,
                $"Successfully retrieved {filteredTags.Count} tags."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving problem tags. CorrelationId: {CorrelationId}",
                correlationId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "An unexpected error occurred while retrieving tags. Please try again later."));
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

 private string GetCurrentUsername()
    {
        return User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
    }
    private bool IsValidFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return false;

        // Check file size (max 10MB)
        const long maxFileSize = 10 * 1024 * 1024;
        if (file.Length > maxFileSize)
            return false;

        // Check file extension
        var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt", ".jpg", ".jpeg", ".png", ".gif", ".zip", ".rar" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return allowedExtensions.Contains(fileExtension);
    }

    private string NormalizeTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
            return string.Empty;

        var tagList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                          .Select(t => t.Trim())
                          .Where(t => !string.IsNullOrEmpty(t) && t.Length <= 50)
                          .Take(10) // Limit to 10 tags
                          .Distinct(StringComparer.OrdinalIgnoreCase)
                          .ToList();

        return string.Join(", ", tagList);
    }
    private async Task<string> SaveFileAsync(IFormFile file)
    {
        var uploadsDir = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads");
        if (!Directory.Exists(uploadsDir))
        {
            Directory.CreateDirectory(uploadsDir);
        }

        var fileExtension = Path.GetExtension(file.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(uploadsDir, uniqueFileName);

        using var fileStream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(fileStream);

        return $"uploads/{uniqueFileName}";
    }


    private bool DeleteFileAsync(string filePath)
    {
        try
        {
            var fullPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, filePath);
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }    [HttpGet("tags/popular")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPopularTags([FromQuery] int limit = 10)
    {
        // Fetch all tags to memory
        var allTags = await _context.Problems
            .Where(p => !string.IsNullOrEmpty(p.Tags))
            .Select(p => p.Tags)
            .ToListAsync();

        // Split and count tags in memory
        var tagCounts = allTags
            .SelectMany(tags => tags.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Select(t => t.Trim().ToLower())
            .GroupBy(t => t)
            .Select(g => new { Name = g.Key, UsageCount = g.Count() })
            .OrderByDescending(g => g.UsageCount)
            .Take(limit)
            .ToList();

        var result = tagCounts.Select((t, idx) => new
        {
            id = idx + 1,
            name = t.Name,
            usageCount = t.UsageCount
        }).ToList();

        return Ok(new { success = true, message = "Popular tags loaded", data = result });
    }
}

#region DTOs and Response Models


public class PaginatedProblemsResponse
{
    public List<ProblemSummaryDto> Problems { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public Dictionary<string, object> AppliedFilters { get; set; } = new();
}


public class ProblemSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string? AttachmentPath { get; set; }
    public int LikeCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
    public int SolutionsCount { get; set; }
    public bool CanEdit { get; set; }
    public bool HasAttachment { get; set; }
}


public class ProblemCreatedDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool HasAttachment { get; set; }
    public string? AttachmentPath { get; set; }
}


public class ProblemUpdatedDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public bool HasAttachment { get; set; }
    public string? AttachmentPath { get; set; }
    public DateTime LastModified { get; set; }
}


public class ProblemDeletedDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime DeletedDate { get; set; }
    public int SolutionsAffected { get; set; }
    public int LikesCount { get; set; }
}


public class ProblemLikeDto
{
    public int ProblemId { get; set; }
    public int UserId { get; set; }
    public int NewLikeCount { get; set; }
    public bool IsLiked { get; set; }
    public DateTime LikedDate { get; set; }
}


public class TagInfoDto
{
    // ...existing code...


    public string? AzureDevOpsLink { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; }


    public DateTime? ApprovedDate { get; set; }


    public string CreatedBy { get; set; } = string.Empty;


    public string? ApprovedBy { get; set; }

    public string ProblemTitle { get; set; } = string.Empty;


    public bool HasAttachment { get; set; }

    public bool CanEdit { get; set; }


    public bool CanDelete { get; set; }
}






#endregion
