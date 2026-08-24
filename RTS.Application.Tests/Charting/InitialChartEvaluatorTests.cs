using RTS.Application.Charting;

namespace RTS.Application.Tests.Charting;

public sealed class InitialChartEvaluatorTests
{
    [Fact]
    public void Evaluate_scores_constructive_evidence_and_explains_every_rule()
    {
        var definition = InitialChartSpecification.Views.Single(view => view.Key == "3M");
        var start = new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc);
        var points = new[]
        {
            Point(start, 104m, 100m),
            Point(start.AddDays(1), 105m, 200m)
        };

        var result = InitialChartEvaluator.Evaluate([
            new FinancialChartView(definition, points, "test", start.AddDays(1), [])]);

        var timeframe = Assert.Single(result.Timeframes);
        Assert.Equal(100m, timeframe.Score);
        Assert.Equal(TechnicalFindingTone.Bullish, result.Tone);
        Assert.Equal(7, timeframe.Findings.Count);
        Assert.All(timeframe.Findings, finding => Assert.False(string.IsNullOrWhiteSpace(finding.Explanation)));
    }

    [Fact]
    public void Evaluate_failed_filter_overrides_a_high_weighted_score()
    {
        var definition = InitialChartSpecification.Views.Single(view => view.Key == "3M");
        var timestamp = new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc);
        var points = new[] { Point(timestamp, 90m, 100m), Point(timestamp.AddDays(1), 94m, 200m) };

        var result = InitialChartEvaluator.Evaluate([
            new FinancialChartView(definition, points, "test", timestamp.AddDays(1), [])]);

        Assert.False(result.PassedMandatoryGates);
        Assert.Equal(TechnicalFindingTone.Bearish, result.Tone);
        Assert.Contains(Assert.Single(result.Timeframes).Findings,
            finding => finding.Purpose == RTS.Domain.Rules.RulePurpose.Filter && finding.Passed == false);
    }

    [Fact]
    public void Evaluate_uses_supplied_rule_weights_and_thresholds()
    {
        var definition = InitialChartSpecification.Views.Single(view => view.Key == "3M");
        var timestamp = new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc);
        var configuration = new ChartEvaluationConfiguration("Custom", "2",
            new Dictionary<string, decimal> { ["3M"] = 100m },
            [new("RSI must exceed 70", "Technical.Rsi", RTS.Domain.Rules.RulePurpose.Score,
                RTS.Domain.Rules.ComparisonOperator.GreaterThan, 70m, Weight: 100m)]);

        var result = InitialChartEvaluator.Evaluate([
            new FinancialChartView(definition, [Point(timestamp, 105m, 100m)], "test", timestamp, [])], configuration);

        Assert.Equal(0m, result.OverallScore);
        Assert.Equal("Custom", result.ConfigurationName);
        Assert.False(Assert.Single(Assert.Single(result.Timeframes).Findings).Passed);
    }

    [Fact]
    public void Evaluate_reports_unavailable_when_no_chart_data_exists()
    {
        var result = InitialChartEvaluator.Evaluate([]);

        Assert.Equal(TechnicalFindingTone.Unavailable, result.Tone);
        Assert.Equal(0m, result.OverallScore);
    }

    private static FinancialChartPoint Point(DateTime timestamp, decimal close, decimal volume) =>
        new(timestamp, close - 1m, close + 1m, close - 2m, close, volume,
            Sma50: 100m, BollingerUpper: 110m, BollingerMiddle: 100m, BollingerLower: 90m,
            Macd: 2m, MacdSignal: 1m, MacdHistogram: 1m, Rsi: 60m);
}
