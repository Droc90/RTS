using RTS.Domain.TradingModels;
using RTS.Domain.TradingModels.Timeframes;

namespace RTS.Domain.Tests.TradingModels;

public sealed class TradingModelTimeframeTests
{
    [Fact]
    public void AddTimeframe_WithValidConfiguration_AddsTimeframe()
    {
        var draft = CreateDraft();

        var timeframe = draft.AddTimeframe(
            "5 Day",
            5,
            TimeframeLookbackUnit.TradingDays,
            5,
            BarIntervalUnit.Minutes,
            MarketSessionMode.RegularHoursOnly);

        Assert.NotEqual(Guid.Empty, timeframe.ExternalId);
        Assert.Equal("5 Day", timeframe.Name);
        Assert.Equal(1, timeframe.DisplayOrder);
        Assert.Equal(5, timeframe.LookbackValue);
        Assert.Equal(
            TimeframeLookbackUnit.TradingDays,
            timeframe.LookbackUnit);
        Assert.Equal(5, timeframe.BarIntervalValue);
        Assert.Equal(
            BarIntervalUnit.Minutes,
            timeframe.BarIntervalUnit);
        Assert.Equal(
            MarketSessionMode.RegularHoursOnly,
            timeframe.MarketSessionMode);
    }

    [Fact]
    public void AddTimeframe_WithMultipleTimeframes_AssignsDisplayOrder()
    {
        var draft = CreateDraft();

        var first = AddFiveDayTimeframe(draft);

        var second = draft.AddTimeframe(
            "1 Month",
            1,
            TimeframeLookbackUnit.Months,
            1,
            BarIntervalUnit.Days,
            MarketSessionMode.RegularHoursOnly);

        Assert.Equal(1, first.DisplayOrder);
        Assert.Equal(2, second.DisplayOrder);
    }

    [Fact]
    public void AddTimeframe_TrimsName()
    {
        var draft = CreateDraft();

        var timeframe = draft.AddTimeframe(
            "  5 Day  ",
            5,
            TimeframeLookbackUnit.TradingDays,
            5,
            BarIntervalUnit.Minutes,
            MarketSessionMode.RegularHoursOnly);

        Assert.Equal("5 Day", timeframe.Name);
    }

    [Fact]
    public void AddTimeframe_WithDuplicateName_ThrowsInvalidOperationException()
    {
        var draft = CreateDraft();

        AddFiveDayTimeframe(draft);

        Assert.Throws<InvalidOperationException>(
            () => draft.AddTimeframe(
                "5 DAY",
                10,
                TimeframeLookbackUnit.TradingDays,
                15,
                BarIntervalUnit.Minutes,
                MarketSessionMode.IncludeExtendedHours));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddTimeframe_WithBlankName_ThrowsArgumentException(
        string name)
    {
        var draft = CreateDraft();

        Assert.Throws<ArgumentException>(
            () => draft.AddTimeframe(
                name,
                5,
                TimeframeLookbackUnit.TradingDays,
                5,
                BarIntervalUnit.Minutes,
                MarketSessionMode.RegularHoursOnly));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddTimeframe_WithInvalidLookback_ThrowsArgumentOutOfRangeException(
        int lookbackValue)
    {
        var draft = CreateDraft();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => draft.AddTimeframe(
                "Invalid",
                lookbackValue,
                TimeframeLookbackUnit.TradingDays,
                5,
                BarIntervalUnit.Minutes,
                MarketSessionMode.RegularHoursOnly));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddTimeframe_WithInvalidBarInterval_ThrowsArgumentOutOfRangeException(
        int intervalValue)
    {
        var draft = CreateDraft();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => draft.AddTimeframe(
                "Invalid",
                5,
                TimeframeLookbackUnit.TradingDays,
                intervalValue,
                BarIntervalUnit.Minutes,
                MarketSessionMode.RegularHoursOnly));
    }

    [Fact]
    public void AddTimeframe_WithUndefinedEnum_ThrowsArgumentOutOfRangeException()
    {
        var draft = CreateDraft();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => draft.AddTimeframe(
                "Invalid",
                5,
                (TimeframeLookbackUnit)999,
                5,
                BarIntervalUnit.Minutes,
                MarketSessionMode.RegularHoursOnly));
    }

    [Fact]
    public void AddTimeframe_AfterPublishing_ThrowsInvalidOperationException()
    {
        var model = CreateModel();
        var draft = Assert.Single(model.Versions);

        model.PublishDraftVersion(
            draft.ExternalId,
            draft.CreatedUtc.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(
            () => AddFiveDayTimeframe(draft));
    }

    private static TradingModelTimeframe AddFiveDayTimeframe(
        TradingModelVersion draft)
    {
        return draft.AddTimeframe(
            "5 Day",
            5,
            TimeframeLookbackUnit.TradingDays,
            5,
            BarIntervalUnit.Minutes,
            MarketSessionMode.RegularHoursOnly);
    }

    private static TradingModelVersion CreateDraft()
    {
        return Assert.Single(CreateModel().Versions);
    }

    private static TradingModel CreateModel()
    {
        return TradingModel.Create(
            "Test Model",
            "Model used by domain tests.",
            ownerUserId: 42);
    }
}