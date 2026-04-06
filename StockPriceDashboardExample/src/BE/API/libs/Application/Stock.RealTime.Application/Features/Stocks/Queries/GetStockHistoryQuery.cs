using Cortex.Mediator.Queries;
using Stock.RealTime.Application.Responses.Stocks;

namespace Stock.RealTime.Application.Features.Stocks.Queries
{
    public sealed record GetStockHistoryQuery(string Ticker, int Days) : IQuery<IEnumerable<StockPriceInfoResponse>?>;
}
