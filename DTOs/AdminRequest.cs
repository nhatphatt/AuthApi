namespace AuthApi.DTOs
{
    public class UserWithSubscriptionDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // Subscription info
        public int? SubscriptionId { get; set; }
        public string PlanType { get; set; } = "Free";
        public bool IsPaid { get; set; } = false;
        public DateTime? PaidAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public decimal Amount { get; set; } = 0;
        public string PaymentMethod { get; set; } = string.Empty;
        public int ChatTokensUsed { get; set; } = 0;
        public int ChatTokensLimit { get; set; } = 100;
        public int RemainingTokens => ChatTokensLimit - ChatTokensUsed;
        public bool IsActive => IsPaid && ExpiresAt > DateTime.UtcNow;
        
        // Chat statistics
        public int TotalChatMessages { get; set; } = 0;
        public DateTime? LastChatAt { get; set; }
    }

    public class PlanAnalyticsDto
    {
        public string PlanType { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int TokenLimit { get; set; }
        public int DurationDays { get; set; }
        public List<string> Features { get; set; } = new List<string>();
        
        // Statistics
        public int TotalSubscribers { get; set; }
        public int ActiveSubscribers { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public int TotalTokensUsed { get; set; }
    }

    public class SystemAnalyticsDto
    {
        public int TotalUsers { get; set; }
        public int TotalSubscriptions { get; set; }
        public int ActiveSubscriptions { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public int TotalChatMessages { get; set; }
        public int TotalTokensUsed { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        
        public List<PlanAnalyticsDto> PlanAnalytics { get; set; } = new List<PlanAnalyticsDto>();
    }
} 