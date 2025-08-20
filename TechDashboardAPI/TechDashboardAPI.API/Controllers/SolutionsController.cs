using TechDashboardAPI.Application.DTOs.Problem;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
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
public class SolutionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<SolutionsController> _logger;

 public SolutionsController(
        ApplicationDbContext context, 
        IWebHostEnvironment environment,
        ILogger<SolutionsController> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

 [HttpGet("problem/{problemId:int}")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedSolutionsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSolutionsByProblem(
        int problemId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "created",
        [FromQuery] string sortOrder = "desc")
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Getting solutions by problem. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}, Page: {Page}, PageSize: {PageSize}",
            correlationId, problemId, page, pageSize);

        try
        {
            // Validate parameters
            if (problemId <= 0)
            {
                _logger.LogWarning("Invalid problem ID provided. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}", correlationId, problemId);
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid problem ID. Problem ID must be a positive integer."));
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

            // Verify problem exists and is active
            var problemExists = await _context.Problems
                .AnyAsync(p => p.Id == problemId && p.IsActive);

            if (!problemExists)
            {
                _logger.LogWarning("Problem not found or inactive. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}", correlationId, problemId);
                return NotFound(ApiResponse<object>.ErrorResponse(
                    $"Problem with ID {problemId} not found or is inactive."));
            }

            var query = _context.Solutions
                .Include(s => s.User)
                .Include(s => s.Problem)
                .Include(s => s.ApprovedByUser)
                .Where(s => s.ProblemId == problemId && s.IsActive);

            // Apply sorting
            query = sortBy.ToLowerInvariant() switch
            {
                "approved" => sortOrder.ToLowerInvariant() == "asc" 
                    ? query.OrderBy(s => s.ApprovedDate).ThenByDescending(s => s.CreatedDate)
                    : query.OrderByDescending(s => s.ApprovedDate).ThenByDescending(s => s.CreatedDate),
                "author" => sortOrder.ToLowerInvariant() == "asc" 
                    ? query.OrderBy(s => s.User.Username).ThenByDescending(s => s.CreatedDate)
                    : query.OrderByDescending(s => s.User.Username).ThenByDescending(s => s.CreatedDate),
                _ => sortOrder.ToLowerInvariant() == "asc" 
                    ? query.OrderBy(s => s.CreatedDate)
                    : query.OrderByDescending(s => s.CreatedDate)
            };

            var totalCount = await query.CountAsync();
            var solutions = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new SolutionDto
                {
                    Id = s.Id,
                    Content = s.Content,
                    AttachmentPath = s.AttachmentPath,
                    AzureDevOpsLink = s.AzureDevOpsLink,
                    Status = s.Status.ToString(),
                    CreatedDate = s.CreatedDate,
                    ApprovedDate = s.ApprovedDate,
                    CreatedBy = s.User.Username,
                    ApprovedBy = s.ApprovedByUser != null ? s.ApprovedByUser.Username : null,
                    ProblemTitle = s.Problem.Title,
                    HasAttachment = !string.IsNullOrEmpty(s.AttachmentPath),
                    CanEdit = s.UserId == userId && s.Status == SolutionStatus.Pending,
                    CanDelete = s.UserId == userId && s.Status == SolutionStatus.Pending
                })
                .ToListAsync();

            var response = new PaginatedSolutionsResponse
            {
                Solutions = solutions,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                HasNextPage = page * pageSize < totalCount,
                HasPreviousPage = page > 1,
                ProblemId = problemId,
                AppliedFilters = new Dictionary<string, object>
                {
                    ["problemId"] = problemId,
                    ["status"] = "Approved",
                    ["sortBy"] = sortBy,
                    ["sortOrder"] = sortOrder
                }
            };

            _logger.LogInformation("Successfully retrieved solutions by problem. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}, TotalCount: {TotalCount}",
                correlationId, problemId, totalCount);

            return Ok(ApiResponse<PaginatedSolutionsResponse>.SuccessResponse(
                response, 
                $"Successfully retrieved {solutions.Count} approved solutions for problem {problemId}."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving solutions by problem. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}",
                correlationId, problemId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                ApiResponse<object>.ErrorResponse(
                    "An unexpected error occurred while retrieving solutions. Please try again later."));
        }
    }
    [HttpGet("pending")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedPendingSolutionsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPendingSolutions(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "created",
        [FromQuery] string sortOrder = "desc")
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Getting pending solutions. CorrelationId: {CorrelationId}, Page: {Page}, PageSize: {PageSize}",
            correlationId, page, pageSize);

        try
        {
            // Validate parameters
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 50) pageSize = 10;

            var currentUserId = GetCurrentUserId();
            if (currentUserId <= 0)
            {
                _logger.LogWarning("Unable to retrieve current user ID from token. CorrelationId: {CorrelationId}", correlationId);
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Invalid authentication token. Please login again."));
            }

            var query = _context.Solutions
                .Include(s => s.User)
                .Include(s => s.Problem)
                    .ThenInclude(p => p.Project)
                        .ThenInclude(pr => pr.Department)
                .Where(s => s.Status == SolutionStatus.Pending && s.IsActive);

            // Apply sorting
            query = sortBy.ToLowerInvariant() switch
            {
                "problem" => sortOrder.ToLowerInvariant() == "asc" 
                    ? query.OrderBy(s => s.Problem.Title).ThenByDescending(s => s.CreatedDate)
                    : query.OrderByDescending(s => s.Problem.Title).ThenByDescending(s => s.CreatedDate),
                "author" => sortOrder.ToLowerInvariant() == "asc" 
                    ? query.OrderBy(s => s.User.Username).ThenByDescending(s => s.CreatedDate)
                    : query.OrderByDescending(s => s.User.Username).ThenByDescending(s => s.CreatedDate),
                _ => sortOrder.ToLowerInvariant() == "asc" 
                    ? query.OrderBy(s => s.CreatedDate)
                    : query.OrderByDescending(s => s.CreatedDate)
            };

            var totalCount = await query.CountAsync();
            var pendingSolutions = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new PendingSolutionDto
                {
                    Id = s.Id,
                    ProblemId = s.ProblemId,
                    ProblemTitle = s.Problem.Title,
                    ProjectName = s.Problem.Project.Name,
                    DepartmentName = s.Problem.Project.Department.Name,
                    Content = s.Content,
                    AttachmentPath = s.AttachmentPath,
                    AzureDevOpsLink = s.AzureDevOpsLink,
                    CreatedDate = s.CreatedDate,
                    CreatedBy = s.User.Username,
                    HasAttachment = !string.IsNullOrEmpty(s.AttachmentPath),
                    DaysWaiting = (int)(DateTime.UtcNow - s.CreatedDate).TotalDays,
                    CanApprove = true,
                    CanReject = true
                })
                .ToListAsync();

            var response = new PaginatedPendingSolutionsResponse
            {
                PendingSolutions = pendingSolutions,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                HasNextPage = page * pageSize < totalCount,
                HasPreviousPage = page > 1,
                AppliedFilters = new Dictionary<string, object>
                {
                    ["status"] = "Pending",
                    ["sortBy"] = sortBy,
                    ["sortOrder"] = sortOrder
                }
            };

            _logger.LogInformation("Successfully retrieved pending solutions. CorrelationId: {CorrelationId}, TotalCount: {TotalCount}",
                correlationId, totalCount);

            return Ok(ApiResponse<PaginatedPendingSolutionsResponse>.SuccessResponse(
                response, 
                $"Successfully retrieved {pendingSolutions.Count} pending solutions awaiting approval."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending solutions. CorrelationId: {CorrelationId}",
                correlationId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                ApiResponse<object>.ErrorResponse(
                    "An unexpected error occurred while retrieving pending solutions. Please try again later."));
        }
    }
[HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SolutionCreatedDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> CreateSolution([FromForm] CreateSolutionRequest request)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Creating new solution. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}",
            correlationId, request.ProblemId);

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

            // Verify problem exists and is active
            var problem = await _context.Problems
                .Include(p => p.Project)
                    .ThenInclude(pr => pr.Department)
                .FirstOrDefaultAsync(p => p.Id == request.ProblemId && p.IsActive);

            if (problem == null)
            {
                _logger.LogWarning("Problem not found or inactive. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}",
                    correlationId, request.ProblemId);
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    $"Problem with ID {request.ProblemId} not found or is inactive."));
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

            var solution = new Solution
            {
                ProblemId = request.ProblemId,
                UserId = userId,
                Content = request.Content.Trim(),
                AzureDevOpsLink = !string.IsNullOrWhiteSpace(request.AzureDevOpsLink) ? request.AzureDevOpsLink.Trim() : null,
                Status = SolutionStatus.Pending,
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            };

            // Handle file upload
            if (request.Attachment != null && request.Attachment.Length > 0)
            {
                try
                {
                    var attachmentPath = await SaveFileAsync(request.Attachment);
                    solution.AttachmentPath = attachmentPath;
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

            _context.Solutions.Add(solution);
            await _context.SaveChangesAsync();

            var createdSolution = new SolutionCreatedDto
            {
                Id = solution.Id,
                ProblemId = solution.ProblemId,
                ProblemTitle = problem.Title,
                Content = solution.Content,
                AzureDevOpsLink = solution.AzureDevOpsLink,
                Status = solution.Status.ToString(),
                CreatedDate = solution.CreatedDate,
                HasAttachment = !string.IsNullOrEmpty(solution.AttachmentPath),
                AttachmentPath = solution.AttachmentPath
            };

            _logger.LogInformation("Solution created successfully. CorrelationId: {CorrelationId}, SolutionId: {SolutionId}, Status: {Status}",
                correlationId, solution.Id, solution.Status);

            return CreatedAtAction(
                nameof(GetSolutionsByProblem),
                new { problemId = solution.ProblemId },
                ApiResponse<SolutionCreatedDto>.SuccessResponse(
                    createdSolution,
                    "Solution submitted successfully and is pending administrative approval."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating solution. CorrelationId: {CorrelationId}, ProblemId: {ProblemId}",
                correlationId, request.ProblemId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "An unexpected error occurred while creating the solution. Please try again later."));
        }
    }

 [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<SolutionUpdatedDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    public async Task<IActionResult> UpdateSolution(int id, [FromForm] UpdateSolutionRequest request)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Starting solution update process. CorrelationId: {CorrelationId}, SolutionId: {SolutionId}, UserId: {UserId}",
            correlationId, id, GetCurrentUserId());

        try
        {
            // Input validation
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                _logger.LogWarning("Solution update validation failed. CorrelationId: {CorrelationId}, Errors: {Errors}",
                    correlationId, string.Join(", ", errors));

                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Validation failed: " + string.Join(", ", errors)));
            }

            var userId = GetCurrentUserId();
            var username = GetCurrentUsername();
            var isAdmin = User.IsInRole("Admin");

            // Fetch solution with related data
            var solution = await _context.Solutions
                .Include(s => s.Problem)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (solution == null)
            {
                _logger.LogWarning("Solution not found for update. CorrelationId: {CorrelationId}, SolutionId: {SolutionId}",
                    correlationId, id);

                return NotFound(ApiResponse<object>.ErrorResponse(
                    "Solution not found"));
            }

            // Authorization check - only owner or admin can update
            if (solution.UserId != userId && !isAdmin)
            {
                _logger.LogWarning("Unauthorized solution update attempt. CorrelationId: {CorrelationId}, SolutionId: {SolutionId}, UserId: {UserId}, OwnerId: {OwnerId}",
                    correlationId, id, userId, solution.UserId);

                return Forbid(ApiResponse<object>.ErrorResponse(
                    "You can only update your own solutions").Message);
            }

            // Status validation - only pending solutions can be updated
            if (solution.Status != SolutionStatus.Pending)
            {
                _logger.LogWarning("Attempt to update non-pending solution. CorrelationId: {CorrelationId}, SolutionId: {SolutionId}, Status: {Status}",
                    correlationId, id, solution.Status);

                return Conflict(ApiResponse<object>.ErrorResponse(
                    $"Cannot update solution with status '{solution.Status}'. Only pending solutions can be modified."));
            }

            var oldAttachmentPath = solution.AttachmentPath;

            // Update solution content
            solution.Content = request.Content.Trim();
            solution.AzureDevOpsLink = string.IsNullOrWhiteSpace(request.AzureDevOpsLink) 
                ? null 
                : request.AzureDevOpsLink.Trim();

            // Handle file attachment
            if (request.Attachment != null && request.Attachment.Length > 0)
            {
                // Validate file
                if (!IsValidFile(request.Attachment))
                {
                    _logger.LogWarning("Invalid file attachment in solution update. CorrelationId: {CorrelationId}, FileName: {FileName}, Size: {Size}",
                        correlationId, request.Attachment.FileName, request.Attachment.Length);

                    return BadRequest(ApiResponse<object>.ErrorResponse(
                        "Invalid file. Allowed types: PDF, Word, Excel, PowerPoint, Images. Maximum size: 10MB"));
                }

                try
                {
                    // Save new file
                    var newAttachmentPath = await SaveFileAsync(request.Attachment);
                    solution.AttachmentPath = newAttachmentPath;

                    _logger.LogInformation("File attachment updated. CorrelationId: {CorrelationId}, SolutionId: {SolutionId}, NewPath: {NewPath}",
                        correlationId, id, newAttachmentPath);

                    // Delete old file after successful upload
                    if (!string.IsNullOrEmpty(oldAttachmentPath))
                    {
                        DeleteFileAsync(oldAttachmentPath);
                    }
                }
                catch (Exception fileEx)
                {
                    _logger.LogError(fileEx, "File upload failed during solution update. CorrelationId: {CorrelationId}, SolutionId: {SolutionId}",
                        correlationId, id);

                    return BadRequest(ApiResponse<object>.ErrorResponse(
                        "File upload failed. Please try again with a different file."));
                }
            }

            // Save changes
            await _context.SaveChangesAsync();

            // Create response
            var updatedSolution = new SolutionUpdatedDto
            {
                Id = solution.Id,
                ProblemId = solution.ProblemId,
                ProblemTitle = solution.Problem.Title,
                Content = solution.Content,
                AzureDevOpsLink = solution.AzureDevOpsLink,
                Status = solution.Status.ToString(),
                HasAttachment = !string.IsNullOrEmpty(solution.AttachmentPath),
                AttachmentPath = solution.AttachmentPath,
                UpdatedDate = DateTime.UtcNow,
                UpdatedBy = username
            };

            _logger.LogInformation("Solution updated successfully. CorrelationId: {CorrelationId}, SolutionId: {SolutionId}, HasAttachment: {HasAttachment}",
                correlationId, id, updatedSolution.HasAttachment);

            return Ok(ApiResponse<SolutionUpdatedDto>.SuccessResponse(
                updatedSolution,
                "Solution updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during solution update. CorrelationId: {CorrelationId}, SolutionId: {SolutionId}",
                correlationId, id);

            return StatusCode(500, ApiResponse<object>.ErrorResponse(
                "An unexpected error occurred while updating the solution"));
        }
    }

[HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<SolutionDeletedDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    public async Task<IActionResult> DeleteSolution(int id)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Starting solution deletion process. CorrelationId: {CorrelationId}, SolutionId: {SolutionId}, UserId: {UserId}",
            correlationId, id, GetCurrentUserId());

        try
        {
            var userId = GetCurrentUserId();
            var username = GetCurrentUsername();
            var isAdmin = User.IsInRole("Admin");

            // Fetch solution with related data for comprehensive logging
            var solution = await _context.Solutions
                .Include(s => s.Problem)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (solution == null)
            {
                _logger.LogWarning("Solution not found for deletion. CorrelationId: {CorrelationId}, SolutionId: {SolutionId}",
                    correlationId, id);

                return NotFound(ApiResponse<object>.ErrorResponse(
                    "Solution not found"));
            }

            // Authorization check - only owner or admin can delete
            if (solution.UserId != userId && !isAdmin)
            {
                _logger.LogWarning("Unauthorized solution deletion attempt. CorrelationId: {CorrelationId}, SolutionId: {SolutionId}, UserId: {UserId}, OwnerId: {OwnerId}",
                    correlationId, id, userId, solution.UserId);

                return Forbid(ApiResponse<object>.ErrorResponse(
                    "You can only delete your own solutions").Message);
            }

            // Status validation - only pending solutions can be deleted
            if (solution.Status != SolutionStatus.Pending)
            {
                _logger.LogWarning("Attempt to delete non-pending solution. CorrelationId: {CorrelationId}, SolutionId: {SolutionId}, Status: {Status}",
                    correlationId, id, solution.Status);

                return Conflict(ApiResponse<object>.ErrorResponse(
                    $"Cannot delete solution with status '{solution.Status}'. Only pending solutions can be deleted."));
            }

            // Store information for response
            var attachmentPath = solution.AttachmentPath;
            var problemTitle = solution.Problem.Title;
            var createdDate = solution.CreatedDate;

            // Delete associated file if exists
            if (!string.IsNullOrEmpty(attachmentPath))
            {
                try
                {
                    var deleted = DeleteFileAsync(attachmentPath);
                    _logger.LogInformation("Solution attachment deleted. CorrelationId: {CorrelationId}, SolutionId: {SolutionId}, AttachmentPath: {AttachmentPath}, DeleteSuccessful: {DeleteSuccessful}",
                        correlationId, id, attachmentPath, deleted);
                }
                catch (Exception fileEx)
                {
                    _logger.LogWarning(fileEx, "Failed to delete solution attachment file. CorrelationId: {CorrelationId}, SolutionId: {SolutionId}, AttachmentPath: {AttachmentPath}",
                        correlationId, id, attachmentPath);
                    // Continue with solution deletion even if file deletion fails
                }
            }

            // Remove solution from database
            _context.Solutions.Remove(solution);
            await _context.SaveChangesAsync();

            // Create detailed response
            var deletedSolution = new SolutionDeletedDto
            {
                Id = id,
                ProblemId = solution.ProblemId,
                ProblemTitle = problemTitle,
                Content = solution.Content ?? string.Empty,
                HadAttachment = !string.IsNullOrEmpty(attachmentPath),
                AttachmentPath = attachmentPath,
                OriginalCreatedDate = createdDate,
                CreatedBy = solution.User?.Username ?? "Unknown",
                DeletedDate = DateTime.UtcNow,
                DeletedBy = username
            };

            _logger.LogInformation("Solution deleted successfully. CorrelationId: {CorrelationId}, SolutionId: {SolutionId}, ProblemId: {ProblemId}, HadAttachment: {HadAttachment}",
                correlationId, id, solution.ProblemId, deletedSolution.HadAttachment);

            return Ok(ApiResponse<SolutionDeletedDto>.SuccessResponse(
                deletedSolution,
                "Solution deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during solution deletion. CorrelationId: {CorrelationId}, SolutionId: {SolutionId}",
                correlationId, id);

            return StatusCode(500, ApiResponse<object>.ErrorResponse(
                "An unexpected error occurred while deleting the solution"));
        }
    }
