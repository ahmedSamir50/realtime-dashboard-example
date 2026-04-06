namespace Stock.RealTime.Domain.Stocks
{
    public sealed class StockPrice
    {
       
        public int Id { get; set; }
        public string Ticker { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public DateTimeOffset Timestamp { get; set; }


    }
}
