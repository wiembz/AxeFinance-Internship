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
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UsersController> _logger;

   
    public UsersController(ApplicationDbContext context, ILogger<UsersController> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(int id)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Getting user profile. CorrelationId: {CorrelationId}, UserId: {UserId}",
            correlationId, id);

        try
        {
            // Validate parameters
            if (id <= 0)
            {
                _logger.LogWarning("Invalid user ID provided. CorrelationId: {CorrelationId}, UserId: {UserId}", correlationId, id);
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid user ID. User ID must be a positive integer."));
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId <= 0)
            {
                _logger.LogWarning("Unable to retrieve current user ID from token. CorrelationId: {CorrelationId}", correlationId);
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Invalid authentication token. Please login again."));
            }

            var isAdmin = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

            // Users can only access their own profile unless they're admin
            if (id != currentUserId && !isAdmin)
            {
                _logger.LogWarning("User not authorized to access profile. CorrelationId: {CorrelationId}, RequestedUserId: {RequestedUserId}, CurrentUserId: {CurrentUserId}",
                    correlationId, id, currentUserId);
                return Forbid(ApiResponse<object>.ErrorResponse(
                    "You are not authorized to access this user profile. Users can only access their own profiles.").Message);
            }

            var user = await _context.Users
                .Include(u => u.Department)
                .Where(u => u.Id == id && u.IsActive)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                _logger.LogWarning("User not found or inactive. CorrelationId: {CorrelationId}, UserId: {UserId}", correlationId, id);
                return NotFound(ApiResponse<object>.ErrorResponse(
                    $"User with ID {id} not found or is inactive."));
            }

            var userProfile = new UserProfileDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role.ToString(),
                DepartmentId = user.DepartmentId,
                DepartmentName = user.Department?.Name,
                CreatedDate = user.CreatedDate,
                IsActive = user.IsActive,
                CanEdit = id == currentUserId || isAdmin,
                CanDelete = isAdmin && id != currentUserId // Admins can't delete themselves
            };

            _logger.LogInformation("Successfully retrieved user profile. CorrelationId: {CorrelationId}, UserId: {UserId}, Username: {Username}",
                correlationId, id, user.Username);

            return Ok(ApiResponse<UserProfileDto>.SuccessResponse(
                userProfile, 
                "User profile retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile. CorrelationId: {CorrelationId}, UserId: {UserId}",
                correlationId, id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                ApiResponse<object>.ErrorResponse(
                    "An unexpected error occurred while retrieving the user profile. Please try again later."));
        }
    }

[HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedUsersResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllUsers(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10,
        [FromQuery] string? role = null,
        [FromQuery] int? departmentId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string sortBy = "created",
        [FromQuery] string sortOrder = "desc")
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Getting all users. CorrelationId: {CorrelationId}, Page: {Page}, PageSize: {PageSize}, Role: {Role}, DepartmentId: {DepartmentId}",
            correlationId, page, pageSize, role, departmentId);

        try
        {
            // Validate parameters
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 10;

            var currentUserId = GetCurrentUserId();
            if (currentUserId <= 0)
            {
                _logger.LogWarning("Unable to retrieve current user ID from token. CorrelationId: {CorrelationId}", correlationId);
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Invalid authentication token. Please login again."));
            }

            var query = _context.Users
                .Include(u => u.Department)
                .AsQueryable();

            // Apply filters
            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }
            else
            {
                query = query.Where(u => u.IsActive); // Default to active users only
            }

            if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<UserRole>(role, true, out var userRole))
            {
                query = query.Where(u => u.Role == userRole);
            }

            if (departmentId.HasValue && departmentId.Value > 0)
            {
                query = query.Where(u => u.DepartmentId == departmentId.Value);
            }

            // Apply sorting
            query = sortBy.ToLowerInvariant() switch
            {
                "username" => sortOrder.ToLowerInvariant() == "asc" 
                    ? query.OrderBy(u => u.Username).ThenByDescending(u => u.CreatedDate)
                    : query.OrderByDescending(u => u.Username).ThenByDescending(u => u.CreatedDate),
                "email" => sortOrder.ToLowerInvariant() == "asc" 
                    ? query.OrderBy(u => u.Email).ThenByDescending(u => u.CreatedDate)
                    : query.OrderByDescending(u => u.Email).ThenByDescending(u => u.CreatedDate),
                "role" => sortOrder.ToLowerInvariant() == "asc" 
                    ? query.OrderBy(u => u.Role).ThenByDescending(u => u.CreatedDate)
                    : query.OrderByDescending(u => u.Role).ThenByDescending(u => u.CreatedDate),
                _ => sortOrder.ToLowerInvariant() == "asc" 
                    ? query.OrderBy(u => u.CreatedDate)
                    : query.OrderByDescending(u => u.CreatedDate)
            };

            var totalCount = await query.CountAsync();
            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserSummaryDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    Role = u.Role.ToString(),
                    DepartmentId = u.DepartmentId,
                    DepartmentName = u.Department != null ? u.Department.Name : null,
                    CreatedDate = u.CreatedDate,
                    IsActive = u.IsActive,
                    CanEdit = true, // Admins can edit all users
                    CanDeactivate = u.Id != currentUserId // Can't deactivate self
                })
                .ToListAsync();

            var response = new PaginatedUsersResponse
            {
                Users = users,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                HasNextPage = page * pageSize < totalCount,
                HasPreviousPage = page > 1,
                AppliedFilters = new Dictionary<string, object>
                {
                    ["role"] = role ?? string.Empty,
                    ["departmentId"] = departmentId ?? 0,
                    ["isActive"] = isActive ?? true,
                    ["sortBy"] = sortBy,
                    ["sortOrder"] = sortOrder
                }
            };

            _logger.LogInformation("Successfully retrieved users. CorrelationId: {CorrelationId}, TotalCount: {TotalCount}",
                correlationId, totalCount);

            return Ok(ApiResponse<PaginatedUsersResponse>.SuccessResponse(
                response, 
                $"Successfully retrieved {users.Count} users."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users. CorrelationId: {CorrelationId}",
                correlationId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                ApiResponse<object>.ErrorResponse(
                    "An unexpected error occurred while retrieving users. Please try again later."));
        }
    }

 [HttpPut("{userId:int}/role")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<UserRoleUpdatedDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserRole(int userId, [FromBody] UpdateUserRoleRequest request)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Updating user role. CorrelationId: {CorrelationId}, UserId: {UserId}, NewRole: {NewRole}",
            correlationId, userId, request.Role);

        try
        {
            // Validate parameters
            if (userId <= 0)
            {
                _logger.LogWarning("Invalid user ID provided. CorrelationId: {CorrelationId}, UserId: {UserId}", correlationId, userId);
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid user ID. User ID must be a positive integer."));
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

            var currentUserId = GetCurrentUserId();
            if (currentUserId <= 0)
            {
                _logger.LogWarning("Unable to retrieve current user ID from token. CorrelationId: {CorrelationId}", correlationId);
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Invalid authentication token. Please login again."));
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

            if (user == null)
            {
                _logger.LogWarning("User not found or inactive. CorrelationId: {CorrelationId}, UserId: {UserId}", correlationId, userId);
                return NotFound(ApiResponse<object>.ErrorResponse(
                    $"User with ID {userId} not found or is inactive."));
            }

            if (!Enum.TryParse<UserRole>(request.Role, true, out var newRole))
            {
                _logger.LogWarning("Invalid role specified. CorrelationId: {CorrelationId}, Role: {Role}", correlationId, request.Role);
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    $"Invalid role '{request.Role}' specified. Valid roles are: {string.Join(", ", Enum.GetNames<UserRole>())}."));
            }

            // Store old role for logging
            var oldRole = user.Role;
            user.Role = newRole;
            await _context.SaveChangesAsync();

            var roleUpdate = new UserRoleUpdatedDto
            {
                UserId = userId,
                Username = user.Username,
                OldRole = oldRole.ToString(),
                NewRole = newRole.ToString(),
                UpdatedDate = DateTime.UtcNow,
                UpdatedBy = GetCurrentUsername()
            };

            _logger.LogInformation("User role updated successfully. CorrelationId: {CorrelationId}, UserId: {UserId}, OldRole: {OldRole}, NewRole: {NewRole}",
                correlationId, userId, oldRole, newRole);

            return Ok(ApiResponse<UserRoleUpdatedDto>.SuccessResponse(
                roleUpdate,
                "User role updated successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user role. CorrelationId: {CorrelationId}, UserId: {UserId}",
                correlationId, userId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "An unexpected error occurred while updating the user role. Please try again later."));
        }
    }

