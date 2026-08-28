using RTS.Domain.Rules;

namespace RTS.Application.Charting;

public enum TechnicalFindingTone { Bullish, Neutral, Bearish, Unavailable }

public sealed record ChartEvaluationRule(string Name, string MetricKey, RulePurpose Purpose,
    ComparisonOperator Operator, decimal PrimaryValue, decimal? SecondaryValue = null, decimal? Weight = null);

public sealed record ChartEvaluationConfiguration(string Name, string Version,
    IReadOnlyDictionary<string, decimal> TimeframeWeights, IReadOnlyCollection<ChartEvaluationRule> Rules);

public interface IChartEvaluationConfigurationProvider
{
    Task<ChartEvaluationConfiguration> GetAsync(Guid userExternalId,
        CancellationToken cancellationToken = default);
}

public sealed record TechnicalRuleFinding(string Name, TechnicalFindingTone Tone, decimal Weight,
    decimal Contribution, string Explanation, RulePurpose Purpose = RulePurpose.Score,
    bool? Passed = null, string? MetricKey = null);

public sealed record TimeframeEvaluation(string TimeframeKey, string TimeframeName, decimal Score,
    TechnicalFindingTone Tone, IReadOnlyCollection<TechnicalRuleFinding> Findings,
    bool PassedMandatoryGates = true);

public enum ScenarioLevelKind { CurrentPrice, TrendReference, SupportReference, ResistanceReference }

public sealed record EvaluationScenarioLevel(
    ScenarioLevelKind Kind,
    string Name,
    decimal Price,
    decimal DistanceFromCurrentPercent,
    string TimeframeKey,
    string CalculationBasis);

public sealed record ChartEvaluationSummary(decimal OverallScore, TechnicalFindingTone Tone,
    IReadOnlyCollection<TimeframeEvaluation> Timeframes, string Explanation,
    bool PassedMandatoryGates = true, string ConfigurationName = "", string ConfigurationVersion = "",
    IReadOnlyCollection<string>? BullishEvidence = null,
    IReadOnlyCollection<string>? BearishEvidence = null,
    IReadOnlyCollection<string>? ConflictingEvidence = null,
    decimal ConfidenceScore = 0,
    string ConfidenceExplanation = "",
    IReadOnlyCollection<string>? Risks = null,
    IReadOnlyCollection<string>? Uncertainties = null,
    IReadOnlyCollection<string>? MissingDataWarnings = null,
    IReadOnlyCollection<EvaluationScenarioLevel>? ScenarioLevels = null);

public static class InitialChartEvaluationConfiguration
{
    public static ChartEvaluationConfiguration Create() => new(
        "RTS Initial Technical Model", "1.0",
        new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        { ["5D"] = 30m, ["1M"] = 30m, ["3M"] = 40m },
        [
            new("Trend integrity gate", "Technical.PriceVsSma", RulePurpose.Filter,
                ComparisonOperator.GreaterThanOrEqual, -5m),
            new("Trend versus SMA50", "Technical.PriceVsSma", RulePurpose.Score,
                ComparisonOperator.GreaterThanOrEqual, 0m, Weight: 25m),
            new("MACD momentum", "Technical.Macd", RulePurpose.Score,
                ComparisonOperator.GreaterThanOrEqual, 0m, Weight: 25m),
            new("RSI condition", "Technical.Rsi", RulePurpose.Score,
                ComparisonOperator.Between, 45m, 70m, 20m),
            new("Bollinger position", "Technical.BollingerPosition", RulePurpose.Score,
                ComparisonOperator.Between, 0.5m, 1m, 15m),
            new("Volume confirmation", "Technical.VolumeRatio", RulePurpose.Score,
                ComparisonOperator.GreaterThanOrEqual, 1m, Weight: 15m),
            new("RSI extreme warning", "Technical.Rsi", RulePurpose.Warning,
                ComparisonOperator.OutsideRange, 30m, 80m)
        ]);
}

public static class InitialChartEvaluator
{
    public static ChartEvaluationSummary Evaluate(IEnumerable<FinancialChartView> source) =>
        Evaluate(source, InitialChartEvaluationConfiguration.Create());

