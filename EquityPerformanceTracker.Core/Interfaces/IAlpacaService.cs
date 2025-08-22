using EquityPerformanceTracker.Core.Models;

namespace EquityPerformanceTracker.Core.Interfaces
{
    public interface IAlpacaService : IDisposable
    {
        Task<decimal> GetCurrentPriceAsync(string symbol);
        Task<Dictionary<string, decimal>> GetCurrentPricesAsync(List<string> symbols);
        Task<bool> IsMarketOpenAsync();
        Task<List<MarketHour>> GetMarketHoursAsync(DateTime date);
        Task<string> GetAccountStatusAsync();
        Task<bool> TestConnectionAsync();
    }

    public class MarketHour
    {
        public DateTime Open { get; set; }
        public DateTime Close { get; set; }
        public bool IsOpen { get; set; }
    }
}




//using EquityPerformanceTracker.Core.Models;

//namespace EquityPerformanceTracker.Core.Interfaces
//{
//    public interface IAlpacaService : IDisposable
//    {
//        Task<decimal> GetCurrentPriceAsync(string symbol);
//        Task<Dictionary<string, decimal>> GetCurrentPricesAsync(List<string> symbols);
//        Task<bool> IsMarketOpenAsync();
//        Task<List<MarketHour>> GetMarketHoursAsync(DateTime date);
//        Task<string> GetAccountStatusAsync();
//    }

//    public class MarketHour
//    {
//        public DateTime Open { get; set; }
//        public DateTime Close { get; set; }
//        public bool IsOpen { get; set; }
//    }
//}