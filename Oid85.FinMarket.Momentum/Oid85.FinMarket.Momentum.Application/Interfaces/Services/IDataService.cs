using Oid85.FinMarket.Momentum.Core.Models;

namespace Oid85.FinMarket.Momentum.Application.Interfaces.Services
{
    public interface IDataService
    {
        Task<Dictionary<string, List<Candle>>> GetCandleDataAsync(List<string> tickers);
        Task<Dictionary<string, Instrument>> GetInstrumentDataAsync(List<string> tickers);
        double? GetPrice(string ticker, DateOnly date);
        double? GetLowPrice(string ticker, DateOnly date);
        Task<List<Candle>> GetMoneyEquivalentDataAsync(DateOnly from, DateOnly to);
    }
}
