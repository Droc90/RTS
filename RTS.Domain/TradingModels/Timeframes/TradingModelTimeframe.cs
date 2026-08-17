using RTS.Domain.TradingModels.Indicators;

namespace RTS.Domain.TradingModels.Timeframes;

public sealed class TradingModelTimeframe
{
    private readonly List<TradingModelIndicator> _indicators = [];

    private TradingModelTimeframe()
    {
    }

    public long Id { get; private set; }

    public Guid ExternalId { get; private set; }

    public long TradingModelVersionId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public int DisplayOrder { get; private set; }

    public int LookbackValue { get; private set; }

    public TimeframeLookbackUnit LookbackUnit { get; private set; }

    public int BarIntervalValue { get; private set; }

    public BarIntervalUnit BarIntervalUnit { get; private set; }

    public MarketSessionMode MarketSessionMode { get; private set; }

    public IReadOnlyCollection<TradingModelIndicator> Indicators =>
        _indicators;

    internal static TradingModelTimeframe Create(
        string name,
        int displayOrder,
        int lookbackValue,
        TimeframeLookbackUnit lookbackUnit,
        int barIntervalValue,
        BarIntervalUnit barIntervalUnit,
        MarketSessionMode marketSessionMode)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "A timeframe name is required.",
                nameof(name));
        }

        if (name.Trim().Length > 100)
        {
            throw new ArgumentException(
                "A timeframe name cannot exceed 100 characters.",
                nameof(name));
        }

        if (displayOrder <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(displayOrder),
                "Display order must be positive.");
        }

        if (lookbackValue <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lookbackValue),
                "Lookback value must be positive.");
        }

        if (!Enum.IsDefined(lookbackUnit))
        {
            throw new ArgumentOutOfRangeException(
                nameof(lookbackUnit));
        }

        if (barIntervalValue <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(barIntervalValue),
                "Bar interval value must be positive.");
        }

        if (!Enum.IsDefined(barIntervalUnit))
        {
            throw new ArgumentOutOfRangeException(
                nameof(barIntervalUnit));
        }

        if (!Enum.IsDefined(marketSessionMode))
        {
            throw new ArgumentOutOfRangeException(
                nameof(marketSessionMode));
        }

        return new TradingModelTimeframe
        {
            ExternalId = Guid.NewGuid(),
            Name = name.Trim(),
            DisplayOrder = displayOrder,
            LookbackValue = lookbackValue,
            LookbackUnit = lookbackUnit,
            BarIntervalValue = barIntervalValue,
            BarIntervalUnit = barIntervalUnit,
            MarketSessionMode = marketSessionMode
        };
    }

    internal TradingModelIndicator AddSimpleMovingAverage(
        int period)
    {
        return AddIndicator(
            TradingModelIndicator.CreateSimpleMovingAverage(
                NextIndicatorDisplayOrder(),
                period));
    }

    internal TradingModelIndicator AddBollingerBands(
        int period,
        decimal standardDeviations)
    {
        return AddIndicator(
            TradingModelIndicator.CreateBollingerBands(
                NextIndicatorDisplayOrder(),
                period,
                standardDeviations));
    }

    internal TradingModelIndicator AddMacd(
        int fastPeriod,
        int slowPeriod,
        int signalPeriod)
    {
        return AddIndicator(
            TradingModelIndicator.CreateMacd(
                NextIndicatorDisplayOrder(),
                fastPeriod,
                slowPeriod,
                signalPeriod));
    }

    internal TradingModelIndicator AddRelativeStrengthIndex(
        int period)
    {
        return AddIndicator(
            TradingModelIndicator.CreateRelativeStrengthIndex(
                NextIndicatorDisplayOrder(),
                period));
    }

    internal TradingModelIndicator AddVolume()
    {
        return AddIndicator(
            TradingModelIndicator.CreateVolume(
                NextIndicatorDisplayOrder()));
    }

    private TradingModelIndicator AddIndicator(
        TradingModelIndicator indicator)
    {
        if (_indicators.Any(
                existing =>
                    string.Equals(
                        existing.Name,
                        indicator.Name,
                        StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "An indicator with the same configuration already exists.");
        }

        _indicators.Add(indicator);

        return indicator;
    }

    private int NextIndicatorDisplayOrder()
    {
        return _indicators.Count + 1;
    }
}