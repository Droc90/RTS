using RTS.Application.CandidateDiscovery;
using RTS.Domain.CandidateDiscovery;

namespace RTS.Application.Tests.CandidateDiscovery;

public sealed class CandidateDiscoveryServiceTests
{
    private static readonly DateTime Timestamp = DateTime.UtcNow.AddDays(-1);

    [Fact]
    public void Run_screens_ranks_explains_and_preserves_history()
    {
        var strategy = InitialCandidateMethodology.Create(DateTime.UtcNow);
        var universe = CandidateUniverse.Create("us-liquid", "US liquid securities", [AssetType.CommonStock], ["AAA", "BBB"]);
        var observations = new[]
        {
            Observation("AAA", price: 20m, liquidity: 30_000_000m, revenueGrowth: 12m, earningsGrowth: 8m, forwardPe: 25m),
            Observation("BBB", price: 3m, liquidity: 30_000_000m, revenueGrowth: -2m, earningsGrowth: 5m, forwardPe: 55m)
        };

        var result = new CandidateDiscoveryService().Run(
            universe,
            strategy.Versions.Single(),
            observations,
            new Dictionary<string, CandidateWorkflowStatus> { ["BBB"] = CandidateWorkflowStatus.Deferred },
            Timestamp);

        var first = result.Candidates.First();
        Assert.Equal("AAA", first.Symbol);
        Assert.Equal(ScreeningOutcome.Passed, first.Outcome);
        Assert.Equal(80m, first.Score);
        var second = result.Candidates.Last();
        Assert.Equal(ScreeningOutcome.Failed, second.Outcome);
        Assert.Equal(CandidateWorkflowStatus.Deferred, second.Status);
        Assert.Contains(second.Factors, factor => factor.Outcome == ScreeningOutcome.Warning);
        Assert.Contains("Minimum price", result.CriteriaSnapshot);
    }

    [Fact]
    public void Manual_watchlist_and_csv_export_are_normalized_and_repeatable()
    {
        var service = new CandidateDiscoveryService();
        var manual = service.CreateManual(" msft ", AssetType.CommonStock, Timestamp);
        var watchlist = service.CreateWatchlist(["aapl", "MSFT"], AssetType.CommonStock, Timestamp);
        Assert.Equal("MSFT", manual.Symbol);
        Assert.All(watchlist, item => Assert.Equal(CandidateSourceType.Watchlist, item.Source));

        var strategy = InitialCandidateMethodology.Create(DateTime.UtcNow);
        var universe = CandidateUniverse.Create("manual", "Manual", [AssetType.CommonStock], ["MSFT"]);
        var result = service.Run(universe, strategy.Versions.Single(), [Observation("MSFT", 20m, 30_000_000m, 1m, 1m, 20m)], startedUtc: Timestamp);
        var csv = service.ExportCsv(result);
        Assert.Contains("Rank,Symbol", csv);
        Assert.Contains("\"MSFT\"", csv);
    }

    [Fact]
    public void Run_rejects_assets_outside_the_universe()
    {
        var strategy = InitialCandidateMethodology.Create(DateTime.UtcNow);
        var universe = CandidateUniverse.Create("stocks", "Stocks", [AssetType.CommonStock], ["SPY"]);
        var observation = Observation("SPY", 500m, 1_000_000_000m, 1m, 1m, 20m) with { AssetType = AssetType.ExchangeTradedFund };
        Assert.Throws<InvalidOperationException>(() => new CandidateDiscoveryService().Run(universe, strategy.Versions.Single(), [observation], startedUtc: Timestamp));
    }

    private static CandidateObservation Observation(string symbol, decimal price, decimal liquidity, decimal revenueGrowth, decimal earningsGrowth, decimal forwardPe) =>
        new(symbol, AssetType.CommonStock, Timestamp, new Dictionary<string, object>
        {
            ["Market.Price"] = price,
            ["Market.AverageDailyDollarVolume"] = liquidity,
            ["Fundamental.RevenueGrowth"] = revenueGrowth,
            ["Fundamental.EarningsGrowth"] = earningsGrowth,
            ["Fundamental.ForwardPriceToEarningsRatio"] = forwardPe
        });
}
