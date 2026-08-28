using RTS.Domain.TradingModels.Timeframes;

namespace RTS.Application.MarketData;

public sealed record MarketDataCoveragePolicy(
    IReadOnlySet<string> SupportedExchanges,
    IReadOnlySet<string> SupportedCurrencies,
    TimeSpan MaximumQuoteAge,
    TimeSpan MaximumBarAge,
    bool CorporateActionsRequired,
    string LicensingNotice);

public sealed record MarketDataRequest(
    string Symbol,
    DateTime FromUtc,
    DateTime ToUtc,
    int IntervalValue,
    BarIntervalUnit IntervalUnit,
    MarketSessionMode SessionMode,
    bool RetrieveExtendedHours,
    bool DisplayExtendedHours,
    bool IncludeExtendedHoursInCalculations,
    bool AdjustForCorporateActions);

public static class MarketDataRequests
{
    public static MarketDataRequest InitialModel(
        string symbol,
        DateTime fromUtc,
        DateTime toUtc,
        int intervalValue,
        BarIntervalUnit intervalUnit,
        bool adjustForCorporateActions = true) =>
        new(symbol, fromUtc, toUtc, intervalValue, intervalUnit,
            MarketSessionMode.RegularHoursOnly,
            RetrieveExtendedHours: false,
            DisplayExtendedHours: false,
            IncludeExtendedHoursInCalculations: false,
            adjustForCorporateActions);
}

public sealed record PriceBar(
    DateTime TimestampUtc,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal Volume,
    bool IsExtendedHours,
    bool IsAdjusted);

public sealed record MarketQuote(
    string Symbol,
    decimal Bid,
    decimal Ask,
    decimal Last,
    DateTime TimestampUtc,
    decimal? PreviousClose = null,
    decimal? ChangePercent = null);

public enum CorporateActionType
{
    Split = 1,
    CashDividend = 2,
    SymbolChange = 3
}

public sealed record CorporateAction(
    string Symbol,
    CorporateActionType Type,
    DateTime EffectiveUtc,
    decimal? Factor,
    decimal? CashAmount,
    string? NewSymbol);

public sealed record NormalizedMarketData(
    MarketDataRequest Request,
    IReadOnlyCollection<PriceBar> Bars,
    IReadOnlyCollection<DateTime> MissingBarTimestampsUtc,
    DateTime RetrievedUtc,
    string ProviderKey);

public interface IMarketPriceDataProvider
{
    string ProviderKey { get; }
    MarketDataCoveragePolicy Coverage { get; }
    Task<IReadOnlyCollection<PriceBar>> GetBarsAsync(MarketDataRequest request, CancellationToken cancellationToken = default);
    Task<MarketQuote> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CorporateAction>> GetCorporateActionsAsync(string symbol, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
}
