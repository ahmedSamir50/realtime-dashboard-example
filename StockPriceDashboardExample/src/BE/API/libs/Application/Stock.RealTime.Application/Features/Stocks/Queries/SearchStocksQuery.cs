using Cortex.Mediator;
using Cortex.Mediator.Queries;
using Stock.RealTime.Application.Responses.Stocks;

namespace Stock.RealTime.Application.Features.Stocks.Queries
{
    public sealed record SearchStocksQuery(string Query) : IQuery<IEnumerable<StockSearchResponse>?>;
}
