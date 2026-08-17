using System.Globalization;

namespace RTS.Domain.TradingModels.Indicators;

public sealed class TradingModelIndicatorParameter
{
    private TradingModelIndicatorParameter()
    {
    }

    public long Id { get; private set; }

    public Guid ExternalId { get; private set; }

    public long TradingModelIndicatorId { get; private set; }

    public string Key { get; private set; } = string.Empty;

    public IndicatorParameterValueType ValueType { get; private set; }

    public string Value { get; private set; } = string.Empty;

    internal static TradingModelIndicatorParameter CreateInteger(
        string key,
        int value)
    {
        return Create(
            key,
            IndicatorParameterValueType.Integer,
            value.ToString(CultureInfo.InvariantCulture));
    }

    internal static TradingModelIndicatorParameter CreateDecimal(
        string key,
        decimal value)
    {
        return Create(
            key,
            IndicatorParameterValueType.Decimal,
            value.ToString(CultureInfo.InvariantCulture));
    }

    internal static TradingModelIndicatorParameter CreateBoolean(
        string key,
        bool value)
    {
        return Create(
            key,
            IndicatorParameterValueType.Boolean,
            value ? "true" : "false");
    }

    internal static TradingModelIndicatorParameter CreateText(
        string key,
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A text parameter value is required.",
                nameof(value));
        }

        return Create(
            key,
            IndicatorParameterValueType.Text,
            value.Trim());
    }

    private static TradingModelIndicatorParameter Create(
        string key,
        IndicatorParameterValueType valueType,
        string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException(
                "An indicator parameter key is required.",
                nameof(key));
        }

        var normalizedKey = key.Trim();

        if (normalizedKey.Length > 100)
        {
            throw new ArgumentException(
                "An indicator parameter key cannot exceed 100 characters.",
                nameof(key));
        }

        if (value.Length > 500)
        {
            throw new ArgumentException(
                "An indicator parameter value cannot exceed 500 characters.",
                nameof(value));
        }

        return new TradingModelIndicatorParameter
        {
            ExternalId = Guid.NewGuid(),
            Key = normalizedKey,
            ValueType = valueType,
            Value = value
        };
    }
}