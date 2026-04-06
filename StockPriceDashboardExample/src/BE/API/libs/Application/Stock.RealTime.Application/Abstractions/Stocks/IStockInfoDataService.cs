using Stock.RealTime.Application.Responses.Stocks;

namespace Stock.RealTime.Application.Abstractions.Stocks
{
    public interface IStockInfoDataService
    {
        public Task<StockPriceInfoResponse?> GetLatestStockPriceAsync(string ticker);
        public Task<IEnumerable<StockPriceInfoResponse>?> GetStockHistoryAsync(string ticker, int days = 7);
    }
}
