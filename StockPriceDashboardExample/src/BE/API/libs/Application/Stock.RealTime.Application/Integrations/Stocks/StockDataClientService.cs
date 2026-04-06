using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OoplesFinance.YahooFinanceAPI;
using OoplesFinance.YahooFinanceAPI.Enums;
using Stock.RealTime.Application.Abstractions.Stocks;
using Stock.RealTime.Application.Responses.Stocks;

namespace Stock.RealTime.Application.Integrations.Stocks
{
    public sealed class StockDataClientService : IStockDataClientService
    {
        private readonly IConfiguration _configuration;
        private readonly HybridCache _hybridCache;
        private readonly ILogger<StockDataClientService> _logger;

        // Cache options: 60s in Redis (L2), 15s in local memory (L1)
        // L1 is shorter so updates propagate across instances within 15s
        private static readonly HybridCacheEntryOptions CacheEntryOptions = new()
        {
            Expiration = TimeSpan.FromSeconds(60),
            LocalCacheExpiration = TimeSpan.FromSeconds(15)
        };

        public StockDataClientService(
                                      IConfiguration configuration,
                                      HybridCache hybridCache,
                                      ILogger<StockDataClientService> logger)
        {
            _configuration = configuration;
            _hybridCache = hybridCache;
            _logger = logger;
        }

        public async Task<StockPriceInfoResponse?> GetStockDataByTickerAsync(string ticker)
        {
            try
            {
                _logger.LogInformation("Attempting to fetch stock data for ticker {Ticker}", ticker);

                // HybridCache: checks L1 (in-memory) → L2 (Redis) → factory (Yahoo API)
                StockPriceInfoResponse? stockPriceInfoResponse = await _hybridCache.GetOrCreateAsync(
                    key: $"StockData_{ticker}",
                    factory: ct => FetchStockDataFromApiAsync(ticker, ct),
                    options: CacheEntryOptions
                );

                if (stockPriceInfoResponse is null)
                {
                    _logger.LogWarning("No stock data found for ticker {Ticker} after API call.", ticker);
                }
                else
                {
                    _logger.LogInformation("Completed fetching stock data for ticker {Ticker} , {@Stock}",
                                           ticker,
                                           stockPriceInfoResponse);
                }

                return stockPriceInfoResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching stock data for ticker {Ticker}", ticker);
            }
            return null;
        }

        public async Task<IEnumerable<StockSearchResponse>> SearchStocksAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return Enumerable.Empty<StockSearchResponse>();

            _logger.LogInformation("Searching for stocks matching query: {Query}", query);

            var searchResults = await _hybridCache.GetOrCreateAsync(
                key: $"StockSearch_{query}",
                factory: ct => FetchStockSearchFromApiAsync(query, ct),
                options: new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) }
            );

            return searchResults ?? Enumerable.Empty<StockSearchResponse>();
        }

        private async ValueTask<IEnumerable<StockSearchResponse>> FetchStockSearchFromApiAsync(string query, CancellationToken ct)
        {
            var yahooClient = new YahooClient();
            var results = await yahooClient.GetAutoCompleteInfoAsync(query);
            
            if (results == null) return Enumerable.Empty<StockSearchResponse>();

            return results.Select(r => new StockSearchResponse
            {
                Ticker = r.Symbol ?? string.Empty,
                Name = r.Name ?? string.Empty,
                Exchange = r.ExchDisp ?? string.Empty,
                Type = r.TypeDisp ?? string.Empty
            });
        }

        private async ValueTask<StockPriceInfoResponse?> FetchStockDataFromApiAsync(string ticker, CancellationToken ct)
        {
            var yahooClient = new YahooClient();
            var tickerPrice = await yahooClient.GetPriceInfoAsync(ticker);
            if (tickerPrice is null ||
                tickerPrice.RegularMarketPrice is null ||
                tickerPrice.RegularMarketPrice.Raw is null)
            {
                _logger.LogWarning("No price information found for ticker {Ticker} from Yahoo Finance API.", ticker);
                return null;
            }
            return new StockPriceInfoResponse
            {
                Ticker = ticker,
                Price = (decimal)tickerPrice.RegularMarketPrice.Raw.Value
            };
        }

        public async Task<IEnumerable<StockPriceInfoResponse>?> GetStockHistoryAsync(string ticker, int days = 7)
        {
            try
            {
                _logger.LogInformation("Fetching historical data for {Ticker} over last {Days} days", ticker, days);

                var history = await _hybridCache.GetOrCreateAsync(
                    key: $"StockHistory_{ticker}_{days}",
                    factory: ct => FetchHistoryFromApiAsync(ticker, days, ct),
                    options: new HybridCacheEntryOptions { Expiration = TimeSpan.FromSeconds(120) }
                );

                return history;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching history for {Ticker}", ticker);
                return null;
            }
        }

        private async ValueTask<IEnumerable<StockPriceInfoResponse>?> FetchHistoryFromApiAsync(string ticker, int days, CancellationToken ct)
        {
            var yahooClient = new YahooClient();
            var startDate = DateTime.UtcNow.AddDays(-days);
            // Daily frequency for a 7 day trend is standard
            var results = await yahooClient.GetHistoricalDataAsync(ticker, DataFrequency.Daily, startDate);
            
            if (results == null) return null;

            return results.Select(r => new StockPriceInfoResponse
            {
                Ticker = ticker,
                Price = (decimal)r.Close,
                Date = r.Date
            });
        }
    }
}