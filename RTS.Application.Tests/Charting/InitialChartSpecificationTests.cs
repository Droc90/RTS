using RTS.Application.Charting;
using RTS.Domain.TradingModels.Timeframes;

namespace RTS.Application.Tests.Charting;

public sealed class InitialChartSpecificationTests
{
    [Fact]
    public void Views_reproduce_the_initial_chart_specification()
    {
        var views = InitialChartSpecification.Views.ToArray();
        Assert.Equal(["5D", "1M", "3M"], views.Select(view => view.Key));
        Assert.Equal((5, BarIntervalUnit.Minutes), (views[0].BarIntervalValue, views[0].BarIntervalUnit));
        Assert.All(views, view =>
        {
            Assert.Contains("Bollinger Bands (20, 2)", view.Indicators);
            Assert.Contains("SMA50", view.Indicators);
            Assert.Contains("MACD (12, 26, 9)", view.Indicators);
            Assert.Contains("RSI (14)", view.Indicators);
            Assert.Contains("Volume", view.Indicators);
        });
    }

    [Fact]
    public void Requests_use_regular_hours_and_exact_view_ranges()
    {
        var asOf = new DateTime(2026, 8, 19, 20, 0, 0, DateTimeKind.Utc);
        var requests = InitialChartSpecification.CreateRequests("ABC", asOf).ToArray();
        Assert.Equal(TimeSpan.FromDays(5), requests[0].ToUtc - requests[0].FromUtc);
        Assert.Equal(TimeSpan.FromDays(30), requests[1].ToUtc - requests[1].FromUtc);
        Assert.Equal(TimeSpan.FromDays(90), requests[2].ToUtc - requests[2].FromUtc);
        Assert.All(requests, request =>
        {
            Assert.Equal(MarketSessionMode.RegularHoursOnly, request.SessionMode);
            Assert.False(request.RetrieveExtendedHours);
            Assert.True(request.AdjustForCorporateActions);
        });
    }
}
