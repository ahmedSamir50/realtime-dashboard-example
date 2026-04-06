using Stock.RealTime.Application.Responses.Stocks;

namespace Stock.RealTime.Application.Abstractions.Stocks
{
    public interface IStockDataClientService
    {
        public Task<StockPriceInfoResponse?> GetStockDataByTickerAsync(string ticker);
        public Task<IEnumerable<StockPriceInfoResponse>?> GetStockHistoryAsync(string ticker, int days = 7);
        public Task<IEnumerable<StockSearchResponse>> SearchStocksAsync(string query);
    }
}