using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using TechDashboardAPI.Application.DTOs;
using TechDashboardAPI.Domain.Entities;
using TechDashboardAPI.Domain.Enums;
using TechDashboardAPI.Infrastructure.Data;
using TechDashboardAPI.Application.Responses;

namespace TechDashboardAPI.API.Controllers;


[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class ProjectsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(
        ApplicationDbContext context,
        ILogger<ProjectsController> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedProjectsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAllProjects(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortOrder = "asc",
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool isActive = true,
        [FromQuery] int? departmentId = null)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Getting all projects. CorrelationId: {CorrelationId}, Page: {Page}, PageSize: {PageSize}, DepartmentId: {DepartmentId}",
            correlationId, page, pageSize, departmentId);

        try
        {
            // Validate parameters
            if (page <= 0 || pageSize <= 0)
            {
                _logger.LogWarning("Invalid pagination parameters. CorrelationId: {CorrelationId}, Page: {Page}, PageSize: {PageSize}", 
                    correlationId, page, pageSize);
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid pagination parameters. Page and PageSize must be positive integers."));
            }

            if (pageSize > 100)
            {
                _logger.LogWarning("Page size too large. CorrelationId: {CorrelationId}, PageSize: {PageSize}", correlationId, pageSize);
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Page size cannot exceed 100 items."));
            }

            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                _logger.LogWarning("Unable to retrieve user ID from token. CorrelationId: {CorrelationId}", correlationId);
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Invalid authentication token. Please login again."));
            }

            // Build query
            var query = _context.Projects
                .Include(p => p.Department)
                .Include(p => p.CreatedByUser)
                .Where(p => p.IsActive == isActive);

            // Filter by department if specified
            if (departmentId.HasValue)
            {
                query = query.Where(p => p.DepartmentId == departmentId.Value);
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerSearchTerm = searchTerm.ToLower();
                query = query.Where(p => 
                    p.Name.ToLower().Contains(lowerSearchTerm) ||
                    p.Description.ToLower().Contains(lowerSearchTerm) ||
                    p.Department.Name.ToLower().Contains(lowerSearchTerm));
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = sortBy.ToLower() switch
            {
                "name" => sortOrder.ToLower() == "desc" ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
                "department" => sortOrder.ToLower() == "desc" ? query.OrderByDescending(p => p.Department.Name) : query.OrderBy(p => p.Department.Name),
                "createddate" => sortOrder.ToLower() == "desc" ? query.OrderByDescending(p => p.CreatedDate) : query.OrderBy(p => p.CreatedDate),
                _ => query.OrderBy(p => p.Name)
            };

            // Apply pagination
            var projects = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProjectSummaryDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    DepartmentId = p.DepartmentId,
                    DepartmentName = p.Department.Name,
                    CreatedBy = p.CreatedByUser.Username,
                    CreatedDate = p.CreatedDate,
                    IsActive = p.IsActive,
                    ProblemsCount = p.Problems.Count(pr => pr.IsActive),
                    CanEdit = true,
                    CanDelete = true
                })
                .ToListAsync();

            var response = new PaginatedProjectsResponse
            {
                Projects = projects,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                HasNextPage = page < (int)Math.Ceiling((double)totalCount / pageSize),
                HasPreviousPage = page > 1
            };

            _logger.LogInformation("Successfully retrieved all projects. CorrelationId: {CorrelationId}, TotalCount: {TotalCount}",
                correlationId, totalCount);

            return Ok(ApiResponse<PaginatedProjectsResponse>.SuccessResponse(
                response, 
                $"Successfully retrieved {projects.Count} projects."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all projects. CorrelationId: {CorrelationId}",
                correlationId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                ApiResponse<object>.ErrorResponse(
                    "An unexpected error occurred while retrieving projects. Please try again later."));
        }
    }

      [HttpGet("department/{departmentId}")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedProjectsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProjectsByDepartment(
        int departmentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortOrder = "asc",
        [FromQuery] string? searchTerm = null)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Getting projects by department. CorrelationId: {CorrelationId}, DepartmentId: {DepartmentId}, Page: {Page}, PageSize: {PageSize}",
            correlationId, departmentId, page, pageSize);

        try
        {
            // Validate parameters
            if (departmentId <= 0)
            {
                _logger.LogWarning("Invalid department ID provided. CorrelationId: {CorrelationId}, DepartmentId: {DepartmentId}", correlationId, departmentId);
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid department ID. Department ID must be a positive integer."));
            }

            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 50) pageSize = 10;

            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                _logger.LogWarning("Unable to retrieve user ID from token. CorrelationId: {CorrelationId}", correlationId);
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Invalid authentication token. Please login again."));
            }

            // Verify department exists
            var departmentExists = await _context.Departments
                .AnyAsync(d => d.Id == departmentId);

            if (!departmentExists)
            {
                _logger.LogWarning("Department not found. CorrelationId: {CorrelationId}, DepartmentId: {DepartmentId}", correlationId, departmentId);
                return NotFound(ApiResponse<object>.ErrorResponse(
                    $"Department with ID {departmentId} not found."));
            }

            var query = _context.Projects
                .Include(p => p.Department)
                .Include(p => p.CreatedByUser)
                .Include(p => p.Problems)
                .Where(p => p.DepartmentId == departmentId && p.IsActive);

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var search = searchTerm.Trim().ToLower();
                query = query.Where(p => 
                    p.Name.ToLower().Contains(search) ||
                    p.Description.ToLower().Contains(search));
            }

            // Apply sorting
            query = sortBy.ToLowerInvariant() switch
            {
                "created" => sortOrder.ToLowerInvariant() == "desc" 
                    ? query.OrderByDescending(p => p.CreatedDate).ThenBy(p => p.Name)
                    : query.OrderBy(p => p.CreatedDate).ThenBy(p => p.Name),
                "problems" => sortOrder.ToLowerInvariant() == "desc" 
                    ? query.OrderByDescending(p => p.Problems.Count(pr => pr.IsActive)).ThenBy(p => p.Name)
                    : query.OrderBy(p => p.Problems.Count(pr => pr.IsActive)).ThenBy(p => p.Name),
                _ => sortOrder.ToLowerInvariant() == "desc" 
                    ? query.OrderByDescending(p => p.Name)
                    : query.OrderBy(p => p.Name)
            };

            var totalCount = await query.CountAsync();
            var projects = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProjectSummaryDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    DepartmentId = p.DepartmentId,
                    DepartmentName = p.Department.Name,
                    CreatedDate = p.CreatedDate,
                    CreatedBy = p.CreatedByUser.Username,
                    ProblemsCount = p.Problems.Count(pr => pr.IsActive),
                    IsActive = p.IsActive,
                    CanEdit = p.CreatedBy == userId || User.IsInRole("Admin"),
                    CanDelete = p.CreatedBy == userId || User.IsInRole("Admin")
                })
                .ToListAsync();

            var response = new PaginatedProjectsResponse
            {
                Projects = projects,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                HasNextPage = page * pageSize < totalCount,
                HasPreviousPage = page > 1,
                DepartmentId = departmentId,
                AppliedFilters = new Dictionary<string, object>
                {
                    ["departmentId"] = departmentId,
                    ["searchTerm"] = searchTerm ?? string.Empty,
                    ["sortBy"] = sortBy,
                    ["sortOrder"] = sortOrder
                }
            };

            _logger.LogInformation("Successfully retrieved projects by department. CorrelationId: {CorrelationId}, DepartmentId: {DepartmentId}, TotalCount: {TotalCount}",
                correlationId, departmentId, totalCount);

            return Ok(ApiResponse<PaginatedProjectsResponse>.SuccessResponse(
                response, 
                $"Successfully retrieved {projects.Count} projects for department {departmentId}."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving projects by department. CorrelationId: {CorrelationId}, DepartmentId: {DepartmentId}",
                correlationId, departmentId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                ApiResponse<object>.ErrorResponse(
                    "An unexpected error occurred while retrieving department projects. Please try again later."));
        }
    }

 [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ProjectDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProjectById(int id)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Getting project by ID. CorrelationId: {CorrelationId}, ProjectId: {ProjectId}",
            correlationId, id);

        try
        {
            // Validate parameters
            if (id <= 0)
            {
                _logger.LogWarning("Invalid project ID provided. CorrelationId: {CorrelationId}, ProjectId: {ProjectId}", correlationId, id);
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid project ID. Project ID must be a positive integer."));
            }

            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                _logger.LogWarning("Unable to retrieve user ID from token. CorrelationId: {CorrelationId}", correlationId);
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Invalid authentication token. Please login again."));
            }

            var project = await _context.Projects
                .Include(p => p.Department)
                .Include(p => p.CreatedByUser)
                .Include(p => p.Problems.Where(pr => pr.IsActive))
                    .ThenInclude(pr => pr.CreatedByUser)
                .Include(p => p.Problems.Where(pr => pr.IsActive))
                    .ThenInclude(pr => pr.Solutions)
                .Where(p => p.Id == id && p.IsActive)
                .FirstOrDefaultAsync();

            if (project == null)
            {
                _logger.LogWarning("Project not found or inactive. CorrelationId: {CorrelationId}, ProjectId: {ProjectId}", correlationId, id);
                return NotFound(ApiResponse<object>.ErrorResponse(
                    $"Project with ID {id} not found or is inactive."));
            }

            var projectDetail = new ProjectDetailDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                DepartmentId = project.DepartmentId,
                DepartmentName = project.Department.Name,
                CreatedDate = project.CreatedDate,
                CreatedBy = project.CreatedByUser.Username,
                IsActive = project.IsActive,
                CanEdit = project.CreatedBy == userId || User.IsInRole("Admin"),
                CanDelete = project.CreatedBy == userId || User.IsInRole("Admin"),
                Problems = project.Problems.Select(pr => new ProjectProblemSummaryDto
                {
                    Id = pr.Id,
                    Title = pr.Title,
                    Description = pr.Description,
                    Tags = pr.Tags ?? string.Empty,
                    Status = pr.Status.ToString(),
                    CreatedDate = pr.CreatedDate,
                    CreatedBy = pr.CreatedByUser.Username,
                    SolutionsCount = pr.Solutions.Count(s => s.IsActive),
                    LikesCount = pr.LikeCount,
                    HasAttachment = !string.IsNullOrEmpty(pr.AttachmentPath)
                }).OrderByDescending(pr => pr.CreatedDate).ToList()
            };

            _logger.LogInformation("Successfully retrieved project details. CorrelationId: {CorrelationId}, ProjectId: {ProjectId}, ProblemsCount: {ProblemsCount}",
                correlationId, id, projectDetail.Problems.Count);

            return Ok(ApiResponse<ProjectDetailDto>.SuccessResponse(
                projectDetail, 
                $"Successfully retrieved project '{projectDetail.Name}' with {projectDetail.Problems.Count} problems."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving project by ID. CorrelationId: {CorrelationId}, ProjectId: {ProjectId}",
                correlationId, id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                ApiResponse<object>.ErrorResponse(
                    "An unexpected error occurred while retrieving the project. Please try again later."));
        }
    }

  [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<ProjectCreatedDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Creating new project. CorrelationId: {CorrelationId}, DepartmentId: {DepartmentId}",
            correlationId, request.DepartmentId);

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
            var username = GetCurrentUsername();
            
            if (userId <= 0)
            {
                _logger.LogWarning("Unable to retrieve user ID from token. CorrelationId: {CorrelationId}", correlationId);
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Invalid authentication token. Please login again."));
            }

            // Verify department exists
            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.Id == request.DepartmentId);

            if (department == null)
            {
                _logger.LogWarning("Department not found. CorrelationId: {CorrelationId}, DepartmentId: {DepartmentId}",
                    correlationId, request.DepartmentId);
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    $"Department with ID {request.DepartmentId} not found."));
            }

            // Check for duplicate project name within department
            var duplicateExists = await _context.Projects
                .AnyAsync(p => p.Name.ToLower() == request.Name.ToLower() && 
                               p.DepartmentId == request.DepartmentId && 
                               p.IsActive);

            if (duplicateExists)
            {
                _logger.LogWarning("Duplicate project name in department. CorrelationId: {CorrelationId}, ProjectName: {ProjectName}, DepartmentId: {DepartmentId}",
                    correlationId, request.Name, request.DepartmentId);
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    $"A project with the name '{request.Name}' already exists in the {department.Name} department."));
            }

            var project = new Project
            {
                Name = request.Name.Trim(),
                Description = request.Description.Trim(),
                DepartmentId = request.DepartmentId,
                CreatedBy = userId,
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Create response DTO
            var createdProject = new ProjectCreatedDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                DepartmentId = project.DepartmentId,
                DepartmentName = department.Name,
                CreatedDate = project.CreatedDate,
                CreatedBy = username,
                IsActive = project.IsActive
            };

            _logger.LogInformation("Project created successfully. CorrelationId: {CorrelationId}, ProjectId: {ProjectId}, DepartmentId: {DepartmentId}",
                correlationId, project.Id, project.DepartmentId);

            return CreatedAtAction(
                nameof(GetProjectById),
                new { id = project.Id },
                ApiResponse<ProjectCreatedDto>.SuccessResponse(
                    createdProject,
                    $"Project '{createdProject.Name}' created successfully in {createdProject.DepartmentName} department."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating project. CorrelationId: {CorrelationId}, DepartmentId: {DepartmentId}",
                correlationId, request.DepartmentId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "An unexpected error occurred while creating the project. Please try again later."));
        }
    }

  [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<ProjectUpdatedDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateProject(int id, [FromBody] UpdateProjectRequest request)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Starting project update process. CorrelationId: {CorrelationId}, ProjectId: {ProjectId}, UserId: {UserId}",
            correlationId, id, GetCurrentUserId());

        try
        {
            // Input validation
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                             .Select(e => e.ErrorMessage)
                                             .ToList();
                _logger.LogWarning("Project update validation failed. CorrelationId: {CorrelationId}, Errors: {Errors}",
                    correlationId, string.Join(", ", errors));
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Validation failed: " + string.Join(", ", errors)));
            }

            if (id <= 0)
            {
                _logger.LogWarning("Invalid project ID provided. CorrelationId: {CorrelationId}, ProjectId: {ProjectId}", correlationId, id);
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid project ID. Project ID must be a positive integer."));
            }

            var userId = GetCurrentUserId();
            var username = GetCurrentUsername();

            if (userId <= 0)
            {
                _logger.LogWarning("Unable to retrieve user ID from token. CorrelationId: {CorrelationId}", correlationId);
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Invalid authentication token. Please login again."));
            }

            // Fetch project with department information
            var project = await _context.Projects
                .Include(p => p.Department)
                .Include(p => p.CreatedByUser)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (project == null)
            {
                _logger.LogWarning("Project not found for update. CorrelationId: {CorrelationId}, ProjectId: {ProjectId}",
                    correlationId, id);
                return NotFound(ApiResponse<object>.ErrorResponse(
                    $"Project with ID {id} not found or is inactive."));
            }

            // Authorization check - only project creator or admin can update
            if (project.CreatedBy != userId && !User.IsInRole("SuperAdmin"))
            {
                _logger.LogWarning("Unauthorized project update attempt. CorrelationId: {CorrelationId}, ProjectId: {ProjectId}, UserId: {UserId}, CreatorId: {CreatorId}",
                    correlationId, id, userId, project.CreatedBy);
                return Forbid(ApiResponse<object>.ErrorResponse(
                    "You can only update projects you created, unless you are a super administrator.").Message);
            }

            // Check for duplicate name within department (excluding current project)
            var duplicateExists = await _context.Projects
                .AnyAsync(p => p.Name.ToLower() == request.Name.ToLower() && 
                               p.DepartmentId == project.DepartmentId && 
                               p.Id != id && 
                               p.IsActive);

            if (duplicateExists)
            {
                _logger.LogWarning("Duplicate project name in department during update. CorrelationId: {CorrelationId}, ProjectName: {ProjectName}, DepartmentId: {DepartmentId}",
                    correlationId, request.Name, project.DepartmentId);
                return Conflict(ApiResponse<object>.ErrorResponse(
                    $"A project with the name '{request.Name}' already exists in the {project.Department.Name} department."));
            }

            // Track changes
            var originalName = project.Name;
            var originalDescription = project.Description;

            // Update project
            project.Name = request.Name.Trim();
            project.Description = request.Description.Trim();

            await _context.SaveChangesAsync();

            // Create response DTO
            var updatedProject = new ProjectUpdatedDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                DepartmentId = project.DepartmentId,
                DepartmentName = project.Department.Name,
                UpdatedDate = DateTime.UtcNow,
                UpdatedBy = username,
                NameChanged = originalName != project.Name,
                DescriptionChanged = originalDescription != project.Description
            };

            _logger.LogInformation("Project updated successfully. CorrelationId: {CorrelationId}, ProjectId: {ProjectId}, NameChanged: {NameChanged}, DescriptionChanged: {DescriptionChanged}",
                correlationId, id, updatedProject.NameChanged, updatedProject.DescriptionChanged);

            return Ok(ApiResponse<ProjectUpdatedDto>.SuccessResponse(
                updatedProject,
                $"Project '{updatedProject.Name}' updated successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during project update. CorrelationId: {CorrelationId}, ProjectId: {ProjectId}",
                correlationId, id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "An unexpected error occurred while updating the project. Please try again later."));
        }
    }

 [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<ProjectDeletedDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteProject(int id)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Starting project deletion process. CorrelationId: {CorrelationId}, ProjectId: {ProjectId}, UserId: {UserId}",
            correlationId, id, GetCurrentUserId());

        try
        {
            if (id <= 0)
            {
                _logger.LogWarning("Invalid project ID for deletion. CorrelationId: {CorrelationId}, ProjectId: {ProjectId}",
                    correlationId, id);
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid project ID. Project ID must be a positive integer."));
            }

            var userId = GetCurrentUserId();
            var username = GetCurrentUsername();

            if (userId <= 0)
            {
                _logger.LogWarning("Unable to retrieve user ID from token for deletion. CorrelationId: {CorrelationId}",
                    correlationId);
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Invalid authentication token. Please login again."));
            }

            // Fetch project with related data
            var project = await _context.Projects
                .Include(p => p.Department)
                .Include(p => p.CreatedByUser)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (project == null)
            {
                _logger.LogWarning("Project not found for deletion. CorrelationId: {CorrelationId}, ProjectId: {ProjectId}",
                    correlationId, id);
                return NotFound(ApiResponse<object>.ErrorResponse(
                    $"Project with ID {id} not found or is already inactive."));
            }

            // Authorization check - only project creator or admin can delete
            if (project.CreatedBy != userId && !User.IsInRole("SuperAdmin"))
            {
                _logger.LogWarning("Unauthorized project deletion attempt. CorrelationId: {CorrelationId}, ProjectId: {ProjectId}, UserId: {UserId}, CreatorId: {CreatorId}",
                    correlationId, id, userId, project.CreatedBy);
                return Forbid(ApiResponse<object>.ErrorResponse(
                    "You can only delete projects you created, unless you are a super administrator.").Message);
            }

            // Check for dependencies (Problems)
            var hasActiveProblems = await _context.Problems
                .AnyAsync(p => p.ProjectId == id && p.IsActive);

            if (hasActiveProblems)
            {
                _logger.LogWarning("Project deletion blocked due to dependencies. CorrelationId: {CorrelationId}, ProjectId: {ProjectId}, HasProblems: {HasProblems}",
                    correlationId, id, hasActiveProblems);

                return Conflict(ApiResponse<object>.ErrorResponse(
                    $"Cannot delete project '{project.Name}' because it has active problems. Please remove or deactivate them first."));
            }

            // Perform soft delete
            var projectName = project.Name;
            var departmentName = project.Department.Name;

            project.IsActive = false;
            project.LastUpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Create response DTO
            var deletedProject = new ProjectDeletedDto
            {
                Id = project.Id,
                Name = projectName,
                DepartmentName = departmentName,
                DeletedBy = username
            };

            _logger.LogInformation("Project deleted successfully. CorrelationId: {CorrelationId}, ProjectId: {ProjectId}, ProjectName: {ProjectName}",
                correlationId, id, projectName);

            return Ok(ApiResponse<ProjectDeletedDto>.SuccessResponse(
                deletedProject,
                $"Project '{projectName}' has been successfully deleted from the {departmentName} department."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during project deletion. CorrelationId: {CorrelationId}, ProjectId: {ProjectId}",
                correlationId, id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "An unexpected error occurred while deleting the project. Please try again later."));
        }
    }

    // Endpoint to get all departments (for dropdown)
    [HttpGet("department-dropdown")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDepartments()
    {
        try
        {
            var departments = await _context.Departments
                .Select(d => new { d.Id, d.Name })
                .ToListAsync();
            return Ok(new { success = true, data = departments });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = $"Failed to get departments: {ex.Message}" });
        }
    }

    // Endpoint to get projects by department (for registration dropdown)
    [HttpGet("/api/projects/by-department/{departmentId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProjectsForDropdown(int departmentId)
    {
        try
        {
            var projects = await _context.Projects
                .Where(p => p.DepartmentId == departmentId && p.IsActive)
                .Select(p => new { p.Id, p.Name })
                .ToListAsync();
            return Ok(new { success = true, data = projects });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = $"Failed to get projects: {ex.Message}" });
        }
    }

    // Endpoint to get problems by project
    [HttpGet("/api/problems/project/{projectId}")]
    [Authorize]
    public async Task<IActionResult> GetProblemsByProject(int projectId)
    {
        try
        {
            var problems = await _context.Problems
                .Where(pr => pr.ProjectId == projectId && pr.IsActive)
                .Select(pr => new {
                    pr.Id,
                    pr.Title,
                    pr.Description,
                    pr.Tags,
                    pr.CreatedDate,
                    pr.LikeCount
                })
                .ToListAsync();
            return Ok(new { success = true, data = problems });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = $"Failed to get problems: {ex.Message}" });
        }
    }

    // Endpoint to add a new problem (contributor/admin only, must belong to department/project)
    [HttpPost("/api/problems")]
    [Authorize(Roles = "Contributor,Admin")]
    public async Task<IActionResult> AddProblem([FromBody] CreateProblemRequest request)
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
                return Forbid();

            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == request.ProjectId && p.IsActive);
            if (project == null)
                return BadRequest(new { success = false, message = "Project not found" });

            // Only allow if user is in the department/project or is admin
            if (user.Role != UserRole.Admin && (user.DepartmentId != project.DepartmentId || (user.ProjectId.HasValue && user.ProjectId != project.Id)))
            {
                return Forbid("You do not have access to this project");
            }

            var problem = new Problem
            {
                Title = request.Title,
                Description = request.Description,
                Tags = request.Tags ?? string.Empty,
                ProjectId = request.ProjectId,
                CreatedBy = userId,
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            };
            _context.Problems.Add(problem);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Problem added successfully", data = new { problem.Id } });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = $"Failed to add problem: {ex.Message}" });
        }
    }

    // Endpoint to suggest a solution (contributor/admin only, must belong to department/project)
    [HttpPost("/api/solutions")]
    [Authorize(Roles = "Contributor,Admin")]
    public async Task<IActionResult> SuggestSolution([FromBody] SuggestSolutionRequest request)
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
                return Forbid();

            var problem = await _context.Problems.Include(pr => pr.Project).FirstOrDefaultAsync(pr => pr.Id == request.ProblemId && pr.IsActive);
            if (problem == null)
                return BadRequest(new { success = false, message = "Problem not found" });

            // Only allow if user is in the department/project or is admin
            if (user.Role != UserRole.Admin && (user.DepartmentId != problem.Project.DepartmentId || (user.ProjectId.HasValue && user.ProjectId != problem.ProjectId)))
            {
                return Forbid("You do not have access to this problem");
            }

            var solution = new Solution
            {
                ProblemId = request.ProblemId,
                UserId = userId,
                Content = request.Content,
                CreatedDate = DateTime.UtcNow,
                Status = SolutionStatus.Pending
            };
            _context.Solutions.Add(solution);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Solution suggested successfully. Awaiting review." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = $"Failed to suggest solution: {ex.Message}" });
        }
    }

    #region Private Helper Methods
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    private string GetCurrentUsername()
    {
        return User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
    }

 private List<string> ValidateProjectData(string name, string description, int departmentId)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(name))
            errors.Add("Project name is required.");
        else if (name.Trim().Length < 3)
            errors.Add("Project name must be at least 3 characters long.");
        else if (name.Trim().Length > 100)
            errors.Add("Project name cannot exceed 100 characters.");

        if (string.IsNullOrWhiteSpace(description))
            errors.Add("Project description is required.");
        else if (description.Trim().Length < 10)
            errors.Add("Project description must be at least 10 characters long.");
        else if (description.Trim().Length > 1000)
            errors.Add("Project description cannot exceed 1000 characters.");

        if (departmentId <= 0)
            errors.Add("Valid department ID is required.");

        return errors;
    }

    #endregion
}

