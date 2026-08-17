using RTS.Domain.TradingModels;
using RTS.Domain.TradingModels.Indicators;
using RTS.Domain.TradingModels.Timeframes;

namespace RTS.Domain.Tests.TradingModels;

public sealed class TradingModelIndicatorTests
{
    [Fact]
    public void AddSimpleMovingAverage_AddsConfiguredIndicator()
    {
        var (_, draft, timeframe) = CreateDraftWithTimeframe();

        var indicator = draft.AddSimpleMovingAverage(
            timeframe.ExternalId,
            period: 50);

        Assert.NotEqual(Guid.Empty, indicator.ExternalId);
        Assert.Equal(
            TradingIndicatorType.SimpleMovingAverage,
            indicator.IndicatorType);
        Assert.Equal("SMA (50)", indicator.Name);
        Assert.Equal(IndicatorPane.Price, indicator.Pane);
        Assert.Equal(1, indicator.DisplayOrder);
        Assert.True(indicator.IsEnabled);

        var parameter = Assert.Single(indicator.Parameters);

        Assert.Equal("Period", parameter.Key);
        Assert.Equal(
            IndicatorParameterValueType.Integer,
            parameter.ValueType);
        Assert.Equal("50", parameter.Value);
    }

    [Fact]
    public void AddBollingerBands_AddsPeriodAndDeviationParameters()
    {
        var (_, draft, timeframe) = CreateDraftWithTimeframe();

        var indicator = draft.AddBollingerBands(
            timeframe.ExternalId,
            period: 20,
            standardDeviations: 2m);

        Assert.Equal(
            TradingIndicatorType.BollingerBands,
            indicator.IndicatorType);
        Assert.Equal(
            "Bollinger Bands (20, 2)",
            indicator.Name);
        Assert.Equal(IndicatorPane.Price, indicator.Pane);

        AssertParameter(
            indicator,
            "Period",
            IndicatorParameterValueType.Integer,
            "20");

        AssertParameter(
            indicator,
            "StandardDeviations",
            IndicatorParameterValueType.Decimal,
            "2");
    }

    [Fact]
    public void AddMacd_AddsThreePeriodParameters()
    {
        var (_, draft, timeframe) = CreateDraftWithTimeframe();

        var indicator = draft.AddMacd(
            timeframe.ExternalId,
            fastPeriod: 12,
            slowPeriod: 26,
            signalPeriod: 9);

        Assert.Equal(
            TradingIndicatorType.Macd,
            indicator.IndicatorType);
        Assert.Equal("MACD (12, 26, 9)", indicator.Name);
        Assert.Equal(IndicatorPane.Separate, indicator.Pane);

        AssertParameter(
            indicator,
            "FastPeriod",
            IndicatorParameterValueType.Integer,
            "12");

        AssertParameter(
            indicator,
            "SlowPeriod",
            IndicatorParameterValueType.Integer,
            "26");

        AssertParameter(
            indicator,
            "SignalPeriod",
            IndicatorParameterValueType.Integer,
            "9");
    }

    [Fact]
    public void AddRelativeStrengthIndex_AddsConfiguredIndicator()
    {
        var (_, draft, timeframe) = CreateDraftWithTimeframe();

        var indicator = draft.AddRelativeStrengthIndex(
            timeframe.ExternalId,
            period: 14);

        Assert.Equal(
            TradingIndicatorType.RelativeStrengthIndex,
            indicator.IndicatorType);
        Assert.Equal("RSI (14)", indicator.Name);
        Assert.Equal(IndicatorPane.Separate, indicator.Pane);

        AssertParameter(
            indicator,
            "Period",
            IndicatorParameterValueType.Integer,
            "14");
    }

    [Fact]
    public void AddVolume_AddsIndicatorWithoutParameters()
    {
        var (_, draft, timeframe) = CreateDraftWithTimeframe();

        var indicator = draft.AddVolume(
            timeframe.ExternalId);

        Assert.Equal(
            TradingIndicatorType.Volume,
            indicator.IndicatorType);
        Assert.Equal("Volume", indicator.Name);
        Assert.Equal(IndicatorPane.Volume, indicator.Pane);
        Assert.Empty(indicator.Parameters);
    }

