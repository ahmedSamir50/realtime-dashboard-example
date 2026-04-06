namespace Stock.RealTime.Core.Options;

public sealed class  StockUpdaterJobOptions
{
    public TimeSpan UpdateInterval { get; set; } = TimeSpan.FromSeconds(5);

    public double MaxPriceChangePercentage { get; set; } = 0.02; // 2% price change limit
}
