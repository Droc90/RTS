using RTS.Application.TradingConfiguration;
using RTS.Domain.Rules;
using RTS.Domain.ScreeningStrategies;
using RTS.Domain.TradingModels;
using RTS.Domain.TradingModels.Indicators;
using RTS.Domain.TradingModels.Timeframes;

namespace RTS.Application.Tests.TradingConfiguration;

public sealed class TradingConfigurationValidatorTests
{
    private readonly MetricRegistry _metricRegistry = new();
    private readonly IndicatorRegistry _indicatorRegistry = new();

    [Fact]
    public void MetricRegistry_FindsMetricCaseInsensitively()
    {
        var found = _metricRegistry.TryGet(
            "fundamental.pricetoearningsratio",
            out var definition);

        Assert.True(found);
        Assert.NotNull(definition);
        Assert.Equal(
            RuleValueType.Decimal,
            definition.ValueType);
        Assert.True(
            definition.Usage.HasFlag(
                MetricUsage.Screening));
    }

    [Fact]
    public void IndicatorRegistry_DefinesInitialIndicators()
    {
        var definitions =
            _indicatorRegistry.GetAll();

        Assert.Equal(5, definitions.Count);

        var macd = definitions.Single(
            definition =>
                definition.IndicatorType ==
                TradingIndicatorType.Macd);

        Assert.Equal(3, macd.Parameters.Count);
    }

    [Fact]
    public void Validate_CompleteTradingModel_ReturnsValid()
    {
        var version = CreateValidTradingModelVersion();

        var result = CreateValidator().Validate(version);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Validate_EmptyTradingModel_ReturnsRequiredErrors()
    {
        var model = TradingModel.Create(
            "Empty Model",
            null,
            ownerUserId: 42);

        var version = Assert.Single(model.Versions);

        var result = CreateValidator().Validate(version);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code ==
                "TradingModel.Timeframes.Required");

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code ==
                "TradingModel.Criteria.Required");
    }

    [Fact]
    public void Validate_TradingModelWithUnsupportedMetric_ReturnsError()
    {
        var model = TradingModel.Create(
            "Unsupported Metric Model",
            null,
            ownerUserId: 42);

        var version = Assert.Single(model.Versions);

        var timeframe = version.AddTimeframe(
            "Daily",
            1,
            TimeframeLookbackUnit.Months,
            1,
            BarIntervalUnit.Days,
            MarketSessionMode.RegularHoursOnly);

        version.AddVolume(timeframe.ExternalId);

        version.AddCriterion(
            "Unknown metric",
            "Unknown.Metric",
            RulePurpose.Filter,
            ComparisonOperator.LessThan,
            RuleValue.FromDecimal(10m));

        var result = CreateValidator().Validate(version);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code ==
                "Metric.Unsupported");
    }

    [Fact]
    public void Validate_TradingModelWithScreeningOnlyMetric_ReturnsError()
    {
        var model = TradingModel.Create(
            "Invalid Usage Model",
            null,
            ownerUserId: 42);

        var version = Assert.Single(model.Versions);

        var timeframe = version.AddTimeframe(
            "Daily",
            1,
            TimeframeLookbackUnit.Months,
            1,
            BarIntervalUnit.Days,
            MarketSessionMode.RegularHoursOnly);

        version.AddVolume(timeframe.ExternalId);

        version.AddCriterion(
            "Liquidity",
            "Market.AverageDailyDollarVolume",
            RulePurpose.Filter,
            ComparisonOperator.GreaterThan,
            RuleValue.FromDecimal(5_000_000m));

        var result = CreateValidator().Validate(version);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code ==
                "Metric.Usage");
    }

    [Fact]
    public void Validate_CompleteScreeningStrategy_ReturnsValid()
    {
        var strategy = ScreeningStrategy.Create(
            "Valid Screening",
            null,
            ownerUserId: 42);

        var version = Assert.Single(strategy.Versions);

        version.AddRule(
            "Maximum P/E",
            "Fundamental.PriceToEarningsRatio",
            RulePurpose.Filter,
            ComparisonOperator.LessThan,
            RuleValue.FromDecimal(15m));

        var result = CreateValidator().Validate(version);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Validate_ScreeningStrategyWithoutFilter_ReturnsWarning()
    {
        var strategy = ScreeningStrategy.Create(
            "Ranking Only",
            null,
            ownerUserId: 42);

        var version = Assert.Single(strategy.Versions);

        version.AddRule(
            "Revenue growth",
            "Fundamental.RevenueGrowth",
            RulePurpose.Score,
            ComparisonOperator.GreaterThan,
            RuleValue.FromPercentage(10m),
            weight: 100m);

        var result = CreateValidator().Validate(version);

        Assert.True(result.IsValid);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code ==
                "ScreeningStrategy.Filter.Missing" &&
                issue.Severity ==
                ConfigurationValidationSeverity.Warning);
    }

    private TradingConfigurationValidator CreateValidator()
    {
        return new TradingConfigurationValidator(
            _metricRegistry,
            _indicatorRegistry);
    }

    private static TradingModelVersion
        CreateValidTradingModelVersion()
    {
        var model = TradingModel.Create(
            "Valid Model",
            null,
            ownerUserId: 42);

        var version = Assert.Single(model.Versions);

        var timeframe = version.AddTimeframe(
            "Daily",
            1,
            TimeframeLookbackUnit.Months,
            1,
            BarIntervalUnit.Days,
            MarketSessionMode.RegularHoursOnly);

        version.AddVolume(timeframe.ExternalId);

        version.AddCriterion(
            "Maximum P/E",
            "Fundamental.PriceToEarningsRatio",
            RulePurpose.Filter,
            ComparisonOperator.LessThan,
            RuleValue.FromDecimal(15m));

        return version;
    }
}