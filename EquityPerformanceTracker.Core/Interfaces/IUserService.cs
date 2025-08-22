using EquityPerformanceTracker.Core.Models;

namespace EquityPerformanceTracker.Core.Interfaces
{
    public interface IUserService
    {
        Task<ApplicationUser?> GetUserByIdAsync(string userId);
        Task<ApplicationUser?> GetUserByEmailAsync(string email);
        Task<bool> UpdateUserProfileAsync(ApplicationUser user);
        Task<bool> StartTrialAsync(string userId);
        Task<bool> ActivateSubscriptionAsync(string userId, DateTime endDate);
        Task<bool> CancelSubscriptionAsync(string userId);
        Task<bool> IsUserSubscribedAsync(string userId);
        Task<bool> HasActiveTrialAsync(string userId);
        Task<bool> CanAccessServiceAsync(string userId);
        Task<List<ApplicationUser>> GetSubscribedUsersAsync();
        Task<List<ApplicationUser>> GetTrialUsersAsync();
        Task<int> GetTotalActiveUsersAsync();
        Task UpdateLastLoginAsync(string userId);
    }
}