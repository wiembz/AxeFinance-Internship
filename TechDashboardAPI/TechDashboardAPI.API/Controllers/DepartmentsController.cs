using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechDashboardAPI.Application.DTOs;
using TechDashboardAPI.Application.Interfaces;
using TechDashboardAPI.Application.Responses;

namespace TechDashboardAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;
    private readonly ILogger<DepartmentsController> _logger;

   
    public DepartmentsController(
        IDepartmentService departmentService,
        ILogger<DepartmentsController> logger)
    {
        _departmentService = departmentService ?? throw new ArgumentNullException(nameof(departmentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PaginatedDepartmentsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetDepartments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortOrder = "asc")
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Retrieving departments list. CorrelationId: {CorrelationId}, Page: {Page}, PageSize: {PageSize}, SearchTerm: {SearchTerm}",
            correlationId, page, pageSize, searchTerm ?? "None");

        try
        {
            // Validate pagination parameters
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 20;

            var result = await _departmentService.GetAllDepartmentsAsync(
                page, pageSize, searchTerm, sortBy, sortOrder);

            if (!result.Success)
            {
                _logger.LogWarning("Failed to retrieve departments. CorrelationId: {CorrelationId}, Error: {Error}",
                    correlationId, result.Message);
                return BadRequest(ApiResponse<object>.ErrorResponse(result.Message ?? "Failed to retrieve departments."));
            }

            _logger.LogInformation("Successfully retrieved departments. CorrelationId: {CorrelationId}, Count: {Count}",
                correlationId, result.Data?.TotalCount ?? 0);

            return Ok(ApiResponse<PaginatedDepartmentsResponse>.SuccessResponse(
                result.Data!,
                $"Successfully retrieved {result.Data?.Departments?.Count ?? 0} departments."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving departments. CorrelationId: {CorrelationId}",
                correlationId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "An unexpected error occurred while retrieving departments. Please try again later."));
        }
    }
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDepartment(int id)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Retrieving department details. CorrelationId: {CorrelationId}, DepartmentId: {DepartmentId}",
            correlationId, id);

        try
        {
            if (id <= 0)
            {
                _logger.LogWarning("Invalid department ID provided. CorrelationId: {CorrelationId}, DepartmentId: {DepartmentId}",
                    correlationId, id);
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid department ID. Department ID must be a positive integer."));
            }

            var result = await _departmentService.GetDepartmentByIdAsync(id);

            if (!result.Success)
            {
                _logger.LogWarning("Department not found. CorrelationId: {CorrelationId}, DepartmentId: {DepartmentId}, Error: {Error}",
                    correlationId, id, result.Message);
                return NotFound(ApiResponse<object>.ErrorResponse(result.Message ?? "Department not found."));
            }

            _logger.LogInformation("Successfully retrieved department details. CorrelationId: {CorrelationId}, DepartmentId: {DepartmentId}, DepartmentName: {DepartmentName}",
                correlationId, id, result.Data?.Name ?? "Unknown");

            return Ok(ApiResponse<DepartmentResponseDto>.SuccessResponse(
                result.Data!,
                $"Successfully retrieved department '{result.Data?.Name ?? "Unknown"}'."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving department details. CorrelationId: {CorrelationId}, DepartmentId: {DepartmentId}",
                correlationId, id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "An unexpected error occurred while retrieving the department. Please try again later."));
        }
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentDto dto)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Creating new department. CorrelationId: {CorrelationId}, DepartmentName: {DepartmentName}, UserId: {UserId}",
            correlationId, dto.Name, GetCurrentUserId());

        try
        {
            // Validate model state
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                             .Select(e => e.ErrorMessage)
                                             .ToList();
                _logger.LogWarning("Department creation validation failed. CorrelationId: {CorrelationId}, Errors: {Errors}",
                    correlationId, string.Join(", ", errors));
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Validation failed: " + string.Join(", ", errors)));
            }

            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                _logger.LogWarning("Unable to retrieve user ID from token during department creation. CorrelationId: {CorrelationId}",
                    correlationId);
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Invalid authentication token. Please login again."));
            }

            var result = await _departmentService.CreateDepartmentAsync(dto, userId);

            if (!result.Success)
            {
                _logger.LogWarning("Failed to create department. CorrelationId: {CorrelationId}, Error: {Error}, DepartmentName: {DepartmentName}",
                    correlationId, result.Message, dto.Name);
                return BadRequest(ApiResponse<object>.ErrorResponse(result.Message ?? "Failed to create department."));
            }

            _logger.LogInformation("Department created successfully. CorrelationId: {CorrelationId}, DepartmentId: {DepartmentId}, DepartmentName: {DepartmentName}",
                correlationId, result.Data?.Id ?? 0, result.Data?.Name ?? "Unknown");

            return CreatedAtAction(
                nameof(GetDepartment),
                new { id = result.Data!.Id },
                ApiResponse<DepartmentResponseDto>.SuccessResponse(
                    result.Data,
                    $"Department '{result.Data.Name}' created successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating department. CorrelationId: {CorrelationId}, DepartmentName: {DepartmentName}",
                correlationId, dto.Name);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "An unexpected error occurred while creating the department. Please try again later."));
        }
    }
    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDepartment(int id, [FromBody] UpdateDepartmentDto dto)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Updating department. CorrelationId: {CorrelationId}, DepartmentId: {DepartmentId}, UserId: {UserId}",
            correlationId, id, GetCurrentUserId());

        try
        {
            // Input validation
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                             .Select(e => e.ErrorMessage)
                                             .ToList();
                _logger.LogWarning("Department update validation failed. CorrelationId: {CorrelationId}, Errors: {Errors}",
                    correlationId, string.Join(", ", errors));
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Validation failed: " + string.Join(", ", errors)));
            }

            if (id <= 0)
            {
                _logger.LogWarning("Invalid department ID for update. CorrelationId: {CorrelationId}, DepartmentId: {DepartmentId}",
                    correlationId, id);
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid department ID. Department ID must be a positive integer."));
            }

            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                _logger.LogWarning("Unable to retrieve user ID from token during department update. CorrelationId: {CorrelationId}",
                    correlationId);
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Invalid authentication token. Please login again."));
            }

            var result = await _departmentService.UpdateDepartmentAsync(id, dto, userId);

            if (!result.Success)
            {
                _logger.LogWarning("Failed to update department. CorrelationId: {CorrelationId}, DepartmentId: {DepartmentId}, Error: {Error}",
                    correlationId, id, result.Message);
                
                // Check if it's a not found vs bad request
                if (result.Message?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse(result.Message));
                }
                
                return BadRequest(ApiResponse<object>.ErrorResponse(result.Message ?? "Failed to update department."));
            }

            _logger.LogInformation("Department updated successfully. CorrelationId: {CorrelationId}, DepartmentId: {DepartmentId}, DepartmentName: {DepartmentName}",
                correlationId, id, result.Data?.Name ?? "Unknown");

            return Ok(ApiResponse<DepartmentResponseDto>.SuccessResponse(
                result.Data!,
                $"Department '{result.Data?.Name ?? "Unknown"}' updated successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating department. CorrelationId: {CorrelationId}, DepartmentId: {DepartmentId}",
                correlationId, id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "An unexpected error occurred while updating the department. Please try again later."));
        }
    }
    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteDepartment(int id)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Deleting department. CorrelationId: {CorrelationId}, DepartmentId: {DepartmentId}, UserId: {UserId}",
            correlationId, id, GetCurrentUserId());

        try
        {
            if (id <= 0)
            {
                _logger.LogWarning("Invalid department ID for deletion. CorrelationId: {CorrelationId}, DepartmentId: {DepartmentId}",
                    correlationId, id);
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid department ID. Department ID must be a positive integer."));
            }

            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                _logger.LogWarning("Unable to retrieve user ID from token during department deletion. CorrelationId: {CorrelationId}",
                    correlationId);
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Invalid authentication token. Please login again."));
            }

            var result = await _departmentService.DeleteDepartmentAsync(id, userId);

            if (!result.Success)
            {
                _logger.LogWarning("Failed to delete department. CorrelationId: {CorrelationId}, DepartmentId: {DepartmentId}, Error: {Error}",
                    correlationId, id, result.Message);

                // Check for specific error types
                if (result.Message?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse(result.Message));
                }
                
                if (result.Message?.Contains("cannot be deleted", StringComparison.OrdinalIgnoreCase) == true ||
                    result.Message?.Contains("dependencies", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return Conflict(ApiResponse<object>.ErrorResponse(result.Message));
                }

                return BadRequest(ApiResponse<object>.ErrorResponse(result.Message ?? "Failed to delete department."));
            }

            _logger.LogInformation("Department deleted successfully. CorrelationId: {CorrelationId}, DepartmentId: {DepartmentId}",
                correlationId, id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting department. CorrelationId: {CorrelationId}, DepartmentId: {DepartmentId}",
                correlationId, id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "An unexpected error occurred while deleting the department. Please try again later."));
        }
    }

    #region Private Helper Methods
 private int GetCurrentUserId()
    {
        string? userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("nameid")?.Value
            ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
            ?? User.FindFirst("sub")?.Value;
        
        return int.TryParse(userIdStr, out var userId) && userId > 0 ? userId : 0;
    }
    private string GetCurrentUsername()
    {
        return User.FindFirst(ClaimTypes.Name)?.Value 
            ?? User.FindFirst("name")?.Value 
            ?? "Unknown";
    }

    #endregion
}