[HttpPost("{id}/approve")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<SolutionApprovalDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    public async Task<IActionResult> ApproveSolution(int id, [FromBody] ApproveSolutionRequest request)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Starting solution approval process. CorrelationId: {CorrelationId}, SolutionId: {SolutionId}, AdminId: {AdminId}, IsApproved: {IsApproved}",
            correlationId, id, GetCurrentUserId(), request.IsApproved);

        try
        {
            // Input validation
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                _logger.LogWarning("Solution approval validation failed. CorrelationId: {CorrelationId}, Errors: {Errors}",
                    correlationId, string.Join(", ", errors));

                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Validation failed: " + string.Join(", ", errors)));
            }

            var adminId = GetCurrentUserId();
            var adminUsername = GetCurrentUsername();

            // Fetch solution with complete context
            var solution = await _context.Solutions
                .Include(s => s.Problem)
                    .ThenInclude(p => p.Project)
                        .ThenInclude(pr => pr.Department)
                .Include(s => s.User)
                .Include(s => s.ApprovedByUser)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (solution == null)
            {
                _logger.LogWarning("Solution not found for approval. CorrelationId: {CorrelationId}, SolutionId: {SolutionId}",
                    correlationId, id);

                return NotFound(ApiResponse<object>.ErrorResponse(
                    "Solution not found"));
            }

            // Status validation - only pending solutions can be processed
            if (solution.Status != SolutionStatus.Pending)
            {
                _logger.LogWarning("Attempt to approve non-pending solution. CorrelationId: {CorrelationId}, SolutionId: {SolutionId}, CurrentStatus: {CurrentStatus}",
                    correlationId, id, solution.Status);

                return Conflict(ApiResponse<object>.ErrorResponse(
                    $"Solution is already {solution.Status.ToString().ToLower()}. Only pending solutions can be processed."));
            }

            var previousStatus = solution.Status;
            var newStatus = request.IsApproved ? SolutionStatus.Approved : SolutionStatus.Rejected;
            
            // Update solution status
            solution.Status = newStatus;
            solution.ApprovedBy = adminId;
            solution.ApprovedDate = DateTime.UtcNow;

            // Save changes
            await _context.SaveChangesAsync();

            // Create comprehensive response
            var approvalResult = new SolutionApprovalDto
            {
                Id = solution.Id,
                ProblemId = solution.ProblemId,
                ProblemTitle = solution.Problem.Title,
                ProjectName = solution.Problem.Project.Name,
                DepartmentName = solution.Problem.Project.Department.Name,
                Content = solution.Content ?? string.Empty,
                Status = newStatus.ToString(),
                PreviousStatus = previousStatus.ToString(),
                IsApproved = request.IsApproved,
                ApprovedBy = adminUsername,
                ApprovedDate = solution.ApprovedDate.Value,
                CreatedBy = solution.User?.Username ?? "Unknown",
                CreatedDate = solution.CreatedDate,
                HasAttachment = !string.IsNullOrEmpty(solution.AttachmentPath),
                AttachmentPath = solution.AttachmentPath,
                AzureDevOpsLink = solution.AzureDevOpsLink,
                ProcessingNotes = request.Notes
            };

            var actionText = request.IsApproved ? "approved" : "rejected";
            _logger.LogInformation("Solution {Action} successfully. CorrelationId: {CorrelationId}, SolutionId: {SolutionId}, ProblemId: {ProblemId}, AdminId: {AdminId}",
                actionText, correlationId, id, solution.ProblemId, adminId);

            return Ok(ApiResponse<SolutionApprovalDto>.SuccessResponse(
                approvalResult,
                $"Solution {actionText} successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during solution approval. CorrelationId: {CorrelationId}, SolutionId: {SolutionId}",
                correlationId, id);

            return StatusCode(500, ApiResponse<object>.ErrorResponse(
                "An unexpected error occurred while processing the solution"));
        }
    }
 [HttpGet("my-solutions")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserSolutionsResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetMySolutions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        [FromQuery] string sortBy = "CreatedDate",
        [FromQuery] string sortOrder = "desc",
        [FromQuery] string? searchTerm = null)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Starting user solutions retrieval. CorrelationId: {CorrelationId}, UserId: {UserId}, Page: {Page}, PageSize: {PageSize}",
            correlationId, GetCurrentUserId(), page, pageSize);

        try
        {
            // Validation
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 50) pageSize = 10;

            var userId = GetCurrentUserId();
            var username = GetCurrentUsername();

            // Build base query
            var query = _context.Solutions
                .Include(s => s.Problem)
                    .ThenInclude(p => p.Project)
                        .ThenInclude(pr => pr.Department)
                .Include(s => s.ApprovedByUser)
                .Where(s => s.UserId == userId);

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (Enum.TryParse<SolutionStatus>(status, true, out var statusEnum))
                {
                    query = query.Where(s => s.Status == statusEnum);
                }
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var search = searchTerm.Trim().ToLower();
                query = query.Where(s => 
                    s.Content.ToLower().Contains(search) ||
                    s.Problem.Title.ToLower().Contains(search) ||
                    s.Problem.Description.ToLower().Contains(search));
            }

            // Get total count for pagination
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = sortBy.ToLower() switch
            {
                "status" => sortOrder.ToLower() == "asc" 
                    ? query.OrderBy(s => s.Status).ThenByDescending(s => s.CreatedDate)
                    : query.OrderByDescending(s => s.Status).ThenByDescending(s => s.CreatedDate),
                "problemtitle" => sortOrder.ToLower() == "asc"
                    ? query.OrderBy(s => s.Problem.Title).ThenByDescending(s => s.CreatedDate)
                    : query.OrderByDescending(s => s.Problem.Title).ThenByDescending(s => s.CreatedDate),
                _ => sortOrder.ToLower() == "asc"
                    ? query.OrderBy(s => s.CreatedDate)
                    : query.OrderByDescending(s => s.CreatedDate)
            };

            // Apply pagination and project to DTO
            var solutions = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new UserSolutionDto
                {
                    Id = s.Id,
                    ProblemId = s.ProblemId,
                    ProblemTitle = s.Problem.Title,
                    ProjectName = s.Problem.Project.Name,
                    DepartmentName = s.Problem.Project.Department.Name,
                    Content = s.Content ?? string.Empty,
                    Status = s.Status.ToString(),
                    CreatedDate = s.CreatedDate,
                    ApprovedDate = s.ApprovedDate,
                    ApprovedBy = s.ApprovedByUser != null ? s.ApprovedByUser.Username : null,
                    HasAttachment = !string.IsNullOrEmpty(s.AttachmentPath),
                    AttachmentPath = s.AttachmentPath,
                    AzureDevOpsLink = s.AzureDevOpsLink,
                    CanEdit = s.Status == SolutionStatus.Pending,
                    CanDelete = s.Status == SolutionStatus.Pending,
                    DaysOld = (DateTime.UtcNow - s.CreatedDate).Days
                })
                .ToListAsync();

            // Create response
            var response = new UserSolutionsResponse
            {
                Solutions = solutions,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                HasNextPage = page < Math.Ceiling((double)totalCount / pageSize),
                HasPreviousPage = page > 1,
                Username = username,
                SummaryStats = new UserSolutionsSummary
                {
                    TotalSolutions = totalCount,
                    PendingSolutions = await _context.Solutions.CountAsync(s => s.UserId == userId && s.Status == SolutionStatus.Pending),
                    ApprovedSolutions = await _context.Solutions.CountAsync(s => s.UserId == userId && s.Status == SolutionStatus.Approved),
                    RejectedSolutions = await _context.Solutions.CountAsync(s => s.UserId == userId && s.Status == SolutionStatus.Rejected)
                },
                AppliedFilters = new Dictionary<string, object>
                {
                    ["status"] = status ?? "all",
                    ["searchTerm"] = searchTerm ?? string.Empty,
                    ["sortBy"] = sortBy,
                    ["sortOrder"] = sortOrder
                }
            };

            _logger.LogInformation("User solutions retrieved successfully. CorrelationId: {CorrelationId}, UserId: {UserId}, Count: {Count}, TotalCount: {TotalCount}",
                correlationId, userId, solutions.Count, totalCount);

            return Ok(ApiResponse<UserSolutionsResponse>.SuccessResponse(
                response,
                "Your solutions retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during user solutions retrieval. CorrelationId: {CorrelationId}, UserId: {UserId}",
                correlationId, GetCurrentUserId());

            return StatusCode(500, ApiResponse<object>.ErrorResponse(
                "An unexpected error occurred while retrieving your solutions"));
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
    }
}

