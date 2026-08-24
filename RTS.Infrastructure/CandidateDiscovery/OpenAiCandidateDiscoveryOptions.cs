namespace RTS.Infrastructure.CandidateDiscovery;

public sealed class OpenAiCandidateDiscoveryOptions
{
    public const string SectionName = "AI:OpenAI:CandidateDiscovery";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "https://api.openai.com/v1/responses";
    public int TimeoutSeconds { get; set; } = 120;
}
