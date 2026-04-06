using Cortex.Mediator.Queries;
using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using Stock.RealTime.Application.Abstractions.Stocks;
using Stock.RealTime.Application.Features.Stocks.Queries;
using Stock.RealTime.Application.Responses.Stocks;

namespace Stock.RealTime.Application.Features.Stocks.Handlers
{
    public sealed class GetStockHistoryHandler : IQueryHandler<GetStockHistoryQuery, IEnumerable<StockPriceInfoResponse>?>
    {
        private readonly NpgsqlDataSource _dataSource;
        private readonly IStockDataClientService _stockDataClientService;
        private readonly ILogger<GetStockHistoryHandler> _logger;

        public GetStockHistoryHandler(NpgsqlDataSource dataSource,
                                       IStockDataClientService stockDataClientService,
                                       ILogger<GetStockHistoryHandler> logger)
        {
            _dataSource = dataSource;
            _stockDataClientService = stockDataClientService;
            _logger = logger;
        }

        public async Task<IEnumerable<StockPriceInfoResponse>?> Handle(GetStockHistoryQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var historyFromDb = (await GetStockHistoryFromDatabase(request.Ticker, request.Days))?.ToList();
                
                if (historyFromDb != null && historyFromDb.Any())
                {
                    var earliest = historyFromDb.MinBy(h => h.Date);
                    var latest = historyFromDb.MaxBy(h => h.Date);
                    
                    var hasEnoughHistory = earliest?.Date <= DateTime.UtcNow.AddDays(-request.Days).AddHours(1); 
                    var isLatestFresh = latest?.Date >= DateTime.UtcNow.AddMinutes(-2);

                    if (hasEnoughHistory && isLatestFresh)
                    {
                        _logger.LogInformation("Returning complete cached history from DB for {Ticker}", request.Ticker);
                        return historyFromDb;
                    }
                }

                _logger.LogInformation("History coverage insufficient for {Ticker} ({Days} days), fetching full trend from API", request.Ticker, request.Days);
                var historyFromApi = await _stockDataClientService.GetStockHistoryAsync(request.Ticker, request.Days);
                
                if (historyFromApi != null)
                {
                    await SaveHistoryToDatabaseAsync(request.Ticker, historyFromApi);
                    return historyFromApi;
                }

                return historyFromDb; 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching stock history for ticker {Ticker}", request.Ticker);
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
                await using var connection = await _dataSource.OpenConnectionAsync();
                await using var transaction = await connection.BeginTransactionAsync();

                foreach (var point in history)
                {
                    var cmd = connection.CreateCommand();
                    cmd.CommandText = "INSERT INTO public.stock_prices (ticker, price, timestamp) SELECT @ticker, @price, @timestamp WHERE NOT EXISTS (SELECT 1 FROM public.stock_prices WHERE ticker = @ticker AND timestamp = @timestamp)";
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
    }
}
