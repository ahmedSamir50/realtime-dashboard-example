using Cortex.Mediator;
using Cortex.Mediator.Queries;
using Stock.RealTime.Application.Abstractions.Stocks;
using Stock.RealTime.Application.Features.Stocks.Queries;
using Stock.RealTime.Application.Responses.Stocks;

namespace Stock.RealTime.Application.Features.Stocks.Handlers
{
    public sealed class SearchStocksHandler : IQueryHandler<SearchStocksQuery, IEnumerable<StockSearchResponse>?>
    {
        private readonly IStockDataClientService _stockDataClientService;

        public SearchStocksHandler(IStockDataClientService stockDataClientService)
        {
            _stockDataClientService = stockDataClientService;
        }

        public async Task<IEnumerable<StockSearchResponse>?> Handle(SearchStocksQuery request, CancellationToken cancellationToken)
        {
            return await _stockDataClientService.SearchStocksAsync(request.Query);
        }
    }
}
