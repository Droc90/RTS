using RTS.Domain.TradingModels.Indicators;

namespace RTS.Application.MarketData;

public interface IMarketDataService
{
    Task<NormalizedMarketData> GetNormalizedBarsAsync(MarketDataRequest request, CancellationToken cancellationToken = default);
    Task<NormalizedMarketData> GetNormalizedBarsWithWarmupAsync(MarketDataRequest request, IEnumerable<TradingModelIndicator> indicators, CancellationToken cancellationToken = default);
    Task<MarketQuote> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CorporateAction>> GetCorporateActionsAsync(string symbol, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
}

public sealed class MarketDataService(IMarketPriceDataProvider provider, IMarketDataNormalizer normalizer) : IMarketDataService
{
    public async Task<NormalizedMarketData> GetNormalizedBarsAsync(MarketDataRequest request, CancellationToken cancellationToken = default)
    {
        var bars = await provider.GetBarsAsync(request, cancellationToken);
        return normalizer.Normalize(request, bars, provider.ProviderKey, DateTime.UtcNow);
    }

    public Task<NormalizedMarketData> GetNormalizedBarsWithWarmupAsync(MarketDataRequest request, IEnumerable<TradingModelIndicator> indicators, CancellationToken cancellationToken = default)
    {
        var warmupBars = MarketDataWarmup.RequiredBars(indicators);
        var interval = MarketDataNormalizer.ToTimeSpan(request.IntervalValue, request.IntervalUnit)
            ?? throw new NotSupportedException("Warm-up retrieval requires a fixed-length bar interval.");
        var expanded = request with { FromUtc = request.FromUtc - TimeSpan.FromTicks(interval.Ticks * warmupBars) };
        return GetNormalizedBarsAsync(expanded, cancellationToken);
    }

    public Task<MarketQuote> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default) =>
        provider.GetQuoteAsync(symbol, cancellationToken);

    public Task<IReadOnlyCollection<CorporateAction>> GetCorporateActionsAsync(string symbol, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default) =>
        provider.GetCorporateActionsAsync(symbol, fromUtc, toUtc, cancellationToken);
}

public interface IMarketDataSnapshotStore
{
    Task<Guid> SaveAsync(Guid evaluationJobExternalId, NormalizedMarketData data, CancellationToken cancellationToken = default);
    Task<NormalizedMarketData?> GetAsync(Guid snapshotExternalId, CancellationToken cancellationToken = default);
}

public static class MarketDataWarmup
{
    public static int RequiredBars(IEnumerable<TradingModelIndicator> indicators)
    {
        var periods = indicators.SelectMany(indicator => indicator.Parameters)
            .Where(parameter => parameter.Key.EndsWith("Period", StringComparison.OrdinalIgnoreCase))
            .Select(parameter => int.TryParse(parameter.Value, out var value) ? value : 0);
        var maximum = periods.DefaultIfEmpty(0).Max();
        return maximum == 0 ? 0 : maximum + Math.Max(10, maximum / 2);
    }
}