[HttpPut("{userId:int}/activate")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<UserActivationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateUser(int userId)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Activating user account. CorrelationId: {CorrelationId}, UserId: {UserId}",
            correlationId, userId);

        try
        {
            // Validate parameters
            if (userId <= 0)
            {
                _logger.LogWarning("Invalid user ID provided. CorrelationId: {CorrelationId}, UserId: {UserId}", correlationId, userId);
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid user ID. User ID must be a positive integer."));
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId <= 0)
            {
                _logger.LogWarning("Unable to retrieve current user ID from token. CorrelationId: {CorrelationId}", correlationId);
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Invalid authentication token. Please login again."));
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning("User not found. CorrelationId: {CorrelationId}, UserId: {UserId}", correlationId, userId);
                return NotFound(ApiResponse<object>.ErrorResponse(
                    $"User with ID {userId} not found."));
            }

            // Check if user is already active
            if (user.IsActive)
            {
                _logger.LogWarning("User already active. CorrelationId: {CorrelationId}, UserId: {UserId}", correlationId, userId);
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "User account is already active."));
            }

            user.IsActive = true;
            await _context.SaveChangesAsync();

            var activationResult = new UserActivationDto
            {
                UserId = userId,
                Username = user.Username,
                IsActive = true,
                UpdatedDate = DateTime.UtcNow,
                UpdatedBy = GetCurrentUsername()
            };

            _logger.LogInformation("User account activated successfully. CorrelationId: {CorrelationId}, UserId: {UserId}, Username: {Username}",
                correlationId, userId, user.Username);

            return Ok(ApiResponse<UserActivationDto>.SuccessResponse(
                activationResult,
                "User account activated successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating user account. CorrelationId: {CorrelationId}, UserId: {UserId}",
                correlationId, userId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "An unexpected error occurred while activating the user account. Please try again later."));
        }
    }

 [HttpPut("{userId:int}/deactivate")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<UserActivationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateUser(int userId)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Deactivating user account. CorrelationId: {CorrelationId}, UserId: {UserId}",
            correlationId, userId);

        try
        {
            // Validate parameters
            if (userId <= 0)
            {
                _logger.LogWarning("Invalid user ID provided. CorrelationId: {CorrelationId}, UserId: {UserId}", correlationId, userId);
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid user ID. User ID must be a positive integer."));
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId <= 0)
            {
                _logger.LogWarning("Unable to retrieve current user ID from token. CorrelationId: {CorrelationId}", correlationId);
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Invalid authentication token. Please login again."));
            }

            // Prevent self-deactivation
            if (userId == currentUserId)
            {
                _logger.LogWarning("User attempting to deactivate their own account. CorrelationId: {CorrelationId}, UserId: {UserId}", correlationId, userId);
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "You cannot deactivate your own account for security reasons."));
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning("User not found. CorrelationId: {CorrelationId}, UserId: {UserId}", correlationId, userId);
                return NotFound(ApiResponse<object>.ErrorResponse(
                    $"User with ID {userId} not found."));
            }

            // Check if user is already inactive
            if (!user.IsActive)
            {
                _logger.LogWarning("User already inactive. CorrelationId: {CorrelationId}, UserId: {UserId}", correlationId, userId);
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "User account is already inactive."));
            }

            user.IsActive = false;
            await _context.SaveChangesAsync();

            var deactivationResult = new UserActivationDto
            {
                UserId = userId,
                Username = user.Username,
                IsActive = false,
                UpdatedDate = DateTime.UtcNow,
                UpdatedBy = GetCurrentUsername()
            };

            _logger.LogInformation("User account deactivated successfully. CorrelationId: {CorrelationId}, UserId: {UserId}, Username: {Username}",
                correlationId, userId, user.Username);

            return Ok(ApiResponse<UserActivationDto>.SuccessResponse(
                deactivationResult,
                "User account deactivated successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user account. CorrelationId: {CorrelationId}, UserId: {UserId}",
                correlationId, userId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "An unexpected error occurred while deactivating the user account. Please try again later."));
        }
    }
