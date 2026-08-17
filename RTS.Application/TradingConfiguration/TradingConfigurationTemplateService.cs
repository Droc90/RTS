using System.Globalization;
using RTS.Domain.Rules;
using RTS.Domain.ScreeningStrategies;
using RTS.Domain.TradingModels;
using RTS.Domain.TradingModels.Indicators;

namespace RTS.Application.TradingConfiguration;

public interface ITradingConfigurationTemplateService
{
    TradingModel CopyTradingModel(
        TradingModel systemTemplate,
        string newName,
        long ownerUserId);

    ScreeningStrategy CopyScreeningStrategy(
        ScreeningStrategy systemTemplate,
        string newName,
        long ownerUserId);
}

public sealed class TradingConfigurationTemplateService
    : ITradingConfigurationTemplateService
{
    private readonly ITradingConfigurationValidator _validator;

    public TradingConfigurationTemplateService(
        ITradingConfigurationValidator validator)
    {
        _validator = validator;
    }

    public TradingModel CopyTradingModel(
        TradingModel systemTemplate,
        string newName,
        long ownerUserId)
    {
        ArgumentNullException.ThrowIfNull(systemTemplate);

        EnsureSystemTemplate(
            systemTemplate.OwnerUserId,
            "trading model");

        var sourceVersion = systemTemplate.Versions
            .SingleOrDefault(
                version =>
                    version.Status ==
                    TradingModelVersionStatus.Published)
            ?? throw new InvalidOperationException(
                "The system trading-model template has no published version.");

        var copy = TradingModel.Create(
            newName,
            systemTemplate.Description,
            ownerUserId);

        var targetVersion = AssertSingle(
            copy.Versions,
            "The copied trading model did not create a draft.");

        foreach (var sourceTimeframe in
                 sourceVersion.Timeframes.OrderBy(
                     timeframe =>
                         timeframe.DisplayOrder))
        {
            var targetTimeframe =
                targetVersion.AddTimeframe(
                    sourceTimeframe.Name,
                    sourceTimeframe.LookbackValue,
                    sourceTimeframe.LookbackUnit,
                    sourceTimeframe.BarIntervalValue,
                    sourceTimeframe.BarIntervalUnit,
                    sourceTimeframe.MarketSessionMode);

            foreach (var sourceIndicator in
                     sourceTimeframe.Indicators.OrderBy(
                         indicator =>
                             indicator.DisplayOrder))
            {
                CopyIndicator(
                    sourceIndicator,
                    targetVersion,
                    targetTimeframe.ExternalId);
            }
        }

        foreach (var criterion in
                 sourceVersion.Criteria.OrderBy(
                     item =>
                         item.DisplayOrder))
        {
            targetVersion.AddCriterion(
                criterion.Name,
                criterion.MetricKey,
                criterion.Purpose,
                criterion.Operator,
                CopyValue(criterion.PrimaryValue),
                criterion.SecondaryValue is null
                    ? null
                    : CopyValue(criterion.SecondaryValue),
                criterion.Weight);
        }

        EnsureValid(
            _validator.Validate(targetVersion),
            "The copied trading model is invalid.");

        return copy;
    }

    public ScreeningStrategy CopyScreeningStrategy(
        ScreeningStrategy systemTemplate,
        string newName,
        long ownerUserId)
    {
        ArgumentNullException.ThrowIfNull(systemTemplate);

        EnsureSystemTemplate(
            systemTemplate.OwnerUserId,
            "screening strategy");

        var sourceVersion = systemTemplate.Versions
            .SingleOrDefault(
                version =>
                    version.Status ==
                    ScreeningStrategyVersionStatus.Published)
            ?? throw new InvalidOperationException(
                "The system screening-strategy template has no published version.");

        var copy = ScreeningStrategy.Create(
            newName,
            systemTemplate.Description,
            ownerUserId);

        var targetVersion = AssertSingle(
            copy.Versions,
            "The copied screening strategy did not create a draft.");

        foreach (var rule in sourceVersion.Rules.OrderBy(
                     item =>
                         item.DisplayOrder))
        {
            targetVersion.AddRule(
                rule.Name,
                rule.MetricKey,
                rule.Purpose,
                rule.Operator,
                CopyValue(rule.PrimaryValue),
                rule.SecondaryValue is null
                    ? null
                    : CopyValue(rule.SecondaryValue),
                rule.Weight);
        }

        EnsureValid(
            _validator.Validate(targetVersion),
            "The copied screening strategy is invalid.");

        return copy;
    }

    private static void CopyIndicator(
        TradingModelIndicator source,
        TradingModelVersion targetVersion,
        Guid targetTimeframeExternalId)
    {
        switch (source.IndicatorType)
        {
            case TradingIndicatorType.SimpleMovingAverage:
                targetVersion.AddSimpleMovingAverage(
                    targetTimeframeExternalId,
                    GetInteger(source, "Period"));
                break;

            case TradingIndicatorType.BollingerBands:
                targetVersion.AddBollingerBands(
                    targetTimeframeExternalId,
                    GetInteger(source, "Period"),
                    GetDecimal(
                        source,
                        "StandardDeviations"));
                break;

            case TradingIndicatorType.Macd:
                targetVersion.AddMacd(
                    targetTimeframeExternalId,
                    GetInteger(source, "FastPeriod"),
                    GetInteger(source, "SlowPeriod"),
                    GetInteger(source, "SignalPeriod"));
                break;

            case TradingIndicatorType.RelativeStrengthIndex:
                targetVersion.AddRelativeStrengthIndex(
                    targetTimeframeExternalId,
                    GetInteger(source, "Period"));
                break;

            case TradingIndicatorType.Volume:
                targetVersion.AddVolume(
                    targetTimeframeExternalId);
                break;

            default:
                throw new InvalidOperationException(
                    $"Indicator type '{source.IndicatorType}' cannot be copied.");
        }
    }

    private static int GetInteger(
        TradingModelIndicator indicator,
        string key)
    {
        var parameter = GetParameter(
            indicator,
            key,
            IndicatorParameterValueType.Integer);

        return int.Parse(
            parameter.Value,
            CultureInfo.InvariantCulture);
    }

    private static decimal GetDecimal(
        TradingModelIndicator indicator,
        string key)
    {
        var parameter = GetParameter(
            indicator,
            key,
            IndicatorParameterValueType.Decimal);

        return decimal.Parse(
            parameter.Value,
            CultureInfo.InvariantCulture);
    }

    private static TradingModelIndicatorParameter GetParameter(
        TradingModelIndicator indicator,
        string key,
        IndicatorParameterValueType expectedType)
    {
        var parameter = indicator.Parameters.SingleOrDefault(
            item =>
                string.Equals(
                    item.Key,
                    key,
                    StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"Indicator '{indicator.Name}' is missing parameter '{key}'.");

        if (parameter.ValueType != expectedType)
        {
            throw new InvalidOperationException(
                $"Indicator parameter '{key}' has the wrong value type.");
        }

        return parameter;
    }

    private static RuleValue CopyValue(
        RuleValue source)
    {
        return source.ValueType switch
        {
            RuleValueType.Integer =>
                RuleValue.FromInteger(
                    source.AsInteger()),

            RuleValueType.Decimal =>
                RuleValue.FromDecimal(
                    source.AsDecimal()),

            RuleValueType.Percentage =>
                RuleValue.FromPercentage(
                    source.AsDecimal()),

            RuleValueType.Boolean =>
                RuleValue.FromBoolean(
                    source.AsBoolean()),

            RuleValueType.Text =>
                RuleValue.FromText(
                    source.AsText()),

            _ => throw new InvalidOperationException(
                $"Rule value type '{source.ValueType}' cannot be copied.")
        };
    }

    private static T AssertSingle<T>(
        IReadOnlyCollection<T> items,
        string message)
    {
        if (items.Count != 1)
        {
            throw new InvalidOperationException(message);
        }

        return items.Single();
    }

    private static void EnsureSystemTemplate(
        long? ownerUserId,
        string configurationType)
    {
        if (ownerUserId is not null)
        {
            throw new InvalidOperationException(
                $"Only an RTS system {configurationType} template can be copied.");
        }
    }

    private static void EnsureValid(
        ConfigurationValidationResult result,
        string message)
    {
        if (result.IsValid)
        {
            return;
        }

        var errors = result.Issues
            .Where(
                issue =>
                    issue.Severity ==
                    ConfigurationValidationSeverity.Error)
            .Select(issue => issue.Message);

        throw new InvalidOperationException(
            $"{message} {string.Join(" ", errors)}");
    }
}