using System.Globalization;
using System.Text;
using System.Text.Json;
using RTS.Domain.CandidateDiscovery;
using RTS.Domain.Rules;
using RTS.Domain.ScreeningStrategies;

namespace RTS.Application.CandidateDiscovery;

public interface ICandidateDiscoveryService
{
    DiscoveryRunResult Run(
        CandidateUniverse universe,
        ScreeningStrategyVersion strategyVersion,
        IEnumerable<CandidateObservation> observations,
        IReadOnlyDictionary<string, CandidateWorkflowStatus>? symbolHistory = null,
        DateTime? startedUtc = null);

    CandidateObservation CreateManual(string symbol, AssetType assetType, DateTime dataTimestampUtc);
    IReadOnlyCollection<CandidateObservation> CreateWatchlist(IEnumerable<string> symbols, AssetType assetType, DateTime dataTimestampUtc);
    string ExportCsv(DiscoveryRunResult run);
}

public sealed class CandidateDiscoveryService : ICandidateDiscoveryService
{
    public CandidateObservation CreateManual(string symbol, AssetType assetType, DateTime dataTimestampUtc) =>
        CreateEntry(symbol, assetType, dataTimestampUtc, CandidateSourceType.Manual);

    public IReadOnlyCollection<CandidateObservation> CreateWatchlist(IEnumerable<string> symbols, AssetType assetType, DateTime dataTimestampUtc) =>
        symbols.Select(symbol => CreateEntry(symbol, assetType, dataTimestampUtc, CandidateSourceType.Watchlist)).ToArray();

    public DiscoveryRunResult Run(
        CandidateUniverse universe,
        ScreeningStrategyVersion strategyVersion,
        IEnumerable<CandidateObservation> observations,
        IReadOnlyDictionary<string, CandidateWorkflowStatus>? symbolHistory = null,
        DateTime? startedUtc = null)
    {
        ArgumentNullException.ThrowIfNull(universe);
        ArgumentNullException.ThrowIfNull(strategyVersion);
        var started = EnsureUtc(startedUtc ?? DateTime.UtcNow, nameof(startedUtc));
        if (started > DateTime.UtcNow)
            throw new ArgumentOutOfRangeException(nameof(startedUtc), "The discovery run cannot start in the future.");
        var rules = strategyVersion.Rules.Where(rule => rule.IsEnabled).OrderBy(rule => rule.DisplayOrder).ToArray();

        var candidates = observations.Select(observation => Evaluate(universe, rules, observation, symbolHistory)).ToArray();
        var ranked = candidates.OrderBy(candidate => candidate.Outcome).ThenByDescending(candidate => candidate.Score).ThenBy(candidate => candidate.Symbol, StringComparer.Ordinal).Select((candidate, index) => candidate with { Rank = index + 1 }).ToArray();
        var snapshot = JsonSerializer.Serialize(rules.Select(rule => new { rule.Name, rule.MetricKey, rule.Purpose, rule.Operator, PrimaryValue = rule.PrimaryValue.Value, SecondaryValue = rule.SecondaryValue?.Value, rule.Weight }));

        return new DiscoveryRunResult(Guid.NewGuid(), strategyVersion.ExternalId, strategyVersion.VersionNumber, universe.Key, started, DateTime.UtcNow, snapshot, ranked);
    }

    public string ExportCsv(DiscoveryRunResult run)
    {
        var result = new StringBuilder("Rank,Symbol,AssetType,Source,Outcome,Score,Status,DataTimestampUtc\r\n");
        foreach (var candidate in run.Candidates.OrderBy(candidate => candidate.Rank))
            result.Append(Csv(candidate.Rank)).Append(',').Append(Csv(candidate.Symbol)).Append(',').Append(Csv(candidate.AssetType)).Append(',').Append(Csv(candidate.Source)).Append(',').Append(Csv(candidate.Outcome)).Append(',').Append(Csv(candidate.Score)).Append(',').Append(Csv(candidate.Status)).Append(',').Append(Csv(candidate.DataTimestampUtc.ToString("O", CultureInfo.InvariantCulture))).Append("\r\n");
        return result.ToString();
    }

