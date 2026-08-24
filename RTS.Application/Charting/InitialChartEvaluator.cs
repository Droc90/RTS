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

public sealed record ChartEvaluationSummary(decimal OverallScore, TechnicalFindingTone Tone,
    IReadOnlyCollection<TimeframeEvaluation> Timeframes, string Explanation,
    bool PassedMandatoryGates = true, string ConfigurationName = "", string ConfigurationVersion = "");

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
        var evaluations = source.Select(view => EvaluateTimeframe(view, configuration.Rules)).ToArray();
        var available = evaluations.Where(item => item.Tone != TechnicalFindingTone.Unavailable).ToArray();
        if (available.Length == 0)
            return new(0, TechnicalFindingTone.Unavailable, evaluations,
                "No timeframe contains enough calculated data for technical evaluation.", false,
                configuration.Name, configuration.Version);

        var applicableWeight = available.Sum(item => configuration.TimeframeWeights.GetValueOrDefault(item.TimeframeKey));
        var overall = applicableWeight == 0 ? available.Average(item => item.Score) :
            available.Sum(item => item.Score * configuration.TimeframeWeights.GetValueOrDefault(item.TimeframeKey)) / applicableWeight;
        var gatesPassed = available.All(item => item.PassedMandatoryGates);
        var tone = gatesPassed ? Tone(overall) : TechnicalFindingTone.Bearish;
        var explanation = gatesPassed
            ? $"The configured timeframe weights produced this result. {ToneText(tone)}"
            : "At least one mandatory technical gate failed; the evaluation cannot be constructive regardless of its weighted score.";
        return new(decimal.Round(overall, 1), tone, evaluations, explanation, gatesPassed,
            configuration.Name, configuration.Version);
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

    private static TechnicalFindingTone Tone(decimal score) => score >= 70m ? TechnicalFindingTone.Bullish : score >= 45m ? TechnicalFindingTone.Neutral : TechnicalFindingTone.Bearish;
    private static string ToneText(TechnicalFindingTone tone) => tone switch
    {
        TechnicalFindingTone.Bullish => "The available timeframes are constructively aligned.",
        TechnicalFindingTone.Neutral => "The available timeframes contain mixed evidence.",
        TechnicalFindingTone.Bearish => "The available timeframes do not currently support a constructive setup.",
        _ => "The technical evidence is unavailable."
    };
}
