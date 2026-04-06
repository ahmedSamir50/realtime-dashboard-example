using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OoplesFinance.YahooFinanceAPI;
using OoplesFinance.YahooFinanceAPI.Enums;
using Stock.RealTime.Application.Abstractions.Stocks;
using Stock.RealTime.Application.Responses.Stocks;

namespace Stock.RealTime.Infrastructure.Integrations
{
    public sealed class ExternalStockClient : IStockDataClientService
    {
        private readonly ILogger<ExternalStockClient> _logger;

        public ExternalStockClient(ILogger<ExternalStockClient> logger)
        {
            _logger = logger;
        }

        public async Task<StockPriceInfoResponse?> GetStockDataByTickerAsync(string ticker)
        {
            try
            {
                var yahooClient = new YahooClient();
                var results = await yahooClient.GetPriceInfoAsync(ticker);

                if (results != null)
                {
                    return new StockPriceInfoResponse
                    {
                        Ticker = ticker,
                        Price = (decimal)(results.RegularMarketPrice is null || 
                                            results.RegularMarketPrice.Raw is null ? 
                                            0.0m : (decimal)results.RegularMarketPrice.Raw.Value),
                        Date = DateTime.UtcNow
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching data for {Ticker} from Yahoo Finance", ticker);
                return null;
            }
        }

        public async Task<IEnumerable<StockPriceInfoResponse>?> GetStockHistoryAsync(string ticker, int days)
        {
            try
            {
                var yahooClient = new YahooClient();
                // Determine interval based on days for efficiency
                var interval = days <= 30 ? OoplesFinance.YahooFinanceAPI.Enums.DataFrequency.Daily : OoplesFinance.YahooFinanceAPI.Enums.DataFrequency.Weekly;
                var startDate = DateTime.UtcNow.AddDays(-days);

                var history = await yahooClient.GetHistoricalDataAsync(ticker, interval, startDate);

                if (history != null)
                {
                    return history.Select(h => new StockPriceInfoResponse
                    {
                        Ticker = ticker,
                        Price = (decimal)h.Close,
                        Date = h.Date
                    }).OrderByDescending(h => h.Date);
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching history for {Ticker} from Yahoo Finance", ticker);
                return null;
            }
        }

        public async Task<IEnumerable<StockSearchResponse>?> SearchStocksAsync(string query)
        {
            try
            {
                var yahooClient = new YahooClient();
                var results = await yahooClient.GetAutoCompleteInfoAsync(query);

                if (results != null && results.Any())
                {
                    return results.Select(q => new StockSearchResponse
                    {
                        Ticker = q.Symbol,
                        Exchange = q.Exch ?? "Unknown",
                        Name = q.Name ?? "Unknown",
                        Type = q.Type ?? "Unknown",
                    });
                }
                return Enumerable.Empty<StockSearchResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for {Query} in Yahoo Finance", query);
                return Enumerable.Empty<StockSearchResponse>();
            }
        }
    }
}
