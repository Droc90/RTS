using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using RTS.Application.CandidateDiscovery;
using RTS.Application.Charting;
using RTS.Domain.CandidateDiscovery;
using RTS.Infrastructure.CandidateDiscovery;

namespace RTS.Infrastructure.EvaluationJobs;

public sealed class OpenAiEvaluationResearchProvider(IOptions<OpenAiCandidateDiscoveryOptions> configuredOptions)
    : IEvaluationResearchProvider
{
    public const string PromptVersion = "1.1";
    public const string SchemaVersion = "1.0";
    private static readonly HttpClient Client = new();
    private readonly OpenAiCandidateDiscoveryOptions options = configuredOptions.Value;

    public async Task<EvaluationResearchResult> ResearchAsync(EvaluationResearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateConfiguration();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));
        using var message = new HttpRequestMessage(HttpMethod.Post, options.Endpoint);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        message.Content = JsonContent.Create(new
        {
            model = options.Model,
            tools = new[] { new { type = "web_search" } },
            input = BuildPrompt(request),
            text = new
            {
                format = new
                {
                    type = "json_schema", name = "rts_full_evaluation_research", strict = true,
                    schema = CreateSchema()
                }
            }
        });
        using var response = await Client.SendAsync(message, timeout.Token);
        var responseText = await response.Content.ReadAsStringAsync(timeout.Token);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI evaluation research failed ({(int)response.StatusCode}): {ReadError(responseText)}");
        var json = ExtractOutputText(responseText);
        var usage = OpenAiUsageReader.Read(responseText);
        var payload = JsonSerializer.Deserialize<ProviderResult>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("OpenAI returned an empty evaluation-research result.");
        var evidence = payload.Evidence.Select(ToEvidence).ToArray();
        if (evidence.Length == 0)
            throw new InvalidOperationException("OpenAI returned evaluation research without cited evidence.");
        return new(payload.Overview, payload.FundamentalFindings, payload.Catalysts, payload.Risks,
            payload.Uncertainties, evidence,
            new AiDiscoveryProvenance("OpenAI", options.Model, PromptVersion, SchemaVersion, usage));
    }

    private string BuildPrompt(EvaluationResearchRequest request) =>
        "You are the research component of the Ranked Trading System full evaluation. " +
        "Research current fundamental quality, dated catalysts, and material risks for the supplied security. " +
        "Write for a beginning investor. Lead with why each fact matters, avoid unnecessary company-product detail and industry jargon, and keep each finding concise. " +
        "Use primary sources when possible. Every factual finding must be supported by at least one returned evidence item. " +
        "Clearly separate facts from uncertainty. Do not recalculate, override, or reinterpret the deterministic technical score. " +
        "Do not provide personalized financial advice, entries, stops, price targets, or claims that a trade should be made. " +
        $"Evaluation input JSON: {JsonSerializer.Serialize(request)}";

    private static object CreateSchema() => new
    {
        type = "object", additionalProperties = false,
        required = new[] { "overview", "fundamental_findings", "catalysts", "risks", "uncertainties", "evidence" },
        properties = new
        {
            overview = new { type = "string" },
            fundamental_findings = new { type = "array", items = new { type = "string" } },
            catalysts = new { type = "array", items = new { type = "string" } },
            risks = new { type = "array", items = new { type = "string" } },
            uncertainties = new { type = "array", items = new { type = "string" } },
            evidence = new
            {
                type = "array", minItems = 1,
                items = new
                {
                    type = "object", additionalProperties = false,
                    required = new[] { "title", "url", "published_utc", "retrieved_utc", "quality", "summary" },
                    properties = new
                    {
                        title = new { type = "string" }, url = new { type = "string" },
                        published_utc = new { type = "string" }, retrieved_utc = new { type = "string" },
                        quality = new { type = "string", @enum = new[] { "Primary", "Secondary" } },
                        summary = new { type = "string" }
                    }
                }
            }
        }
    };

    private static CandidateEvidence ToEvidence(ProviderEvidence source)
    {
        if (!Uri.TryCreate(source.Url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") ||
            !DateTimeOffset.TryParse(source.PublishedUtc, out var published) ||
            !DateTimeOffset.TryParse(source.RetrievedUtc, out var retrieved) || retrieved < published ||
            !Enum.TryParse<EvidenceQuality>(source.Quality, out var quality))
            throw new InvalidOperationException("OpenAI returned invalid full-evaluation evidence.");
        return new(source.Title, uri, published.UtcDateTime, retrieved.UtcDateTime, quality, source.Summary);
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException("OpenAI full-evaluation research is not configured. Configure the candidate-discovery OpenAI API key.");
        if (string.IsNullOrWhiteSpace(options.Model))
            throw new InvalidOperationException("OpenAI full-evaluation research requires an explicitly configured model.");
        if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var endpoint) || endpoint.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("The OpenAI Responses endpoint must be an absolute HTTPS URL.");
    }

    private static string ExtractOutputText(string responseText)
    {
        using var document = JsonDocument.Parse(responseText);
        foreach (var output in document.RootElement.GetProperty("output").EnumerateArray())
            if (output.TryGetProperty("content", out var content))
                foreach (var part in content.EnumerateArray())
                    if (part.TryGetProperty("type", out var type) && type.GetString() == "output_text" &&
                        part.TryGetProperty("text", out var text)) return text.GetString()!;
        throw new InvalidOperationException("OpenAI returned no structured full-evaluation output text.");
    }

    private static string ReadError(string responseText)
    {
        try
        {
            using var document = JsonDocument.Parse(responseText);
            var root = document.RootElement;
            var error = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("error", out var value)
                ? value
                : root;
            return ReadErrorValue(error) ?? "Unknown error.";
        }
        catch (JsonException)
        {
            return "The provider returned an unreadable error.";
        }
    }

    private static string? ReadErrorValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Object when value.TryGetProperty("message", out var message) => ReadErrorValue(message),
        JsonValueKind.Object when value.TryGetProperty("error", out var error) => ReadErrorValue(error),
        JsonValueKind.Array => JoinErrors(value),
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        _ => value.ToString()
    };

    private static string? JoinErrors(JsonElement values)
    {
        var messages = values.EnumerateArray()
            .Select(ReadErrorValue)
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .ToArray();
        return messages.Length == 0 ? null : string.Join("; ", messages);
    }

    private sealed record ProviderResult(
        string Overview,
        [property: JsonPropertyName("fundamental_findings")] string[] FundamentalFindings,
        string[] Catalysts, string[] Risks, string[] Uncertainties, ProviderEvidence[] Evidence);
    private sealed record ProviderEvidence(
        string Title, string Url,
        [property: JsonPropertyName("published_utc")] string PublishedUtc,
        [property: JsonPropertyName("retrieved_utc")] string RetrievedUtc,
        string Quality, string Summary);
}
