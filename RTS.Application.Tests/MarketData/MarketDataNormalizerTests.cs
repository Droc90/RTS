using RTS.Application.MarketData;
using RTS.Domain.TradingModels.Timeframes;

namespace RTS.Application.Tests.MarketData;

public sealed class MarketDataNormalizerTests
{
    private static readonly DateTime Start = new(2026, 8, 17, 14, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Normalize_orders_bars_filters_extended_hours_and_reports_gaps()
    {
        var request = Request(includeExtended: false);
        var bars = new[]
        {
            Bar(Start.AddMinutes(3)),
            Bar(Start, extended: true),
            Bar(Start.AddMinutes(1))
        };

        var result = new MarketDataNormalizer().Normalize(request, bars, "test", Start.AddHours(1));

        Assert.Equal([Start.AddMinutes(1), Start.AddMinutes(3)], result.Bars.Select(bar => bar.TimestampUtc));
        Assert.Equal(Start.AddMinutes(2), Assert.Single(result.MissingBarTimestampsUtc));
    }

    [Fact]
    public void Normalize_rejects_inconsistent_ohlc_values()
    {
        var invalid = Bar(Start) with { High = 9m };
        Assert.Throws<InvalidOperationException>(() => new MarketDataNormalizer().Normalize(Request(false), [invalid], "test", Start.AddHours(1)));
    }

    [Fact]
    public void Normalize_requires_adjusted_bars_when_requested()
    {
        Assert.Throws<InvalidOperationException>(() => new MarketDataNormalizer().Normalize(Request(false) with { AdjustForCorporateActions = true }, [Bar(Start) with { IsAdjusted = false }], "test", Start.AddHours(1)));
    }

    [Fact]
    public void Regular_hours_request_cannot_silently_include_extended_hours()
    {
        Assert.Throws<ArgumentException>(() => new MarketDataNormalizer().Normalize(Request(true), [Bar(Start)], "test", Start.AddHours(1)));
    }

    private static MarketDataRequest Request(bool includeExtended) => new("ABC", Start, Start.AddHours(1), 1, BarIntervalUnit.Minutes, MarketSessionMode.RegularHoursOnly, includeExtended, includeExtended, includeExtended, false);
    private static PriceBar Bar(DateTime timestamp, bool extended = false) => new(timestamp, 10m, 12m, 9m, 11m, 1000m, extended, true);
}
