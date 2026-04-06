using Microsoft.Extensions.Logging;
using Stock.RealTime.Application.Abstractions.RealTime;
using System.Collections.Concurrent;

namespace Stock.RealTime.Infrastructure.Services.RealTime
{
    public sealed class ActiveTickerManagerService : IActiveTickerManagerService
    {
        private readonly ConcurrentDictionary<string, byte> _activeTickers = new();
        private readonly ILogger<ActiveTickerManagerService> _logger;

        public ActiveTickerManagerService(ILogger<ActiveTickerManagerService> logger)
        {
            _logger = logger;
        }

        public void AddTicker(string ticker)
        {
            _logger.LogInformation("AddTicker called for {Ticker}", ticker);
            _activeTickers.TryAdd(ticker, 0);
        }

        public IReadOnlyCollection<string> ActiveTickers => _activeTickers.Keys.ToArray();

        public void RemoveTicker(string ticker)
        {
            _activeTickers.TryRemove(ticker, out _);
        }
    }
}
