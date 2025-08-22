using EquityPerformanceTracker.Core.Models;

namespace EquityPerformanceTracker.Core.Interfaces
{
    public interface IPortfolioService
    {
        Task<Portfolio> CreatePortfolioAsync(Portfolio portfolio);
        Task<Portfolio?> GetPortfolioAsync(int id);
        Task<List<Portfolio>> GetAllPortfoliosAsync();
        Task<Portfolio> UpdatePortfolioAsync(Portfolio portfolio);
        Task<bool> DeletePortfolioAsync(int id);
        Task UpdatePortfolioPricesAsync(int portfolioId);
        Task<PerformanceSnapshot> CreatePerformanceSnapshotAsync(int portfolioId);
        Task<List<Portfolio>> GetUserPortfoliosAsync(string userId);
        Task<PortfolioHolding> AddHoldingAsync(PortfolioHolding holding);
        Task<PortfolioHolding?> GetHoldingAsync(int holdingId);
        Task<PortfolioHolding> UpdateHoldingAsync(PortfolioHolding holding);
        Task<bool> DeleteHoldingAsync(int holdingId);
        Task<List<Transaction>> GetPortfolioTransactionsAsync(int portfolioId);
        Task<Transaction> CreateSellTransactionAsync(Transaction transaction);
        Task<bool> CanSellShares(int holdingId, int sharesToSell);
        Task<Transaction?> GetTransactionAsync(int transactionId);
        Task<Transaction> UpdateTransactionAsync(Transaction transaction);
        Task<bool> DeleteTransactionAsync(int transactionId);
    }
}