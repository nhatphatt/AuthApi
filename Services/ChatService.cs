using Microsoft.EntityFrameworkCore;
using AuthApi.Data;
using AuthApi.DTOs;
using AuthApi.Interfaces;
using AuthApi.Models;

namespace AuthApi.Services
{
    public class ChatService : IChatService
    {
        private readonly ApplicationDbContext _context;
        private readonly IOpenAIService _openAIService;
        private readonly IPaymentService _paymentService;

        public ChatService(ApplicationDbContext context, IOpenAIService openAIService, IPaymentService paymentService)
        {
            _context = context;
            _openAIService = openAIService;
            _paymentService = paymentService;
        }

        public async Task<ApiResponse<ChatResponse>> SendMessageAsync(int userId, ChatRequest request)
        {
            try
            {
                // Check if user has permission to chat
                if (!await HasChatPermissionAsync(userId))
                {
                    return new ApiResponse<ChatResponse>
                    {
                        Success = false,
                        Message = "You need an active subscription to use the chatbot. Please upgrade your plan."
                    };
                }

                // Check remaining tokens
                var remainingTokens = await GetRemainingTokensAsync(userId);
                if (remainingTokens <= 0)
                {
                    return new ApiResponse<ChatResponse>
                    {
                        Success = false,
                        Message = "You have exceeded your token limit. Please upgrade your plan or wait for next month."
                    };
                }

                // Get AI response
                var (aiResponse, tokensUsed) = await _openAIService.GetChatCompletionAsync(request.Message, request.Model);

                // Check if tokens used exceed remaining tokens
                if (tokensUsed > remainingTokens)
                {
                    return new ApiResponse<ChatResponse>
                    {
                        Success = false,
                        Message = $"This request would use {tokensUsed} tokens, but you only have {remainingTokens} remaining."
                    };
                }

                // Save chat history
                var chatHistory = new ChatHistory
                {
                    UserId = userId,
                    UserMessage = request.Message,
                    AiResponse = aiResponse,
                    TokensUsed = tokensUsed,
                    Model = request.Model
                };

                _context.ChatHistories.Add(chatHistory);

                // Update user's token usage
                var subscription = await _context.Subscriptions
                    .Where(s => s.UserId == userId)
                    .OrderByDescending(s => s.CreatedAt)
                    .FirstOrDefaultAsync();

                if (subscription != null)
                {
                    subscription.ChatTokensUsed += tokensUsed;
                    subscription.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return new ApiResponse<ChatResponse>
                {
                    Success = true,
                    Message = "Message sent successfully",
                    Data = new ChatResponse
                    {
                        Response = aiResponse,
                        TokensUsed = tokensUsed,
                        Model = request.Model,
                        Timestamp = DateTime.UtcNow
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<ChatResponse>
                {
                    Success = false,
                    Message = $"Failed to send message: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<List<ChatHistoryResponse>>> GetChatHistoryAsync(int userId, int page = 1, int pageSize = 20)
        {
            try
            {
                var skip = (page - 1) * pageSize;
                
                var chatHistories = await _context.ChatHistories
                    .Where(ch => ch.UserId == userId)
                    .OrderByDescending(ch => ch.CreatedAt)
                    .Skip(skip)
                    .Take(pageSize)
                    .Select(ch => new ChatHistoryResponse
                    {
                        Id = ch.Id,
                        UserMessage = ch.UserMessage,
                        AiResponse = ch.AiResponse,
                        TokensUsed = ch.TokensUsed,
                        Model = ch.Model,
                        CreatedAt = ch.CreatedAt
                    })
                    .ToListAsync();

                return new ApiResponse<List<ChatHistoryResponse>>
                {
                    Success = true,
                    Data = chatHistories,
                    Message = $"Retrieved {chatHistories.Count} chat records"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<ChatHistoryResponse>>
                {
                    Success = false,
                    Message = $"Failed to get chat history: {ex.Message}"
                };
            }
        }

        public async Task<bool> HasChatPermissionAsync(int userId)
        {
            try
            {
                var subscription = await _context.Subscriptions
                    .Where(s => s.UserId == userId)
                    .OrderByDescending(s => s.CreatedAt)
                    .FirstOrDefaultAsync();

                if (subscription == null)
                {
                    // Create default free subscription
                    subscription = new Subscription
                    {
                        UserId = userId,
                        PlanType = "Free",
                        IsPaid = false,
                        ChatTokensUsed = 0,
                        ChatTokensLimit = 500
                    };
                    _context.Subscriptions.Add(subscription);
                    await _context.SaveChangesAsync();
                    return true; // Free tier gets some tokens
                }

                // Free tier: always has permission (limited tokens)
                if (subscription.PlanType == "Free")
                    return true;

                // Paid tiers: check if subscription is active
                return subscription.IsPaid && 
                       subscription.ExpiresAt.HasValue && 
                       subscription.ExpiresAt > DateTime.UtcNow;
            }
            catch
            {
                return false;
            }
        }

        public async Task<int> GetRemainingTokensAsync(int userId)
        {
            try
            {
                var subscription = await _context.Subscriptions
                    .Where(s => s.UserId == userId)
                    .OrderByDescending(s => s.CreatedAt)
                    .FirstOrDefaultAsync();

                if (subscription == null)
                {
                    return 500; // Default free tier tokens
                }

                return Math.Max(0, subscription.ChatTokensLimit - subscription.ChatTokensUsed);
            }
            catch
            {
                return 0;
            }
        }
    }
} 