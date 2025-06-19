using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProtectedController : ControllerBase
    {
        /// <summary>
        /// Get current user information (requires authentication)
        /// </summary>
        /// <returns>Current user details</returns>
        [HttpGet("profile")]
        public ActionResult GetProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            return Ok(new
            {
                UserId = userId,
                Username = username,
                Role = role,
                Message = "This is a protected endpoint - you are authenticated!"
            });
        }

        /// <summary>
        /// Admin only endpoint (requires Admin role)
        /// </summary>
        /// <returns>Admin data</returns>
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public ActionResult GetAdminData()
        {
            return Ok(new
            {
                Message = "This is an admin-only endpoint!",
                Data = "Secret admin data",
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// User data endpoint (requires User or Admin role)
        /// </summary>
        /// <returns>User data</returns>
        [HttpGet("user")]
        [Authorize(Roles = "User,Admin")]
        public ActionResult GetUserData()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            return Ok(new
            {
                Message = $"Hello {username}! This endpoint is accessible to Users and Admins.",
                Role = role,
                Timestamp = DateTime.UtcNow
            });
        }
    }
} 