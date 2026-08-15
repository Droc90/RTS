using System.ComponentModel.DataAnnotations;
using RTS.Web.Configuration;

namespace RTS.Integration.Tests.Configuration;

public sealed class SecurityOptionsTests
{
    [Fact]
    public void Validate_DefaultSettingsAreValid()
    {
        var options = new SecurityOptions();

        var results = Validate(options);

        Assert.Empty(results);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(731)]
    public void Validate_RejectsInvalidHstsMaxAge(
        int maxAgeDays)
    {
        var options = new SecurityOptions
        {
            HstsMaxAgeDays = maxAgeDays
        };

        var results = Validate(options);

        Assert.Contains(
            results,
            result => result.MemberNames.Contains(
                nameof(SecurityOptions.HstsMaxAgeDays)));
    }

    private static IReadOnlyCollection<ValidationResult> Validate(
        SecurityOptions options)
    {
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(
            options,
            new ValidationContext(options),
            results,
            validateAllProperties: true);

        return results;
    }
}