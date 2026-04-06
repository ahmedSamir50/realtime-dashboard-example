using Microsoft.AspNetCore.SignalR;
using Stock.RealTime.Application.Abstractions.RealTime;
using Stock.RealTime.Infrastructure.Abstractions.RealTime;

namespace Stock.RealTime.Infrastructure.Services.RealTime
{
    public sealed class StocksFeedClientHub : Hub<IStocksFeedClientHub>
    {
        private readonly IActiveTickerManagerService _activeTickerManager;

        public StocksFeedClientHub(IActiveTickerManagerService activeTickerManager)
        {
            _activeTickerManager = activeTickerManager;
        }

        public async Task JoinStockGroupInterest(string stockTicker) 
        {
            var ticker = stockTicker.Trim().ToUpperInvariant();
            await Groups.AddToGroupAsync(Context.ConnectionId, ticker);
            _activeTickerManager.AddTicker(ticker);
        }
    }
}
