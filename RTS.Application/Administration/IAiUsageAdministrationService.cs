namespace RTS.Application.Administration;

public sealed record AiUsageSummary(
    int RequestCount,
    long InputTokens,
    long CachedInputTokens,
    long OutputTokens,
    long ReasoningOutputTokens,
    long TotalTokens,
    int WebSearchCalls,
    IReadOnlyCollection<AiUsageItem> Items);

public sealed record AiUsageItem(
    Guid ExternalId,
    DateTime RecordedUtc,
    string UserEmail,
    string OperationType,
    string Provider,
    string Model,
    string PromptVersion,
    int InputTokens,
    int CachedInputTokens,
    int OutputTokens,
    int ReasoningOutputTokens,
    int TotalTokens,
    int WebSearchCalls,
    Guid? OperationExternalId);

public interface IAiUsageAdministrationService
{
    Task<AiUsageSummary> GetUsageAsync(
        Guid actingUserExternalId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);
}
