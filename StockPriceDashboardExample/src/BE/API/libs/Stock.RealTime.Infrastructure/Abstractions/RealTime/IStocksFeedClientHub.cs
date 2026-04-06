using Stock.RealTime.Application.Responses.Stocks;

namespace Stock.RealTime.Infrastructure.Abstractions.RealTime
{
    public interface IStocksFeedClientHub
    {
        Task PriceUpdate(StockPriceInfoResponse price);
    }
}