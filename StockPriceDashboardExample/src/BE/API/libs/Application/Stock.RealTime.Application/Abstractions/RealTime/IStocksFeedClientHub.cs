
namespace Stock.RealTime.Application.Abstractions.RealTime
{
    public sealed record StockPriceUpdateMessage(string StockSymbolTicker, decimal Price);

    public interface IStocksFeedClientHub
    {
        Task ReciveStockPriceUpdate(StockPriceUpdateMessage updates);
    }
}
