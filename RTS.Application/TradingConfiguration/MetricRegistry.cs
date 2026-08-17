using RTS.Domain.Rules;

namespace RTS.Application.TradingConfiguration;

[Flags]
public enum MetricUsage
{
    Screening = 1,
    Evaluation = 2
}

public sealed record MetricDefinition(
    string Key,
    string DisplayName,
    RuleValueType ValueType,
    MetricUsage Usage,
    IReadOnlySet<ComparisonOperator> SupportedOperators);

public interface IMetricRegistry
{
    IReadOnlyCollection<MetricDefinition> GetAll();

    bool TryGet(
        string metricKey,
        out MetricDefinition? definition);
}

public sealed class MetricRegistry : IMetricRegistry
{
    private static readonly IReadOnlySet<ComparisonOperator>
        NumericOperators =
            new HashSet<ComparisonOperator>
            {
                ComparisonOperator.Equal,
                ComparisonOperator.NotEqual,
                ComparisonOperator.GreaterThan,
                ComparisonOperator.GreaterThanOrEqual,
                ComparisonOperator.LessThan,
                ComparisonOperator.LessThanOrEqual,
                ComparisonOperator.Between,
                ComparisonOperator.OutsideRange,
                ComparisonOperator.CrossesAbove,
                ComparisonOperator.CrossesBelow
            };

    private static readonly IReadOnlyDictionary<
        string,
        MetricDefinition> Definitions =
            CreateDefinitions();

    public IReadOnlyCollection<MetricDefinition> GetAll()
    {
        return Definitions.Values.ToArray();
    }

    public bool TryGet(
        string metricKey,
        out MetricDefinition? definition)
    {
        if (string.IsNullOrWhiteSpace(metricKey))
        {
            definition = null;
            return false;
        }

        return Definitions.TryGetValue(
            metricKey.Trim(),
            out definition);
    }

    private static IReadOnlyDictionary<string, MetricDefinition>
        CreateDefinitions()
    {
        var definitions = new[]
        {
            Numeric(
                "Fundamental.PriceToEarningsRatio",
                "Price-to-earnings ratio",
                RuleValueType.Decimal,
                MetricUsage.Screening |
                MetricUsage.Evaluation),

            Numeric(
                "Fundamental.ForwardPriceToEarningsRatio",
                "Forward price-to-earnings ratio",
                RuleValueType.Decimal,
                MetricUsage.Screening |
                MetricUsage.Evaluation),

            Numeric(
                "Fundamental.RevenueGrowth",
                "Revenue growth",
                RuleValueType.Percentage,
                MetricUsage.Screening |
                MetricUsage.Evaluation),

            Numeric(
                "Fundamental.EarningsGrowth",
                "Earnings growth",
                RuleValueType.Percentage,
                MetricUsage.Screening |
                MetricUsage.Evaluation),

            Numeric(
                "Market.AverageDailyDollarVolume",
                "Average daily dollar volume",
                RuleValueType.Decimal,
                MetricUsage.Screening),

            Numeric(
                "Technical.Rsi",
                "Relative Strength Index",
                RuleValueType.Decimal,
                MetricUsage.Evaluation),

            Numeric(
                "Technical.Macd",
                "MACD",
                RuleValueType.Decimal,
                MetricUsage.Evaluation),

            Numeric(
                "Technical.PriceVsSma",
                "Price versus moving average",
                RuleValueType.Percentage,
                MetricUsage.Evaluation),

            Numeric(
                "Technical.BollingerPosition",
                "Bollinger Band position",
                RuleValueType.Decimal,
                MetricUsage.Evaluation),

            Numeric(
                "Technical.VolumeRatio",
                "Volume ratio",
                RuleValueType.Decimal,
                MetricUsage.Evaluation)
        };

        return definitions.ToDictionary(
            definition => definition.Key,
            StringComparer.OrdinalIgnoreCase);
    }

    private static MetricDefinition Numeric(
        string key,
        string displayName,
        RuleValueType valueType,
        MetricUsage usage)
    {
        return new MetricDefinition(
            key,
            displayName,
            valueType,
            usage,
            NumericOperators);
    }
}