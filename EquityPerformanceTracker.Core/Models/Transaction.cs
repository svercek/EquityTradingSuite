using System.ComponentModel.DataAnnotations;

namespace EquityPerformanceTracker.Core.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        
        [Required]
        public int PortfolioId { get; set; }
        
        [Required]
        public int HoldingId { get; set; }  // Links to the original holding
        
        [Required]
        [MaxLength(10)]
        public string Symbol { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;
        
        [Required]
        public TransactionType Type { get; set; } = TransactionType.Sell;
        
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Shares must be greater than 0")]
        public int Shares { get; set; }
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }  // Selling price per share
        
        [Required]
        public DateTime TransactionDate { get; set; }
        
        public DateTime CreatedDate { get; set; }
        
        [MaxLength(500)]
        public string Notes { get; set; } = string.Empty;
        
        // Navigation Properties
        public virtual Portfolio Portfolio { get; set; } = null!;
        public virtual PortfolioHolding Holding { get; set; } = null!;
        
        // Calculated Properties
        public decimal TotalValue => Shares * Price;
        public decimal GainLoss => (Price - Holding?.PurchasePrice ?? 0) * Shares;
        public decimal GainLossPercentage => Holding?.PurchasePrice > 0 ? 
            ((Price - Holding.PurchasePrice) / Holding.PurchasePrice) * 100 : 0;
  
        // Add this calculated property to your Transaction class
        public decimal PurchasePrice => Holding?.PurchasePrice ?? 0;
    }
    
    public enum TransactionType
    {
        Sell = 1
        // Future: Buy = 2, Dividend = 3, Split = 4
    }
}