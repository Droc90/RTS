using System.Globalization;

namespace RTS.Domain.Rules;

public sealed class RuleValue
{
    private RuleValue()
    {
    }

    public RuleValueType ValueType { get; private set; }

    public string Value { get; private set; } = string.Empty;

    public static RuleValue FromInteger(int value)
    {
        return Create(
            RuleValueType.Integer,
            value.ToString(CultureInfo.InvariantCulture));
    }

    public static RuleValue FromDecimal(decimal value)
    {
        return Create(
            RuleValueType.Decimal,
            value.ToString(CultureInfo.InvariantCulture));
    }

    public static RuleValue FromPercentage(decimal value)
    {
        return Create(
            RuleValueType.Percentage,
            value.ToString(CultureInfo.InvariantCulture));
    }

    public static RuleValue FromBoolean(bool value)
    {
        return Create(
            RuleValueType.Boolean,
            value ? "true" : "false");
    }

    public static RuleValue FromText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A text rule value is required.",
                nameof(value));
        }

        return Create(
            RuleValueType.Text,
            value.Trim());
    }

    public int AsInteger()
    {
        EnsureType(RuleValueType.Integer);

        return int.Parse(
            Value,
            CultureInfo.InvariantCulture);
    }

    public decimal AsDecimal()
    {
        if (ValueType is not (
            RuleValueType.Decimal or
            RuleValueType.Percentage))
        {
            throw new InvalidOperationException(
                "The rule value is not numeric.");
        }

        return decimal.Parse(
            Value,
            CultureInfo.InvariantCulture);
    }

    public bool AsBoolean()
    {
        EnsureType(RuleValueType.Boolean);

        return bool.Parse(Value);
    }

    public string AsText()
    {
        EnsureType(RuleValueType.Text);

        return Value;
    }

    private static RuleValue Create(
        RuleValueType valueType,
        string value)
    {
        if (value.Length > 500)
        {
            throw new ArgumentException(
                "A rule value cannot exceed 500 characters.",
                nameof(value));
        }

        return new RuleValue
        {
            ValueType = valueType,
            Value = value
        };
    }

    private void EnsureType(
        RuleValueType expectedType)
    {
        if (ValueType != expectedType)
        {
            throw new InvalidOperationException(
                $"The rule value is not {expectedType}.");
        }
    }
}