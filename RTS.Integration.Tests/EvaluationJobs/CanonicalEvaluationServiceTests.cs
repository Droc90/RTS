using System.Reflection;
using RTS.Infrastructure.EvaluationJobs;

namespace RTS.Integration.Tests.EvaluationJobs;

public sealed class CanonicalEvaluationServiceTests
{
    [Theory]
    [InlineData("[]")]
    [InlineData("[\"manual\"]")]
    [InlineData("\"manual\"")]
    public void ReadAiProvenance_ReturnsNullForNonObjectCriteriaSnapshots(string snapshot)
    {
        var method = typeof(CanonicalEvaluationService).GetMethod(
            "ReadAiProvenance", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("The AI provenance parser could not be found.");

        var result = method.Invoke(null, [snapshot]);

        Assert.Null(result);
    }
}
