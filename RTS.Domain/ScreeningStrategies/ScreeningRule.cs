using RTS.Domain.Rules;

namespace RTS.Domain.ScreeningStrategies;

public sealed class ScreeningRule
{
    private ScreeningRule()
    {
    }

    public long Id { get; private set; }

    public Guid ExternalId { get; private set; }

    public long ScreeningStrategyVersionId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string MetricKey { get; private set; } = string.Empty;

    public RulePurpose Purpose { get; private set; }

    public ComparisonOperator Operator { get; private set; }

    public RuleValue PrimaryValue { get; private set; } = null!;

    public RuleValue? SecondaryValue { get; private set; }

    public decimal? Weight { get; private set; }

    public int DisplayOrder { get; private set; }

    public bool IsEnabled { get; private set; }

    internal static ScreeningRule Create(
        string name,
        string metricKey,
        RulePurpose purpose,
        ComparisonOperator comparisonOperator,
        RuleValue primaryValue,
        RuleValue? secondaryValue,
        decimal? weight,
        int displayOrder)
    {
        var normalizedName = ValidateRequiredText(
            name,
            maximumLength: 150,
            nameof(name));

        var normalizedMetricKey = ValidateRequiredText(
            metricKey,
            maximumLength: 150,
            nameof(metricKey));

        if (!Enum.IsDefined(purpose))
        {
            throw new ArgumentOutOfRangeException(
                nameof(purpose));
        }

        if (!Enum.IsDefined(comparisonOperator))
        {
            throw new ArgumentOutOfRangeException(
                nameof(comparisonOperator));
        }

        ArgumentNullException.ThrowIfNull(primaryValue);

        ValidateValues(
            comparisonOperator,
            primaryValue,
            secondaryValue);

        ValidateWeight(
            purpose,
            weight);

        if (displayOrder <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(displayOrder),
                "Display order must be positive.");
        }

        return new ScreeningRule
        {
            ExternalId = Guid.NewGuid(),
            Name = normalizedName,
            MetricKey = normalizedMetricKey,
            Purpose = purpose,
            Operator = comparisonOperator,
            PrimaryValue = primaryValue,
            SecondaryValue = secondaryValue,
            Weight = weight,
            DisplayOrder = displayOrder,
            IsEnabled = true
        };
    }

    private static void ValidateValues(
        ComparisonOperator comparisonOperator,
        RuleValue primaryValue,
        RuleValue? secondaryValue)
    {
        var isRange =
            comparisonOperator is
                ComparisonOperator.Between or
                ComparisonOperator.OutsideRange;

        if (isRange && secondaryValue is null)
        {
            throw new ArgumentException(
                "Range operators require a secondary value.",
                nameof(secondaryValue));
        }

        if (!isRange && secondaryValue is not null)
        {
            throw new ArgumentException(
                "A secondary value is only valid for a range operator.",
                nameof(secondaryValue));
        }

        if (secondaryValue is not null &&
            secondaryValue.ValueType != primaryValue.ValueType)
        {
            throw new ArgumentException(
                "Range values must use the same data type.",
                nameof(secondaryValue));
        }

        if (comparisonOperator is
                ComparisonOperator.GreaterThan or
                ComparisonOperator.GreaterThanOrEqual or
                ComparisonOperator.LessThan or
                ComparisonOperator.LessThanOrEqual or
                ComparisonOperator.Between or
                ComparisonOperator.OutsideRange or
                ComparisonOperator.CrossesAbove or
                ComparisonOperator.CrossesBelow)
        {
            EnsureNumeric(primaryValue);

            if (secondaryValue is not null)
            {
                EnsureNumeric(secondaryValue);
            }
        }

        if (comparisonOperator is
                ComparisonOperator.Contains or
                ComparisonOperator.In &&
            primaryValue.ValueType != RuleValueType.Text)
        {
            throw new ArgumentException(
                "Contains and In operators require a text value.",
                nameof(primaryValue));
        }
    }

    private static void ValidateWeight(
        RulePurpose purpose,
        decimal? weight)
    {
        if (purpose == RulePurpose.Score)
        {
            if (weight is null or <= 0 or > 100)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(weight),
                    "Scoring-rule weight must be between 0 and 100.");
            }

            return;
        }

        if (weight is not null)
        {
            throw new ArgumentException(
                "Only scoring rules can have a weight.",
                nameof(weight));
        }
    }

    private static void EnsureNumeric(
        RuleValue value)
    {
        if (value.ValueType is not (
            RuleValueType.Integer or
            RuleValueType.Decimal or
            RuleValueType.Percentage))
        {
            throw new ArgumentException(
                "The selected operator requires a numeric value.",
                nameof(value));
        }
    }

    private static string ValidateRequiredText(
        string value,
        int maximumLength,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A value is required.",
                parameterName);
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > maximumLength)
        {
            throw new ArgumentException(
                $"The value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalizedValue;
    }
}