using System.ComponentModel.DataAnnotations;

namespace AuthApi.Models
{
    public class ChatHistory
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public string UserMessage { get; set; } = string.Empty;

        [Required]
        public string AiResponse { get; set; } = string.Empty;

        public int TokensUsed { get; set; } = 0;

        [StringLength(100)]
        public string Model { get; set; } = "gpt-4o";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual User User { get; set; } = null!;
    }
} 
