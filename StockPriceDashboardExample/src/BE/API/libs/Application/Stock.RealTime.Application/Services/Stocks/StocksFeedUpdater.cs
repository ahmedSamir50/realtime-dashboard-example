using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stock.RealTime.Application.Abstractions.RealTime;
using Stock.RealTime.Application.Abstractions.Stocks;
using Stock.RealTime.Application.Responses.Stocks;
using Stock.RealTime.Application.Services.RealTime;
using Stock.RealTime.Core.Options;

namespace Stock.RealTime.Application.Services.Stocks;

public sealed class StocksFeedUpdater : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<StocksFeedUpdater> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHubContext<StocksFeedClientHub, IStocksFeedClientHub> _hubContext;
    private readonly IOptions<StockUpdaterJobOptions> _opt;
    private readonly Random _random = new ();
    private readonly StockUpdaterJobOptions _stockUpdaterJobOptions;

    public StocksFeedUpdater(IServiceScopeFactory serviceScopeFactory,
                             ILogger<StocksFeedUpdater> logger,
                             IConfiguration configuration,
                             IHubContext<StocksFeedClientHub, IStocksFeedClientHub> hubContext,
                             IOptions<StockUpdaterJobOptions> stockUpdaterJobOptions)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _configuration = configuration;
        _hubContext = hubContext;
        _opt = stockUpdaterJobOptions;
        _stockUpdaterJobOptions = stockUpdaterJobOptions.Value;

    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await  UpdateStockPricesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending stock price updates");
            }
            await Task.Delay(_stockUpdaterJobOptions.UpdateInterval, stoppingToken);
        }
    }

    private async Task UpdateStockPricesAsync()
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        IStockInfoDataService stockDataService = scope.ServiceProvider.GetRequiredService<IStockInfoDataService>();
        IActiveTickerManagerService activeTickerManagerService = scope.ServiceProvider.GetRequiredService<IActiveTickerManagerService>();
        var activeTickers = activeTickerManagerService.ActiveTickers;
        _logger.LogInformation("StocksFeedUpdater: Active Tickers Count: {Count}", activeTickers.Count);
        if (activeTickers is null || !activeTickers.Any()) 
            return;

        foreach ( string stockTicker in activeTickers) { 

            var currentPriceResponse = await stockDataService.GetLatestStockPriceAsync(stockTicker);
            if (currentPriceResponse is null) continue;

            var newPrice = CalculateNewPrice(currentPriceResponse);
            var updateInfo = new StockPriceUpdateMessage(stockTicker, newPrice);

            // only send update to clients subscribed to this stock ticker
            await _hubContext.Clients.Group(stockTicker).ReciveStockPriceUpdate(updateInfo);

            _logger.LogInformation("Updated price for {StockTicker}: {NewPrice}", stockTicker, newPrice);

        }

    }

    private decimal CalculateNewPrice(StockPriceInfoResponse currentStockPrice)
    {
        double MinConcideredChange = _stockUpdaterJobOptions.MaxPriceChangePercentage ;
        // TODO descripe
        decimal priceFactore = (decimal)(_random.NextDouble() * MinConcideredChange * 2 - MinConcideredChange);
        decimal priceChange = currentStockPrice.Price * priceFactore ;
        decimal newPrice = Math.Max(0, currentStockPrice.Price + priceChange)   ;
        newPrice = Math.Round(newPrice, 2);
        return newPrice;
    }
}