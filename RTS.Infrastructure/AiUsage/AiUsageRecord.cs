namespace RTS.Infrastructure.AiUsage;

public sealed class AiUsageRecord
{
    public long Id { get; set; }
    public Guid ExternalId { get; set; }
    public long OwnerUserId { get; set; }
    public Guid? OperationExternalId { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string PromptVersion { get; set; } = string.Empty;
    public string SchemaVersion { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int CachedInputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int ReasoningOutputTokens { get; set; }
    public int TotalTokens { get; set; }
    public int WebSearchCalls { get; set; }
    public DateTime RecordedUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