    public static ChartEvaluationSummary Evaluate(IEnumerable<FinancialChartView> source,
        ChartEvaluationConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        Validate(configuration);
        var views = source.ToArray();
        var evaluations = views.Select(view => EvaluateTimeframe(view, configuration.Rules)).ToArray();
        var available = evaluations.Where(item => item.Tone != TechnicalFindingTone.Unavailable).ToArray();
        if (available.Length == 0)
            return new(0, TechnicalFindingTone.Unavailable, evaluations,
                "No timeframe contains enough calculated data for technical evaluation.", false,
                configuration.Name, configuration.Version, ConfidenceScore: 0,
                ConfidenceExplanation: "Confidence is unavailable because no timeframe could be evaluated.",
                MissingDataWarnings: ["No configured timeframe contains enough calculated market data."]);

        var applicableWeight = available.Sum(item => configuration.TimeframeWeights.GetValueOrDefault(item.TimeframeKey));
        var overall = applicableWeight == 0 ? available.Average(item => item.Score) :
            available.Sum(item => item.Score * configuration.TimeframeWeights.GetValueOrDefault(item.TimeframeKey)) / applicableWeight;
        var gatesPassed = available.All(item => item.PassedMandatoryGates);
        var tone = gatesPassed ? Tone(overall) : TechnicalFindingTone.Bearish;
        var explanation = gatesPassed
            ? $"The configured timeframe weights produced this result. {ToneText(tone)}"
            : "At least one mandatory technical gate failed; the evaluation cannot be constructive regardless of its weighted score.";
        var (bullish, bearish, conflicting) = SummarizeEvidence(evaluations);
        var (confidence, confidenceExplanation, risks, uncertainties, missingData) =
            AssessReliability(views, evaluations, configuration, conflicting);
        var scenarioLevels = CalculateScenarioLevels(views);
        return new(decimal.Round(overall, 1), tone, evaluations, explanation, gatesPassed,
            configuration.Name, configuration.Version, bullish, bearish, conflicting,
            confidence, confidenceExplanation, risks, uncertainties, missingData, scenarioLevels);
    }

    private static TimeframeEvaluation EvaluateTimeframe(FinancialChartView view,
        IReadOnlyCollection<ChartEvaluationRule> rules)
    {
        if (view.Points.Count == 0)
            return new(view.Definition.Key, view.Definition.Name, 0, TechnicalFindingTone.Unavailable, [], false);
        var findings = rules.Select(rule => EvaluateRule(rule, view.Points)).ToArray();
        var scoring = findings.Where(item => item.Purpose == RulePurpose.Score && item.Passed.HasValue).ToArray();
        var possible = scoring.Sum(item => item.Weight);
        var score = possible == 0 ? 0 : scoring.Sum(item => item.Contribution) / possible * 100m;
        var gatesPassed = findings.Where(item => item.Purpose == RulePurpose.Filter).All(item => item.Passed == true);
        var tone = scoring.Length == 0 ? TechnicalFindingTone.Unavailable : gatesPassed ? Tone(score) : TechnicalFindingTone.Bearish;
        return new(view.Definition.Key, view.Definition.Name, decimal.Round(score, 1), tone, findings, gatesPassed);
    }

    private static TechnicalRuleFinding EvaluateRule(ChartEvaluationRule rule,
        IReadOnlyCollection<FinancialChartPoint> points)
    {
        var ordered = points.OrderBy(point => point.TimestampUtc).ToArray();
        var current = Metric(rule.MetricKey, ordered, ordered.Length - 1);
        var previous = ordered.Length > 1 ? Metric(rule.MetricKey, ordered, ordered.Length - 2) : null;
        var weight = rule.Weight ?? 0m;
        if (!current.HasValue)
            return new(rule.Name, TechnicalFindingTone.Unavailable, weight, 0,
                $"{MetricName(rule.MetricKey)} is unavailable because indicator warm-up is incomplete.",
                rule.Purpose, null, rule.MetricKey);

        var passed = Compare(current.Value, previous, rule);
        var contribution = rule.Purpose == RulePurpose.Score && passed ? weight : 0m;
        var tone = rule.Purpose == RulePurpose.Warning
            ? passed ? TechnicalFindingTone.Bearish : TechnicalFindingTone.Neutral
            : passed ? TechnicalFindingTone.Bullish : TechnicalFindingTone.Bearish;
        var result = rule.Purpose switch
        {
            RulePurpose.Filter => passed ? "Mandatory gate passed." : "Mandatory gate failed.",
            RulePurpose.Warning => passed ? "Warning triggered." : "Warning not triggered.",
            _ => passed ? $"Awarded {weight:0.##} points." : $"Withheld {weight:0.##} points."
        };
        return new(rule.Name, tone, weight, contribution,
            $"{MetricName(rule.MetricKey)} is {current.Value:0.###}; requires {Describe(rule)}. {result}",
            rule.Purpose, passed, rule.MetricKey);
    }

