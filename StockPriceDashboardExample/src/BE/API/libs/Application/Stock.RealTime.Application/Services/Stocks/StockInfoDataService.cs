using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using Stock.RealTime.Application.Abstractions.RealTime;
using Stock.RealTime.Application.Abstractions.Stocks;
using Stock.RealTime.Application.Responses.Stocks;
using Stock.RealTime.Domain.Stocks;

namespace Stock.RealTime.Application.Services.Stocks
{
    public sealed class StockInfoDataService : IStockInfoDataService
    {
        #region Fields
        private readonly NpgsqlDataSource _dataSource;
        private readonly IStockDataClientService _stockDataClientService;
        private readonly ILogger<StockInfoDataService> _logger;
        private readonly IActiveTickerManagerService _activeTickerManagerService;

        #endregion Fields

        #region CTOR

        public StockInfoDataService(NpgsqlDataSource dataSource,
                                    IStockDataClientService stockDataClientService,
                                    ILogger<StockInfoDataService> logger,
                                    IActiveTickerManagerService activeTickerManagerService)
        {
            _dataSource = dataSource;
            this._stockDataClientService = stockDataClientService;
            _logger = logger;
            this._activeTickerManagerService = activeTickerManagerService;
        }

        #endregion CTOR


        public async Task<IEnumerable<StockPriceInfoResponse>?> GetStockHistoryAsync(string ticker, int days = 7)
        {
            try
            {
                var historyFromDb = (await GetStockHistoryFromDatabase(ticker, days))?.ToList();
                
                if (historyFromDb != null && historyFromDb.Any())
                {
                    var earliest = historyFromDb.MinBy(h => h.Date);
                    var latest = historyFromDb.MaxBy(h => h.Date);
                    
                    // Requirement: 
                    // 1. We must have data going back at least 'days' ago
                    // 2. The latest data point should be fresh (less than 2 mins as per latest cache logic)
                    var hasEnoughHistory = earliest?.Date <= DateTime.UtcNow.AddDays(-days).AddHours(1); // Allow 1h buffer
                    var isLatestFresh = latest?.Date >= DateTime.UtcNow.AddMinutes(-2);

                    if (hasEnoughHistory && isLatestFresh)
                    {
                        _logger.LogInformation("Returning complete cached history from DB for {Ticker}", ticker);
                        return historyFromDb;
                    }
                }

                _logger.LogInformation("History coverage insufficient for {Ticker} ({Days} days), fetching full trend from API", ticker, days);
                var historyFromApi = await _stockDataClientService.GetStockHistoryAsync(ticker, days);
                
                if (historyFromApi != null)
                {
                    await SaveHistoryToDatabaseAsync(ticker, historyFromApi);
                    return historyFromApi;
                }

                return historyFromDb; 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching stock history for ticker {Ticker}", ticker);
                return null;
            }
        }

        private async Task<IEnumerable<StockPriceInfoResponse>> GetStockHistoryFromDatabase(string ticker, int days)
        {
            try
            {
                using Npgsql.NpgsqlConnection dbConn = await _dataSource.OpenConnectionAsync();
                var sql = """
                    SELECT ticker as Ticker, price as Price, timestamp as Date
                    FROM public.stock_prices 
                    WHERE ticker = @ticker AND timestamp >= @since
                    ORDER BY timestamp ASC
                    """;
                
                var results = await dbConn.QueryAsync<StockPriceInfoResponse>(sql, new { 
                    ticker, 
                    since = DateTime.UtcNow.AddDays(-days) 
                });
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading history from DB for {Ticker}", ticker);
                return Enumerable.Empty<StockPriceInfoResponse>();
            }
        }

        private async Task SaveHistoryToDatabaseAsync(string ticker, IEnumerable<StockPriceInfoResponse> history)
        {
            try
            {
                // We'll use a transaction to ensure all points are saved
                await using var connection = await _dataSource.OpenConnectionAsync();
                await using var transaction = await connection.BeginTransactionAsync();

                foreach (var point in history)
                {
                    // For history, we only save if we don't have a record for that specific date (simplified)
                    // Or we just insert them all if they are from different hours/days
                    var cmd = connection.CreateCommand();
                    cmd.CommandText = """
                        INSERT INTO public.stock_prices (ticker, price, timestamp)
                        SELECT @ticker, @price, @timestamp
                        WHERE NOT EXISTS (
                            SELECT 1 FROM public.stock_prices 
                            WHERE ticker = @ticker AND timestamp = @timestamp
                        )
                        """;
                    cmd.Parameters.AddWithValue("ticker", ticker);
                    cmd.Parameters.AddWithValue("price", point.Price);
                    cmd.Parameters.AddWithValue("timestamp", point.Date ?? DateTime.UtcNow);
                    await cmd.ExecuteNonQueryAsync();
                }
                
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving history to DB for {Ticker}", ticker);
            }
        }

        public async Task<StockPriceInfoResponse?> GetLatestStockPriceAsync(string ticker)
        {
            try
            {
                StockPriceInfoResponse? priceFromDb = await GetStockDataFromDatabase(ticker);
                if (priceFromDb is not null)
                {
                    _activeTickerManagerService.AddTicker(ticker);
                    return priceFromDb;
                }

                 priceFromDb = await _stockDataClientService.GetStockDataByTickerAsync(ticker);

                if (priceFromDb is not null)
                {
                    await SaveStockPriceToDatabaseAsync(ticker, priceFromDb.Price);
                    _activeTickerManagerService.AddTicker(ticker);
                    return priceFromDb;
                }
                else
                {
                    _logger.LogWarning("Stock data for ticker {Ticker} not found in external API", ticker);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching latest stock price for ticker {Ticker}", ticker);
                return null;
            }
        }

        private async Task<StockPriceInfoResponse?> GetStockDataFromDatabase(string ticker)
        {
            try
            {
                await using var connection = await _dataSource.OpenConnectionAsync();
                var command = connection.CreateCommand();
                command.CommandText = """
                    SELECT ticker , price, timestamp 
                    FROM public.stock_prices 
                    WHERE ticker = @ticker 
                    ORDER BY timestamp DESC LIMIT 1
                    """;
                    
                using Npgsql.NpgsqlConnection dbConn = await _dataSource.OpenConnectionAsync();
                StockPrice result = await dbConn.QueryFirstOrDefaultAsync<StockPrice>(command.CommandText, new { ticker });
                if (result is not null) { 
                    return new StockPriceInfoResponse
                    {
                        Ticker = result.Ticker,
                        Price = result.Price
                    };
                }

                // not found in database, fetch from external API
                var apiResult = await _stockDataClientService.GetStockDataByTickerAsync(ticker);
                if(apiResult is  null)
                   _logger.LogWarning("Stock data for ticker {Ticker} not found in external API", ticker);

                await SaveStockPriceToDatabaseAsync(ticker, apiResult?.Price ?? 0);

                return apiResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching stock data from database for ticker {Ticker}", ticker);
            }
            return null;
        }

        private async Task SaveStockPriceToDatabaseAsync(string ticker, decimal v)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO public.stock_prices (ticker, price, timestamp) 
                VALUES (@ticker, @price, @timestamp)
                """;
            command.Parameters.AddWithValue("ticker", ticker);
            command.Parameters.AddWithValue("price", v);
            command.Parameters.AddWithValue("timestamp", DateTime.UtcNow);
            
            await command.ExecuteNonQueryAsync();

        }
    }
}
