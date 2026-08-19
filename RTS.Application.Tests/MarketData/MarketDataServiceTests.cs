using RTS.Application.MarketData;
using RTS.Domain.TradingModels.Timeframes;

namespace RTS.Application.Tests.MarketData;

public sealed class MarketDataServiceTests
{
    [Fact]
    public async Task Service_retrieves_through_provider_and_returns_owned_normalized_contracts()
    {
        var start = new DateTime(2026, 8, 17, 14, 0, 0, DateTimeKind.Utc);
        var request = new MarketDataRequest("ABC", start, start.AddHours(1), 1, BarIntervalUnit.Minutes, MarketSessionMode.RegularHoursOnly, false, false, false, true);
        var provider = new StubProvider([new PriceBar(start, 10m, 11m, 9m, 10.5m, 100m, false, true)]);

        var result = await new MarketDataService(provider, new MarketDataNormalizer()).GetNormalizedBarsAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal("stub", result.ProviderKey);
        Assert.Single(result.Bars);
        Assert.Same(request, provider.LastRequest);
    }

    [Fact]
    public async Task Service_exposes_quotes_and_corporate_actions_through_the_provider_boundary()
    {
        var start = new DateTime(2026, 8, 17, 14, 0, 0, DateTimeKind.Utc);
        var provider = new StubProvider([]);
        var service = new MarketDataService(provider, new MarketDataNormalizer());

        var quote = await service.GetQuoteAsync("ABC", TestContext.Current.CancellationToken);
        var actions = await service.GetCorporateActionsAsync("ABC", start, start.AddDays(1), TestContext.Current.CancellationToken);

        Assert.Equal("ABC", quote.Symbol);
        Assert.Equal(CorporateActionType.Split, Assert.Single(actions).Type);
    }

    private sealed class StubProvider(IReadOnlyCollection<PriceBar> bars) : IMarketPriceDataProvider
    {
        public MarketDataRequest? LastRequest { get; private set; }
        public string ProviderKey => "stub";
        public MarketDataCoveragePolicy Coverage => new(new HashSet<string> { "XNYS" }, new HashSet<string> { "USD" }, TimeSpan.FromSeconds(15), TimeSpan.FromMinutes(5), true, "Test data only.");
        public Task<IReadOnlyCollection<PriceBar>> GetBarsAsync(MarketDataRequest request, CancellationToken cancellationToken = default) { LastRequest = request; return Task.FromResult(bars); }
        public Task<MarketQuote> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default) => Task.FromResult(new MarketQuote(symbol, 10m, 11m, 10.5m, DateTime.UtcNow));
        public Task<IReadOnlyCollection<CorporateAction>> GetCorporateActionsAsync(string symbol, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<CorporateAction>>([new CorporateAction(symbol, CorporateActionType.Split, fromUtc, 2m, null, null)]);
    }
}