[HttpPut("profile")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileUpdatedDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserProfile([FromBody] UpdateProfileRequest request)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Updating user profile. CorrelationId: {CorrelationId}, Username: {Username}",
            correlationId, request.Username);

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
                _logger.LogWarning("Unable to retrieve current user ID from token. CorrelationId: {CorrelationId}", correlationId);
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Invalid authentication token. Please login again."));
            }

            var user = await _context.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

            if (user == null)
            {
                _logger.LogWarning("User not found or inactive. CorrelationId: {CorrelationId}, UserId: {UserId}", correlationId, userId);
                return NotFound(ApiResponse<object>.ErrorResponse(
                    "User not found or is inactive."));
            }

            // Store old values for tracking changes
            var oldUsername = user.Username;
            var oldDepartmentId = user.DepartmentId;
            var oldDepartmentName = user.Department?.Name;

            // Check if username is already taken by another user
            if (await _context.Users.AnyAsync(u => u.Username == request.Username.Trim() && u.Id != userId))
            {
                _logger.LogWarning("Username already taken. CorrelationId: {CorrelationId}, Username: {Username}", correlationId, request.Username);
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    $"Username '{request.Username}' is already taken. Please choose a different username."));
            }

            user.Username = request.Username.Trim();

            // Handle department change
            string? newDepartmentName = oldDepartmentName;
            if (request.DepartmentId.HasValue)
            {
                var department = await _context.Departments
                    .FirstOrDefaultAsync(d => d.Id == request.DepartmentId.Value);

                if (department == null)
                {
                    _logger.LogWarning("Department not found. CorrelationId: {CorrelationId}, DepartmentId: {DepartmentId}", 
                        correlationId, request.DepartmentId.Value);
                    return BadRequest(ApiResponse<object>.ErrorResponse(
                        $"Department with ID {request.DepartmentId.Value} not found."));
                }

                user.DepartmentId = request.DepartmentId.Value;
                newDepartmentName = department.Name;
            }
            else if (request.DepartmentId == null)
            {
                // Allow removing department assignment
                user.DepartmentId = null;
                newDepartmentName = null;
            }

            await _context.SaveChangesAsync();

            var profileUpdate = new UserProfileUpdatedDto
            {
                UserId = userId,
                Username = user.Username,
                DepartmentName = newDepartmentName,
                UpdatedDate = DateTime.UtcNow,
                UsernameChanged = oldUsername != user.Username,
                DepartmentChanged = oldDepartmentId != user.DepartmentId
            };

            _logger.LogInformation("User profile updated successfully. CorrelationId: {CorrelationId}, UserId: {UserId}, UsernameChanged: {UsernameChanged}, DepartmentChanged: {DepartmentChanged}",
                correlationId, userId, profileUpdate.UsernameChanged, profileUpdate.DepartmentChanged);

            return Ok(ApiResponse<UserProfileUpdatedDto>.SuccessResponse(
                profileUpdate,
                "Profile updated successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile. CorrelationId: {CorrelationId}",
                correlationId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "An unexpected error occurred while updating the profile. Please try again later."));
        }
    }

   [HttpGet("my-problems")]
    [ProducesResponseType(typeof(ApiResponse<UserProblemsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMyProblems(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "created",
        [FromQuery] string sortOrder = "desc")
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Getting user's problems. CorrelationId: {CorrelationId}, Page: {Page}, PageSize: {PageSize}",
            correlationId, page, pageSize);

        try
        {
            // Validate parameters
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 10;

            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                _logger.LogWarning("Unable to retrieve current user ID from token. CorrelationId: {CorrelationId}", correlationId);
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Invalid authentication token. Please login again."));
            }

            var query = _context.Problems
                .Include(p => p.Project)
                    .ThenInclude(pr => pr.Department)
                .Include(p => p.Solutions.Where(s => s.Status == Domain.Enums.SolutionStatus.Approved && s.IsActive))
                .Where(p => p.CreatedBy == userId && p.IsActive);

            // Apply sorting
            query = sortBy.ToLowerInvariant() switch
            {
                "likes" => sortOrder.ToLowerInvariant() == "asc" 
                    ? query.OrderBy(p => p.LikeCount).ThenByDescending(p => p.CreatedDate)
                    : query.OrderByDescending(p => p.LikeCount).ThenByDescending(p => p.CreatedDate),
                "title" => sortOrder.ToLowerInvariant() == "asc" 
                    ? query.OrderBy(p => p.Title).ThenByDescending(p => p.CreatedDate)
                    : query.OrderByDescending(p => p.Title).ThenByDescending(p => p.CreatedDate),
                "solutions" => sortOrder.ToLowerInvariant() == "asc" 
                    ? query.OrderBy(p => p.Solutions.Count).ThenByDescending(p => p.CreatedDate)
                    : query.OrderByDescending(p => p.Solutions.Count).ThenByDescending(p => p.CreatedDate),
                _ => sortOrder.ToLowerInvariant() == "asc" 
                    ? query.OrderBy(p => p.CreatedDate)
                    : query.OrderByDescending(p => p.CreatedDate)
            };

            var totalCount = await query.CountAsync();
            var problems = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new UserProblemDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Description.Length > 200 ? p.Description.Substring(0, 200) + "..." : p.Description,
                    Tags = p.Tags ?? string.Empty,
                    CreatedDate = p.CreatedDate,
                    ProjectName = p.Project.Name,
                    DepartmentName = p.Project.Department.Name,
                    LikeCount = p.LikeCount,
                    SolutionsCount = p.Solutions.Count
                })
                .ToListAsync();

            var response = new UserProblemsResponse
            {
                Problems = problems,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                HasNextPage = page * pageSize < totalCount,
                HasPreviousPage = page > 1
            };

            _logger.LogInformation("Successfully retrieved user's problems. CorrelationId: {CorrelationId}, UserId: {UserId}, TotalCount: {TotalCount}",
                correlationId, userId, totalCount);

            return Ok(ApiResponse<UserProblemsResponse>.SuccessResponse(
                response, 
                $"Successfully retrieved {problems.Count} problems."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user's problems. CorrelationId: {CorrelationId}",
                correlationId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                ApiResponse<object>.ErrorResponse(
                    "An unexpected error occurred while retrieving your problems. Please try again later."));
        }
    }

  [HttpGet("my-likes")]
    [ProducesResponseType(typeof(ApiResponse<UserLikedProblemsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMyLikedProblems(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "liked",
        [FromQuery] string sortOrder = "desc")
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Getting user's liked problems. CorrelationId: {CorrelationId}, Page: {Page}, PageSize: {PageSize}",
            correlationId, page, pageSize);

        try
        {
            // Validate parameters
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 10;

            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                _logger.LogWarning("Unable to retrieve current user ID from token. CorrelationId: {CorrelationId}", correlationId);
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Invalid authentication token. Please login again."));
            }

            var query = _context.ProblemLikes
                .Include(pl => pl.Problem)
                    .ThenInclude(p => p.Project)
                        .ThenInclude(pr => pr.Department)
                .Include(pl => pl.Problem.CreatedByUser)
                .Include(pl => pl.Problem.Solutions.Where(s => s.Status == Domain.Enums.SolutionStatus.Approved && s.IsActive))
                .Where(pl => pl.UserId == userId && pl.Problem.IsActive);

            // Apply sorting
            query = sortBy.ToLowerInvariant() switch
            {
                "created" => sortOrder.ToLowerInvariant() == "asc" 
                    ? query.OrderBy(pl => pl.Problem.CreatedDate).ThenByDescending(pl => pl.CreatedDate)
                    : query.OrderByDescending(pl => pl.Problem.CreatedDate).ThenByDescending(pl => pl.CreatedDate),
                "title" => sortOrder.ToLowerInvariant() == "asc" 
                    ? query.OrderBy(pl => pl.Problem.Title).ThenByDescending(pl => pl.CreatedDate)
                    : query.OrderByDescending(pl => pl.Problem.Title).ThenByDescending(pl => pl.CreatedDate),
                "likes" => sortOrder.ToLowerInvariant() == "asc" 
                    ? query.OrderBy(pl => pl.Problem.LikeCount).ThenByDescending(pl => pl.CreatedDate)
                    : query.OrderByDescending(pl => pl.Problem.LikeCount).ThenByDescending(pl => pl.CreatedDate),
                _ => sortOrder.ToLowerInvariant() == "asc" 
                    ? query.OrderBy(pl => pl.CreatedDate)
                    : query.OrderByDescending(pl => pl.CreatedDate)
            };

            var totalCount = await query.CountAsync();
            var likedProblems = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(pl => new UserLikedProblemDto
                {
                    Id = pl.Problem.Id,
                    Title = pl.Problem.Title,
                    Description = pl.Problem.Description.Length > 200 ? pl.Problem.Description.Substring(0, 200) + "..." : pl.Problem.Description,
                    Tags = pl.Problem.Tags ?? string.Empty,
                    CreatedDate = pl.Problem.CreatedDate,
                    CreatedBy = pl.Problem.CreatedByUser.Username,
                    ProjectName = pl.Problem.Project.Name,
                    DepartmentName = pl.Problem.Project.Department.Name,
                    LikeCount = pl.Problem.LikeCount,
                    SolutionsCount = pl.Problem.Solutions.Count,
                    LikedDate = pl.CreatedDate
                })
                .ToListAsync();

            var response = new UserLikedProblemsResponse
            {
                LikedProblems = likedProblems,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                HasNextPage = page * pageSize < totalCount,
                HasPreviousPage = page > 1
            };

            _logger.LogInformation("Successfully retrieved user's liked problems. CorrelationId: {CorrelationId}, UserId: {UserId}, TotalCount: {TotalCount}",
                correlationId, userId, totalCount);

            return Ok(ApiResponse<UserLikedProblemsResponse>.SuccessResponse(
                response, 
                $"Successfully retrieved {likedProblems.Count} liked problems."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user's liked problems. CorrelationId: {CorrelationId}",
                correlationId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                ApiResponse<object>.ErrorResponse(
                    "An unexpected error occurred while retrieving your liked problems. Please try again later."));
        }
    }
 [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<UserDashboardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserDashboard()
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Getting user dashboard. CorrelationId: {CorrelationId}",
            correlationId);

        try
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                _logger.LogWarning("Unable to retrieve current user ID from token. CorrelationId: {CorrelationId}", correlationId);
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Invalid authentication token. Please login again."));
            }

            // Verify user exists and is active
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId && u.IsActive);
            if (!userExists)
            {
                _logger.LogWarning("User not found or inactive. CorrelationId: {CorrelationId}, UserId: {UserId}", correlationId, userId);
                return NotFound(ApiResponse<object>.ErrorResponse(
                    "User not found or is inactive."));
            }

            var recentActivity = await GetRecentActivity(userId);

            var dashboard = new UserDashboardDto
            {
                MyProblemsCount = await _context.Problems.CountAsync(p => p.CreatedBy == userId && p.IsActive),
                MySolutionsCount = await _context.Solutions.CountAsync(s => s.UserId == userId && s.IsActive),
                MyLikesCount = await _context.ProblemLikes.CountAsync(pl => pl.UserId == userId),
                MyApprovedSolutions = await _context.Solutions.CountAsync(s => s.UserId == userId && s.Status == Domain.Enums.SolutionStatus.Approved && s.IsActive),
                RecentActivity = recentActivity
            };

            _logger.LogInformation("Successfully retrieved user dashboard. CorrelationId: {CorrelationId}, UserId: {UserId}, ProblemsCount: {ProblemsCount}, SolutionsCount: {SolutionsCount}",
                correlationId, userId, dashboard.MyProblemsCount, dashboard.MySolutionsCount);

            return Ok(ApiResponse<UserDashboardDto>.SuccessResponse(
                dashboard,
                "Dashboard data retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user dashboard. CorrelationId: {CorrelationId}",
                correlationId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "An unexpected error occurred while retrieving dashboard data. Please try again later."));
        }
    }

 private async Task<List<UserActivityDto>> GetRecentActivity(int userId)
    {
        var activities = new List<UserActivityDto>();

        // Recent problems created
        var recentProblems = await _context.Problems
            .Include(p => p.Project)
            .Where(p => p.CreatedBy == userId && p.IsActive)
            .OrderByDescending(p => p.CreatedDate)
            .Take(3)
            .Select(p => new UserActivityDto
            {
                Type = "Problem Created",
                Title = p.Title,
                ProjectName = p.Project.Name,
                Date = p.CreatedDate,
                Id = p.Id
            })
            .ToListAsync();

        // Recent solutions submitted
        var recentSolutions = await _context.Solutions
            .Include(s => s.Problem)
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderByDescending(s => s.CreatedDate)
            .Take(3)
            .Select(s => new UserActivityDto
            {
                Type = "Solution Submitted",
                Title = s.Problem.Title,
                Status = s.Status.ToString(),
                Date = s.CreatedDate,
                Id = s.Id
            })
            .ToListAsync();

        // Recent likes
        var recentLikes = await _context.ProblemLikes
            .Include(pl => pl.Problem)
                .ThenInclude(p => p.Project)
            .Where(pl => pl.UserId == userId && pl.Problem.IsActive)
            .OrderByDescending(pl => pl.CreatedDate)
            .Take(3)
            .Select(pl => new UserActivityDto
            {
                Type = "Problem Liked",
                Title = pl.Problem.Title,
                ProjectName = pl.Problem.Project.Name,
                Date = pl.CreatedDate,
                Id = pl.Problem.Id
            })
            .ToListAsync();

        activities.AddRange(recentProblems);
        activities.AddRange(recentSolutions);
        activities.AddRange(recentLikes);

        return activities.OrderByDescending(a => a.Date).Take(10).ToList();
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
}

