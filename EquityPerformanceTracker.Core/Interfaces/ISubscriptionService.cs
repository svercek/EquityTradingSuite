
using EquityPerformanceTracker.Core.Models;

namespace EquityPerformanceTracker.Core.Interfaces
{
    public interface ISubscriptionService
    {
        Task<Subscription> CreateSubscriptionAsync(string userId, decimal amount, string paymentMethod, string transactionId);
        Task<List<Subscription>> GetUserSubscriptionsAsync(string userId);
        Task<Subscription?> GetActiveSubscriptionAsync(string userId);
        Task<bool> CancelSubscriptionAsync(int subscriptionId);
        Task<bool> RenewSubscriptionAsync(string userId, string transactionId);
        Task<List<Subscription>> GetExpiringSubscriptionsAsync(int daysFromNow = 7);
        Task<decimal> GetMonthlyRevenueAsync(DateTime month);
        Task<int> GetActiveSubscriptionCountAsync();
        Task<bool> ProcessPaymentFailureAsync(string userId, string reason);
        Task<List<Subscription>> GetAllSubscriptionsAsync();
    }
}