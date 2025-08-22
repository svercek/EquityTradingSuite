//SubscriptionService.cs
// EquityPerformanceTracker/Services/SubscriptionService.cs
using Microsoft.EntityFrameworkCore;
using EquityPerformanceTracker.Core.Interfaces;
using EquityPerformanceTracker.Core.Models;
using EquityPerformanceTracker.Data.Context;

namespace EquityPerformanceTracker.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(
            ApplicationDbContext context,
            IUserService userService,
            ILogger<SubscriptionService> logger)
        {
            _context = context;
            _userService = userService;
            _logger = logger;
        }

        public async Task<Subscription> CreateSubscriptionAsync(string userId, decimal amount, string paymentMethod, string transactionId)
        {
            try
            {
                var subscription = new Subscription
                {
                    UserId = userId,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddMonths(1), // Monthly subscription
                    Amount = amount,
                    PaymentMethod = paymentMethod,
                    TransactionId = transactionId,
                    Status = SubscriptionStatus.Active,
                    CreatedDate = DateTime.UtcNow
                };

                _context.Subscriptions.Add(subscription);
                await _context.SaveChangesAsync();

                // Update user subscription status
                await _userService.ActivateSubscriptionAsync(userId, subscription.EndDate);

                _logger.LogInformation("Subscription created for user: {UserId}, Transaction: {TransactionId}", 
                    userId, transactionId);

                return subscription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscription for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<List<Subscription>> GetUserSubscriptionsAsync(string userId)
        {
            try
            {
                return await _context.Subscriptions
                    .Where(s => s.UserId == userId)
                    .OrderByDescending(s => s.CreatedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscriptions for user: {UserId}", userId);
                return new List<Subscription>();
            }
        }

        public async Task<Subscription?> GetActiveSubscriptionAsync(string userId)
        {
            try
            {
                return await _context.Subscriptions
                    .Where(s => s.UserId == userId && 
                               s.Status == SubscriptionStatus.Active && 
                               s.EndDate > DateTime.UtcNow)
                    .OrderByDescending(s => s.EndDate)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active subscription for user: {UserId}", userId);
                return null;
            }
        }

        public async Task<bool> CancelSubscriptionAsync(int subscriptionId)
        {
            try
            {
                var subscription = await _context.Subscriptions.FindAsync(subscriptionId);
                if (subscription == null)
                {
                    _logger.LogWarning("Subscription not found: {SubscriptionId}", subscriptionId);
                    return false;
                }

                subscription.Status = SubscriptionStatus.Cancelled;
                subscription.EndDate = DateTime.UtcNow; // End immediately

                await _context.SaveChangesAsync();

                // Update user subscription status
                await _userService.CancelSubscriptionAsync(subscription.UserId);

                _logger.LogInformation("Subscription cancelled: {SubscriptionId} for user: {UserId}", 
                    subscriptionId, subscription.UserId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling subscription: {SubscriptionId}", subscriptionId);
                return false;
            }
        }

        public async Task<bool> RenewSubscriptionAsync(string userId, string transactionId)
        {
            try
            {
                var currentSubscription = await GetActiveSubscriptionAsync(userId);
                if (currentSubscription == null)
                {
                    _logger.LogWarning("No active subscription found for renewal: {UserId}", userId);
                    return false;
                }

                // Create new subscription starting from current end date
                var newSubscription = new Subscription
                {
                    UserId = userId,
                    StartDate = currentSubscription.EndDate,
                    EndDate = currentSubscription.EndDate.AddMonths(1),
                    Amount = currentSubscription.Amount, // Same amount
                    PaymentMethod = currentSubscription.PaymentMethod,
                    TransactionId = transactionId,
                    Status = SubscriptionStatus.Active,
                    CreatedDate = DateTime.UtcNow
                };

                _context.Subscriptions.Add(newSubscription);
                await _context.SaveChangesAsync();

                // Update user subscription end date
                await _userService.ActivateSubscriptionAsync(userId, newSubscription.EndDate);

                _logger.LogInformation("Subscription renewed for user: {UserId}, new end date: {EndDate}", 
                    userId, newSubscription.EndDate);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error renewing subscription for user: {UserId}", userId);
                return false;
            }
        }

        public async Task<List<Subscription>> GetExpiringSubscriptionsAsync(int daysFromNow = 7)
        {
            try
            {
                var expiryDate = DateTime.UtcNow.AddDays(daysFromNow);
                
                return await _context.Subscriptions
                    .Where(s => s.Status == SubscriptionStatus.Active && 
                               s.EndDate <= expiryDate && 
                               s.EndDate > DateTime.UtcNow)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expiring subscriptions");
                return new List<Subscription>();
            }
        }

        public async Task<decimal> GetMonthlyRevenueAsync(DateTime month)
        {
            try
            {
                var startOfMonth = new DateTime(month.Year, month.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1);

                return await _context.Subscriptions
                    .Where(s => s.CreatedDate >= startOfMonth && 
                               s.CreatedDate < endOfMonth &&
                               s.Status == SubscriptionStatus.Active)
                    .SumAsync(s => s.Amount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating monthly revenue for: {Month}", month);
                return 0;
            }
        }

        public async Task<int> GetActiveSubscriptionCountAsync()
        {
            try
            {
                return await _context.Subscriptions
                    .CountAsync(s => s.Status == SubscriptionStatus.Active && 
                                    s.EndDate > DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active subscription count");
                return 0;
            }
        }

        public async Task<bool> ProcessPaymentFailureAsync(string userId, string reason)
        {
            try
            {
                var activeSubscription = await GetActiveSubscriptionAsync(userId);
                if (activeSubscription != null)
                {
                    activeSubscription.Status = SubscriptionStatus.PaymentFailed;
                    await _context.SaveChangesAsync();

                    // Cancel user's subscription access
                    await _userService.CancelSubscriptionAsync(userId);

                    _logger.LogWarning("Payment failed for user: {UserId}, reason: {Reason}", userId, reason);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment failure for user: {UserId}", userId);
                return false;
            }
        }

        public async Task<List<Subscription>> GetAllSubscriptionsAsync()
        {
            try
            {
                return await _context.Subscriptions
                    .OrderByDescending(s => s.CreatedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all subscriptions");
                return new List<Subscription>();
            }
        }
    }
}