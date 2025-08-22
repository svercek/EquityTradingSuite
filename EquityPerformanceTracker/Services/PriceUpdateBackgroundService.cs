using EquityPerformanceTracker.Core.Interfaces;

namespace EquityPerformanceTracker.Services
{
    public class PriceUpdateBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PriceUpdateBackgroundService> _logger;
        private readonly TimeSpan _updateInterval = TimeSpan.FromMinutes(15); // Update every 15 minutes

        public PriceUpdateBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<PriceUpdateBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Price Update Background Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdateAllPortfolioPricesAsync();
                    await Task.Delay(_updateInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Price Update Background Service is stopping");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in Price Update Background Service");
                    // Wait a bit before retrying
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
        }

        private async Task UpdateAllPortfolioPricesAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var portfolioService = scope.ServiceProvider.GetRequiredService<IPortfolioService>();
            var alpacaService = scope.ServiceProvider.GetRequiredService<IAlpacaService>();

            try
            {
                // Only update during market hours or slightly after
                var isMarketOpen = await alpacaService.IsMarketOpenAsync();
                var currentHour = DateTime.Now.Hour;
                var shouldUpdate = isMarketOpen || (currentHour >= 16 && currentHour <= 18); // Market hours + 2 hours after close

                if (!shouldUpdate)
                {
                    _logger.LogDebug("Skipping price update - market is closed");
                    return;
                }

                _logger.LogInformation("Starting automated price update for all portfolios");

                var portfolios = await portfolioService.GetAllPortfoliosAsync();
                var updateTasks = portfolios.Select(async portfolio =>
                {
                    try
                    {
                        await portfolioService.UpdatePortfolioPricesAsync(portfolio.Id);
                        _logger.LogDebug("Updated prices for portfolio: {PortfolioId}", portfolio.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to update prices for portfolio: {PortfolioId}", portfolio.Id);
                    }
                });

                await Task.WhenAll(updateTasks);
                _logger.LogInformation("Completed automated price update for {Count} portfolios", portfolios.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during automated price update");
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Price Update Background Service is stopping");
            await base.StopAsync(stoppingToken);
        }
    }
}