    private static CandidateScreeningResult Evaluate(CandidateUniverse universe, IReadOnlyCollection<ScreeningRule> rules, CandidateObservation observation, IReadOnlyDictionary<string, CandidateWorkflowStatus>? history)
    {
        var symbol = CandidateUniverse.NormalizeSymbol(observation.Symbol);
        if (!universe.SupportedAssetTypes.Contains(observation.AssetType))
            throw new InvalidOperationException($"Asset type '{observation.AssetType}' is not supported by universe '{universe.Name}'.");
        if (universe.Symbols.Count > 0 && !universe.Symbols.Contains(symbol, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Symbol '{symbol}' is not in universe '{universe.Name}'.");
        EnsureUtc(observation.DataTimestampUtc, nameof(observation.DataTimestampUtc));

        var factors = rules.Select(rule => EvaluateRule(rule, observation.Metrics)).ToArray();
        var outcome = factors.Any(factor => factor.Outcome == ScreeningOutcome.Failed) ? ScreeningOutcome.Failed : factors.Any(factor => factor.Outcome == ScreeningOutcome.Warning) ? ScreeningOutcome.Warning : ScreeningOutcome.Passed;
        var score = factors.Sum(factor => factor.ScoreContribution);
        var status = history is not null && history.TryGetValue(symbol, out var prior) ? prior : CandidateWorkflowStatus.Proposed;
        return new CandidateScreeningResult(symbol, observation.AssetType, observation.Source, outcome, score, 0, observation.DataTimestampUtc, status, factors, observation.Evidence ?? []);
    }

    private static RuleExplanation EvaluateRule(ScreeningRule rule, IReadOnlyDictionary<string, object> metrics)
    {
        if (!metrics.TryGetValue(rule.MetricKey, out var actual))
        {
            var missingOutcome = rule.Purpose == RulePurpose.Filter ? ScreeningOutcome.Failed : ScreeningOutcome.Warning;
            return new(rule.Name, rule.MetricKey, missingOutcome, $"No value was supplied for {rule.MetricKey}.", 0);
        }

        var passed = Compare(actual, rule);
        var outcome = rule.Purpose switch
        {
            RulePurpose.Warning when passed => ScreeningOutcome.Warning,
            RulePurpose.Filter when !passed => ScreeningOutcome.Failed,
            _ => ScreeningOutcome.Passed
        };
        var contribution = rule.Purpose == RulePurpose.Score && passed ? rule.Weight!.Value : 0;
        return new(rule.Name, rule.MetricKey, outcome, $"Observed {Convert.ToString(actual, CultureInfo.InvariantCulture)} {Describe(passed)} {rule.Operator} {rule.PrimaryValue.Value}.", contribution);
    }

    private static bool Compare(object actual, ScreeningRule rule)
    {
        if (rule.PrimaryValue.ValueType == RuleValueType.Text)
        {
            var left = Convert.ToString(actual, CultureInfo.InvariantCulture) ?? string.Empty;
            var right = rule.PrimaryValue.AsText();
            return rule.Operator switch
            {
                ComparisonOperator.Equal => string.Equals(left, right, StringComparison.OrdinalIgnoreCase),
                ComparisonOperator.NotEqual => !string.Equals(left, right, StringComparison.OrdinalIgnoreCase),
                ComparisonOperator.Contains => left.Contains(right, StringComparison.OrdinalIgnoreCase),
                ComparisonOperator.In => right.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Contains(left, StringComparer.OrdinalIgnoreCase),
                _ => throw new NotSupportedException($"Operator '{rule.Operator}' is not supported for text screening.")
            };
        }

        var leftNumber = Convert.ToDecimal(actual, CultureInfo.InvariantCulture);
        var rightNumber = rule.PrimaryValue.ValueType == RuleValueType.Integer ? rule.PrimaryValue.AsInteger() : rule.PrimaryValue.AsDecimal();
        var second = rule.SecondaryValue is null ? 0 : rule.SecondaryValue.ValueType == RuleValueType.Integer ? rule.SecondaryValue.AsInteger() : rule.SecondaryValue.AsDecimal();
        return rule.Operator switch
        {
            ComparisonOperator.Equal => leftNumber == rightNumber,
            ComparisonOperator.NotEqual => leftNumber != rightNumber,
            ComparisonOperator.GreaterThan => leftNumber > rightNumber,
            ComparisonOperator.GreaterThanOrEqual => leftNumber >= rightNumber,
            ComparisonOperator.LessThan => leftNumber < rightNumber,
            ComparisonOperator.LessThanOrEqual => leftNumber <= rightNumber,
            ComparisonOperator.Between => leftNumber >= rightNumber && leftNumber <= second,
            ComparisonOperator.OutsideRange => leftNumber < rightNumber || leftNumber > second,
            _ => throw new NotSupportedException($"Operator '{rule.Operator}' requires time-series data and cannot be used by the discovery screen.")
        };
    }

    private static CandidateObservation CreateEntry(string symbol, AssetType assetType, DateTime timestamp, CandidateSourceType source)
    {
        if (!Enum.IsDefined(assetType)) throw new ArgumentOutOfRangeException(nameof(assetType));
        return new(CandidateUniverse.NormalizeSymbol(symbol), assetType, EnsureUtc(timestamp, nameof(timestamp)), new Dictionary<string, object>(), source);
    }

    private static DateTime EnsureUtc(DateTime value, string parameterName) => value.Kind == DateTimeKind.Utc ? value : throw new ArgumentException("The timestamp must be UTC.", parameterName);
    private static string Describe(bool passed) => passed ? "satisfied" : "did not satisfy";
    private static string Csv(object value) => $"\"{Convert.ToString(value, CultureInfo.InvariantCulture)!.Replace("\"", "\"\"")}\"";
}
