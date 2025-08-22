namespace EquityPerformanceTracker.Core.Models
{
    public class PerformanceSnapshot
    {
        public int Id { get; set; }
        public int PortfolioId { get; set; }
        public DateTime SnapshotDate { get; set; }
        public decimal PortfolioValue { get; set; }
        public decimal DayChange { get; set; }
        public decimal DayChangePercentage { get; set; }
        public decimal TotalGainLoss { get; set; }
        public decimal TotalGainLossPercentage { get; set; }

        // Navigation Properties
        public virtual Portfolio Portfolio { get; set; } = null!;
    }
}
