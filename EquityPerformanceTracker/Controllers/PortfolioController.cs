using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EquityPerformanceTracker.Core.Interfaces;
using EquityPerformanceTracker.Core.Models;
using System.Security.Claims;

namespace EquityPerformanceTracker.Controllers
{
    [Authorize]
    public class PortfolioController : Controller
    {
        private readonly IPortfolioService _portfolioService;
        private readonly IAlpacaService _alpacaService;

        public PortfolioController(IPortfolioService portfolioService, IAlpacaService alpacaService)
        {
            _portfolioService = portfolioService;
            _alpacaService = alpacaService;
        }

        // GET: /Portfolio
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var portfolios = await _portfolioService.GetUserPortfoliosAsync(userId!);
            return View(portfolios);
        }

        // GET: /Portfolio/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Portfolio/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Portfolio portfolio)
        {
            try
            {
                // Check authentication first
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["Error"] = "You must be logged in to create a portfolio.";
                    return RedirectToAction("Login", "Account", new { area = "Identity" });
                }

                if (ModelState.IsValid)
                {
                    // Set portfolio properties
                    portfolio.UserId = userId;
                    portfolio.CreatedDate = DateTime.UtcNow;
                    portfolio.LastUpdated = DateTime.UtcNow;
                    portfolio.CurrentValue = portfolio.InitialValue;

                    // Create portfolio
                    var createdPortfolio = await _portfolioService.CreatePortfolioAsync(portfolio);

                    TempData["Success"] = $"Portfolio '{portfolio.Name}' created successfully!";
                    return RedirectToAction("Index");
                }
                else
                {
                    // Log validation errors for debugging
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    TempData["Error"] = "Please fix the following errors: " + string.Join(", ", errors);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating portfolio: {ex.Message}";
            }

            return View(portfolio);
        }

        // GET: /Portfolio/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var portfolio = await _portfolioService.GetPortfolioAsync(id);
            
            if (portfolio == null || portfolio.UserId != userId)
            {
                return NotFound();
            }

            // Load transactions for this portfolio
            var transactions = await _portfolioService.GetPortfolioTransactionsAsync(id);
            ViewBag.Transactions = transactions;