#region DTOs and Response Models


public class PaginatedSolutionsResponse
{
    public List<SolutionDto> Solutions { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public int ProblemId { get; set; }
    public Dictionary<string, object> AppliedFilters { get; set; } = new();
}


public class PaginatedPendingSolutionsResponse
{
    public List<PendingSolutionDto> PendingSolutions { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public Dictionary<string, object> AppliedFilters { get; set; } = new();
}

public class PendingSolutionDto
{
    public int Id { get; set; }
    public int ProblemId { get; set; }
    public string ProblemTitle { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? AttachmentPath { get; set; }
    public string? AzureDevOpsLink { get; set; }
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public bool HasAttachment { get; set; }
    public int DaysWaiting { get; set; }
    public bool CanApprove { get; set; }
    public bool CanReject { get; set; }
}

public class SolutionCreatedDto
{
    public int Id { get; set; }
    public int ProblemId { get; set; }
    public string ProblemTitle { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? AzureDevOpsLink { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public bool HasAttachment { get; set; }
    public string? AttachmentPath { get; set; }
}

public class SolutionUpdatedDto
{
    public int Id { get; set; }
    public int ProblemId { get; set; }
    public string ProblemTitle { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? AzureDevOpsLink { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool HasAttachment { get; set; }
    public string? AttachmentPath { get; set; }
    public DateTime UpdatedDate { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public bool ContentChanged { get; set; }
    public bool AttachmentChanged { get; set; }
}


public class SolutionDeletedDto
{
    public int Id { get; set; }
    public int ProblemId { get; set; }
    public string ProblemTitle { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool HadAttachment { get; set; }
    public string? AttachmentPath { get; set; }
    public DateTime OriginalCreatedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime DeletedDate { get; set; }
    public string DeletedBy { get; set; } = string.Empty;
}

public class SolutionApprovalDto
{
    public int Id { get; set; }
    public int ProblemId { get; set; }
    public string ProblemTitle { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PreviousStatus { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public string ApprovedBy { get; set; } = string.Empty;
    public DateTime ApprovedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public bool HasAttachment { get; set; }
    public string? AttachmentPath { get; set; }
    public string? AzureDevOpsLink { get; set; }
    public string? ProcessingNotes { get; set; }
}


public class UserSolutionsResponse
{
    public List<UserSolutionDto> Solutions { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public string Username { get; set; } = string.Empty;
    public UserSolutionsSummary SummaryStats { get; set; } = new();
    public Dictionary<string, object> AppliedFilters { get; set; } = new();
}


public class UserSolutionDto
{
    public int Id { get; set; }
    public int ProblemId { get; set; }
    public string ProblemTitle { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? AttachmentPath { get; set; }
    public string? AzureDevOpsLink { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? ApprovedBy { get; set; }
    public bool HasAttachment { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public int DaysOld { get; set; }
}

public class UserSolutionsSummary
{
    public int TotalSolutions { get; set; }
    public int PendingSolutions { get; set; }
    public int ApprovedSolutions { get; set; }
    public int RejectedSolutions { get; set; }
}

#endregion

#region Request Models


public class CreateSolutionRequest
{
    [Required(ErrorMessage = "Problem ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Problem ID must be a positive integer")]
    public int ProblemId { get; set; }

    [Required(ErrorMessage = "Solution content is required")]
    [StringLength(5000, MinimumLength = 10, ErrorMessage = "Solution content must be between 10 and 5000 characters")]
    public string Content { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Azure DevOps link cannot exceed 500 characters")]
    [Url(ErrorMessage = "Azure DevOps link must be a valid URL")]
    public string? AzureDevOpsLink { get; set; }

    public IFormFile? Attachment { get; set; }
}


public class UpdateSolutionRequest
{
    [Required(ErrorMessage = "Solution content is required")]
    [StringLength(5000, MinimumLength = 10, ErrorMessage = "Solution content must be between 10 and 5000 characters")]
    public string Content { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Azure DevOps link cannot exceed 500 characters")]
    [Url(ErrorMessage = "Azure DevOps link must be a valid URL")]
    public string? AzureDevOpsLink { get; set; }

    public IFormFile? Attachment { get; set; }

    public bool RemoveExistingAttachment { get; set; } = false;
}

public class ApproveSolutionRequest
{
    [Required(ErrorMessage = "Approval decision is required")]
    public bool IsApproved { get; set; }

    [StringLength(500, ErrorMessage = "Rejection reason cannot exceed 500 characters")]
    public string? RejectionReason { get; set; }

    [StringLength(1000, ErrorMessage = "Processing notes cannot exceed 1000 characters")]
    public string? Notes { get; set; }
}

#endregion