    [Fact]
    public void AddIndicators_AssignsDisplayOrder()
    {
        var (_, draft, timeframe) = CreateDraftWithTimeframe();

        var first = draft.AddVolume(
            timeframe.ExternalId);

        var second = draft.AddSimpleMovingAverage(
            timeframe.ExternalId,
            period: 50);

        Assert.Equal(1, first.DisplayOrder);
        Assert.Equal(2, second.DisplayOrder);
    }

    [Fact]
    public void AddSimpleMovingAverage_AllowsDifferentPeriods()
    {
        var (_, draft, timeframe) = CreateDraftWithTimeframe();

        draft.AddSimpleMovingAverage(
            timeframe.ExternalId,
            period: 20);

        draft.AddSimpleMovingAverage(
            timeframe.ExternalId,
            period: 50);

        Assert.Equal(2, timeframe.Indicators.Count);
    }

    [Fact]
    public void AddIndicator_WithDuplicateConfiguration_ThrowsInvalidOperationException()
    {
        var (_, draft, timeframe) = CreateDraftWithTimeframe();

        draft.AddSimpleMovingAverage(
            timeframe.ExternalId,
            period: 50);

        Assert.Throws<InvalidOperationException>(
            () => draft.AddSimpleMovingAverage(
                timeframe.ExternalId,
                period: 50));
    }

    [Fact]
    public void AddIndicator_WithUnknownTimeframe_ThrowsInvalidOperationException()
    {
        var (_, draft, _) = CreateDraftWithTimeframe();

        Assert.Throws<InvalidOperationException>(
            () => draft.AddVolume(Guid.NewGuid()));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddSimpleMovingAverage_WithInvalidPeriod_ThrowsArgumentOutOfRangeException(
        int period)
    {
        var (_, draft, timeframe) = CreateDraftWithTimeframe();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => draft.AddSimpleMovingAverage(
                timeframe.ExternalId,
                period));
    }

    [Theory]
    [InlineData(12, 12)]
    [InlineData(26, 12)]
    public void AddMacd_WhenFastPeriodIsNotLessThanSlowPeriod_ThrowsArgumentException(
        int fastPeriod,
        int slowPeriod)
    {
        var (_, draft, timeframe) = CreateDraftWithTimeframe();

        Assert.Throws<ArgumentException>(
            () => draft.AddMacd(
                timeframe.ExternalId,
                fastPeriod,
                slowPeriod,
                signalPeriod: 9));
    }

    [Fact]
    public void AddIndicator_AfterPublishing_ThrowsInvalidOperationException()
    {
        var (model, draft, timeframe) = CreateDraftWithTimeframe();

        model.PublishDraftVersion(
            draft.ExternalId,
            draft.CreatedUtc.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(
            () => draft.AddVolume(
                timeframe.ExternalId));
    }

    private static void AssertParameter(
        TradingModelIndicator indicator,
        string key,
        IndicatorParameterValueType valueType,
        string value)
    {
        var parameter = Assert.Single(
            indicator.Parameters,
            item => item.Key == key);

        Assert.Equal(valueType, parameter.ValueType);
        Assert.Equal(value, parameter.Value);
    }

    private static (
        TradingModel Model,
        TradingModelVersion Draft,
        TradingModelTimeframe Timeframe)
        CreateDraftWithTimeframe()
    {
        var model = TradingModel.Create(
            "Indicator Test Model",
            "Model used by indicator tests.",
            ownerUserId: 42);

        var draft = Assert.Single(model.Versions);

        var timeframe = draft.AddTimeframe(
            "5 Day",
            5,
            TimeframeLookbackUnit.TradingDays,
            5,
            BarIntervalUnit.Minutes,
            MarketSessionMode.RegularHoursOnly);

        return (model, draft, timeframe);
    }
}