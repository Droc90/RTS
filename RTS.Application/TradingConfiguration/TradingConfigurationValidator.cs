using RTS.Domain.Rules;
using RTS.Domain.ScreeningStrategies;
using RTS.Domain.TradingModels;
using RTS.Domain.TradingModels.Indicators;

namespace RTS.Application.TradingConfiguration;

public enum ConfigurationValidationSeverity
{
    Error = 1,
    Warning = 2
}

public sealed record ConfigurationValidationIssue(
    string Code,
    string Message,
    ConfigurationValidationSeverity Severity);

public sealed class ConfigurationValidationResult
{
    public ConfigurationValidationResult(
        IReadOnlyCollection<ConfigurationValidationIssue> issues)
    {
        Issues = issues;
    }

    public IReadOnlyCollection<ConfigurationValidationIssue> Issues
    {
        get;
    }

    public bool IsValid =>
        Issues.All(
            issue =>
                issue.Severity !=
                ConfigurationValidationSeverity.Error);
}

public interface ITradingConfigurationValidator
{
    ConfigurationValidationResult Validate(
        TradingModelVersion version);

    ConfigurationValidationResult Validate(
        ScreeningStrategyVersion version);
}

public sealed class TradingConfigurationValidator
    : ITradingConfigurationValidator
{
    private readonly IMetricRegistry _metricRegistry;
    private readonly IIndicatorRegistry _indicatorRegistry;

    public TradingConfigurationValidator(
        IMetricRegistry metricRegistry,
        IIndicatorRegistry indicatorRegistry)
    {
        _metricRegistry = metricRegistry;
        _indicatorRegistry = indicatorRegistry;
    }

    public ConfigurationValidationResult Validate(
        TradingModelVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);

        var issues =
            new List<ConfigurationValidationIssue>();

        if (version.Timeframes.Count == 0)
        {
            AddError(
                issues,
                "TradingModel.Timeframes.Required",
                "A trading model requires at least one timeframe.");
        }

        foreach (var timeframe in version.Timeframes)
        {
            if (timeframe.Indicators.Count == 0)
            {
                AddError(
                    issues,
                    "TradingModel.Indicators.Required",
                    $"Timeframe '{timeframe.Name}' requires at least one indicator.");

                continue;
            }

            foreach (var indicator in timeframe.Indicators)
            {
                ValidateIndicator(
                    indicator,
                    timeframe.Name,
                    issues);
            }
        }

        if (version.Criteria.Count == 0)
        {
            AddError(
                issues,
                "TradingModel.Criteria.Required",
                "A trading model requires at least one evaluation criterion.");
        }

        foreach (var criterion in version.Criteria)
        {
            ValidateMetricRule(
                criterion.Name,
                criterion.MetricKey,
                criterion.Operator,
                criterion.PrimaryValue,
                MetricUsage.Evaluation,
                issues);
        }

        ValidateScoreWeights(
            version.Criteria
                .Where(
                    criterion =>
                        criterion.Purpose ==
                        RulePurpose.Score)
                .Select(
                    criterion =>
                        criterion.Weight),
            "TradingModel.ScoreWeights",
            issues);

        return new ConfigurationValidationResult(
            issues);
    }

    public ConfigurationValidationResult Validate(
        ScreeningStrategyVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);

        var issues =
            new List<ConfigurationValidationIssue>();

        if (version.Rules.Count == 0)
        {
            AddError(
                issues,
                "ScreeningStrategy.Rules.Required",
                "A screening strategy requires at least one rule.");
        }

        if (!version.Rules.Any(
                rule =>
                    rule.Purpose ==
                    RulePurpose.Filter))
        {
            AddWarning(
                issues,
                "ScreeningStrategy.Filter.Missing",
                "The screening strategy has no filter rules and will not exclude any candidates.");
        }

        foreach (var rule in version.Rules)
        {
            ValidateMetricRule(
                rule.Name,
                rule.MetricKey,
                rule.Operator,
                rule.PrimaryValue,
                MetricUsage.Screening,
                issues);
        }

        ValidateScoreWeights(
            version.Rules
                .Where(
                    rule =>
                        rule.Purpose ==
                        RulePurpose.Score)
                .Select(rule => rule.Weight),
            "ScreeningStrategy.ScoreWeights",
            issues);

        return new ConfigurationValidationResult(
            issues);
    }

    private void ValidateIndicator(
        TradingModelIndicator indicator,
        string timeframeName,
        ICollection<ConfigurationValidationIssue> issues)
    {
        if (!_indicatorRegistry.TryGet(
                indicator.IndicatorType,
                out var definition) ||
            definition is null)
        {
            AddError(
                issues,
                "Indicator.Unsupported",
                $"Indicator '{indicator.Name}' on timeframe '{timeframeName}' is not registered.");

            return;
        }

        foreach (var requiredParameter in
                 definition.Parameters.Where(
                     parameter =>
                         parameter.IsRequired))
        {
            var configuredParameter =
                indicator.Parameters.SingleOrDefault(
                    parameter =>
                        string.Equals(
                            parameter.Key,
                            requiredParameter.Key,
                            StringComparison.OrdinalIgnoreCase));

            if (configuredParameter is null)
            {
                AddError(
                    issues,
                    "Indicator.Parameter.Required",
                    $"Indicator '{indicator.Name}' is missing parameter '{requiredParameter.Key}'.");

                continue;
            }

            if (configuredParameter.ValueType !=
                requiredParameter.ValueType)
            {
                AddError(
                    issues,
                    "Indicator.Parameter.Type",
                    $"Indicator parameter '{requiredParameter.Key}' has the wrong value type.");
            }
        }

        foreach (var configuredParameter in indicator.Parameters)
        {
            if (!definition.Parameters.Any(
                    parameter =>
                        string.Equals(
                            parameter.Key,
                            configuredParameter.Key,
                            StringComparison.OrdinalIgnoreCase)))
            {
                AddError(
                    issues,
                    "Indicator.Parameter.Unsupported",
                    $"Indicator '{indicator.Name}' contains unsupported parameter '{configuredParameter.Key}'.");
            }
        }
    }

    private void ValidateMetricRule(
        string ruleName,
        string metricKey,
        ComparisonOperator comparisonOperator,
        RuleValue primaryValue,
        MetricUsage requiredUsage,
        ICollection<ConfigurationValidationIssue> issues)
    {
        if (!_metricRegistry.TryGet(
                metricKey,
                out var metric) ||
            metric is null)
        {
            AddError(
                issues,
                "Metric.Unsupported",
                $"Rule '{ruleName}' uses unregistered metric '{metricKey}'.");

            return;
        }

        if (!metric.Usage.HasFlag(requiredUsage))
        {
            AddError(
                issues,
                "Metric.Usage",
                $"Metric '{metricKey}' cannot be used in this configuration.");
        }

        if (metric.ValueType != primaryValue.ValueType)
        {
            AddError(
                issues,
                "Metric.ValueType",
                $"Rule '{ruleName}' does not use the value type required by metric '{metricKey}'.");
        }

        if (!metric.SupportedOperators.Contains(
                comparisonOperator))
        {
            AddError(
                issues,
                "Metric.Operator",
                $"Metric '{metricKey}' does not support operator '{comparisonOperator}'.");
        }
    }

    private static void ValidateScoreWeights(
        IEnumerable<decimal?> weights,
        string codePrefix,
        ICollection<ConfigurationValidationIssue> issues)
    {
        var configuredWeights = weights
            .Where(weight => weight.HasValue)
            .Select(weight => weight!.Value)
            .ToArray();

        if (configuredWeights.Length == 0)
        {
            return;
        }

        var totalWeight = configuredWeights.Sum();

        if (totalWeight > 100m)
        {
            AddError(
                issues,
                $"{codePrefix}.Exceeded",
                "Combined scoring-rule weights cannot exceed 100.");
        }
        else if (totalWeight < 100m)
        {
            AddWarning(
                issues,
                $"{codePrefix}.Incomplete",
                $"Combined scoring-rule weights total {totalWeight} rather than 100.");
        }
    }

    private static void AddError(
        ICollection<ConfigurationValidationIssue> issues,
        string code,
        string message)
    {
        issues.Add(
            new ConfigurationValidationIssue(
                code,
                message,
                ConfigurationValidationSeverity.Error));
    }

    private static void AddWarning(
        ICollection<ConfigurationValidationIssue> issues,
        string code,
        string message)
    {
        issues.Add(
            new ConfigurationValidationIssue(
                code,
                message,
                ConfigurationValidationSeverity.Warning));
    }
}