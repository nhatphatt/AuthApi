using System.ComponentModel.DataAnnotations;

namespace AuthApi.DTOs
{
    public class PaymentRequest
    {
        [Required]
        [StringLength(50)]
        public string PlanType { get; set; } = string.Empty; // Basic, Premium

        [Required]
        [Range(0.01, 999999.99)]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(100)]
        public string PaymentMethod { get; set; } = string.Empty; // CreditCard, BankTransfer, etc.

        [StringLength(200)]
        public string TransactionId { get; set; } = string.Empty;
    }

    public class PaymentResponse
    {
        public int SubscriptionId { get; set; }
        public string PlanType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsSuccess { get; set; }
        public DateTime PaidAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int NewTokenLimit { get; set; }
    }

    public class SubscriptionStatus
    {
        public int Id { get; set; }
        public string PlanType { get; set; } = string.Empty;
        public bool IsPaid { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public decimal Amount { get; set; }
        public int ChatTokensUsed { get; set; }
        public int ChatTokensLimit { get; set; }
        public int RemainingTokens => ChatTokensLimit - ChatTokensUsed;
        public bool IsActive => IsPaid && ExpiresAt > DateTime.UtcNow;
    }

    public class PlanInfo
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int TokenLimit { get; set; }
        public int DurationDays { get; set; }
        public List<string> Features { get; set; } = new List<string>();
    }
} 