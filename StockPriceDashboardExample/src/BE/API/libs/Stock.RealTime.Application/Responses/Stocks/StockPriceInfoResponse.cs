namespace Stock.RealTime.Application.Responses.Stocks
{
    public sealed class StockPriceInfoResponse
    {
        public string Ticker { get; set; }
        public decimal Price { get; set; } = 0;
        public DateTime? Date { get; set; }
    }
}