    private static decimal? Metric(string key, FinancialChartPoint[] points, int index)
    {
        if (index < 0) return null;
        var point = points[index];
        return key.ToUpperInvariant() switch
        {
            "TECHNICAL.RSI" => point.Rsi,
            "TECHNICAL.MACD" => point.MacdHistogram,
            "TECHNICAL.PRICEVSSMA" => point.Sma50 is > 0 ? (point.Close / point.Sma50.Value - 1m) * 100m : null,
            "TECHNICAL.BOLLINGERPOSITION" => BollingerPosition(point),
            "TECHNICAL.VOLUMERATIO" => VolumeRatio(points, index),
            _ => null
        };
    }

    private static decimal? BollingerPosition(FinancialChartPoint point)
    {
        if (!point.BollingerUpper.HasValue || !point.BollingerLower.HasValue) return null;
        var width = point.BollingerUpper.Value - point.BollingerLower.Value;
        return width == 0 ? 0.5m : (point.Close - point.BollingerLower.Value) / width;
    }

    private static decimal? VolumeRatio(FinancialChartPoint[] points, int index)
    {
        var prior = points.Take(index).TakeLast(20).ToArray();
        if (prior.Length == 0) return null;
        var average = prior.Average(point => point.Volume);
        return average > 0 ? points[index].Volume / average : null;
    }

    private static bool Compare(decimal current, decimal? previous, ChartEvaluationRule rule) => rule.Operator switch
    {
        ComparisonOperator.Equal => current == rule.PrimaryValue,
        ComparisonOperator.NotEqual => current != rule.PrimaryValue,
        ComparisonOperator.GreaterThan => current > rule.PrimaryValue,
        ComparisonOperator.GreaterThanOrEqual => current >= rule.PrimaryValue,
        ComparisonOperator.LessThan => current < rule.PrimaryValue,
        ComparisonOperator.LessThanOrEqual => current <= rule.PrimaryValue,
        ComparisonOperator.Between => current >= rule.PrimaryValue && current <= rule.SecondaryValue!.Value,
        ComparisonOperator.OutsideRange => current < rule.PrimaryValue || current > rule.SecondaryValue!.Value,
        ComparisonOperator.CrossesAbove => previous <= rule.PrimaryValue && current > rule.PrimaryValue,
        ComparisonOperator.CrossesBelow => previous >= rule.PrimaryValue && current < rule.PrimaryValue,
        _ => throw new NotSupportedException($"Operator {rule.Operator} is not supported for numeric evaluation metrics.")
    };

