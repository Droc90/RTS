using RTS.Application.MarketData;
using RTS.Domain.TradingModels.Timeframes;

namespace RTS.Application.Charting;

public sealed record ChartViewDefinition(
    string Key,
    string Name,
    TimeSpan Lookback,
    int BarIntervalValue,
    BarIntervalUnit BarIntervalUnit,
    IReadOnlyCollection<string> Indicators);

public sealed record FinancialChartPoint(
    DateTime TimestampUtc,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal Volume,
    decimal? Sma50 = null,
    decimal? BollingerUpper = null,
    decimal? BollingerMiddle = null,
    decimal? BollingerLower = null,
    decimal? Macd = null,
    decimal? MacdSignal = null,
    decimal? MacdHistogram = null,
    decimal? Rsi = null);

public sealed record FinancialChartView(
    ChartViewDefinition Definition,
    IReadOnlyCollection<FinancialChartPoint> Points,
    string ProviderKey,
    DateTime RetrievedUtc,
    IReadOnlyCollection<DateTime> MissingBarsUtc,
    IReadOnlyCollection<FinancialChartPoint>? IndicatorSourcePoints = null);

public static class InitialChartSpecification
{
    private static readonly string[] Indicators = ["Volume", "Bollinger Bands (20, 2)", "SMA50", "MACD (12, 26, 9)", "RSI (14)"];

    public static IReadOnlyCollection<ChartViewDefinition> Views { get; } =
    [
        new("5D", "5-day", TimeSpan.FromDays(5), 5, BarIntervalUnit.Minutes, Indicators),
        new("1M", "1-month", TimeSpan.FromDays(30), 1, BarIntervalUnit.Days, Indicators),
        new("3M", "3-month", TimeSpan.FromDays(90), 1, BarIntervalUnit.Days, Indicators)
    ];

    public static IReadOnlyCollection<MarketDataRequest> CreateRequests(string symbol, DateTime asOfUtc) =>
        Views.Select(view => MarketDataRequests.InitialModel(symbol, asOfUtc - view.Lookback, asOfUtc,
            view.BarIntervalValue, view.BarIntervalUnit)).ToArray();
}

public interface IFinancialChartService
{
    Task<IReadOnlyCollection<FinancialChartView>> GetViewsAsync(
        Guid userExternalId,
        Guid evaluationJobExternalId,
        CancellationToken cancellationToken = default);
}
