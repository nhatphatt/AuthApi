using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AuthApi.DTOs;
using AuthApi.Interfaces;

namespace AuthApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        /// <summary>
        /// Process payment for a subscription plan
        /// </summary>
        /// <param name="request">Payment details</param>
        /// <returns>Payment result</returns>
        [HttpPost("process")]
        public async Task<ActionResult<ApiResponse<PaymentResponse>>> ProcessPayment([FromBody] PaymentRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new ApiResponse<PaymentResponse>
                {
                    Success = false,
                    Message = "User not authenticated"
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<PaymentResponse>
                {
                    Success = false,
                    Message = "Invalid payment data"
                });
            }

            var result = await _paymentService.ProcessPaymentAsync(userId.Value, request);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Get current subscription status
        /// </summary>
        /// <returns>Subscription details</returns>
        [HttpGet("subscription")]
        public async Task<ActionResult<ApiResponse<SubscriptionStatus>>> GetSubscriptionStatus()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new ApiResponse<SubscriptionStatus>
                {
                    Success = false,
                    Message = "User not authenticated"
                });
            }

            var result = await _paymentService.GetSubscriptionStatusAsync(userId.Value);
            return Ok(result);
        }

        /// <summary>
        /// Get available subscription plans
        /// </summary>
        /// <returns>List of available plans</returns>
        [HttpGet("plans")]
        public async Task<ActionResult<ApiResponse<List<PlanInfo>>>> GetAvailablePlans()
        {
            var result = await _paymentService.GetAvailablePlansAsync();
            return Ok(result);
        }

        /// <summary>
        /// Admin endpoint: Update subscription status
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="planType">Plan type</param>
        /// <param name="isPaid">Payment status</param>
        /// <returns>Update result</returns>
        [HttpPut("admin/update-subscription")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateSubscription(
            [FromQuery] int userId,
            [FromQuery] string planType,
            [FromQuery] bool isPaid)
        {
            if (string.IsNullOrEmpty(planType))
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Plan type is required"
                });
            }

            var result = await _paymentService.UpdateSubscriptionAsync(userId, planType, isPaid);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Admin endpoint: Get all users with subscription details
        /// </summary>
        /// <returns>List of users with their subscription information</returns>
        [HttpGet("admin/users")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<List<UserWithSubscriptionDto>>>> GetAllUsersWithSubscriptions()
        {
            var result = await _paymentService.GetAllUsersWithSubscriptionsAsync();
            return Ok(result);
        }

        /// <summary>
        /// Admin endpoint: Get system analytics and plan statistics
        /// </summary>
        /// <returns>Comprehensive system analytics including plan performance</returns>
        [HttpGet("admin/analytics")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<SystemAnalyticsDto>>> GetSystemAnalytics()
        {
            var result = await _paymentService.GetSystemAnalyticsAsync();
            return Ok(result);
        }

        /// <summary>
        /// Check if user has active subscription
        /// </summary>
        /// <returns>Active subscription status</returns>
        [HttpGet("active")]
        public async Task<ActionResult> CheckActiveSubscription()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { hasActiveSubscription = false, message = "User not authenticated" });
            }

            var hasActiveSubscription = await _paymentService.HasActiveSubscriptionAsync(userId.Value);
            return Ok(new
            {
                hasActiveSubscription = hasActiveSubscription,
                message = hasActiveSubscription
                    ? "You have an active subscription"
                    : "No active subscription found"
            });
        }

        /// <summary>
        /// Simulate payment (for testing purposes)
        /// </summary>
        /// <param name="planType">Plan to purchase</param>
        /// <returns>Simulated payment result</returns>
        [HttpPost("simulate/{planType}")]
        public async Task<ActionResult<ApiResponse<PaymentResponse>>> SimulatePayment(string planType)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new ApiResponse<PaymentResponse>
                {
                    Success = false,
                    Message = "User not authenticated"
                });
            }

            var plans = await _paymentService.GetAvailablePlansAsync();
            var plan = plans.Data?.FirstOrDefault(p => p.Name.Equals(planType, StringComparison.OrdinalIgnoreCase));
            
            if (plan == null)
            {
                return BadRequest(new ApiResponse<PaymentResponse>
                {
                    Success = false,
                    Message = "Invalid plan type"
                });
            }

            var paymentRequest = new PaymentRequest
            {
                PlanType = plan.Name,
                Amount = plan.Price,
                PaymentMethod = "Simulation",
                TransactionId = $"SIM_{Guid.NewGuid().ToString()[..8]}"
            };

            var result = await _paymentService.ProcessPaymentAsync(userId.Value, paymentRequest);
            return Ok(result);
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return null;
        }
    }
} 