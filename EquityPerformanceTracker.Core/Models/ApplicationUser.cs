using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EquityPerformanceTracker.Core.Models
{
    public class ApplicationUser : IdentityUser
    {
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;
        
        public DateTime CreatedDate { get; set; }
        public DateTime LastLoginDate { get; set; }
        
        // Subscription Information
        public bool IsSubscribed { get; set; }
        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
        public DateTime? TrialEndDate { get; set; }
        public bool IsTrialActive { get; set; }
        
        // Computed Properties (NOT mapped to database)
        [NotMapped]
        public bool HasActiveSubscription => IsSubscribed && SubscriptionEndDate > DateTime.UtcNow;
        
        [NotMapped]
        public bool HasActiveTrial => IsTrialActive && TrialEndDate > DateTime.UtcNow;
        
        [NotMapped]
        public bool CanAccessService => HasActiveSubscription || HasActiveTrial;
        
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}