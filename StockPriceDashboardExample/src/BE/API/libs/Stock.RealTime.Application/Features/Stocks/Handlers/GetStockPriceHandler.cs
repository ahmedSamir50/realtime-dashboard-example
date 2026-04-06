using Cortex.Mediator.Queries;
using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using Stock.RealTime.Application.Abstractions.RealTime;
using Stock.RealTime.Application.Abstractions.Stocks;
using Stock.RealTime.Application.Features.Stocks.Queries;
using Stock.RealTime.Application.Responses.Stocks;
using Stock.RealTime.Domain.Stocks;


namespace Stock.RealTime.Application.Features.Stocks.Handlers
{
    public sealed class GetStockPriceHandler : IQueryHandler<GetStockPriceQuery, StockPriceInfoResponse?>
    {
        private readonly NpgsqlDataSource _dataSource;
        private readonly IStockDataClientService _stockDataClientService;
        private readonly ILogger<GetStockPriceHandler> _logger;
        private readonly IActiveTickerManagerService _activeTickerManagerService;

        public GetStockPriceHandler(NpgsqlDataSource dataSource,
                                     IStockDataClientService stockDataClientService,
                                     ILogger<GetStockPriceHandler> logger,
                                     IActiveTickerManagerService activeTickerManagerService)
        {
            _dataSource = dataSource;
            _stockDataClientService = stockDataClientService;
            _logger = logger;
            _activeTickerManagerService = activeTickerManagerService;
        }

        public async Task<StockPriceInfoResponse?> Handle(GetStockPriceQuery request, CancellationToken cancellationToken)
        {
            try
            {
                StockPriceInfoResponse? priceFromDb = await GetStockDataFromDatabase(request.Ticker);
                if (priceFromDb is not null)
                {
                    _activeTickerManagerService.AddTicker(request.Ticker);
                    return priceFromDb;
                }

                priceFromDb = await _stockDataClientService.GetStockDataByTickerAsync(request.Ticker);

                if (priceFromDb is not null)
                {
                    await SaveStockPriceToDatabaseAsync(request.Ticker, priceFromDb.Price);
                    _activeTickerManagerService.AddTicker(request.Ticker);
                    return priceFromDb;
                }
                
                _logger.LogWarning("Stock data for ticker {Ticker} not found in external API", request.Ticker);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching latest stock price for ticker {Ticker}", request.Ticker);
                return null;
            }
        }

        private async Task<StockPriceInfoResponse?> GetStockDataFromDatabase(string ticker)
        {
            try
            {
                using Npgsql.NpgsqlConnection dbConn = await _dataSource.OpenConnectionAsync();
                var sql = """
                    SELECT ticker , price, timestamp 
                    FROM public.stock_prices 
                    WHERE ticker = @ticker 
                    ORDER BY timestamp DESC LIMIT 1
                    """;
                    
                StockPrice? result = await dbConn.QueryFirstOrDefaultAsync<StockPrice>(sql, new { ticker });
                if (result is not null) { 
                    return new StockPriceInfoResponse
                    {
                        Ticker = result.Ticker,
                        Price = result.Price,
                        Date = result.Timestamp.DateTime
                    };
                }

                // If not found, fallback to API
                var apiResult = await _stockDataClientService.GetStockDataByTickerAsync(ticker);
                if (apiResult != null)
                {
                    await SaveStockPriceToDatabaseAsync(ticker, apiResult.Price);
                }
                return apiResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching stock data from database for ticker {Ticker}", ticker);
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
