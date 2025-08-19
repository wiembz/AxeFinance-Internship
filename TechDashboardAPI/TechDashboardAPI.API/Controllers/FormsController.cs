using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TechDashboardAPI.Domain.Entities;
using TechDashboardAPI.Infrastructure.Data;
using TechDashboardAPI.Application.DTOs;

namespace TechDashboardAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FormsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public FormsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetForms([FromQuery] int? projectId = null)
    {
        try
        {
            var query = _context.ProblemForms
                .Include(f => f.Project)
                    .ThenInclude(p => p.Department)
                .Include(f => f.CreatedByUser)
                .Where(f => f.IsActive);

            if (projectId.HasValue)
            {
                query = query.Where(f => f.ProjectId == projectId);
            }

            var formEntities = await query
                .OrderByDescending(f => f.CreatedDate)
                .ToListAsync();

            var forms = formEntities.Select(f => new
            {
                f.Id,
                f.Name,
                ProjectName = f.Project.Name,
                DepartmentName = f.Project.Department.Name,
                FormFields = System.Text.Json.JsonSerializer.Deserialize<List<FormFieldDto>>(f.FormFields),
                f.CreatedDate,
                CreatedBy = f.CreatedByUser.Username
            }).ToList();

            return Ok(new { success = true, data = forms });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = $"Failed to get forms: {ex.Message}" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetForm(int id)
    {
        try
        {
            var form = await _context.ProblemForms
                .Include(f => f.Project)
                    .ThenInclude(p => p.Department)
                .Include(f => f.CreatedByUser)
                .FirstOrDefaultAsync(f => f.Id == id && f.IsActive);

            if (form == null)
            {
                return NotFound(new { success = false, message = "Form not found" });
            }

            return Ok(new 
            { 
                success = true, 
                data = new
                {
                    form.Id,
                    form.Name,
                    ProjectName = form.Project.Name,
                    DepartmentName = form.Project.Department.Name,
                    FormFields = System.Text.Json.JsonSerializer.Deserialize<List<FormFieldDto>>(form.FormFields),
                    form.CreatedDate,
                    CreatedBy = form.CreatedByUser.Username
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = $"Failed to get form: {ex.Message}" });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateForm([FromBody] CreateProblemFormRequest request)
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Verify project exists
            var projectExists = await _context.Projects
                .AnyAsync(p => p.Id == request.ProjectId && p.IsActive);

            if (!projectExists)
            {
                return BadRequest(new { success = false, message = "Project not found" });
            }

            var formFields = System.Text.Json.JsonSerializer.Serialize(request.FormFields);

            var problemForm = new ProblemForm
            {
                Name = request.Name,
                ProjectId = request.ProjectId,
                FormFields = formFields,
                CreatedBy = userId,
                CreatedDate = DateTime.UtcNow
            };

            _context.ProblemForms.Add(problemForm);
            await _context.SaveChangesAsync();

            return Ok(new { 
                success = true, 
                message = "Problem form created successfully", 
                data = new { 
                    problemForm.Id, 
                    problemForm.Name 
                } 
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = $"Failed to create problem form: {ex.Message}" });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateForm(int id, [FromBody] UpdateProblemFormRequest request)
    {
        try
        {
            var form = await _context.ProblemForms
                .FirstOrDefaultAsync(f => f.Id == id && f.IsActive);

            if (form == null)
            {
                return NotFound(new { success = false, message = "Form not found" });
            }

            // Verify project exists if changing project
            if (request.ProjectId != form.ProjectId)
            {
                var projectExists = await _context.Projects
                    .AnyAsync(p => p.Id == request.ProjectId && p.IsActive);

                if (!projectExists)
                {
                    return BadRequest(new { success = false, message = "Project not found" });
                }
            }

            form.Name = request.Name;
            form.ProjectId = request.ProjectId;
            form.FormFields = System.Text.Json.JsonSerializer.Serialize(request.FormFields);

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Form updated successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = $"Failed to update form: {ex.Message}" });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteForm(int id)
    {
        try
        {
            var form = await _context.ProblemForms
                .FirstOrDefaultAsync(f => f.Id == id && f.IsActive);

            if (form == null)
            {
                return NotFound(new { success = false, message = "Form not found" });
            }

            form.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Form deleted successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = $"Failed to delete form: {ex.Message}" });
        }
    }

    public class UpdateProblemFormRequest
    {
        public string Name { get; set; } = string.Empty;
        public int ProjectId { get; set; }
        public List<FormFieldDto> FormFields { get; set; } = new List<FormFieldDto>();
    }

    public class CreateProblemFormRequest
    {
        public string Name { get; set; } = string.Empty;
        public int ProjectId { get; set; }
        public List<FormFieldDto> FormFields { get; set; } = new List<FormFieldDto>();
    }

    public class UpdateUserRoleRequest
    {
        public string Role { get; set; } = string.Empty;
    }
}
