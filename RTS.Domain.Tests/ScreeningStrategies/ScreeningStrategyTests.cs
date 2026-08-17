using RTS.Domain.Rules;
using RTS.Domain.ScreeningStrategies;

namespace RTS.Domain.Tests.ScreeningStrategies;

public sealed class ScreeningStrategyTests
{
    [Fact]
    public void Create_WithOwner_CreatesUserStrategy()
    {
        var strategy = ScreeningStrategy.Create(
            "  Value Candidates  ",
            "  Finds value-oriented candidates.  ",
            ownerUserId: 42);

        Assert.NotEqual(Guid.Empty, strategy.ExternalId);
        Assert.Equal(42, strategy.OwnerUserId);
        Assert.Equal("Value Candidates", strategy.Name);
        Assert.Equal(
            "Finds value-oriented candidates.",
            strategy.Description);
        Assert.True(strategy.IsActive);
    }

    [Fact]
    public void Create_WithoutOwner_CreatesSystemTemplate()
    {
        var strategy = ScreeningStrategy.Create(
            "RTS Standard Screening",
            description: null,
            ownerUserId: null);

        Assert.Null(strategy.OwnerUserId);
        Assert.Null(strategy.Description);
    }

    [Fact]
    public void Create_AutomaticallyCreatesFirstDraft()
    {
        var strategy = CreateStrategy();

        var draft = Assert.Single(strategy.Versions);

        Assert.Equal(1, draft.VersionNumber);
        Assert.Equal(
            ScreeningStrategyVersionStatus.Draft,
            draft.Status);
    }

    [Fact]
    public void AddRule_AddsFilterRule()
    {
        var draft = CreateDraft();

        var rule = AddMaximumPeRule(draft);

        Assert.NotEqual(Guid.Empty, rule.ExternalId);
        Assert.Equal("Maximum P/E", rule.Name);
        Assert.Equal(
            "Fundamental.PriceToEarningsRatio",
            rule.MetricKey);
        Assert.Equal(RulePurpose.Filter, rule.Purpose);
        Assert.Equal(
            ComparisonOperator.LessThan,
            rule.Operator);
        Assert.Equal(
            15m,
            rule.PrimaryValue.AsDecimal());
        Assert.Equal(1, rule.DisplayOrder);
    }

    [Fact]
    public void AddRule_AddsWeightedRankingRule()
    {
        var draft = CreateDraft();

        var rule = draft.AddRule(
            "Revenue growth ranking",
            "Fundamental.RevenueGrowth",
            RulePurpose.Score,
            ComparisonOperator.GreaterThan,
            RuleValue.FromPercentage(10m),
            weight: 20m);

        Assert.Equal(RulePurpose.Score, rule.Purpose);
        Assert.Equal(20m, rule.Weight);
    }

    [Fact]
    public void AddRule_WithDuplicateName_ThrowsInvalidOperationException()
    {
        var draft = CreateDraft();

        AddMaximumPeRule(draft);

        Assert.Throws<InvalidOperationException>(
            () => draft.AddRule(
                "MAXIMUM P/E",
                "Fundamental.ForwardPriceToEarningsRatio",
                RulePurpose.Filter,
                ComparisonOperator.LessThan,
                RuleValue.FromDecimal(17m)));
    }

    [Fact]
    public void PublishDraftVersion_PublishesDraft()
    {
        var strategy = CreateStrategy();
        var draft = Assert.Single(strategy.Versions);
        var publishedUtc =
            draft.CreatedUtc.AddMinutes(1);

        strategy.PublishDraftVersion(
            draft.ExternalId,
            publishedUtc);

        Assert.Equal(
            ScreeningStrategyVersionStatus.Published,
            draft.Status);
        Assert.Equal(
            publishedUtc,
            draft.PublishedUtc);
    }

    [Fact]
    public void PublishingNewVersion_RetiresPreviousVersion()
    {
        var strategy = CreateStrategy();
        var firstVersion = Assert.Single(
            strategy.Versions);

        strategy.PublishDraftVersion(
            firstVersion.ExternalId,
            firstVersion.CreatedUtc.AddMinutes(1));

        var secondVersion =
            strategy.CreateDraftVersion();

        strategy.PublishDraftVersion(
            secondVersion.ExternalId,
            secondVersion.CreatedUtc.AddMinutes(1));

        Assert.Equal(
            ScreeningStrategyVersionStatus.Retired,
            firstVersion.Status);
        Assert.Equal(
            ScreeningStrategyVersionStatus.Published,
            secondVersion.Status);
    }

    [Fact]
    public void AddRule_AfterPublishing_ThrowsInvalidOperationException()
    {
        var strategy = CreateStrategy();
        var draft = Assert.Single(strategy.Versions);

        strategy.PublishDraftVersion(
            draft.ExternalId,
            draft.CreatedUtc.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(
            () => AddMaximumPeRule(draft));
    }

    [Fact]
    public void CreateDraftVersion_WhenStrategyInactive_ThrowsInvalidOperationException()
    {
        var strategy = CreateStrategy();

        strategy.Deactivate();

        Assert.Throws<InvalidOperationException>(
            () => strategy.CreateDraftVersion());
    }

    [Fact]
    public void PublishUnknownVersion_ThrowsInvalidOperationException()
    {
        var strategy = CreateStrategy();

        Assert.Throws<InvalidOperationException>(
            () => strategy.PublishDraftVersion(
                Guid.NewGuid(),
                DateTime.UtcNow));
    }

    [Fact]
    public void Publish_WithNonUtcTimestamp_ThrowsArgumentException()
    {
        var strategy = CreateStrategy();
        var draft = Assert.Single(strategy.Versions);

        var localTimestamp = DateTime.SpecifyKind(
            draft.CreatedUtc.AddMinutes(1),
            DateTimeKind.Local);

        Assert.Throws<ArgumentException>(
            () => strategy.PublishDraftVersion(
                draft.ExternalId,
                localTimestamp));
    }

    private static ScreeningRule AddMaximumPeRule(
        ScreeningStrategyVersion draft)
    {
        return draft.AddRule(
            "Maximum P/E",
            "Fundamental.PriceToEarningsRatio",
            RulePurpose.Filter,
            ComparisonOperator.LessThan,
            RuleValue.FromDecimal(15m));
    }

    private static ScreeningStrategyVersion CreateDraft()
    {
        return Assert.Single(
            CreateStrategy().Versions);
    }

    private static ScreeningStrategy CreateStrategy()
    {
        return ScreeningStrategy.Create(
            "Screening Test Strategy",
            "Strategy used by domain tests.",
            ownerUserId: 42);
    }
}