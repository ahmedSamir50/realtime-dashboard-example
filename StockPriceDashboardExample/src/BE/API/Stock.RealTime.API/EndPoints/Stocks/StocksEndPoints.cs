using Cortex.Mediator;
using Stock.RealTime.API.Code;
using Stock.RealTime.Application.Features.Stocks.Queries;
using Stock.RealTime.Application.Responses.Stocks;

namespace Stock.RealTime.API.EndPoints.Stocks
{
    public sealed class StocksEndPoints : IEndPoint
    {
        public static void MapEndPoint(IEndpointRouteBuilder endpointRouteBuilder)
        {
            // Map your endpoints here
            var group = endpointRouteBuilder.MapGroup("/api/stocks").WithTags("Stocks");

            group
                .MapGet("/{ticker}", GetStockByTicker)
                .WithName("GetLatestStockPrice");

            group
                .MapGet("/{ticker}/history", GetStockHistory)
                .WithName("GetStockHistory");

            group
                .MapGet("/search", SearchStocks)
                .WithName("SearchStocks");
        }

        public static async Task<IResult> SearchStocks(string query, IMediator mediator)
        {
            var searchResultQuery = new SearchStocksQuery(query);
            var result = await mediator.SendQueryAsync<SearchStocksQuery, IEnumerable<StockSearchResponse>?>(searchResultQuery);
            return Results.Ok(result);
        }

        public static async Task<IResult> GetStockHistory(string ticker, int days, IMediator mediator)
        {
            var query = new GetStockHistoryQuery(ticker, days > 0 ? days : 7);
            var result = await mediator.SendQueryAsync<GetStockHistoryQuery, IEnumerable<StockPriceInfoResponse>?>(query);
            return result is not null ? Results.Ok(result) : Results.NotFound($"History for ticker '{ticker}' not found.");
        }

        public static async Task<IResult> GetStockByTicker(string ticker, IMediator mediator)
        {
            var query = new GetStockPriceQuery(ticker);
            var result = await mediator.SendQueryAsync<GetStockPriceQuery,StockPriceInfoResponse?>(query);
            return result is not null ? Results.Ok(result) : Results.NotFound($"Stock data for ticker '{ticker}' not found.");
        }
    }
}
