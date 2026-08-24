using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using RTS.Application.CandidateDiscovery;
using RTS.Domain.CandidateDiscovery;

namespace RTS.Infrastructure.CandidateDiscovery;

public sealed class OpenAiEducationalCandidateDiscoveryProvider(IOptions<OpenAiCandidateDiscoveryOptions> options) : IEducationalCandidateDiscoveryProvider
{
    private static readonly HttpClient Client = new();
    private readonly OpenAiCandidateDiscoveryOptions options = options.Value;

    public async Task<EducationalDiscoveryReport> DiscoverAsync(EducationalDiscoveryRequest request, CancellationToken cancellationToken = default)
    {
        ValidateConfiguration();
        var generatedUtc = DateTime.UtcNow;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));
        using var message = new HttpRequestMessage(HttpMethod.Post, options.Endpoint);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        message.Content = JsonContent.Create(new
        {
            model = options.Model,
            tools = new[] { new { type = "web_search" } },
            input = BuildPrompt(request, generatedUtc),
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "educational_candidate_discovery",
                    strict = true,
                    schema = CreateSchema()
                }
            }
        });

        using var response = await Client.SendAsync(message, timeout.Token);
        var responseText = await response.Content.ReadAsStringAsync(timeout.Token);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI candidate discovery failed ({(int)response.StatusCode}): {ReadError(responseText)}");
        var json = ExtractOutputText(responseText);
        var payload = JsonSerializer.Deserialize<ProviderReport>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("OpenAI returned an empty candidate-discovery report.");
        return ToReport(payload, generatedUtc, request);
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey)) throw new InvalidOperationException("OpenAI candidate discovery is not configured. Set AI:OpenAI:CandidateDiscovery:ApiKey using user secrets or an environment variable.");
        if (string.IsNullOrWhiteSpace(options.Model)) throw new InvalidOperationException("OpenAI candidate discovery requires an explicitly configured model.");
        if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var endpoint) || endpoint.Scheme != Uri.UriSchemeHttps) throw new InvalidOperationException("The OpenAI Responses endpoint must be an absolute HTTPS URL.");
        if (options.TimeoutSeconds is < 10 or > 300) throw new InvalidOperationException("The OpenAI candidate-discovery timeout must be between 10 and 300 seconds.");
    }

    private static string BuildPrompt(EducationalDiscoveryRequest request, DateTime now) =>
        "You are the educational candidate-discovery component of RTS. Research current, cited market candidates. " +
        "Return educational research, not financial advice. Never claim that candidate quality is a technical evaluation score. " +
        "Use primary sources when possible; every candidate requires at least one working source URL and retrieval timestamp. " +
        "Respect exclusions. Return only credible candidates with a quality score of at least 70/100; omit anything that would immediately fail discovery. " +
        "Candidate quality must be expressed on a 0-100 scale (for example, 85 rather than 0.85). " +
        "The primary_thesis is the short RTS modifier/reason: state the timely catalyst, fundamental signal, portfolio reason, or setup modifier and then what confirmation is still needed. " +
        "Do not use primary_thesis merely to describe the company's business. Keep it to one concise sentence, such as 'Post-earnings cooling and data-center demand; needs charts.' " +
        "Explain portfolio fit, risks, and what a later technical evaluation would inspect. " +
        $"Current UTC: {now:O}. User context JSON: {JsonSerializer.Serialize(request)}";

    private static object CreateSchema() => new
    {
        type = "object", additionalProperties = false,
        required = new[] { "summary", "recommended_evaluation_order", "candidates" },
        properties = new
        {
            summary = new { type = "string" },
            recommended_evaluation_order = new { type = "array", items = new { type = "string" } },
            candidates = new { type = "array", items = new
            {
                type = "object", additionalProperties = false,
                required = new[] { "rank", "symbol", "asset_type", "type_description", "candidate_quality", "initial_status", "primary_thesis", "portfolio_fit", "key_risks", "educational_context", "evidence" },
                properties = new
                {
                    rank = new { type = "integer" }, symbol = new { type = "string" },
                    asset_type = new { type = "string", @enum = new[] { "CommonStock", "ExchangeTradedFund", "AmericanDepositaryReceipt" } },
                    type_description = new { type = "string" }, candidate_quality = new { type = "number", minimum = 70, maximum = 100 },
                    initial_status = new { type = "string" }, primary_thesis = new { type = "string" },
                    portfolio_fit = new { type = "string" }, key_risks = new { type = "string" }, educational_context = new { type = "string" },
                    evidence = new { type = "array", items = new
                    {
                        type = "object", additionalProperties = false,
                        required = new[] { "title", "url", "published_utc", "retrieved_utc", "quality", "summary" },
                        properties = new { title = new { type = "string" }, url = new { type = "string" }, published_utc = new { type = "string" }, retrieved_utc = new { type = "string" }, quality = new { type = "string", @enum = new[] { "Primary", "Secondary" } }, summary = new { type = "string" } }
                    }}
                }
            }}
        }
    };

    private static string ExtractOutputText(string responseText)
    {
        using var document = JsonDocument.Parse(responseText);
        foreach (var output in document.RootElement.GetProperty("output").EnumerateArray())
            if (output.TryGetProperty("content", out var content))
                foreach (var part in content.EnumerateArray())
                    if (part.TryGetProperty("type", out var type) && type.GetString() == "output_text" && part.TryGetProperty("text", out var text)) return text.GetString()!;
        throw new InvalidOperationException("OpenAI returned no structured output text.");
    }

    private static string ReadError(string responseText)
    {
        try { using var document = JsonDocument.Parse(responseText); return document.RootElement.GetProperty("error").GetProperty("message").GetString() ?? "Unknown error."; }
        catch (JsonException) { return "The provider returned an unreadable error."; }
    }

    private static EducationalDiscoveryReport ToReport(ProviderReport payload, DateTime generatedUtc, EducationalDiscoveryRequest request)
    {
        var excluded = request.ExcludedSymbols.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (payload.Candidates.Length > request.MaximumCandidates)
            throw new InvalidOperationException("OpenAI returned more candidates than requested.");
        var candidates = payload.Candidates.Select(item =>
        {
            var symbol = CandidateUniverse.NormalizeSymbol(item.Symbol);
            if (excluded.Contains(symbol)) throw new InvalidOperationException($"OpenAI returned excluded symbol '{symbol}'.");
            var candidateQuality = item.CandidateQuality is > 0 and <= 1 ? item.CandidateQuality * 100 : item.CandidateQuality;
            if (candidateQuality is < 70 or > 100) throw new InvalidOperationException($"Candidate quality for '{symbol}' must be between 70 and 100.");
            var evidence = item.Evidence.Select(source =>
            {
                if (!Uri.TryCreate(source.Url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") ||
                    !DateTimeOffset.TryParse(source.PublishedUtc, out var published) ||
                    !DateTimeOffset.TryParse(source.RetrievedUtc, out var retrieved) || retrieved < published)
                    throw new InvalidOperationException($"OpenAI returned invalid evidence for '{symbol}'.");
                return new CandidateEvidence(source.Title, uri, published.UtcDateTime, retrieved.UtcDateTime,
                    Enum.Parse<EvidenceQuality>(source.Quality), source.Summary);
            }).ToArray();
            if (evidence.Length == 0) throw new InvalidOperationException($"OpenAI returned uncited candidate '{symbol}'.");
            return new EducationalCandidate(item.Rank, symbol, Enum.Parse<AssetType>(item.AssetType), item.TypeDescription,
                candidateQuality, item.InitialStatus, item.PrimaryThesis, item.PortfolioFit, item.KeyRisks, item.EducationalContext, evidence);
        }).OrderBy(item => item.Rank).ToArray();
        return new EducationalDiscoveryReport(Guid.NewGuid(), generatedUtc, payload.Summary, payload.RecommendedEvaluationOrder, candidates);
    }

    private sealed record ProviderReport(
        string Summary,
        [property: JsonPropertyName("recommended_evaluation_order")] string[] RecommendedEvaluationOrder,
        ProviderCandidate[] Candidates);
    private sealed record ProviderCandidate(
        int Rank, string Symbol,
        [property: JsonPropertyName("asset_type")] string AssetType,
        [property: JsonPropertyName("type_description")] string TypeDescription,
        [property: JsonPropertyName("candidate_quality")] decimal CandidateQuality,
        [property: JsonPropertyName("initial_status")] string InitialStatus,
        [property: JsonPropertyName("primary_thesis")] string PrimaryThesis,
        [property: JsonPropertyName("portfolio_fit")] string PortfolioFit,
        [property: JsonPropertyName("key_risks")] string KeyRisks,
        [property: JsonPropertyName("educational_context")] string EducationalContext,
        ProviderEvidence[] Evidence);
    private sealed record ProviderEvidence(
        string Title, string Url,
        [property: JsonPropertyName("published_utc")] string PublishedUtc,
        [property: JsonPropertyName("retrieved_utc")] string RetrievedUtc,
        string Quality, string Summary);
}
