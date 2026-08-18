using RTS.Application.CandidateDiscovery;
using RTS.Domain.CandidateDiscovery;

namespace RTS.Application.Tests.CandidateDiscovery;

public sealed class CatalystDiscoveryServiceTests
{
    private static readonly DateTime Timestamp = DateTime.UtcNow.AddDays(-1);

    [Fact]
    public async Task DiscoverAsync_accepts_cited_results_and_marks_ai_provenance()
    {
        var evidence = new CandidateEvidence("Issuer release", new Uri("https://example.test/release"), Timestamp.AddHours(-1), Timestamp, EvidenceQuality.Primary, "The issuer announced an event.");
        var provider = new StubProvider([new CandidateObservation(" abc ", AssetType.CommonStock, Timestamp, new Dictionary<string, object>(), Evidence: [evidence])]);
        var universe = CandidateUniverse.Create("test", "Test", [AssetType.CommonStock], ["ABC"]);

        var result = Assert.Single(await new CatalystDiscoveryService(provider).DiscoverAsync(universe, TestContext.Current.CancellationToken));

        Assert.Equal("ABC", result.Symbol);
        Assert.Equal(CandidateSourceType.AiCatalyst, result.Source);
        Assert.Equal(EvidenceQuality.Primary, Assert.Single(result.Evidence!).Quality);
    }

    [Fact]
    public async Task DiscoverAsync_rejects_uncited_ai_claims()
    {
        var provider = new StubProvider([new CandidateObservation("ABC", AssetType.CommonStock, Timestamp, new Dictionary<string, object>())]);
        var universe = CandidateUniverse.Create("test", "Test", [AssetType.CommonStock], ["ABC"]);
        await Assert.ThrowsAsync<InvalidOperationException>(() => new CatalystDiscoveryService(provider).DiscoverAsync(universe, TestContext.Current.CancellationToken));
    }

    private sealed class StubProvider(IReadOnlyCollection<CandidateObservation> results) : ICatalystDiscoveryProvider
    {
        public Task<IReadOnlyCollection<CandidateObservation>> DiscoverAsync(CandidateUniverse universe, CancellationToken cancellationToken = default) => Task.FromResult(results);
    }
}
