using Microsoft.AspNetCore.Mvc;
using EquityPerformanceTracker.Core.Interfaces;

namespace EquityPerformanceTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AlpacaTestController : ControllerBase
    {
        private readonly IAlpacaService _alpacaService;

        public AlpacaTestController(IAlpacaService alpacaService)
        {
            _alpacaService = alpacaService;
        }

        [HttpGet("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                var connected = await _alpacaService.TestConnectionAsync();
                return Ok(new { Connected = connected, Message = "Alpaca connection test completed" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("price/{symbol}")]
        public async Task<IActionResult> GetPrice(string symbol)
        {
            try
            {
                var price = await _alpacaService.GetCurrentPriceAsync(symbol.ToUpper());
                return Ok(new { Symbol = symbol.ToUpper(), Price = price, Timestamp = DateTime.Now });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("market-status")]
        public async Task<IActionResult> GetMarketStatus()
        {
            try
            {
                var isOpen = await _alpacaService.IsMarketOpenAsync();
                return Ok(new { IsOpen = isOpen, CheckedAt = DateTime.Now });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("account")]
        public async Task<IActionResult> GetAccountStatus()
        {
            try
            {
                var status = await _alpacaService.GetAccountStatusAsync();
                return Ok(new { Status = status });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}