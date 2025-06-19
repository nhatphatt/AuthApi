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
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly IOpenAIService _openAIService;

        public ChatController(IChatService chatService, IOpenAIService openAIService)
        {
            _chatService = chatService;
            _openAIService = openAIService;
        }

        /// <summary>
        /// Send a message to the AI chatbot
        /// </summary>
        /// <param name="request">Chat message request</param>
        /// <returns>AI response</returns>
        [HttpPost("send")]
        public async Task<ActionResult<ApiResponse<ChatResponse>>> SendMessage([FromBody] ChatRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new ApiResponse<ChatResponse>
                {
                    Success = false,
                    Message = "User not authenticated"
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<ChatResponse>
                {
                    Success = false,
                    Message = "Invalid input data"
                });
            }

            var result = await _chatService.SendMessageAsync(userId.Value, request);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Get chat history for the current user
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 20)</param>
        /// <returns>List of chat history</returns>
        [HttpGet("history")]
        public async Task<ActionResult<ApiResponse<List<ChatHistoryResponse>>>> GetChatHistory(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new ApiResponse<List<ChatHistoryResponse>>
                {
                    Success = false,
                    Message = "User not authenticated"
                });
            }

            var result = await _chatService.GetChatHistoryAsync(userId.Value, page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Check if user has permission to use chat
        /// </summary>
        /// <returns>Permission status</returns>
        [HttpGet("permission")]
        public async Task<ActionResult> CheckChatPermission()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { hasPermission = false, message = "User not authenticated" });
            }

            var hasPermission = await _chatService.HasChatPermissionAsync(userId.Value);
            var remainingTokens = await _chatService.GetRemainingTokensAsync(userId.Value);

            return Ok(new
            {
                hasPermission = hasPermission,
                remainingTokens = remainingTokens,
                message = hasPermission 
                    ? $"You have {remainingTokens} tokens remaining" 
                    : "You need an active subscription to use the chatbot"
            });
        }

        /// <summary>
        /// Get remaining tokens for the current user
        /// </summary>
        /// <returns>Remaining token count</returns>
        [HttpGet("tokens")]
        public async Task<ActionResult> GetRemainingTokens()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { remainingTokens = 0, message = "User not authenticated" });
            }

            var remainingTokens = await _chatService.GetRemainingTokensAsync(userId.Value);
            return Ok(new { remainingTokens = remainingTokens });
        }

        /// <summary>
        /// Test OpenAI API connection (Admin only)
        /// </summary>
        /// <returns>Connection status</returns>
        [HttpGet("test-connection")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> TestOpenAIConnection()
        {
            try
            {
                var isConnected = await _openAIService.ValidateApiConnectionAsync();
                
                if (isConnected)
                {
                    // Test with a simple message
                    var (response, tokens) = await _openAIService.GetChatCompletionAsync("Hello, this is a connection test.", "gpt-3.5-turbo");
                    
                    return Ok(new
                    {
                        success = true,
                        message = "OpenAI API connection successful",
                        apiUrl = "https://api.iunhi.com",
                        testResponse = response,
                        tokensUsed = tokens
                    });
                }
                else
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Failed to connect to OpenAI API",
                        apiUrl = "https://api.iunhi.com",
                        suggestion = "Please check your API key and network connection"
                    });
                }
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = false,
                    message = "Error testing OpenAI API connection",
                    error = ex.Message,
                    apiUrl = "https://api.iunhi.com"
                });
            }
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