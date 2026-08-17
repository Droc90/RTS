using RTS.Domain.Rules;

namespace RTS.Domain.Tests.Rules;

public sealed class RuleValueTests
{
    [Fact]
    public void FromInteger_CreatesIntegerValue()
    {
        var value = RuleValue.FromInteger(15);

        Assert.Equal(
            RuleValueType.Integer,
            value.ValueType);
        Assert.Equal("15", value.Value);
        Assert.Equal(15, value.AsInteger());
    }

    [Fact]
    public void FromDecimal_UsesInvariantFormat()
    {
        var value = RuleValue.FromDecimal(15.75m);

        Assert.Equal(
            RuleValueType.Decimal,
            value.ValueType);
        Assert.Equal("15.75", value.Value);
        Assert.Equal(15.75m, value.AsDecimal());
    }

    [Fact]
    public void FromPercentage_CreatesPercentageValue()
    {
        var value = RuleValue.FromPercentage(12.5m);

        Assert.Equal(
            RuleValueType.Percentage,
            value.ValueType);
        Assert.Equal(12.5m, value.AsDecimal());
    }

    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public void FromBoolean_UsesNormalizedValue(
        bool input,
        string expected)
    {
        var value = RuleValue.FromBoolean(input);

        Assert.Equal(expected, value.Value);
        Assert.Equal(input, value.AsBoolean());
    }

    [Fact]
    public void FromText_TrimsValue()
    {
        var value = RuleValue.FromText(
            "  Technology  ");

        Assert.Equal(
            RuleValueType.Text,
            value.ValueType);
        Assert.Equal("Technology", value.AsText());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FromText_WithBlankValue_ThrowsArgumentException(
        string input)
    {
        Assert.Throws<ArgumentException>(
            () => RuleValue.FromText(input));
    }

    [Fact]
    public void ReadingValueAsWrongType_ThrowsInvalidOperationException()
    {
        var value = RuleValue.FromBoolean(true);

        Assert.Throws<InvalidOperationException>(
            () => value.AsInteger());
    }

    [Fact]
    public void FromText_WhenValueExceedsMaximum_ThrowsArgumentException()
    {
        var input = new string('A', 501);

        Assert.Throws<ArgumentException>(
            () => RuleValue.FromText(input));
    }
}