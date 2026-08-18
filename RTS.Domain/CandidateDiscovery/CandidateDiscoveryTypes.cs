namespace RTS.Domain.CandidateDiscovery;

public enum AssetType
{
    CommonStock = 1,
    ExchangeTradedFund = 2,
    AmericanDepositaryReceipt = 3
}

public enum CandidateSourceType
{
    Manual = 1,
    Watchlist = 2,
    DeterministicScreen = 3,
    AiCatalyst = 4
}

public enum CandidateWorkflowStatus
{
    Proposed = 1,
    Selected = 2,
    Rejected = 3,
    Deferred = 4,
    Excluded = 5,
    PreviouslyEvaluated = 6
}

public enum ScreeningOutcome
{
    Passed = 1,
    Warning = 2,
    Failed = 3
}

public enum EvidenceQuality
{
    Unknown = 1,
    Secondary = 2,
    Primary = 3
}

public sealed record CandidateUniverse(
    string Key,
    string Name,
    IReadOnlySet<AssetType> SupportedAssetTypes,
    IReadOnlyCollection<string> Symbols)
{
    public static CandidateUniverse Create(
        string key,
        string name,
        IEnumerable<AssetType> supportedAssetTypes,
        IEnumerable<string> symbols)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A universe key and name are required.");

        var types = supportedAssetTypes.ToHashSet();
        if (types.Count == 0 || types.Any(type => !Enum.IsDefined(type)))
            throw new ArgumentException("At least one valid asset type is required.");

        return new CandidateUniverse(
            key.Trim(),
            name.Trim(),
            types,
            symbols.Select(NormalizeSymbol).Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    public static string NormalizeSymbol(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("A symbol is required.", nameof(symbol));

        var normalized = symbol.Trim().ToUpperInvariant();
        if (normalized.Length > 15 || normalized.Any(character => !char.IsLetterOrDigit(character) && character is not '.' and not '-'))
            throw new ArgumentException("The symbol format is invalid.", nameof(symbol));

        return normalized;
    }
}
