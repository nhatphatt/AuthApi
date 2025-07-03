using Microsoft.EntityFrameworkCore;
using AuthApi.Data;
using AuthApi.DTOs;
using AuthApi.Interfaces;
using AuthApi.Models;

namespace AuthApi.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly Dictionary<string, PlanInfo> _plans;

        public PaymentService(ApplicationDbContext context)
        {
            _context = context;
            _plans = new Dictionary<string, PlanInfo>
            {
                ["Basic"] = new PlanInfo
                {
                    Name = "Basic",
                    Price = 99000,
                    TokenLimit = 10000,
                    DurationDays = 30,
                    Features = new List<string>
                    {
                        "10,000 tokens/month",
                        "GPT-3.5 Turbo access",
                        "Basic chat history",
                        "Email support"
                    }
                },
                ["Premium"] = new PlanInfo
                {
                    Name = "Premium",
                    Price = 199000,
                    TokenLimit = 50000,
                    DurationDays = 30,
                    Features = new List<string>
                    {
                        "50,000 tokens/month",
                        "GPT-4 access",
                        "Full chat history",
                        "Priority support",
                        "Custom AI personality"
                    }
                }
            };
        }

        public async Task<ApiResponse<PaymentResponse>> ProcessPaymentAsync(int userId, PaymentRequest request)
        {
            try
            {
                if (!_plans.ContainsKey(request.PlanType))
                {
                    return new ApiResponse<PaymentResponse>
                    {
                        Success = false,
                        Message = "Invalid plan type"
                    };
                }

                var plan = _plans[request.PlanType];
                var expiresAt = DateTime.UtcNow.AddDays(plan.DurationDays);

                // Create or update subscription
                var existingSubscription = await _context.Subscriptions
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.PlanType == request.PlanType);

                Subscription subscription;
                if (existingSubscription != null)
                {
                    // Update existing subscription
                    existingSubscription.IsPaid = true;
                    existingSubscription.PaidAt = DateTime.UtcNow;
                    existingSubscription.ExpiresAt = expiresAt;
                    existingSubscription.Amount = request.Amount;
                    existingSubscription.PaymentMethod = request.PaymentMethod;
                    existingSubscription.TransactionId = request.TransactionId;
                    existingSubscription.ChatTokensLimit = plan.TokenLimit;
                    existingSubscription.UpdatedAt = DateTime.UtcNow;
                    subscription = existingSubscription;
                }
                else
                {
                    // Create new subscription
                    subscription = new Subscription
                    {
                        UserId = userId,
                        PlanType = request.PlanType,
                        IsPaid = true,
                        PaidAt = DateTime.UtcNow,
                        ExpiresAt = expiresAt,
                        Amount = request.Amount,
                        PaymentMethod = request.PaymentMethod,
                        TransactionId = request.TransactionId,
                        ChatTokensUsed = 0,
                        ChatTokensLimit = plan.TokenLimit
                    };
                    _context.Subscriptions.Add(subscription);
                }

                await _context.SaveChangesAsync();

                return new ApiResponse<PaymentResponse>
                {
                    Success = true,
                    Message = "Payment processed successfully",
                    Data = new PaymentResponse
                    {
                        SubscriptionId = subscription.Id,
                        PlanType = subscription.PlanType,
                        Amount = subscription.Amount,
                        IsSuccess = true,
                        PaidAt = subscription.PaidAt!.Value,
                        ExpiresAt = subscription.ExpiresAt!.Value,
                        NewTokenLimit = subscription.ChatTokensLimit
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<PaymentResponse>
                {
                    Success = false,
                    Message = $"Payment processing failed: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<SubscriptionStatus>> GetSubscriptionStatusAsync(int userId)
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
                }

                return new ApiResponse<SubscriptionStatus>
                {
                    Success = true,
                    Data = new SubscriptionStatus
                    {
                        Id = subscription.Id,
                        PlanType = subscription.PlanType,
                        IsPaid = subscription.IsPaid,
                        PaidAt = subscription.PaidAt,
                        ExpiresAt = subscription.ExpiresAt,
                        Amount = subscription.Amount,
                        ChatTokensUsed = subscription.ChatTokensUsed,
                        ChatTokensLimit = subscription.ChatTokensLimit
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<SubscriptionStatus>
                {
                    Success = false,
                    Message = $"Failed to get subscription status: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<List<PlanInfo>>> GetAvailablePlansAsync()
        {
            return new ApiResponse<List<PlanInfo>>
            {
                Success = true,
                Data = _plans.Values.ToList()
            };
        }

        public async Task<bool> HasActiveSubscriptionAsync(int userId)
        {
            var subscription = await _context.Subscriptions
                .Where(s => s.UserId == userId && s.IsPaid && s.ExpiresAt > DateTime.UtcNow)
                .AnyAsync();

            return subscription;
        }

        public async Task<ApiResponse<bool>> UpdateSubscriptionAsync(int userId, string planType, bool isPaid)
        {
            try
            {
                // Find the user's most recent subscription (regardless of plan type)
                var subscription = await _context.Subscriptions
                    .Where(s => s.UserId == userId)
                    .OrderByDescending(s => s.CreatedAt)
                    .FirstOrDefaultAsync();

                if (subscription == null)
                {
                    // Create new subscription if user doesn't have one
                    subscription = new Subscription
                    {
                        UserId = userId,
                        PlanType = planType,
                        IsPaid = isPaid,
                        ChatTokensUsed = 0,
                        ChatTokensLimit = planType == "Free" ? 500 : _plans.ContainsKey(planType) ? _plans[planType].TokenLimit : 500
                    };
                    _context.Subscriptions.Add(subscription);
                }
                else
                {
                    // Update existing subscription to new plan type
                    subscription.PlanType = planType;
                    subscription.IsPaid = isPaid;
                    subscription.UpdatedAt = DateTime.UtcNow;
                    
                    // Update token limits based on new plan
                    if (planType == "Free")
                    {
                        subscription.ChatTokensLimit = 500;
                    }
                    else if (_plans.ContainsKey(planType))
                    {
                        subscription.ChatTokensLimit = _plans[planType].TokenLimit;
                    }
                }

                // Set payment and expiration details for paid plans
                if (isPaid)
                {
                    subscription.PaidAt = DateTime.UtcNow;
                    if (planType != "Free" && _plans.ContainsKey(planType))
                    {
                        subscription.ExpiresAt = DateTime.UtcNow.AddDays(_plans[planType].DurationDays);
                        subscription.Amount = _plans[planType].Price;
                    }
                }
                else
                {
                    // If marking as unpaid, clear payment details
                    subscription.PaidAt = null;
                    subscription.ExpiresAt = null;
                    subscription.Amount = 0;
                }

                await _context.SaveChangesAsync();

                return new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = $"Subscription updated successfully to {planType} plan"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"Failed to update subscription: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<List<UserWithSubscriptionDto>>> GetAllUsersWithSubscriptionsAsync()
        {
            try
            {
                var usersWithSubscriptions = await (
                    from user in _context.Users
                    join subscription in _context.Subscriptions on user.Id equals subscription.UserId into subs
                    from subscription in subs.DefaultIfEmpty()
                    select new { user, subscription }
                ).ToListAsync();

                var result = new List<UserWithSubscriptionDto>();

                foreach (var item in usersWithSubscriptions)
                {
                    // Get chat statistics
                    var chatStats = await _context.ChatHistories
                        .Where(ch => ch.UserId == item.user.Id)
                        .GroupBy(ch => ch.UserId)
                        .Select(g => new
                        {
                            TotalMessages = g.Count(),
                            LastChatAt = g.Max(ch => ch.CreatedAt)
                        })
                        .FirstOrDefaultAsync();

                    var userDto = new UserWithSubscriptionDto
                    {
                        Id = item.user.Id,
                        Username = item.user.Username,
                        Role = item.user.Role,
                        CreatedAt = item.user.CreatedAt,
                        UpdatedAt = item.user.UpdatedAt,
                        
                        // Subscription info
                        SubscriptionId = item.subscription?.Id,
                        PlanType = item.subscription?.PlanType ?? "Free",
                        IsPaid = item.subscription?.IsPaid ?? false,
                        PaidAt = item.subscription?.PaidAt,
                        ExpiresAt = item.subscription?.ExpiresAt,
                        Amount = item.subscription?.Amount ?? 0,
                        PaymentMethod = item.subscription?.PaymentMethod ?? string.Empty,
                        ChatTokensUsed = item.subscription?.ChatTokensUsed ?? 0,
                        ChatTokensLimit = item.subscription?.ChatTokensLimit ?? 500,
                        
                        // Chat statistics
                        TotalChatMessages = chatStats?.TotalMessages ?? 0,
                        LastChatAt = chatStats?.LastChatAt
                    };

                    result.Add(userDto);
                }

                return new ApiResponse<List<UserWithSubscriptionDto>>
                {
                    Success = true,
                    Data = result,
                    Message = $"Retrieved {result.Count} users with subscription details"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<UserWithSubscriptionDto>>
                {
                    Success = false,
                    Message = $"Failed to get users with subscriptions: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<SystemAnalyticsDto>> GetSystemAnalyticsAsync()
        {
            try
            {
                var totalUsers = await _context.Users.CountAsync();
                var totalSubscriptions = await _context.Subscriptions.CountAsync();
                var activeSubscriptions = await _context.Subscriptions
                    .CountAsync(s => s.IsPaid && s.ExpiresAt > DateTime.UtcNow);
                
                var totalRevenue = await _context.Subscriptions
                    .Where(s => s.IsPaid)
                    .SumAsync(s => s.Amount);
                
                var monthlyRevenue = await _context.Subscriptions
                    .Where(s => s.IsPaid && s.PaidAt >= DateTime.UtcNow.AddDays(-30))
                    .SumAsync(s => s.Amount);
                
                var totalChatMessages = await _context.ChatHistories.CountAsync();
                var totalTokensUsed = await _context.Subscriptions.SumAsync(s => s.ChatTokensUsed);

                // Plan analytics
                var planAnalytics = new List<PlanAnalyticsDto>();
                
                foreach (var plan in _plans)
                {
                    var planSubscriptions = await _context.Subscriptions
                        .Where(s => s.PlanType == plan.Key)
                        .ToListAsync();
                    
                    var planAnalytic = new PlanAnalyticsDto
                    {
                        PlanType = plan.Key,
                        Price = plan.Value.Price,
                        TokenLimit = plan.Value.TokenLimit,
                        DurationDays = plan.Value.DurationDays,
                        Features = plan.Value.Features,
                        TotalSubscribers = planSubscriptions.Count,
                        ActiveSubscribers = planSubscriptions.Count(s => s.IsPaid && s.ExpiresAt > DateTime.UtcNow),
                        TotalRevenue = planSubscriptions.Where(s => s.IsPaid).Sum(s => s.Amount),
                        MonthlyRevenue = planSubscriptions
                            .Where(s => s.IsPaid && s.PaidAt >= DateTime.UtcNow.AddDays(-30))
                            .Sum(s => s.Amount),
                        TotalTokensUsed = planSubscriptions.Sum(s => s.ChatTokensUsed)
                    };
                    
                    planAnalytics.Add(planAnalytic);
                }

                // Add Free plan analytics
                var freeUsers = await _context.Users
                    .Where(u => !_context.Subscriptions.Any(s => s.UserId == u.Id) || 
                               _context.Subscriptions.Where(s => s.UserId == u.Id).All(s => s.PlanType == "Free"))
                    .CountAsync();

                var freeSubscriptions = await _context.Subscriptions
                    .Where(s => s.PlanType == "Free")
                    .ToListAsync();

                planAnalytics.Add(new PlanAnalyticsDto
                {
                    PlanType = "Free",
                    Price = 0,
                    TokenLimit = 500,
                    DurationDays = 0,
                    Features = new List<string> { "500 tokens", "Basic chat", "Community support" },
                    TotalSubscribers = freeUsers,
                    ActiveSubscribers = freeUsers,
                    TotalRevenue = 0,
                    MonthlyRevenue = 0,
                    TotalTokensUsed = freeSubscriptions.Sum(s => s.ChatTokensUsed)
                });

                var systemAnalytics = new SystemAnalyticsDto
                {
                    TotalUsers = totalUsers,
                    TotalSubscriptions = totalSubscriptions,
                    ActiveSubscriptions = activeSubscriptions,
                    TotalRevenue = totalRevenue,
                    MonthlyRevenue = monthlyRevenue,
                    TotalChatMessages = totalChatMessages,
                    TotalTokensUsed = totalTokensUsed,
                    PlanAnalytics = planAnalytics
                };

                return new ApiResponse<SystemAnalyticsDto>
                {
                    Success = true,
                    Data = systemAnalytics,
                    Message = "System analytics retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<SystemAnalyticsDto>
                {
                    Success = false,
                    Message = $"Failed to get system analytics: {ex.Message}"
                };
            }
        }
    }
} 