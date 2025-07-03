using System.ComponentModel.DataAnnotations;

namespace AuthApi.Models
{
    public class Subscription
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string PlanType { get; set; } = "Free"; // Free, Basic, Premium

        public bool IsPaid { get; set; } = false;

        public DateTime? PaidAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public decimal Amount { get; set; } = 0;

        [StringLength(100)]
        public string PaymentMethod { get; set; } = string.Empty;

        [StringLength(200)]
        public string TransactionId { get; set; } = string.Empty;

        public int ChatTokensUsed { get; set; } = 0;
        public int ChatTokensLimit { get; set; } = 1000; // Free plan limit

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public virtual User User { get; set; } = null!;
    }
} 