#region DTOs and Response Models

public class PaginatedUsersResponse
{
    public List<UserSummaryDto> Users { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public Dictionary<string, object> AppliedFilters { get; set; } = new();
}


public class UserProfileDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsActive { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}


public class UserSummaryDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsActive { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDeactivate { get; set; }
}


public class UserRoleUpdatedDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string OldRole { get; set; } = string.Empty;
    public string NewRole { get; set; } = string.Empty;
    public DateTime UpdatedDate { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}


public class UserActivationDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime UpdatedDate { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}

public class UserProfileUpdatedDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public DateTime UpdatedDate { get; set; }
    public bool UsernameChanged { get; set; }
    public bool DepartmentChanged { get; set; }
}

public class UserDashboardDto
{
    public int MyProblemsCount { get; set; }
    public int MySolutionsCount { get; set; }
    public int MyLikesCount { get; set; }
    public int MyApprovedSolutions { get; set; }
    public List<UserActivityDto> RecentActivity { get; set; } = new();
}

public class UserActivityDto
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? ProjectName { get; set; }
    public string? Status { get; set; }
    public DateTime Date { get; set; }
    public int Id { get; set; }
}


public class UserProblemsResponse
{
    public List<UserProblemDto> Problems { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}


public class UserProblemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int LikeCount { get; set; }
    public int SolutionsCount { get; set; }
}


public class UserLikedProblemsResponse
{
    public List<UserLikedProblemDto> LikedProblems { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}


public class UserLikedProblemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int LikeCount { get; set; }
    public int SolutionsCount { get; set; }
    public DateTime LikedDate { get; set; }
}

#endregion

#region Request Models


public class UpdateProfileRequest
{
    [Required(ErrorMessage = "Username is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
    public string Username { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Department ID must be a positive integer")]
    public int? DepartmentId { get; set; }
}


public class UpdateUserRoleRequest
{
    [Required(ErrorMessage = "Role is required")]
    [StringLength(20, ErrorMessage = "Role name cannot exceed 20 characters")]
    public string Role { get; set; } = string.Empty;
}

#endregion
