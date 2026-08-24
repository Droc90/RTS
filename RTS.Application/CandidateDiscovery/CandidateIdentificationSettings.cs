namespace RTS.Application.CandidateDiscovery;

public sealed record CandidateIdentificationSettings(
    bool IncludeStocks,
    bool IncludeEtfs,
    string EligibleExchanges,
    bool RequireSchwabTradability,
    decimal MinimumSharePrice,
    bool AssumeFractionalSharesAvailable,
    bool IncludeOtcSecurities,
    bool IncludeLeveragedInverseEtfs,
    bool IncludePreviouslyReviewed,
    bool IncludeRejected,
    long MinimumAverageDailyVolume,
    decimal MinimumAverageDollarVolume,
    decimal MaximumBidAskSpreadPercent,
    decimal PreferredBidAskSpreadPercent,
    bool RequireReliableExecution,
    bool RequireCurrentConsistentMarketData,
    IReadOnlyCollection<string> FundamentalCriteria,
    IReadOnlyCollection<string> CatalystCriteria,
    IReadOnlyCollection<string> RiskFlags,
    bool RequireOneShareAffordability,
    decimal MaximumInitialPositionPercent,
    decimal HardSingleNameCapPercent,
    decimal MaximumSectorExposurePercent,
    decimal MaximumCorrelatedClusterExposurePercent,
    bool ConsiderExistingSectorOverlap,
    bool RequireDiversificationBenefit,
    decimal? AvailableCash,
    IReadOnlyCollection<string> EtfCriteria)
{
    public static CandidateIdentificationSettings Defaults { get; } = new(
        true, true, "NYSE, Nasdaq, NYSE American", true, 5m, false, false, false, false, false,
        250_000, 5_000_000m, 0.50m, 0.25m, true, true,
        CandidateIdentificationCriteria.Fundamentals, CandidateIdentificationCriteria.Catalysts,
        CandidateIdentificationCriteria.RiskFlags, true, 20m, 25m, 35m, 40m, true, true, null,
        CandidateIdentificationCriteria.EtfCriteria);
}

public sealed record CandidateIdentificationSettingsDetails(CandidateIdentificationSettings Settings, byte[] RowVersion);

public interface ICandidateIdentificationSettingsService
{
    Task<CandidateIdentificationSettingsDetails> GetOrCreateAsync(Guid userExternalId, CancellationToken cancellationToken = default);
    Task<CandidateIdentificationSettingsDetails> SaveAsync(Guid userExternalId, CandidateIdentificationSettings settings, byte[] rowVersion, CancellationToken cancellationToken = default);
}

public static class CandidateIdentificationCriteria
{
    public static readonly string[] Fundamentals = ["Revenue growth", "EPS growth", "Margin direction", "Operating cash flow", "Free cash flow", "Balance-sheet strength", "Debt and refinancing risk", "Guidance direction", "Analyst-estimate revisions", "Backlog or order growth", "Customer growth and retention", "Share dilution or repurchases", "Valuation relative to growth and industry"];
    public static readonly string[] Catalysts = ["Earnings timing", "Raised or lowered guidance", "Product launches", "Contract awards", "Regulatory decisions", "Acquisition or divestiture", "Capital-return announcements", "Analyst revisions", "Sector rotation", "Commodity-price changes", "Interest-rate changes", "Government or infrastructure spending", "AI/data-center spending", "Geopolitical developments", "Catalyst date and expiration"];
    public static readonly string[] RiskFlags = ["Trading halt", "Going-concern warning", "Bankruptcy or restructuring risk", "Delisting risk", "Accounting restatement", "Fraud investigation", "Severe financing risk", "Material dilution", "Legal or regulatory action", "Product-safety or FDA issue", "Unclassified binary event", "Stale or inconsistent information"];
    public static readonly string[] EtfCriteria = ["Average volume and dollar volume", "Bid/ask spread", "Expense ratio", "Number of holdings", "Top-holding concentration", "Sector/industry concentration", "Index methodology", "Portfolio overlap", "Relative sector performance", "Macro or commodity sensitivity", "Leveraged/inverse structure"];
}
