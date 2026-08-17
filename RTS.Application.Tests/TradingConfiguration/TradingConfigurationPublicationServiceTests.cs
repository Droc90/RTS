using RTS.Application.TradingConfiguration;
using RTS.Domain.Rules;
using RTS.Domain.ScreeningStrategies;
using RTS.Domain.TradingModels;
using RTS.Domain.TradingModels.Timeframes;

namespace RTS.Application.Tests.TradingConfiguration;

public sealed class TradingConfigurationPublicationServiceTests
{
    [Fact]
    public void PublishTradingModel_WithValidConfiguration_PublishesVersion()
    {
        var model = CreateValidTradingModel();
        var draft = Assert.Single(model.Versions);

        var result = CreateService().PublishTradingModel(
            model,
            draft.ExternalId,
            draft.CreatedUtc.AddMinutes(1));

        Assert.True(result.IsPublished);
        Assert.Same(
            draft,
            result.PublishedVersion);
        Assert.Equal(
            TradingModelVersionStatus.Published,
            draft.Status);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void PublishTradingModel_WithInvalidConfiguration_DoesNotPublish()
    {
        var model = TradingModel.Create(
            "Incomplete Model",
            null,
            ownerUserId: 42);

        var draft = Assert.Single(model.Versions);

        var result = CreateService().PublishTradingModel(
            model,
            draft.ExternalId,
            draft.CreatedUtc.AddMinutes(1));

        Assert.False(result.IsPublished);
        Assert.Null(result.PublishedVersion);
        Assert.Equal(
            TradingModelVersionStatus.Draft,
            draft.Status);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Severity ==
                ConfigurationValidationSeverity.Error);
    }

    [Fact]
    public void PublishScreeningStrategy_WithValidConfiguration_PublishesVersion()
    {
        var strategy = ScreeningStrategy.Create(
            "Valid Screening",
            null,
            ownerUserId: 42);

        var draft = Assert.Single(
            strategy.Versions);

        draft.AddRule(
            "Maximum P/E",
            "Fundamental.PriceToEarningsRatio",
            RulePurpose.Filter,
            ComparisonOperator.LessThan,
            RuleValue.FromDecimal(15m));

        var result =
            CreateService().PublishScreeningStrategy(
                strategy,
                draft.ExternalId,
                draft.CreatedUtc.AddMinutes(1));

        Assert.True(result.IsPublished);
        Assert.Equal(
            ScreeningStrategyVersionStatus.Published,
            draft.Status);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void PublishScreeningStrategy_WithWarnings_PublishesAndReturnsWarnings()
    {
        var strategy = ScreeningStrategy.Create(
            "Ranking Strategy",
            null,
            ownerUserId: 42);

        var draft = Assert.Single(
            strategy.Versions);

        draft.AddRule(
            "Revenue growth",
            "Fundamental.RevenueGrowth",
            RulePurpose.Score,
            ComparisonOperator.GreaterThan,
            RuleValue.FromPercentage(10m),
            weight: 100m);

        var result =
            CreateService().PublishScreeningStrategy(
                strategy,
                draft.ExternalId,
                draft.CreatedUtc.AddMinutes(1));

        Assert.True(result.IsPublished);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Severity ==
                ConfigurationValidationSeverity.Warning);
    }

    private static TradingModel CreateValidTradingModel()
    {
        var model = TradingModel.Create(
            "Valid Model",
            null,
            ownerUserId: 42);

        var draft = Assert.Single(model.Versions);

        var timeframe = draft.AddTimeframe(
            "Daily",
            1,
            TimeframeLookbackUnit.Months,
            1,
            BarIntervalUnit.Days,
            MarketSessionMode.RegularHoursOnly);

        draft.AddVolume(
            timeframe.ExternalId);

        draft.AddCriterion(
            "Maximum P/E",
            "Fundamental.PriceToEarningsRatio",
            RulePurpose.Filter,
            ComparisonOperator.LessThan,
            RuleValue.FromDecimal(15m));

        return model;
    }

    private static TradingConfigurationPublicationService
        CreateService()
    {
        var validator =
            new TradingConfigurationValidator(
                new MetricRegistry(),
                new IndicatorRegistry());

        return new TradingConfigurationPublicationService(
            validator);
    }
}