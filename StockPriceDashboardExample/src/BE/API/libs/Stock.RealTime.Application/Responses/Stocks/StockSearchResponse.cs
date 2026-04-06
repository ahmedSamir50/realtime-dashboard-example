namespace Stock.RealTime.Application.Responses.Stocks
{
    public class StockSearchResponse
    {
        public string Ticker { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Exchange { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}
