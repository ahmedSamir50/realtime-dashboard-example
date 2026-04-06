namespace Stock.RealTime.Application.Abstractions.RealTime
{
    public interface IActiveTickerManagerService
    {
        IReadOnlyCollection<string> ActiveTickers { get; }

        void AddTicker(string ticker);
        void RemoveTicker(string ticker);
    }
}