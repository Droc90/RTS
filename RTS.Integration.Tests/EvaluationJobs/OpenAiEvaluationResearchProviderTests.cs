using System.Reflection;
using RTS.Infrastructure.EvaluationJobs;

namespace RTS.Integration.Tests.EvaluationJobs;

public sealed class OpenAiEvaluationResearchProviderTests
{
    [Fact]
    public void ReadError_ReturnsMessageFromErrorObject()
    {
        var message = ReadError("""{"error":{"message":"The request was rejected."}}""");

        Assert.Equal("The request was rejected.", message);
    }

    [Fact]
    public void ReadError_ReturnsMessagesFromErrorArray()
    {
        var message = ReadError("""{"error":[{"message":"First problem."},{"message":"Second problem."}]}""");

        Assert.Equal("First problem.; Second problem.", message);
    }

    private static string ReadError(string response)
    {
        var method = typeof(OpenAiEvaluationResearchProvider).GetMethod(
            "ReadError", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("The OpenAI error parser could not be found.");
        return (string)(method.Invoke(null, [response])
            ?? throw new InvalidOperationException("The OpenAI error parser returned no message."));
    }
}
