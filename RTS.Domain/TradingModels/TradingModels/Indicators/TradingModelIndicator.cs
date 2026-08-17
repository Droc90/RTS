using System.Globalization;

namespace RTS.Domain.TradingModels.Indicators;

public sealed class TradingModelIndicator
{
    private readonly List<TradingModelIndicatorParameter> _parameters = [];

    private TradingModelIndicator()
    {
    }

    public long Id { get; private set; }

    public Guid ExternalId { get; private set; }

    public long TradingModelTimeframeId { get; private set; }

    public TradingIndicatorType IndicatorType { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public IndicatorPane Pane { get; private set; }

    public int DisplayOrder { get; private set; }

    public bool IsEnabled { get; private set; }

    public IReadOnlyCollection<TradingModelIndicatorParameter> Parameters =>
        _parameters;

    internal static TradingModelIndicator CreateSimpleMovingAverage(
        int displayOrder,
        int period)
    {
        EnsurePositive(period, nameof(period));

        var indicator = Create(
            TradingIndicatorType.SimpleMovingAverage,
            $"SMA ({period})",
            IndicatorPane.Price,
            displayOrder);

        indicator._parameters.Add(
            TradingModelIndicatorParameter.CreateInteger(
                "Period",
                period));

        return indicator;
    }

    internal static TradingModelIndicator CreateBollingerBands(
        int displayOrder,
        int period,
        decimal standardDeviations)
    {
        EnsurePositive(period, nameof(period));

        if (standardDeviations <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(standardDeviations),
                "Standard deviations must be positive.");
        }

        var formattedDeviation =
            standardDeviations.ToString(
                CultureInfo.InvariantCulture);

        var indicator = Create(
            TradingIndicatorType.BollingerBands,
            $"Bollinger Bands ({period}, {formattedDeviation})",
            IndicatorPane.Price,
            displayOrder);

        indicator._parameters.Add(
            TradingModelIndicatorParameter.CreateInteger(
                "Period",
                period));

        indicator._parameters.Add(
            TradingModelIndicatorParameter.CreateDecimal(
                "StandardDeviations",
                standardDeviations));

        return indicator;
    }

    internal static TradingModelIndicator CreateMacd(
        int displayOrder,
        int fastPeriod,
        int slowPeriod,
        int signalPeriod)
    {
        EnsurePositive(fastPeriod, nameof(fastPeriod));
        EnsurePositive(slowPeriod, nameof(slowPeriod));
        EnsurePositive(signalPeriod, nameof(signalPeriod));

        if (fastPeriod >= slowPeriod)
        {
            throw new ArgumentException(
                "The MACD fast period must be less than the slow period.",
                nameof(fastPeriod));
        }

        var indicator = Create(
            TradingIndicatorType.Macd,
            $"MACD ({fastPeriod}, {slowPeriod}, {signalPeriod})",
            IndicatorPane.Separate,
            displayOrder);

        indicator._parameters.Add(
            TradingModelIndicatorParameter.CreateInteger(
                "FastPeriod",
                fastPeriod));

        indicator._parameters.Add(
            TradingModelIndicatorParameter.CreateInteger(
                "SlowPeriod",
                slowPeriod));

        indicator._parameters.Add(
            TradingModelIndicatorParameter.CreateInteger(
                "SignalPeriod",
                signalPeriod));

        return indicator;
    }

    internal static TradingModelIndicator CreateRelativeStrengthIndex(
        int displayOrder,
        int period)
    {
        EnsurePositive(period, nameof(period));

        var indicator = Create(
            TradingIndicatorType.RelativeStrengthIndex,
            $"RSI ({period})",
            IndicatorPane.Separate,
            displayOrder);

        indicator._parameters.Add(
            TradingModelIndicatorParameter.CreateInteger(
                "Period",
                period));

        return indicator;
    }

    internal static TradingModelIndicator CreateVolume(
        int displayOrder)
    {
        return Create(
            TradingIndicatorType.Volume,
            "Volume",
            IndicatorPane.Volume,
            displayOrder);
    }

    private static TradingModelIndicator Create(
        TradingIndicatorType indicatorType,
        string name,
        IndicatorPane pane,
        int displayOrder)
    {
        EnsurePositive(
            displayOrder,
            nameof(displayOrder));

        return new TradingModelIndicator
        {
            ExternalId = Guid.NewGuid(),
            IndicatorType = indicatorType,
            Name = name,
            Pane = pane,
            DisplayOrder = displayOrder,
            IsEnabled = true
        };
    }

    private static void EnsurePositive(
        int value,
        string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "The value must be positive.");
        }
    }
}