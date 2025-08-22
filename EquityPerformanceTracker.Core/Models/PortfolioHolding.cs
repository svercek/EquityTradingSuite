using System.ComponentModel.DataAnnotations;

namespace EquityPerformanceTracker.Core.Models
{
    public class PortfolioHolding
    {
        public int Id { get; set; }
        public int PortfolioId { get; set; }
	public int SharesSold { get; set; } = 0;

        [Required]
        [MaxLength(10)]
        public string Symbol { get; set; } = string.Empty;

        [MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        public int Shares { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public DateTime PurchaseDate { get; set; }
        public DateTime LastPriceUpdate { get; set; }
	
        // Navigation Properties
        public virtual Portfolio Portfolio { get; set; } = null!;

        // Calculated Properties
 // Update TotalValue to use remaining shares
	    public decimal TotalValue => RemainingShares * CurrentPrice;
	    public decimal TotalCost => RemainingShares * PurchasePrice;
        public decimal GainLoss => TotalValue - TotalCost;
        public decimal GainLossPercentage => TotalCost != 0 ? (GainLoss / TotalCost) * 100 : 0;
	    public int RemainingShares => Shares - SharesSold;
    }
}