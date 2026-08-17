using RTS.Domain.TradingModels.Indicators;

namespace RTS.Application.TradingConfiguration;

public sealed record IndicatorParameterDefinition(
    string Key,
    IndicatorParameterValueType ValueType,
    bool IsRequired);

public sealed record IndicatorDefinition(
    TradingIndicatorType IndicatorType,
    string DisplayName,
    IndicatorPane DefaultPane,
    IReadOnlyCollection<IndicatorParameterDefinition> Parameters);

public interface IIndicatorRegistry
{
    IReadOnlyCollection<IndicatorDefinition> GetAll();

    bool TryGet(
        TradingIndicatorType indicatorType,
        out IndicatorDefinition? definition);
}

public sealed class IndicatorRegistry : IIndicatorRegistry
{
    private static readonly IReadOnlyDictionary<
        TradingIndicatorType,
        IndicatorDefinition> Definitions =
            CreateDefinitions();

    public IReadOnlyCollection<IndicatorDefinition> GetAll()
    {
        return Definitions.Values.ToArray();
    }

    public bool TryGet(
        TradingIndicatorType indicatorType,
        out IndicatorDefinition? definition)
    {
        return Definitions.TryGetValue(
            indicatorType,
            out definition);
    }

    private static IReadOnlyDictionary<
        TradingIndicatorType,
        IndicatorDefinition> CreateDefinitions()
    {
        var definitions = new[]
        {
            new IndicatorDefinition(
                TradingIndicatorType.SimpleMovingAverage,
                "Simple Moving Average",
                IndicatorPane.Price,
                [
                    Integer("Period")
                ]),

            new IndicatorDefinition(
                TradingIndicatorType.BollingerBands,
                "Bollinger Bands",
                IndicatorPane.Price,
                [
                    Integer("Period"),
                    Decimal("StandardDeviations")
                ]),

            new IndicatorDefinition(
                TradingIndicatorType.Macd,
                "MACD",
                IndicatorPane.Separate,
                [
                    Integer("FastPeriod"),
                    Integer("SlowPeriod"),
                    Integer("SignalPeriod")
                ]),

            new IndicatorDefinition(
                TradingIndicatorType.RelativeStrengthIndex,
                "Relative Strength Index",
                IndicatorPane.Separate,
                [
                    Integer("Period")
                ]),

            new IndicatorDefinition(
                TradingIndicatorType.Volume,
                "Volume",
                IndicatorPane.Volume,
                [])
        };

        return definitions.ToDictionary(
            definition =>
                definition.IndicatorType);
    }

    private static IndicatorParameterDefinition Integer(
        string key)
    {
        return new IndicatorParameterDefinition(
            key,
            IndicatorParameterValueType.Integer,
            IsRequired: true);
    }

    private static IndicatorParameterDefinition Decimal(
        string key)
    {
        return new IndicatorParameterDefinition(
            key,
            IndicatorParameterValueType.Decimal,
            IsRequired: true);
    }
}