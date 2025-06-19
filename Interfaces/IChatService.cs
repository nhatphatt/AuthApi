using AuthApi.DTOs;

namespace AuthApi.Interfaces
{
    public interface IChatService
    {
        Task<ApiResponse<ChatResponse>> SendMessageAsync(int userId, ChatRequest request);
        Task<ApiResponse<List<ChatHistoryResponse>>> GetChatHistoryAsync(int userId, int page = 1, int pageSize = 20);
        Task<bool> HasChatPermissionAsync(int userId);
        Task<int> GetRemainingTokensAsync(int userId);
    }
} 