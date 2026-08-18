using RTS.Domain.CandidateDiscovery;

namespace RTS.Application.CandidateDiscovery;

public sealed class CatalystDiscoveryService(ICatalystDiscoveryProvider provider) : ICatalystDiscoveryService
{
    public async Task<IReadOnlyCollection<CandidateObservation>> DiscoverAsync(
        CandidateUniverse universe,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(universe);
        var discovered = await provider.DiscoverAsync(universe, cancellationToken);

        return discovered.Select(observation =>
        {
            var symbol = CandidateUniverse.NormalizeSymbol(observation.Symbol);
            if (!universe.SupportedAssetTypes.Contains(observation.AssetType))
                throw new InvalidOperationException($"Catalyst result '{symbol}' has an unsupported asset type.");
            if (universe.Symbols.Count > 0 && !universe.Symbols.Contains(symbol, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Catalyst result '{symbol}' is outside the requested universe.");
            if (observation.DataTimestampUtc.Kind != DateTimeKind.Utc)
                throw new InvalidOperationException($"Catalyst result '{symbol}' has a non-UTC data timestamp.");

            var evidence = observation.Evidence?.ToArray() ?? [];
            if (evidence.Length == 0)
                throw new InvalidOperationException($"Catalyst result '{symbol}' has no cited evidence.");
            foreach (var item in evidence)
            {
                if (string.IsNullOrWhiteSpace(item.Title) || string.IsNullOrWhiteSpace(item.Summary) ||
                    item.Source.Scheme is not ("http" or "https") ||
                    item.PublishedUtc.Kind != DateTimeKind.Utc || item.RetrievedUtc.Kind != DateTimeKind.Utc ||
                    item.RetrievedUtc < item.PublishedUtc || item.Quality == EvidenceQuality.Unknown)
                    throw new InvalidOperationException($"Catalyst result '{symbol}' contains invalid or ungrounded evidence.");
            }

            return observation with { Symbol = symbol, Source = CandidateSourceType.AiCatalyst, Evidence = evidence };
        }).ToArray();
    }
}
