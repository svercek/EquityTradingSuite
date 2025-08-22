using EquityPerformanceTracker.Core.Interfaces;
using EquityPerformanceTracker.Core.Models;
using EquityPerformanceTracker.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace EquityPerformanceTracker.Services
{
    public class PortfolioService : IPortfolioService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAlpacaService _alpacaService;
        private readonly ILogger<PortfolioService> _logger;

        public PortfolioService(
            ApplicationDbContext context,
            IAlpacaService alpacaService,
            ILogger<PortfolioService> logger)
        {
            _context = context;
            _alpacaService = alpacaService;
            _logger = logger;
        }

        public async Task<Portfolio> CreatePortfolioAsync(Portfolio portfolio)
        {
            portfolio.CreatedDate = DateTime.UtcNow;
            portfolio.LastUpdated = DateTime.UtcNow;

            _context.Portfolios.Add(portfolio);
            await _context.SaveChangesAsync();

            return portfolio;
        }

        public async Task<Portfolio?> GetPortfolioAsync(int id)
        {
            return await _context.Portfolios
                .Include(p => p.Holdings)
                .Include(p => p.PerformanceHistory)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<Portfolio>> GetAllPortfoliosAsync()
        {
            return await _context.Portfolios
                .Include(p => p.Holdings)
                .ToListAsync();
        }

        public async Task<Portfolio> UpdatePortfolioAsync(Portfolio portfolio)
        {
            portfolio.LastUpdated = DateTime.UtcNow;
            _context.Portfolios.Update(portfolio);
            await _context.SaveChangesAsync();
            return portfolio;
        }

        public async Task<bool> DeletePortfolioAsync(int id)
        {
            var portfolio = await _context.Portfolios.FindAsync(id);
            if (portfolio == null) return false;

            _context.Portfolios.Remove(portfolio);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task UpdatePortfolioPricesAsync(int portfolioId)
        {
            var portfolio = await GetPortfolioAsync(portfolioId);
            if (portfolio == null) return;

            var symbols = portfolio.Holdings.Select(h => h.Symbol).ToList();
            var currentPrices = await _alpacaService.GetCurrentPricesAsync(symbols);

            foreach (var holding in portfolio.Holdings)
            {
                if (currentPrices.ContainsKey(holding.Symbol))
                {
                    holding.CurrentPrice = currentPrices[holding.Symbol];
                    holding.LastPriceUpdate = DateTime.UtcNow;
                }
            }

            portfolio.CurrentValue = portfolio.Holdings.Sum(h => h.TotalValue);
            await UpdatePortfolioAsync(portfolio);
        }

        public async Task<PerformanceSnapshot> CreatePerformanceSnapshotAsync(int portfolioId)
        {
            var portfolio = await GetPortfolioAsync(portfolioId);
            if (portfolio == null) throw new ArgumentException("Portfolio not found");

            var snapshot = new PerformanceSnapshot
            {
                PortfolioId = portfolioId,
                SnapshotDate = DateTime.UtcNow,
                PortfolioValue = portfolio.CurrentValue,
                TotalGainLoss = portfolio.CurrentValue - portfolio.InitialValue,
                TotalGainLossPercentage = portfolio.InitialValue != 0
                    ? ((portfolio.CurrentValue - portfolio.InitialValue) / portfolio.InitialValue) * 100
                    : 0
            };

            _context.PerformanceSnapshots.Add(snapshot);
            await _context.SaveChangesAsync();

            return snapshot;
        }

        public async Task<List<Portfolio>> GetUserPortfoliosAsync(string userId)
        {
            try
            {
                return await _context.Portfolios
                    .Where(p => p.UserId == userId)
                    .Include(p => p.Holdings)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting portfolios for user: {UserId}", userId);
                return new List<Portfolio>();
            }
        }

        public async Task<PortfolioHolding> AddHoldingAsync(PortfolioHolding holding)
        {
            try
            {
                holding.CompanyName = await GetCompanyNameAsync(holding.Symbol);
                _context.PortfolioHoldings.Add(holding);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Added holding {Symbol} to portfolio {PortfolioId}", holding.Symbol, holding.PortfolioId);
                return holding;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding holding {Symbol} to portfolio {PortfolioId}", holding.Symbol, holding.PortfolioId);
                throw;
            }
        }

        private async Task<string> GetCompanyNameAsync(string symbol)
        {
            // Simple company name mapping - you can enhance this later
            var companyNames = new Dictionary<string, string>
            {
                { "AAPL", "Apple Inc." },
                { "MSFT", "Microsoft Corporation" },
                { "GOOGL", "Alphabet Inc." },
                { "AMZN", "Amazon.com Inc." },
                { "TSLA", "Tesla Inc." }
            };

            return companyNames.GetValueOrDefault(symbol, $"{symbol} Corporation");
        }
        public async Task<PortfolioHolding?> GetHoldingAsync(int holdingId)
        {
            try
            {
                return await _context.PortfolioHoldings
                    .Include(h => h.Portfolio)
                    .FirstOrDefaultAsync(h => h.Id == holdingId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting holding {HoldingId}", holdingId);
                return null;
            }
        }

        public async Task<PortfolioHolding> UpdateHoldingAsync(PortfolioHolding holding)
        {
            try
            {
                holding.LastPriceUpdate = DateTime.UtcNow;
                _context.PortfolioHoldings.Update(holding);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated holding {HoldingId}", holding.Id);
                return holding;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating holding {HoldingId}", holding.Id);
                throw;
            }
        }

        public async Task<bool> DeleteHoldingAsync(int holdingId)
        {
            try
            {
                var holding = await _context.PortfolioHoldings.FindAsync(holdingId);
                if (holding == null) return false;

                _context.PortfolioHoldings.Remove(holding);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted holding {HoldingId}", holdingId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting holding {HoldingId}", holdingId);
                return false;
            }
        }

            public async Task<List<Transaction>> GetPortfolioTransactionsAsync(int portfolioId)
        {
            try
            {
                return await _context.Transactions
                    .Where(t => t.PortfolioId == portfolioId)
                    .Include(t => t.Holding)
                    .OrderByDescending(t => t.TransactionDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transactions for portfolio {PortfolioId}", portfolioId);
                return new List<Transaction>();
            }
        }

        public async Task<Transaction> CreateSellTransactionAsync(Transaction transaction)
        {
            try
            {
                // Validate we can sell this many shares
                var holding = await _context.PortfolioHoldings.FindAsync(transaction.HoldingId);
                if (holding == null)
                    throw new ArgumentException("Holding not found");

                var soldShares = await _context.Transactions
                    .Where(t => t.HoldingId == transaction.HoldingId)
                    .SumAsync(t => t.Shares);

                if (soldShares + transaction.Shares > holding.Shares)
                    throw new InvalidOperationException("Cannot sell more shares than owned");

                // Create the transaction
                transaction.CreatedDate = DateTime.UtcNow;
                transaction.Type = TransactionType.Sell;
                transaction.Symbol = holding.Symbol;
                transaction.CompanyName = holding.CompanyName;

                _context.Transactions.Add(transaction);

                // Update holding's sold shares count
                holding.SharesSold += transaction.Shares;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Created sell transaction for {Shares} shares of {Symbol}",
                    transaction.Shares, transaction.Symbol);

                return transaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sell transaction");
                throw;
            }
        }
        public async Task<bool> CanSellShares(int holdingId, int sharesToSell)
        {
            try
            {
                var holding = await _context.PortfolioHoldings.FindAsync(holdingId);
                if (holding == null) return false;

                var soldShares = await _context.Transactions
                    .Where(t => t.HoldingId == holdingId)
                    .SumAsync(t => t.Shares);

                return (soldShares + sharesToSell) <= holding.Shares;
            }
            catch
            {
                return false;
            }
        }
        public async Task<Transaction?> GetTransactionAsync(int transactionId)
        {
            try
            {
                return await _context.Transactions
            .Include(t => t.Portfolio)
            .Include(t => t.Holding)
            .FirstOrDefaultAsync(t => t.Id == transactionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction {TransactionId}", transactionId);
                return null;
            }
        }

        public async Task<Transaction> UpdateTransactionAsync(Transaction transaction)
        {
            try
            {
                // Get original transaction to calculate share difference
                var originalTransaction = await _context.Transactions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == transaction.Id);

                if (originalTransaction == null)
                    throw new ArgumentException("Transaction not found");

                // Update the transaction
                _context.Transactions.Update(transaction);

                // Update the holding's SharesSold count
                var holding = await _context.PortfolioHoldings.FindAsync(transaction.HoldingId);
                if (holding != null)
                {
                    // Adjust for the difference in shares
                    holding.SharesSold = holding.SharesSold - originalTransaction.Shares + transaction.Shares;

                    // Validate we're not overselling
                    if (holding.SharesSold > holding.Shares)
                    {
                        throw new InvalidOperationException("Cannot sell more shares than owned");
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated transaction {TransactionId}", transaction.Id);
                return transaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating transaction {TransactionId}", transaction.Id);
                throw;
            }
        }

        public async Task<bool> DeleteTransactionAsync(int transactionId)
        {
            try
            {
                var transaction = await _context.Transactions
                    .Include(t => t.Holding)
                    .FirstOrDefaultAsync(t => t.Id == transactionId);

                if (transaction == null) return false;

                // Update the holding's SharesSold count
                if (transaction.Holding != null)
                {
                    transaction.Holding.SharesSold -= transaction.Shares;
                }

                _context.Transactions.Remove(transaction);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted transaction {TransactionId}", transactionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting transaction {TransactionId}", transactionId);
                return false;
            }
        }
    }
}