            return View(portfolio);
        }

        // POST: /Portfolio/AddHolding/5
        [HttpPost]
        public async Task<IActionResult> AddHolding(int portfolioId, string symbol, int shares, decimal purchasePrice)
        {
            try
            {
                var holding = new PortfolioHolding
                {
                    PortfolioId = portfolioId,
                    Symbol = symbol.ToUpper(),
                    Shares = shares,
                    PurchasePrice = purchasePrice,
                    CurrentPrice = purchasePrice, // Will be updated by API
                    PurchaseDate = DateTime.Now,
                    LastPriceUpdate = DateTime.Now
                };

                await _portfolioService.AddHoldingAsync(holding);
                
                // Update prices
                await _portfolioService.UpdatePortfolioPricesAsync(portfolioId);
                
                return RedirectToAction(nameof(Details), new { id = portfolioId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id = portfolioId });
            }
        }
        [HttpPost]
        public async Task<IActionResult> UpdatePrices(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var portfolio = await _portfolioService.GetPortfolioAsync(id);

                if (portfolio == null || portfolio.UserId != userId)
                {
                    return Json(new { success = false, message = "Portfolio not found" });
                }

                await _portfolioService.UpdatePortfolioPricesAsync(id);
                return Json(new { success = true, message = "Prices updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> TestCreate()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userName = User.Identity?.Name;

                return Json(new
                {
                    UserId = userId,
                    UserName = userName,
                    IsAuthenticated = User.Identity?.IsAuthenticated,
                    Claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
                });
            }
            catch (Exception ex)
            {
                return Json(new { Error = ex.Message });
            }
        }
        // GET: /Portfolio/EditHolding/5
        public async Task<IActionResult> EditHolding(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var holding = await _portfolioService.GetHoldingAsync(id);

            if (holding == null || holding.Portfolio.UserId != userId)
            {
                return NotFound();
            }

            return View(holding);
        }

        // POST: /Portfolio/EditHolding/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditHolding(PortfolioHolding holding)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var existingHolding = await _portfolioService.GetHoldingAsync(holding.Id);

                if (existingHolding == null || existingHolding.Portfolio.UserId != userId)
                {
                    return NotFound();
                }

                // Update only the editable fields
                existingHolding.Symbol = holding.Symbol.ToUpper();
                existingHolding.Shares = holding.Shares;
                existingHolding.PurchasePrice = holding.PurchasePrice;
                existingHolding.PurchaseDate = holding.PurchaseDate;
                existingHolding.CompanyName = await GetCompanyNameAsync(holding.Symbol);

                await _portfolioService.UpdateHoldingAsync(existingHolding);

                TempData["Success"] = "Holding updated successfully!";
                return RedirectToAction("Details", new { id = existingHolding.PortfolioId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating holding: {ex.Message}";
                return View(holding);
            }
        }

        // POST: /Portfolio/DeleteHolding/5
        [HttpPost]
        public async Task<IActionResult> DeleteHolding(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var holding = await _portfolioService.GetHoldingAsync(id);

                if (holding == null || holding.Portfolio.UserId != userId)
                {
                    return Json(new { success = false, message = "Holding not found" });
                }

                var portfolioId = holding.PortfolioId;
                var success = await _portfolioService.DeleteHoldingAsync(id);

                if (success)
                {
                    // Update portfolio value after deletion
                    await _portfolioService.UpdatePortfolioPricesAsync(portfolioId);

                    return Json(new { success = true, message = "Holding deleted successfully" });
                }

                return Json(new { success = false, message = "Failed to delete holding" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> SellShares([FromBody] SellSharesRequest request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var holding = await _portfolioService.GetHoldingAsync(request.HoldingId);

                if (holding == null)
                {
                    return Json(new { success = false, message = "Holding not found" });
                }

                if (holding.Portfolio.UserId != userId)
                {
                    return Json(new { success = false, message = "Access denied" });
                }

                // Validate shares
                if (!await _portfolioService.CanSellShares(request.HoldingId, request.Shares))
                {
                    return Json(new { success = false, message = "Cannot sell more shares than owned" });
                }

                var transaction = new Transaction
                {
                    PortfolioId = holding.PortfolioId,
                    HoldingId = request.HoldingId,
                    Shares = request.Shares,
                    Price = request.Price,
                    TransactionDate = request.SaleDate,
                    Notes = request.Notes ?? ""
                };

                await _portfolioService.CreateSellTransactionAsync(transaction);

                return Json(new { success = true, message = "Shares sold successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Add this request class to help with model binding
        public class SellSharesRequest
        {
            public int HoldingId { get; set; }
            public int Shares { get; set; }
            public decimal Price { get; set; }
            public DateTime SaleDate { get; set; }
            public string? Notes { get; set; }
        }

        // GET: Edit Transaction
        public async Task<IActionResult> EditTransaction(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var transaction = await _portfolioService.GetTransactionAsync(id);

            if (transaction == null || transaction.Portfolio.UserId != userId)
            {
                return NotFound();
            }

            return View(transaction);
        }

        // POST: Edit Transaction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTransaction(Transaction transaction)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var existingTransaction = await _portfolioService.GetTransactionAsync(transaction.Id);

                if (existingTransaction == null || existingTransaction.Portfolio.UserId != userId)
                {
                    return NotFound();
                }

                // Update editable fields
                existingTransaction.Shares = transaction.Shares;
                existingTransaction.Price = transaction.Price;
                existingTransaction.TransactionDate = transaction.TransactionDate;
                existingTransaction.Notes = transaction.Notes;

                await _portfolioService.UpdateTransactionAsync(existingTransaction);

                TempData["Success"] = "Transaction updated successfully!";
                return RedirectToAction("Details", new { id = existingTransaction.PortfolioId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating transaction: {ex.Message}";
                return View(transaction);
            }
        }

        // POST: Delete Transaction
        [HttpPost]
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var transaction = await _portfolioService.GetTransactionAsync(id);

                if (transaction == null || transaction.Portfolio.UserId != userId)
                {
                    return Json(new { success = false, message = "Transaction not found" });
                }

                var portfolioId = transaction.PortfolioId;
                var success = await _portfolioService.DeleteTransactionAsync(id);

                if (success)
                {
                    return Json(new { success = true, message = "Transaction deleted successfully" });
                }

                return Json(new { success = false, message = "Failed to delete transaction" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task<string> GetCompanyNameAsync(string symbol)
        {
            var companyNames = new Dictionary<string, string>
            {
                { "AAPL", "Apple Inc." },
                { "MSFT", "Microsoft Corporation" },
                { "GOOGL", "Alphabet Inc." },
                { "AMZN", "Amazon.com Inc." },
                { "TSLA", "Tesla Inc." },
                { "META", "Meta Platforms Inc." },
                { "NVDA", "NVIDIA Corporation" },
                { "NFLX", "Netflix Inc." }
            };

            return companyNames.GetValueOrDefault(symbol, $"{symbol} Corporation");
        }
    }




}