using Stock.RealTime.Application.Responses.Stocks;

namespace Stock.RealTime.Application.Abstractions.RealTime
{
    public interface IStocksFeedClientHub
    {
        Task PriceUpdate(StockPriceInfoResponse price);
    }
}