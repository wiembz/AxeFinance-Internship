using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TechDashboardAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { message = "API is running!", timestamp = DateTime.UtcNow });
    }

    [HttpGet("auth-test")]
    [Authorize]
    public IActionResult AuthTest()
    {
        var user = HttpContext.User;
        return Ok(new 
        { 
            message = "Authentication works!", 
            user = new
            {
                id = user.FindFirst("sub")?.Value ?? user.FindFirst("NameIdentifier")?.Value,
                name = user.FindFirst("name")?.Value ?? user.Identity?.Name,
                email = user.FindFirst("email")?.Value,
                role = user.FindFirst("role")?.Value
            }
        });
    }

    [HttpGet("admin-test")]
    [Authorize(Policy = "AdminOrAbove")]
    public IActionResult AdminTest()
    {
        return Ok(new { message = "Admin authorization works!" });
    }

    [HttpGet("superadmin-test")]
    [Authorize(Policy = "SuperAdminOnly")]
    public IActionResult SuperAdminTest()
    {
        return Ok(new { message = "SuperAdmin authorization works!" });
    }
}
