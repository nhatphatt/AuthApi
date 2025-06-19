using AuthApi.DTOs;

namespace AuthApi.Interfaces
{
    public interface IPaymentService
    {
        Task<ApiResponse<PaymentResponse>> ProcessPaymentAsync(int userId, PaymentRequest request);
        Task<ApiResponse<SubscriptionStatus>> GetSubscriptionStatusAsync(int userId);
        Task<ApiResponse<List<PlanInfo>>> GetAvailablePlansAsync();
        Task<bool> HasActiveSubscriptionAsync(int userId);
        Task<ApiResponse<bool>> UpdateSubscriptionAsync(int userId, string planType, bool isPaid);
        
        // Admin methods
        Task<ApiResponse<List<UserWithSubscriptionDto>>> GetAllUsersWithSubscriptionsAsync();
        Task<ApiResponse<SystemAnalyticsDto>> GetSystemAnalyticsAsync();
    }
} 