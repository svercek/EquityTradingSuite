using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EquityPerformanceTracker.Core.Models
{
    public class Portfolio
    {
        public int Id { get; set; }

      
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Portfolio name is required")]
        [StringLength(100, ErrorMessage = "Portfolio name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Initial value is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Initial value must be greater than 0")]
        public decimal InitialValue { get; set; }

        public decimal CurrentValue { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastUpdated { get; set; }

        // Navigation Properties
        public virtual List<PortfolioHolding> Holdings { get; set; } = new();
        public virtual List<PerformanceSnapshot> PerformanceHistory { get; set; } = new();

        // Calculated Properties
        public decimal TotalGainLoss => CurrentValue - InitialValue;
        public decimal GainLossPercentage => InitialValue != 0 ? (TotalGainLoss / InitialValue) * 100 : 0;
        // Add these calculated properties to Portfolio.cs
        [NotMapped]
        public decimal TotalInvested => Holdings.Sum(h => h.Shares * h.PurchasePrice);

        [NotMapped]
        public decimal UnrealizedGains => Holdings.Sum(h => h.GainLoss);

        [NotMapped]
        public decimal ActualTotalGainLoss => CurrentValue - TotalInvested;

        [NotMapped]
        public decimal ActualGainLossPercentage => TotalInvested > 0 ?
            (ActualTotalGainLoss / TotalInvested) * 100 : 0;
    }
}