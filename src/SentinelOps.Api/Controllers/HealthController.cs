using Microsoft.AspNetCore.Mvc;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Api.Controllers;

public class HealthController : BaseApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public IActionResult GetHealth()
    {
        var health = new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        };

        return Ok(ApiResponse<object>.Ok(health, "System is operational"));
    }
}
