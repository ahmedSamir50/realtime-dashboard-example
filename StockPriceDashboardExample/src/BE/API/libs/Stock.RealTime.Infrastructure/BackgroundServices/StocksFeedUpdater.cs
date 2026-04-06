using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stock.RealTime.Application.Abstractions.RealTime;
using Stock.RealTime.Application.Abstractions.Stocks;
using Stock.RealTime.Core.Options;

namespace Stock.RealTime.Infrastructure.BackgroundServices
{
    public sealed class StocksFeedUpdater : BackgroundService
    {
        private readonly ILogger<StocksFeedUpdater> _logger;
        private readonly IStockInfoDataService _stockInfoDataService;
        private readonly IActiveTickerManagerService _activeTickerManagerService;
        private readonly IHubContext<Hub<IStocksFeedClientHub>, IStocksFeedClientHub> _hubContext;
        private readonly StockUpdaterJobOptions _options;

        public StocksFeedUpdater(
            ILogger<StocksFeedUpdater> logger,
            IStockInfoDataService stockInfoDataService,
            IActiveTickerManagerService activeTickerManagerService,
            IHubContext<Hub<IStocksFeedClientHub>, IStocksFeedClientHub> hubContext,
            IOptions<StockUpdaterJobOptions> options)
        {
            _logger = logger;
            _stockInfoDataService = stockInfoDataService;
            _activeTickerManagerService = activeTickerManagerService;
            _hubContext = hubContext;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("StocksFeedUpdater is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var activeTickers = _activeTickerManagerService.ActiveTickers;
                
                foreach (var ticker in activeTickers)
                {
                    try
                    {
                        var price = await _stockInfoDataService.GetLatestStockPriceAsync(ticker);
                        if (price != null)
                        {
                            await _hubContext.Clients.Group(ticker).PriceUpdate(price);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error updating price for ticker {Ticker}", ticker);
                    }
                }

                await Task.Delay(_options.UpdateInterval, stoppingToken);
            }

            _logger.LogInformation("StocksFeedUpdater is stopping.");
        }
    }
}
