using System.Text.Json;
using RTS.Application.CandidateDiscovery;

namespace RTS.Infrastructure.CandidateDiscovery;

internal static class OpenAiUsageReader
{
    public static AiUsageMetrics Read(string responseText)
    {
        using var document = JsonDocument.Parse(responseText);
        var root = document.RootElement;
        var usage = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("usage", out var value)
            ? value
            : default;
        return new(
            ReadInt(usage, "input_tokens"),
            ReadNestedInt(usage, "input_tokens_details", "cached_tokens"),
            ReadInt(usage, "output_tokens"),
            ReadNestedInt(usage, "output_tokens_details", "reasoning_tokens"),
            ReadInt(usage, "total_tokens"),
            CountWebSearchCalls(root));
    }

    private static int ReadInt(JsonElement parent, string name) =>
        parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(name, out var value) &&
        value.TryGetInt32(out var result) ? result : 0;

    private static int ReadNestedInt(JsonElement parent, string objectName, string valueName) =>
        parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(objectName, out var nested)
            ? ReadInt(nested, valueName)
            : 0;

    private static int CountWebSearchCalls(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("output", out var output) ||
            output.ValueKind != JsonValueKind.Array) return 0;
        return output.EnumerateArray().Count(item => item.ValueKind == JsonValueKind.Object &&
            item.TryGetProperty("type", out var type) &&
            string.Equals(type.GetString(), "web_search_call", StringComparison.OrdinalIgnoreCase));
    }
}
