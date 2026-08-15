using RTS.Application.Auditing;

namespace RTS.Application.Tests.Auditing;

public sealed class ApplicationErrorResolutionStatusesTests
{
    [Theory]
    [InlineData(
        ApplicationErrorResolutionStatuses.New)]
    [InlineData(
        ApplicationErrorResolutionStatuses.Investigating)]
    [InlineData(
        ApplicationErrorResolutionStatuses.Resolved)]
    [InlineData(
        ApplicationErrorResolutionStatuses.Ignored)]
    public void IsValid_ReturnsTrueForSupportedStatus(
        string status)
    {
        Assert.True(
            ApplicationErrorResolutionStatuses.IsValid(
                status));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Closed")]
    [InlineData("resolved")]
    public void IsValid_ReturnsFalseForUnsupportedStatus(
        string status)
    {
        Assert.False(
            ApplicationErrorResolutionStatuses.IsValid(
                status));
    }
}