using System.Reflection;
using RTS.Application.CandidateDiscovery;
using RTS.Infrastructure.CandidateDiscovery;

namespace RTS.Integration.Tests.CandidateDiscovery;

public sealed class OpenAiUsageReaderTests
{
    [Fact]
    public void Read_CapturesTokenDetailsAndWebSearchCalls()
    {
        const string response = """
            {
              "usage": {
                "input_tokens": 120,
                "input_tokens_details": { "cached_tokens": 20 },
                "output_tokens": 45,
                "output_tokens_details": { "reasoning_tokens": 10 },
                "total_tokens": 165
              },
              "output": [
                { "type": "web_search_call" },
                { "type": "message" },
                { "type": "web_search_call" }
              ]
            }
            """;

        var usage = Read(response);

        Assert.Equal(new AiUsageMetrics(120, 20, 45, 10, 165, 2), usage);
    }

    [Fact]
    public void Read_DefaultsMissingOptionalUsageDetailsToZero()
    {
        var usage = Read("""{"usage":{"input_tokens":3,"output_tokens":2,"total_tokens":5},"output":[]}""");

        Assert.Equal(new AiUsageMetrics(3, 0, 2, 0, 5, 0), usage);
    }

    private static AiUsageMetrics Read(string response)
    {
        var reader = typeof(OpenAiEducationalCandidateDiscoveryProvider).Assembly
            .GetType("RTS.Infrastructure.CandidateDiscovery.OpenAiUsageReader")
            ?? throw new InvalidOperationException("The OpenAI usage reader could not be found.");
        var method = reader.GetMethod("Read", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("The OpenAI usage reader method could not be found.");
        return (AiUsageMetrics)(method.Invoke(null, [response])
            ?? throw new InvalidOperationException("No usage metrics were returned."));
    }
}
