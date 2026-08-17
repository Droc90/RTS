using RTS.Application.TradingConfiguration;
using RTS.Domain.Rules;
using RTS.Domain.ScreeningStrategies;
using RTS.Domain.TradingModels;
using RTS.Domain.TradingModels.Timeframes;

namespace RTS.Application.Tests.TradingConfiguration;

public sealed class TradingConfigurationTemplateServiceTests
{
    [Fact]
    public void CopyTradingModel_CreatesIndependentUserOwnedDraft()
    {
        var template = CreatePublishedTradingModelTemplate();

        var copy = CreateService().CopyTradingModel(
            template,
            "My Trading Model",
            ownerUserId: 42);

        Assert.Equal(42, copy.OwnerUserId);
        Assert.Equal("My Trading Model", copy.Name);
        Assert.NotEqual(template.ExternalId, copy.ExternalId);

        var sourceVersion = Assert.Single(
            template.Versions);

        var copiedVersion = Assert.Single(
            copy.Versions);

        Assert.Equal(
            TradingModelVersionStatus.Published,
            sourceVersion.Status);
        Assert.Equal(
            TradingModelVersionStatus.Draft,
            copiedVersion.Status);
        Assert.NotEqual(
            sourceVersion.ExternalId,
            copiedVersion.ExternalId);

        var copiedTimeframe = Assert.Single(
            copiedVersion.Timeframes);

        Assert.Single(copiedTimeframe.Indicators);
        Assert.Single(copiedVersion.Criteria);
    }

    [Fact]
    public void CopyScreeningStrategy_CreatesIndependentUserOwnedDraft()
    {
        var template =
            CreatePublishedScreeningStrategyTemplate();

        var copy = CreateService().CopyScreeningStrategy(
            template,
            "My Screening Strategy",
            ownerUserId: 42);

        Assert.Equal(42, copy.OwnerUserId);
        Assert.Equal(
            "My Screening Strategy",
            copy.Name);
        Assert.NotEqual(
            template.ExternalId,
            copy.ExternalId);

        var sourceVersion = Assert.Single(
            template.Versions);

        var copiedVersion = Assert.Single(
            copy.Versions);

        Assert.Equal(
            ScreeningStrategyVersionStatus.Published,
            sourceVersion.Status);
        Assert.Equal(
            ScreeningStrategyVersionStatus.Draft,
            copiedVersion.Status);

        var copiedRule = Assert.Single(
            copiedVersion.Rules);

        Assert.Equal("Maximum P/E", copiedRule.Name);
        Assert.Equal(
            15m,
            copiedRule.PrimaryValue.AsDecimal());
    }

    [Fact]
    public void CopyTradingModel_WhenSourceIsUserOwned_ThrowsInvalidOperationException()
    {
        var userModel = TradingModel.Create(
            "User Model",
            null,
            ownerUserId: 10);

        Assert.Throws<InvalidOperationException>(
            () => CreateService().CopyTradingModel(
                userModel,
                "Another User Model",
                ownerUserId: 42));
    }

    [Fact]
    public void CopyScreeningStrategy_WhenTemplateIsUnpublished_ThrowsInvalidOperationException()
    {
        var template = ScreeningStrategy.Create(
            "Unpublished Template",
            null,
            ownerUserId: null);

        Assert.Throws<InvalidOperationException>(
            () => CreateService().CopyScreeningStrategy(
                template,
                "My Strategy",
                ownerUserId: 42));
    }

    private static TradingModel
        CreatePublishedTradingModelTemplate()
    {
        var template = TradingModel.Create(
            "RTS Trading Template",
            "System trading-model template.",
            ownerUserId: null);

        var version = Assert.Single(
            template.Versions);

        var timeframe = version.AddTimeframe(
            "Daily",
            1,
            TimeframeLookbackUnit.Months,
            1,
            BarIntervalUnit.Days,
            MarketSessionMode.RegularHoursOnly);

        version.AddVolume(
            timeframe.ExternalId);

        version.AddCriterion(
            "Maximum P/E",
            "Fundamental.PriceToEarningsRatio",
            RulePurpose.Filter,
            ComparisonOperator.LessThan,
            RuleValue.FromDecimal(15m));

        template.PublishDraftVersion(
            version.ExternalId,
            version.CreatedUtc.AddMinutes(1));

        return template;
    }

    private static ScreeningStrategy
        CreatePublishedScreeningStrategyTemplate()
    {
        var template = ScreeningStrategy.Create(
            "RTS Screening Template",
            "System screening-strategy template.",
            ownerUserId: null);

        var version = Assert.Single(
            template.Versions);

        version.AddRule(
            "Maximum P/E",
            "Fundamental.PriceToEarningsRatio",
            RulePurpose.Filter,
            ComparisonOperator.LessThan,
            RuleValue.FromDecimal(15m));

        template.PublishDraftVersion(
            version.ExternalId,
            version.CreatedUtc.AddMinutes(1));

        return template;
    }

    private static TradingConfigurationTemplateService
        CreateService()
    {
        var validator =
            new TradingConfigurationValidator(
                new MetricRegistry(),
                new IndicatorRegistry());

        return new TradingConfigurationTemplateService(
            validator);
    }
}