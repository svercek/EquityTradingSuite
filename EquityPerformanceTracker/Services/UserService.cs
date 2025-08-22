
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EquityPerformanceTracker.Core.Interfaces;
using EquityPerformanceTracker.Core.Models;
using EquityPerformanceTracker.Data.Context;

namespace EquityPerformanceTracker.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            ILogger<UserService> logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
        {
            try
            {
                return await _userManager.FindByIdAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", userId);
                return null;
            }
        }

        public async Task<ApplicationUser?> GetUserByEmailAsync(string email)
        {
            try
            {
                return await _userManager.FindByEmailAsync(email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email: {Email}", email);
                return null;
            }
        }

        public async Task<bool> UpdateUserProfileAsync(ApplicationUser user)
        {
            try
            {
                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User profile updated successfully for user: {UserId}", user.Id);
                    return true;
                }
                
                _logger.LogWarning("Failed to update user profile for user: {UserId}. Errors: {Errors}", 
                    user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile for user: {UserId}", user.Id);
                return false;
            }
        }

        public async Task<bool> StartTrialAsync(string userId)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Cannot start trial - user not found: {UserId}", userId);
                    return false;
                }

                if (user.IsTrialActive || user.IsSubscribed)
                {
                    _logger.LogWarning("User already has active trial or subscription: {UserId}", userId);
                    return false;
                }

                // Start 14-day trial
                user.IsTrialActive = true;
                user.TrialEndDate = DateTime.UtcNow.AddDays(14);

                var result = await UpdateUserProfileAsync(user);
                if (result)
                {
                    _logger.LogInformation("Trial started for user: {UserId}, expires: {ExpiryDate}", 
                        userId, user.TrialEndDate);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting trial for user: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> ActivateSubscriptionAsync(string userId, DateTime endDate)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Cannot activate subscription - user not found: {UserId}", userId);
                    return false;
                }

                // End trial if active
                user.IsTrialActive = false;
                user.TrialEndDate = null;

                // Activate subscription
                user.IsSubscribed = true;
                user.SubscriptionStartDate = DateTime.UtcNow;
                user.SubscriptionEndDate = endDate;

                var result = await UpdateUserProfileAsync(user);
                if (result)
                {
                    _logger.LogInformation("Subscription activated for user: {UserId}, expires: {ExpiryDate}", 
                        userId, endDate);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating subscription for user: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> CancelSubscriptionAsync(string userId)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Cannot cancel subscription - user not found: {UserId}", userId);
                    return false;
                }

                user.IsSubscribed = false;
                user.SubscriptionEndDate = DateTime.UtcNow; // End immediately

                var result = await UpdateUserProfileAsync(user);
                if (result)
                {
                    _logger.LogInformation("Subscription cancelled for user: {UserId}", userId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling subscription for user: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> IsUserSubscribedAsync(string userId)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                return user?.HasActiveSubscription ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking subscription status for user: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> HasActiveTrialAsync(string userId)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                return user?.HasActiveTrial ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking trial status for user: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> CanAccessServiceAsync(string userId)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                return user?.CanAccessService ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking service access for user: {UserId}", userId);
                return false;
            }
        }

        public async Task<List<ApplicationUser>> GetSubscribedUsersAsync()
        {
            try
            {
                return await _context.Users
                    .Where(u => u.IsSubscribed && u.SubscriptionEndDate > DateTime.UtcNow)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscribed users");
                return new List<ApplicationUser>();
            }
        }

        public async Task<List<ApplicationUser>> GetTrialUsersAsync()
        {
            try
            {
                return await _context.Users
                    .Where(u => u.IsTrialActive && u.TrialEndDate > DateTime.UtcNow)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trial users");
                return new List<ApplicationUser>();
            }
        }

        public async Task<int> GetTotalActiveUsersAsync()
        {
            try
            {
                return await _context.Users
                    .CountAsync(u => (u.IsSubscribed && u.SubscriptionEndDate > DateTime.UtcNow) ||
                                   (u.IsTrialActive && u.TrialEndDate > DateTime.UtcNow));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total active users count");
                return 0;
            }
        }

        public async Task UpdateLastLoginAsync(string userId)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user != null)
                {
                    user.LastLoginDate = DateTime.UtcNow;
                    await UpdateUserProfileAsync(user);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last login for user: {UserId}", userId);
            }
        }
    }
}
