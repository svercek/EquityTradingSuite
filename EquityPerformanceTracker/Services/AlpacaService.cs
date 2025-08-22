//AlpacaService.cs
//EquityPerformanceTracker\Services\AlpacaService.cs
using Alpaca.Markets;
using EquityPerformanceTracker.Core.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace EquityPerformanceTracker.Services
{
    public class AlpacaService : IAlpacaService
    {
        private readonly IAlpacaTradingClient _tradingClient;
        private readonly IAlpacaDataClient _dataClient;
        private readonly ILogger<AlpacaService> _logger;
        private readonly IConfiguration _configuration;
        
        // Cache for market status to avoid excessive API calls
        private DateTime _lastMarketStatusCheck = DateTime.MinValue;
        private bool _cachedMarketStatus = false;
        private readonly TimeSpan _marketStatusCacheTime = TimeSpan.FromMinutes(5);

        public AlpacaService(IConfiguration configuration, ILogger<AlpacaService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            try
            {
                // Get configuration values
                var apiKey = _configuration["AlpacaConfig:ApiKey"] ?? throw new InvalidOperationException("Alpaca API Key not configured");
                var secretKey = _configuration["AlpacaConfig:SecretKey"] ?? throw new InvalidOperationException("Alpaca Secret Key not configured");

                // Create credentials
                var credentials = new SecretKey(apiKey, secretKey);

                // Initialize Trading Client for paper trading
                _tradingClient = Alpaca.Markets.Environments.Paper.GetAlpacaTradingClient(credentials);

                // Initialize Data Client for market data
                _dataClient = Alpaca.Markets.Environments.Paper.GetAlpacaDataClient(credentials);

                _logger.LogInformation("Alpaca service initialized successfully with version 7.2.0");

                //This is python from https://colab.research.google.com/drive/1OMKy6qjkcr9QxEHTrmoIDieypxMs76r6#scrollTo=pomYP06rtISz
                //# get historical bars by symbol
                //# ref. https://docs.alpaca.markets/reference/stockbars-1
                //convert to c# 
                //now = datetime.now(ZoneInfo("America/New_York"))
                //req = StockBarsRequest(
                //symbol_or_symbols = ['ALGT'],
                //timeframe = TimeFrame(amount = 1, unit = TimeFrameUnit.Day), # specify timeframe
                //start = datetime(2025, 1, 22, tzinfo = None),         #now - timedelta(days = 5),   # specify start datetime, default=the beginning of the current day.
                //# end_date=Non e,                                        # specify end datetime, default=now
                //limit = 10,                                               # specify limit
                //)
                //stock_historical_data_client.get_stock_bars(req).df

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Alpaca service");
                throw;
            }
        }

        public async Task<decimal> GetCurrentPriceAsync(string symbol)
        {
            try
            {
                _logger.LogDebug("Getting current price for symbol: {Symbol}", symbol);

                // Create the request with the symbol
                var request = new LatestMarketDataRequest(symbol);
                var tradeResponse = await _dataClient.GetLatestTradeAsync(request);
                
                if (tradeResponse != null)
                {
                    var price = tradeResponse.Price;
                    _logger.LogDebug("Retrieved trade price for {Symbol}: ${Price}", symbol, price);
                    return price;
                }

                // Fallback: try getting latest bar data
                var barRequest = new LatestMarketDataRequest(symbol);
                var barResponse = await _dataClient.GetLatestBarAsync(barRequest);
                
                if (barResponse != null)
                {
                    var price = barResponse.Close;
                    _logger.LogDebug("Retrieved bar close price for {Symbol}: ${Price}", symbol, price);
                    return price;
                }

                _logger.LogWarning("No price data found for symbol: {Symbol}", symbol);
                throw new InvalidOperationException($"No price data available for symbol: {symbol}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current price for symbol: {Symbol}", symbol);
                throw;
            }
        }

        public async Task<Dictionary<string, decimal>> GetCurrentPricesAsync(List<string> symbols)
        {
            var prices = new Dictionary<string, decimal>();

            try
            {
                _logger.LogDebug("Getting current prices for {Count} symbols", symbols.Count);

                // Process symbols individually with rate limiting
                foreach (var symbol in symbols)
                {
                    try
                    {
                        var price = await GetCurrentPriceAsync(symbol);
                        prices[symbol] = price;
                        
                        // Rate limiting delay
                        await Task.Delay(100);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to get price for symbol: {Symbol}", symbol);
                        // Set to 0 or skip symbol - your choice
                        prices[symbol] = 0m;
                    }
                }

                _logger.LogDebug("Retrieved prices for {Count}/{Total} symbols", 
                    prices.Count(p => p.Value > 0), symbols.Count);
                return prices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current prices for multiple symbols");
                throw;
            }
        }

        public async Task<bool> IsMarketOpenAsync()
        {
            try
            {
                // Check if we have a cached result that's still valid
                if (DateTime.UtcNow - _lastMarketStatusCheck < _marketStatusCacheTime)
                {
                    return _cachedMarketStatus;
                }

                _logger.LogDebug("Checking market status");

                // Get market clock
                var clock = await _tradingClient.GetClockAsync();
                
                _cachedMarketStatus = clock.IsOpen;
                _lastMarketStatusCheck = DateTime.UtcNow;

                _logger.LogDebug("Market is {Status}", _cachedMarketStatus ? "OPEN" : "CLOSED");
                return _cachedMarketStatus;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking market status");
                
                // Fallback to simple time-based check (EST/EDT business hours)
                var now = DateTime.Now;
                var isWeekday = now.DayOfWeek >= DayOfWeek.Monday && now.DayOfWeek <= DayOfWeek.Friday;
                var isBusinessHours = now.Hour >= 9 && now.Hour < 16;
                
                return isWeekday && isBusinessHours;
            }
        }

        public Task<List<MarketHour>> GetMarketHoursAsync(DateTime date)
        {
            try
            {
                _logger.LogDebug("Getting market hours for date: {Date}", date.ToShortDateString());

                var marketHours = new List<MarketHour>();

                // Check if it's a weekday (Monday-Friday)
                if (date.DayOfWeek >= DayOfWeek.Monday && date.DayOfWeek <= DayOfWeek.Friday)
                {
                    // Check for major US holidays (basic implementation)
                    if (!IsMarketHoliday(date))
                    {
                        marketHours.Add(new MarketHour
                        {
                            Open = date.Date.AddHours(9).AddMinutes(30), // 9:30 AM EST
                            Close = date.Date.AddHours(16), // 4:00 PM EST
                            IsOpen = true
                        });
                    }
                    else
                    {
                        // Market is closed for holiday
                        marketHours.Add(new MarketHour
                        {
                            Open = date.Date,
                            Close = date.Date,
                            IsOpen = false
                        });
                    }
                }
                else
                {
                    // Weekend - market is closed
                    marketHours.Add(new MarketHour
                    {
                        Open = date.Date,
                        Close = date.Date,
                        IsOpen = false
                    });
                }

                _logger.LogDebug("Retrieved market hours for {Date}: {IsOpen}", 
                    date.ToShortDateString(), 
                    marketHours.FirstOrDefault()?.IsOpen ?? false);

                return Task.FromResult(marketHours);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting market hours for date: {Date}", date);
                
                // Fallback to standard market hours
                var fallbackHours = new List<MarketHour>
                {
                    new MarketHour
                    {
                        Open = date.Date.AddHours(9).AddMinutes(30), // 9:30 AM EST
                        Close = date.Date.AddHours(16), // 4:00 PM EST
                        IsOpen = date.DayOfWeek >= DayOfWeek.Monday && date.DayOfWeek <= DayOfWeek.Friday
                    }
                };
                
                return Task.FromResult(fallbackHours);
            }
        }

        // Helper method to check for basic US market holidays
        private bool IsMarketHoliday(DateTime date)
        {
            // Basic holiday check - you can expand this
            var year = date.Year;
            
            // New Year's Day
            if (date.Month == 1 && date.Day == 1) return true;
            
            // Independence Day
            if (date.Month == 7 && date.Day == 4) return true;
            
            // Christmas Day
            if (date.Month == 12 && date.Day == 25) return true;
            
            // Martin Luther King Jr. Day (3rd Monday in January)
            if (date.Month == 1 && date.DayOfWeek == DayOfWeek.Monday && 
                date.Day >= 15 && date.Day <= 21) return true;
            
            // Presidents Day (3rd Monday in February)
            if (date.Month == 2 && date.DayOfWeek == DayOfWeek.Monday && 
                date.Day >= 15 && date.Day <= 21) return true;
            
            // Memorial Day (last Monday in May)
            if (date.Month == 5 && date.DayOfWeek == DayOfWeek.Monday && 
                date.Day >= 25) return true;
            
            // Labor Day (1st Monday in September)
            if (date.Month == 9 && date.DayOfWeek == DayOfWeek.Monday && 
                date.Day <= 7) return true;
            
            // Thanksgiving (4th Thursday in November)
            if (date.Month == 11 && date.DayOfWeek == DayOfWeek.Thursday && 
                date.Day >= 22 && date.Day <= 28) return true;
            
            // Add more holidays as needed
            
            return false;
        }

        // Additional helper method for getting account information
        public async Task<string> GetAccountStatusAsync()
        {
            try
            {
                var account = await _tradingClient.GetAccountAsync();
                return $"Account Status: {account.Status}, Buying Power: ${account.BuyingPower:F2}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account status");
                return "Account status unavailable";
            }
        }

        // Method to test connection
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var account = await _tradingClient.GetAccountAsync();
                _logger.LogInformation("Successfully connected to Alpaca. Account ID: {AccountId}", account.AccountId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Alpaca");
                return false;
            }
        }

        // Clean up resources
        public void Dispose()
        {
            try
            {
                _tradingClient?.Dispose();
                _dataClient?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing Alpaca clients");
            }
        }
    }
}