using RTS.Domain.CandidateDiscovery;
using RTS.Domain.TradingModels.Timeframes;

namespace RTS.Application.MarketData;

public interface IMarketDataNormalizer
{
    NormalizedMarketData Normalize(MarketDataRequest request, IEnumerable<PriceBar> bars, string providerKey, DateTime retrievedUtc);
}

public sealed class MarketDataNormalizer : IMarketDataNormalizer
{
    public NormalizedMarketData Normalize(MarketDataRequest request, IEnumerable<PriceBar> bars, string providerKey, DateTime retrievedUtc)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(providerKey)) throw new ArgumentException("A provider key is required.", nameof(providerKey));
        ValidateRequest(request);
        EnsureUtc(retrievedUtc, nameof(retrievedUtc));

        var normalized = bars
            .Where(bar => request.IncludeExtendedHoursInCalculations || !bar.IsExtendedHours)
            .OrderBy(bar => bar.TimestampUtc)
            .ToArray();

        foreach (var bar in normalized) ValidateBar(bar, request.AdjustForCorporateActions);
        if (normalized.Select(bar => bar.TimestampUtc).Distinct().Count() != normalized.Length)
            throw new InvalidOperationException("Market data contains duplicate bar timestamps.");

        var missing = FindMissing(normalized, request).ToArray();
        return new NormalizedMarketData(request, normalized, missing, retrievedUtc, providerKey.Trim());
    }

    private static void ValidateRequest(MarketDataRequest request)
    {
        CandidateUniverse.NormalizeSymbol(request.Symbol);
        EnsureUtc(request.FromUtc, nameof(request.FromUtc));
        EnsureUtc(request.ToUtc, nameof(request.ToUtc));
        if (request.ToUtc <= request.FromUtc) throw new ArgumentException("The end timestamp must follow the start timestamp.");
        if (request.IntervalValue <= 0) throw new ArgumentOutOfRangeException(nameof(request.IntervalValue));
        if (!Enum.IsDefined(request.IntervalUnit) || !Enum.IsDefined(request.SessionMode)) throw new ArgumentOutOfRangeException(nameof(request));
        if ((request.DisplayExtendedHours || request.IncludeExtendedHoursInCalculations) && !request.RetrieveExtendedHours)
            throw new ArgumentException("Extended hours must be retrieved before they can be displayed or used in calculations.");
        if (request.SessionMode == MarketSessionMode.RegularHoursOnly &&
            (request.RetrieveExtendedHours || request.DisplayExtendedHours || request.IncludeExtendedHoursInCalculations))
            throw new ArgumentException("Regular-session requests cannot enable extended-hours behavior.");
    }

    private static void ValidateBar(PriceBar bar, bool adjustedRequired)
    {
        EnsureUtc(bar.TimestampUtc, nameof(bar.TimestampUtc));
        if (bar.Open <= 0 || bar.High <= 0 || bar.Low <= 0 || bar.Close <= 0 || bar.Volume < 0)
            throw new InvalidOperationException("OHLC prices must be positive and volume cannot be negative.");
        if (bar.High < Math.Max(bar.Open, bar.Close) || bar.Low > Math.Min(bar.Open, bar.Close) || bar.High < bar.Low)
            throw new InvalidOperationException("OHLC values are internally inconsistent.");
        if (adjustedRequired && !bar.IsAdjusted)
            throw new InvalidOperationException("The request requires adjusted bars but the provider returned an unadjusted bar.");
    }

    private static IEnumerable<DateTime> FindMissing(IReadOnlyList<PriceBar> bars, MarketDataRequest request)
    {
        if (bars.Count < 2) yield break;
        var interval = ToTimeSpan(request.IntervalValue, request.IntervalUnit);
        if (interval is null) yield break;
        for (var index = 1; index < bars.Count; index++)
        {
            for (var expected = bars[index - 1].TimestampUtc + interval.Value; expected < bars[index].TimestampUtc; expected += interval.Value)
                yield return expected;
        }
    }

    public static TimeSpan? ToTimeSpan(int value, BarIntervalUnit unit) => unit switch
    {
        BarIntervalUnit.Minutes => TimeSpan.FromMinutes(value),
        BarIntervalUnit.Hours => TimeSpan.FromHours(value),
        BarIntervalUnit.Days => TimeSpan.FromDays(value),
        BarIntervalUnit.Weeks => TimeSpan.FromDays(value * 7),
        _ => null
    };

    private static void EnsureUtc(DateTime value, string name)
    {
        if (value.Kind != DateTimeKind.Utc) throw new ArgumentException("Market-data timestamps must be UTC.", name);
    }
}