    private static void Validate(ChartEvaluationConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.Name) || string.IsNullOrWhiteSpace(configuration.Version))
            throw new ArgumentException("Evaluation configuration name and version are required.");
        if (configuration.TimeframeWeights.Count == 0 || configuration.TimeframeWeights.Values.Any(weight => weight <= 0))
            throw new ArgumentException("At least one positive timeframe weight is required.");
        var scoreWeight = configuration.Rules.Where(rule => rule.Purpose == RulePurpose.Score).Sum(rule => rule.Weight ?? 0m);
        if (scoreWeight != 100m) throw new ArgumentException("Enabled scoring-rule weights must total 100.");
        foreach (var rule in configuration.Rules)
        {
            if (rule.Purpose == RulePurpose.Score && rule.Weight is null or <= 0)
                throw new ArgumentException($"Scoring rule '{rule.Name}' requires a positive weight.");
            if (rule.Purpose != RulePurpose.Score && rule.Weight.HasValue)
                throw new ArgumentException($"Non-scoring rule '{rule.Name}' cannot have a weight.");
            if (rule.Operator is ComparisonOperator.Between or ComparisonOperator.OutsideRange && !rule.SecondaryValue.HasValue)
                throw new ArgumentException($"Range rule '{rule.Name}' requires a secondary value.");
        }
    }

    private static string Describe(ChartEvaluationRule rule) => rule.Operator switch
    {
        ComparisonOperator.Equal => $"equal to {rule.PrimaryValue:0.###}",
        ComparisonOperator.NotEqual => $"not equal to {rule.PrimaryValue:0.###}",
        ComparisonOperator.GreaterThan => $"> {rule.PrimaryValue:0.###}",
        ComparisonOperator.GreaterThanOrEqual => $">= {rule.PrimaryValue:0.###}",
        ComparisonOperator.LessThan => $"< {rule.PrimaryValue:0.###}",
        ComparisonOperator.LessThanOrEqual => $"<= {rule.PrimaryValue:0.###}",
        ComparisonOperator.Between => $"between {rule.PrimaryValue:0.###} and {rule.SecondaryValue:0.###}",
        ComparisonOperator.OutsideRange => $"outside {rule.PrimaryValue:0.###}–{rule.SecondaryValue:0.###}",
        ComparisonOperator.CrossesAbove => $"a cross above {rule.PrimaryValue:0.###}",
        ComparisonOperator.CrossesBelow => $"a cross below {rule.PrimaryValue:0.###}",
        _ => rule.Operator.ToString()
    };

    private static string MetricName(string key) => key switch
    {
        "Technical.Rsi" => "RSI",
        "Technical.Macd" => "MACD histogram",
        "Technical.PriceVsSma" => "price versus SMA50 (%)",
        "Technical.BollingerPosition" => "Bollinger position",
        "Technical.VolumeRatio" => "volume ratio",
        _ => key
    };

    private static (IReadOnlyCollection<string> Bullish, IReadOnlyCollection<string> Bearish,
        IReadOnlyCollection<string> Conflicting) SummarizeEvidence(IEnumerable<TimeframeEvaluation> timeframes)
    {
        var available = timeframes.SelectMany(timeframe => timeframe.Findings.Select(finding => new { timeframe, finding }))
            .Where(item => item.finding.Tone != TechnicalFindingTone.Unavailable).ToArray();
        var bullish = available.Where(item => item.finding.Tone == TechnicalFindingTone.Bullish)
            .Select(item => $"{item.timeframe.TimeframeName}: {item.finding.Name} — {item.finding.Explanation}").ToArray();
        var bearish = available.Where(item => item.finding.Tone == TechnicalFindingTone.Bearish)
            .Select(item => $"{item.timeframe.TimeframeName}: {item.finding.Name} — {item.finding.Explanation}").ToArray();
        var conflicting = available.Where(item => !string.IsNullOrWhiteSpace(item.finding.MetricKey))
            .GroupBy(item => item.finding.MetricKey!, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Any(item => item.finding.Tone == TechnicalFindingTone.Bullish) &&
                group.Any(item => item.finding.Tone == TechnicalFindingTone.Bearish))
            .Select(group => $"{MetricName(group.Key)} is constructive in some timeframes and weak in others.")
            .ToArray();
        return (bullish, bearish, conflicting);
    }

    private static (decimal Confidence, string Explanation, IReadOnlyCollection<string> Risks,
        IReadOnlyCollection<string> Uncertainties, IReadOnlyCollection<string> MissingData)
        AssessReliability(IReadOnlyCollection<FinancialChartView> views,
            IReadOnlyCollection<TimeframeEvaluation> evaluations,
            ChartEvaluationConfiguration configuration,
            IReadOnlyCollection<string> conflicting)
    {
        var missingData = new List<string>();
        foreach (var key in configuration.TimeframeWeights.Keys)
        {
            var view = views.FirstOrDefault(item => string.Equals(item.Definition.Key, key,
                StringComparison.OrdinalIgnoreCase));
            if (view is null || view.Points.Count == 0)
                missingData.Add($"The configured {key} timeframe has no usable chart data.");
            else if (view.MissingBarsUtc.Count > 0)
                missingData.Add($"The {view.Definition.Name} timeframe reports {view.MissingBarsUtc.Count} missing intervals.");
        }
        var unavailableCount = evaluations.SelectMany(item => item.Findings)
            .Count(finding => finding.Tone == TechnicalFindingTone.Unavailable);
        if (unavailableCount > 0)
            missingData.Add($"{unavailableCount} rule result{(unavailableCount == 1 ? " is" : "s are")} unavailable because required indicator data is incomplete.");

        var risks = evaluations.SelectMany(timeframe => timeframe.Findings
                .Where(finding => finding.Purpose == RulePurpose.Filter && finding.Passed == false ||
                    finding.Purpose == RulePurpose.Warning && finding.Passed == true)
                .Select(finding => $"{timeframe.TimeframeName}: {finding.Name} — {finding.Explanation}"))
            .ToArray();
        var uncertainties = conflicting.Append(
                "The deterministic technical score and confidence measure chart evidence only; fundamental, catalyst, and portfolio research does not alter that score.")
            .ToArray();
        var configuredCount = Math.Max(1, configuration.TimeframeWeights.Count);
        var availableCount = evaluations.Count(item => item.Tone != TechnicalFindingTone.Unavailable);
        var confidence = 100m * availableCount / configuredCount;
        confidence -= Math.Min(25m, unavailableCount * 5m);
        confidence -= Math.Min(20m, views.Sum(view => view.MissingBarsUtc.Count) * 0.5m);
        confidence -= Math.Min(15m, conflicting.Count * 5m);
        confidence = decimal.Round(Math.Clamp(confidence, 0m, 100m), 0);
        var explanation = confidence >= 85m
            ? "High confidence: configured timeframes and indicators are substantially complete and aligned."
            : confidence >= 60m
                ? "Moderate confidence: the result is usable, but data gaps, unavailable indicators, or conflicting timeframes reduce certainty."
                : "Low confidence: material data or indicator limitations make this evaluation provisional.";
        return (confidence, explanation, risks, uncertainties, missingData);
    }

    private static IReadOnlyCollection<EvaluationScenarioLevel> CalculateScenarioLevels(
        IReadOnlyCollection<FinancialChartView> views)
    {
        var view = views.FirstOrDefault(item => item.Definition.Key == "3M" && item.Points.Count > 0)
            ?? views.FirstOrDefault(item => item.Definition.Key == "1M" && item.Points.Count > 0)
            ?? views.FirstOrDefault(item => item.Points.Count > 0);
        if (view is null) return [];
        var ordered = view.Points.OrderBy(point => point.TimestampUtc).ToArray();
        var current = ordered[^1];
        if (current.Close <= 0) return [];
        var recent = ordered.TakeLast(Math.Min(20, ordered.Length)).ToArray();
        var levels = new List<EvaluationScenarioLevel>
        {
            Level(ScenarioLevelKind.CurrentPrice, "Current price", current.Close, current.Close,
                view.Definition.Key, $"Latest normalized close at {current.TimestampUtc:u}.")
        };
        if (current.Sma50 is > 0)
            levels.Add(Level(ScenarioLevelKind.TrendReference, "SMA50 trend reference",
                current.Sma50.Value, current.Close, view.Definition.Key,
                "Latest 50-period simple moving average; crossing it does not by itself constitute an entry or exit signal."));

        var swingLow = recent.Min(point => point.Low);
        var support = current.BollingerLower is > 0
            ? Math.Max(swingLow, current.BollingerLower.Value)
            : swingLow;
        levels.Add(Level(ScenarioLevelKind.SupportReference, "Support reference", support,
            current.Close, view.Definition.Key,
            current.BollingerLower is > 0
                ? "The higher of the recent 20-bar low and current lower Bollinger Band, selecting the nearer downside reference."
                : "Recent 20-bar low; Bollinger support was unavailable."));

        var swingHigh = recent.Max(point => point.High);
        var resistanceCandidates = new[] { swingHigh, current.BollingerUpper ?? 0m }
            .Where(price => price > current.Close).ToArray();
        var resistance = resistanceCandidates.Length > 0 ? resistanceCandidates.Min() : swingHigh;
        levels.Add(Level(ScenarioLevelKind.ResistanceReference, "Resistance reference", resistance,
            current.Close, view.Definition.Key,
            current.BollingerUpper is > 0
                ? "Nearest level above the current close among the recent 20-bar high and upper Bollinger Band; if neither is above price, the recent high is retained."
                : "Recent 20-bar high; Bollinger resistance was unavailable."));
        return levels;
    }

    private static EvaluationScenarioLevel Level(ScenarioLevelKind kind, string name, decimal price,
        decimal currentPrice, string timeframeKey, string basis) =>
        new(kind, name, decimal.Round(price, 2),
            decimal.Round((price / currentPrice - 1m) * 100m, 1), timeframeKey, basis);

    private static TechnicalFindingTone Tone(decimal score) => score >= 70m ? TechnicalFindingTone.Bullish : score >= 45m ? TechnicalFindingTone.Neutral : TechnicalFindingTone.Bearish;
    private static string ToneText(TechnicalFindingTone tone) => tone switch
    {
        TechnicalFindingTone.Bullish => "The available timeframes are constructively aligned.",
        TechnicalFindingTone.Neutral => "The available timeframes contain mixed evidence.",
        TechnicalFindingTone.Bearish => "The available timeframes do not currently support a constructive setup.",
        _ => "The technical evidence is unavailable."
    };
}
