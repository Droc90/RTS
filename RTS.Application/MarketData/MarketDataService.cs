using RTS.Domain.TradingModels.Indicators;

namespace RTS.Application.MarketData;

public interface IMarketDataService
{
    Task<NormalizedMarketData> GetNormalizedBarsAsync(MarketDataRequest request, CancellationToken cancellationToken = default);
}

public sealed class MarketDataService(IMarketPriceDataProvider provider, IMarketDataNormalizer normalizer) : IMarketDataService
{
    public async Task<NormalizedMarketData> GetNormalizedBarsAsync(MarketDataRequest request, CancellationToken cancellationToken = default)
    {
        var bars = await provider.GetBarsAsync(request, cancellationToken);
        return normalizer.Normalize(request, bars, provider.ProviderKey, DateTime.UtcNow);
    }
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
