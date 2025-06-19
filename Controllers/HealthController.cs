using Microsoft.AspNetCore.Mvc;

namespace AuthApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        /// <summary>
        /// Simple health check endpoint
        /// </summary>
        /// <returns>Health status</returns>
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { 
                status = "healthy", 
                timestamp = DateTime.UtcNow,
                version = "1.0.0"
            });
        }

        /// <summary>
        /// Detailed health check endpoint
        /// </summary>
        /// <returns>Detailed health status</returns>
        [HttpGet("detailed")]
        public IActionResult GetDetailed()
        {
            return Ok(new { 
                status = "healthy",
                timestamp = DateTime.UtcNow,
                version = "1.0.0",
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                uptime = Environment.TickCount64
            });
        }
    }
} 