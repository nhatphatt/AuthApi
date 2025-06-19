using System.ComponentModel.DataAnnotations;

namespace AuthApi.DTOs
{
    public class ChatRequest
    {
        [Required]
        [StringLength(2000)]
        public string Message { get; set; } = string.Empty;

        [StringLength(50)]
        public string Model { get; set; } = "gpt-3.5-turbo";
    }

    public class ChatResponse
    {
        public string Response { get; set; } = string.Empty;
        public int TokensUsed { get; set; }
        public string Model { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class ChatHistoryResponse
    {
        public int Id { get; set; }
        public string UserMessage { get; set; } = string.Empty;
        public string AiResponse { get; set; } = string.Empty;
        public int TokensUsed { get; set; }
        public string Model { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
} 