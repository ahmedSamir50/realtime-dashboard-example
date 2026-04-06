using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using Stock.RealTime.Application.Abstractions.RealTime;
using Stock.RealTime.Application.Abstractions.Stocks;
using Stock.RealTime.Application.Responses.Stocks;
using Stock.RealTime.Domain.Stocks;

namespace Stock.RealTime.Infrastructure.Persistence
{
    public sealed class StockInfoRepository : IStockInfoDataService
    {
        private readonly NpgsqlDataSource _dataSource;
        private readonly IStockDataClientService _stockDataClientService;
        private readonly ILogger<StockInfoRepository> _logger;
        private readonly IActiveTickerManagerService _activeTickerManagerService;

        public StockInfoRepository(NpgsqlDataSource dataSource,
                                     IStockDataClientService stockDataClientService,
                                     ILogger<StockInfoRepository> logger,
                                     IActiveTickerManagerService activeTickerManagerService)
        {
            _dataSource = dataSource;
            _stockDataClientService = stockDataClientService;
            _logger = logger;
            _activeTickerManagerService = activeTickerManagerService;
        }

        public async Task<IEnumerable<StockPriceInfoResponse>?> GetStockHistoryAsync(string ticker, int days = 7)
        {
            try
            {
                var historyFromDb = (await GetStockHistoryFromDatabase(ticker, days))?.ToList();
                
                if (historyFromDb != null && historyFromDb.Any())
                {
                    var earliest = historyFromDb.MinBy(h => h.Date);
                    var latest = historyFromDb.MaxBy(h => h.Date);
                    
                    var hasEnoughHistory = earliest?.Date <= DateTime.UtcNow.AddDays(-days).AddHours(1); 
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
                
                return await dbConn.QueryAsync<StockPriceInfoResponse>(sql, new { 
                    ticker, 
                    since = DateTime.UtcNow.AddDays(-days) 
                });
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
                await using var connection = await _dataSource.OpenConnectionAsync();
                await using var transaction = await connection.BeginTransactionAsync();

                foreach (var point in history)
                {
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
                StockPriceInfoResponse? result = null;
                using (var dbConn = await _dataSource.OpenConnectionAsync())
                {
                    var sql = "SELECT ticker, price, timestamp FROM public.stock_prices WHERE ticker = @ticker ORDER BY timestamp DESC LIMIT 1";
                    var dbRow = await dbConn.QueryFirstOrDefaultAsync<StockPrice>(sql, new { ticker });
                    if (dbRow != null)
                    {
                        result = new StockPriceInfoResponse { Ticker = dbRow.Ticker, Price = dbRow.Price, Date = dbRow.Timestamp.DateTime };
                    }
                }

                if (result is not null)
                {
                    _activeTickerManagerService.AddTicker(ticker);
                    return result;
                }

                result = await _stockDataClientService.GetStockDataByTickerAsync(ticker);
                if (result is not null)
                {
                    await SaveStockPriceToDatabaseAsync(ticker, result.Price);
                    _activeTickerManagerService.AddTicker(ticker);
                    return result;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching latest stock price for {Ticker}", ticker);
                return null;
            }
        }

        private async Task SaveStockPriceToDatabaseAsync(string ticker, decimal price)
        {
            try
            {
                await using var connection = await _dataSource.OpenConnectionAsync();
                var command = connection.CreateCommand();
                command.CommandText = "INSERT INTO public.stock_prices (ticker, price, timestamp) VALUES (@ticker, @price, @timestamp)";
                command.Parameters.AddWithValue("ticker", ticker);
                command.Parameters.AddWithValue("price", price);
                command.Parameters.AddWithValue("timestamp", DateTime.UtcNow);
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving price to DB for {Ticker}", ticker);
            }
        }
    }
}
