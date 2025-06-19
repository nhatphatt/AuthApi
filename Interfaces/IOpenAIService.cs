namespace AuthApi.Interfaces
{
    public interface IOpenAIService
    {
        Task<(string response, int tokensUsed)> GetChatCompletionAsync(string message, string model = "gpt-3.5-turbo");
        Task<bool> ValidateApiConnectionAsync();
    }
} 