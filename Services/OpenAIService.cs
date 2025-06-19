using AuthApi.Interfaces;
using Newtonsoft.Json;
using System.Text;

namespace AuthApi.Services
{
    public class OpenAIService : IOpenAIService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OpenAIService> _logger;
        private readonly string _apiBaseUrl;
        private readonly string _apiKey;

        public OpenAIService(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAIService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _apiBaseUrl = _configuration["OpenAI:ApiBaseUrl"] ?? "https://api.iunhi.com";
            _apiKey = _configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API Key not configured");
            
            // Configure HttpClient timeout and headers
            _httpClient.Timeout = TimeSpan.FromMinutes(2);
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AuthApi-ChatBot/1.0");
        }

        public async Task<(string response, int tokensUsed)> GetChatCompletionAsync(string message, string model = "gpt-3.5-turbo")
        {
            try
            {
                _logger.LogInformation("Sending chat completion request to {ApiUrl} with model {Model}", _apiBaseUrl, model);

                // Build request following OpenAI API standards
                var requestBody = new
                {
                    model = model,
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = "You are a helpful AI assistant. Respond in a friendly and informative manner."
                        },
                        new
                        {
                            role = "user",
                            content = message
                        }
                    },
                    max_tokens = GetMaxTokensForModel(model),
                    temperature = 0.7,
                    top_p = 1.0,
                    frequency_penalty = 0.0,
                    presence_penalty = 0.0,
                    user = "chatbot-user" // For abuse monitoring
                };

                var json = JsonConvert.SerializeObject(requestBody, Formatting.None);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogDebug("Request payload: {Payload}", json);

                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/v1/chat/completions", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("API Response: {Response}", responseContent);

