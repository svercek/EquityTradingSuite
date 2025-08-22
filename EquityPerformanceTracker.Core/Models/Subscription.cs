// EquityPerformanceTracker.Core/Models/Subscription.cs - For tracking subscription history
namespace EquityPerformanceTracker.Core.Models
{
    public class Subscription
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public SubscriptionStatus Status { get; set; }
        public DateTime CreatedDate { get; set; }
        
        // Navigation Properties
        public ApplicationUser ApplicationUser { get; set; } = null!;
    }
    
    public enum SubscriptionStatus
    {
        Active,
        Expired,
        Cancelled,
        PaymentFailed,
        Trial
    }
}