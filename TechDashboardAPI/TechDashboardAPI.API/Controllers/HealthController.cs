using Microsoft.AspNetCore.Mvc;
using TechDashboardAPI.Application.Responses;

namespace TechDashboardAPI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            // Simulate async work
            await Task.CompletedTask;
            var healthData = new
            {
                status = "Healthy",
                checks = new
                {
                    database = "Healthy",
                    memory = "Healthy"
                }
            };

            var response = ApiResponse<object>.SuccessResponse(healthData, "System is healthy");
            return Ok(response);
        }
    }
}