                if (response.IsSuccessStatusCode)
                {
                    // Try to parse as iunhi.com format first
                    try
                    {
                        var iunhiResponse = JsonConvert.DeserializeObject<IunhiChatResponse>(responseContent);
                        if (iunhiResponse?.Content != null && iunhiResponse.Content.Count > 0)
                        {
                            string aiResponse = iunhiResponse.Content[0]?.Text ?? "I apologize, but I couldn't generate a response at this time.";
                            int tokensUsed = (iunhiResponse.Usage?.InputTokens ?? 0) + (iunhiResponse.Usage?.OutputTokens ?? 0);
                            
                            _logger.LogInformation("Successfully received response from iunhi.com. Tokens used: {TokensUsed}", tokensUsed);
                            return (aiResponse, tokensUsed);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug("Failed to parse as iunhi format, trying OpenAI format: {Error}", ex.Message);
                    }

                    // Fallback to standard OpenAI format
                    try
                    {
                        var chatResponse = JsonConvert.DeserializeObject<OpenAIChatResponse>(responseContent);
                        if (chatResponse?.Choices != null && chatResponse.Choices.Count > 0)
                        {
                            string aiResponse = chatResponse.Choices[0]?.Message?.Content ?? "I apologize, but I couldn't generate a response at this time.";
                            int tokensUsed = chatResponse.Usage?.TotalTokens ?? EstimateTokens(message, aiResponse);

                            _logger.LogInformation("Successfully received response from OpenAI format. Tokens used: {TokensUsed}", tokensUsed);
                            return (aiResponse, tokensUsed);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Failed to parse response in any known format: {Error}", ex.Message);
                    }

                    _logger.LogWarning("Received response but couldn't parse content. Raw response: {Response}", responseContent);
                    return ("I'm sorry, I couldn't process your request at this time. Please try again.", EstimateTokens(message, ""));
                }
                else
                {
                    _logger.LogError("OpenAI API Error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    
                    // Handle specific error codes
                    var errorMessage = response.StatusCode switch
                    {
                        System.Net.HttpStatusCode.Unauthorized => "API authentication failed. Please check your API key.",
                        System.Net.HttpStatusCode.TooManyRequests => "Rate limit exceeded. Please try again later.",
                        System.Net.HttpStatusCode.BadRequest => "Invalid request format or parameters.",
                        System.Net.HttpStatusCode.InternalServerError => "OpenAI service is temporarily unavailable.",
                        _ => $"OpenAI API returned an error: {response.StatusCode}"
                    };

                    throw new HttpRequestException($"{errorMessage} Details: {responseContent}");
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError("Request to OpenAI API timed out");
                throw new Exception("The AI service is taking too long to respond. Please try again.", ex);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error occurred while calling OpenAI API");
                throw new Exception($"Network error occurred: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while calling OpenAI API");
                throw new Exception($"An unexpected error occurred: {ex.Message}", ex);
            }
        }

        public async Task<bool> ValidateApiConnectionAsync()
        {
            try
            {
                _logger.LogInformation("Validating connection to OpenAI API at {ApiUrl}", _apiBaseUrl);
                
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/v1/models");
                var isSuccess = response.IsSuccessStatusCode;
                
                if (isSuccess)
                {
                    _logger.LogInformation("Successfully connected to OpenAI API");
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to connect to OpenAI API: {StatusCode} - {Content}", response.StatusCode, content);
                }
                
                return isSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating OpenAI API connection");
                return false;
            }
        }

        private static int GetMaxTokensForModel(string model)
        {
            return model.ToLower() switch
            {
                "gpt-4" or "gpt-4-0314" or "gpt-4-0613" => 1500,
                "gpt-4-32k" or "gpt-4-32k-0314" or "gpt-4-32k-0613" => 3000,
                "gpt-3.5-turbo" or "gpt-3.5-turbo-0301" or "gpt-3.5-turbo-0613" => 1000,
                "gpt-3.5-turbo-16k" or "gpt-3.5-turbo-16k-0613" => 2000,
                _ => 1000 // Default for unknown models
            };
        }

        private static int EstimateTokens(string input, string output)
        {
            // Rough estimation: 1 token ≈ 4 characters for English text
            var totalChars = (input?.Length ?? 0) + (output?.Length ?? 0);
            return Math.Max(10, totalChars / 4);
        }
    }

    // Response models following OpenAI API structure
    public class OpenAIChatResponse
    {
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("object")]
        public string? Object { get; set; }

        [JsonProperty("created")]
        public long Created { get; set; }

        [JsonProperty("model")]
        public string? Model { get; set; }

        [JsonProperty("choices")]
        public List<ChatChoice>? Choices { get; set; }

        [JsonProperty("usage")]
        public TokenUsage? Usage { get; set; }
    }

    public class ChatChoice
    {
        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("message")]
        public ChatMessage? Message { get; set; }

        [JsonProperty("finish_reason")]
        public string? FinishReason { get; set; }
    }

    public class ChatMessage
    {
        [JsonProperty("role")]
        public string? Role { get; set; }

        [JsonProperty("content")]
        public string? Content { get; set; }
    }

    public class TokenUsage
    {
        [JsonProperty("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonProperty("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonProperty("total_tokens")]
        public int TotalTokens { get; set; }
    }

    // Response models for api.iunhi.com format
    public class IunhiChatResponse
    {
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("type")]
        public string? Type { get; set; }

        [JsonProperty("role")]
        public string? Role { get; set; }

        [JsonProperty("content")]
        public List<IunhiContent>? Content { get; set; }

        [JsonProperty("model")]
        public string? Model { get; set; }

        [JsonProperty("stop_reason")]
        public string? StopReason { get; set; }

        [JsonProperty("usage")]
        public IunhiUsage? Usage { get; set; }
    }

    public class IunhiContent
    {
        [JsonProperty("type")]
        public string? Type { get; set; }

        [JsonProperty("text")]
        public string? Text { get; set; }
    }

    public class IunhiUsage
    {
        [JsonProperty("input_tokens")]
        public int InputTokens { get; set; }

        [JsonProperty("output_tokens")]
        public int OutputTokens { get; set; }
    }
} 