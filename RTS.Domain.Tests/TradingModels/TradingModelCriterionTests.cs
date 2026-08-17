using RTS.Domain.Rules;
using RTS.Domain.TradingModels;
using RTS.Domain.TradingModels.Criteria;

namespace RTS.Domain.Tests.TradingModels;

public sealed class TradingModelCriterionTests
{
    [Fact]
    public void AddCriterion_AddsFilterCriterion()
    {
        var draft = CreateDraft();

        var criterion = draft.AddCriterion(
            "Maximum P/E",
            "Fundamental.PriceToEarningsRatio",
            RulePurpose.Filter,
            ComparisonOperator.LessThan,
            RuleValue.FromDecimal(15m));

        Assert.NotEqual(Guid.Empty, criterion.ExternalId);
        Assert.Equal("Maximum P/E", criterion.Name);
        Assert.Equal(
            "Fundamental.PriceToEarningsRatio",
            criterion.MetricKey);
        Assert.Equal(
            RulePurpose.Filter,
            criterion.Purpose);
        Assert.Equal(
            ComparisonOperator.LessThan,
            criterion.Operator);
        Assert.Equal(
            15m,
            criterion.PrimaryValue.AsDecimal());
        Assert.Null(criterion.SecondaryValue);
        Assert.Null(criterion.Weight);
        Assert.Equal(1, criterion.DisplayOrder);
        Assert.True(criterion.IsEnabled);
    }

    [Fact]
    public void AddCriterion_AddsWeightedScoringCriterion()
    {
        var draft = CreateDraft();

        var criterion = draft.AddCriterion(
            "RSI score",
            "Technical.Rsi",
            RulePurpose.Score,
            ComparisonOperator.Between,
            RuleValue.FromDecimal(40m),
            RuleValue.FromDecimal(60m),
            weight: 15m);

        Assert.Equal(
            RulePurpose.Score,
            criterion.Purpose);
        Assert.Equal(15m, criterion.Weight);
        Assert.Equal(
            40m,
            criterion.PrimaryValue.AsDecimal());
        Assert.Equal(
            60m,
            criterion.SecondaryValue!.AsDecimal());
    }

    [Fact]
    public void AddCriterion_AssignsDisplayOrder()
    {
        var draft = CreateDraft();

        var first = AddMaximumPeCriterion(draft);

        var second = draft.AddCriterion(
            "Positive earnings growth",
            "Fundamental.EarningsGrowth",
            RulePurpose.Warning,
            ComparisonOperator.LessThan,
            RuleValue.FromPercentage(0m));

        Assert.Equal(1, first.DisplayOrder);
        Assert.Equal(2, second.DisplayOrder);
    }

    [Fact]
    public void AddCriterion_WithDuplicateName_ThrowsInvalidOperationException()
    {
        var draft = CreateDraft();

        AddMaximumPeCriterion(draft);

        Assert.Throws<InvalidOperationException>(
            () => draft.AddCriterion(
                "MAXIMUM P/E",
                "Fundamental.ForwardPriceToEarningsRatio",
                RulePurpose.Filter,
                ComparisonOperator.LessThan,
                RuleValue.FromDecimal(17m)));
    }

    [Fact]
    public void AddCriterion_RangeWithoutSecondaryValue_ThrowsArgumentException()
    {
        var draft = CreateDraft();

        Assert.Throws<ArgumentException>(
            () => draft.AddCriterion(
                "RSI range",
                "Technical.Rsi",
                RulePurpose.Filter,
                ComparisonOperator.Between,
                RuleValue.FromDecimal(40m)));
    }

    [Fact]
    public void AddCriterion_NonRangeWithSecondaryValue_ThrowsArgumentException()
    {
        var draft = CreateDraft();

        Assert.Throws<ArgumentException>(
            () => draft.AddCriterion(
                "Invalid criterion",
                "Technical.Rsi",
                RulePurpose.Filter,
                ComparisonOperator.LessThan,
                RuleValue.FromDecimal(60m),
                RuleValue.FromDecimal(40m)));
    }

    [Fact]
    public void AddCriterion_WithMismatchedRangeTypes_ThrowsArgumentException()
    {
        var draft = CreateDraft();

        Assert.Throws<ArgumentException>(
            () => draft.AddCriterion(
                "Invalid range",
                "Technical.Rsi",
                RulePurpose.Filter,
                ComparisonOperator.Between,
                RuleValue.FromInteger(40),
                RuleValue.FromDecimal(60m)));
    }

    [Fact]
    public void AddCriterion_NumericOperatorWithTextValue_ThrowsArgumentException()
    {
        var draft = CreateDraft();

        Assert.Throws<ArgumentException>(
            () => draft.AddCriterion(
                "Invalid numeric comparison",
                "Fundamental.Sector",
                RulePurpose.Filter,
                ComparisonOperator.GreaterThan,
                RuleValue.FromText("Technology")));
    }

    [Fact]
    public void AddCriterion_ContainsWithNumericValue_ThrowsArgumentException()
    {
        var draft = CreateDraft();

        Assert.Throws<ArgumentException>(
            () => draft.AddCriterion(
                "Invalid contains comparison",
                "Fundamental.PriceToEarningsRatio",
                RulePurpose.Filter,
                ComparisonOperator.Contains,
                RuleValue.FromDecimal(15m)));
    }

    [Fact]
    public void AddCriterion_ScoreWithoutWeight_ThrowsArgumentOutOfRangeException()
    {
        var draft = CreateDraft();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => draft.AddCriterion(
                "Unweighted score",
                "Technical.Rsi",
                RulePurpose.Score,
                ComparisonOperator.GreaterThan,
                RuleValue.FromDecimal(50m)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void AddCriterion_WithInvalidScoreWeight_ThrowsArgumentOutOfRangeException(
        int weight)
    {
        var draft = CreateDraft();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => draft.AddCriterion(
                "Invalid score",
                "Technical.Rsi",
                RulePurpose.Score,
                ComparisonOperator.GreaterThan,
                RuleValue.FromDecimal(50m),
                weight: weight));
    }

    [Fact]
    public void AddCriterion_NonScoreWithWeight_ThrowsArgumentException()
    {
        var draft = CreateDraft();

        Assert.Throws<ArgumentException>(
            () => draft.AddCriterion(
                "Weighted filter",
                "Fundamental.PriceToEarningsRatio",
                RulePurpose.Filter,
                ComparisonOperator.LessThan,
                RuleValue.FromDecimal(15m),
                weight: 10m));
    }

    [Fact]
    public void AddCriterion_AfterPublishing_ThrowsInvalidOperationException()
    {
        var model = CreateModel();
        var draft = Assert.Single(model.Versions);

        model.PublishDraftVersion(
            draft.ExternalId,
            draft.CreatedUtc.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(
            () => AddMaximumPeCriterion(draft));
    }

    private static TradingModelCriterion AddMaximumPeCriterion(
        TradingModelVersion draft)
    {
        return draft.AddCriterion(
            "Maximum P/E",
            "Fundamental.PriceToEarningsRatio",
            RulePurpose.Filter,
            ComparisonOperator.LessThan,
            RuleValue.FromDecimal(15m));
    }

    private static TradingModelVersion CreateDraft()
    {
        return Assert.Single(CreateModel().Versions);
    }

    private static TradingModel CreateModel()
    {
        return TradingModel.Create(
            "Criterion Test Model",
            "Model used by criterion tests.",
            ownerUserId: 42);
    }
}