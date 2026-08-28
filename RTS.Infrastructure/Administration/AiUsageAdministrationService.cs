using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RTS.Application.Administration;
using RTS.Application.Identity;
using RTS.Infrastructure.Identity;
using RTS.Infrastructure.Persistence;

namespace RTS.Infrastructure.Administration;

public sealed class AiUsageAdministrationService(
    RtsDbContext dbContext,
    UserManager<ApplicationUser> userManager) : IAiUsageAdministrationService
{
    public async Task<AiUsageSummary> GetUsageAsync(Guid actingUserExternalId, DateTime fromUtc,
        DateTime toUtc, CancellationToken cancellationToken = default)
    {
        if (fromUtc.Kind != DateTimeKind.Utc || toUtc.Kind != DateTimeKind.Utc || fromUtc >= toUtc)
            throw new ArgumentException("A valid UTC usage date range is required.");
        var actingUser = await dbContext.Users.SingleOrDefaultAsync(user =>
            user.ExternalId == actingUserExternalId && user.IsActive && !user.IsDeleted, cancellationToken);
        if (actingUser is null || !await userManager.IsInRoleAsync(actingUser, SystemRoleNames.Administrator))
            throw new InvalidOperationException("Administrator access is required.");

        var records = await dbContext.AiUsageRecords.AsNoTracking()
            .Where(item => item.RecordedUtc >= fromUtc && item.RecordedUtc < toUtc)
            .Join(dbContext.Users.AsNoTracking(), item => item.OwnerUserId, user => user.Id,
                (item, user) => new { Usage = item, User = user })
            .OrderByDescending(item => item.Usage.RecordedUtc)
            .Select(item => new AiUsageItem(item.Usage.ExternalId, item.Usage.RecordedUtc, item.User.Email ?? "Unknown user",
                    item.Usage.OperationType, item.Usage.Provider, item.Usage.Model, item.Usage.PromptVersion,
                    item.Usage.InputTokens, item.Usage.CachedInputTokens, item.Usage.OutputTokens,
                    item.Usage.ReasoningOutputTokens, item.Usage.TotalTokens, item.Usage.WebSearchCalls,
                    item.Usage.OperationExternalId))
            .ToArrayAsync(cancellationToken);

        return new(records.Length, records.Sum(item => (long)item.InputTokens),
            records.Sum(item => (long)item.CachedInputTokens), records.Sum(item => (long)item.OutputTokens),
            records.Sum(item => (long)item.ReasoningOutputTokens), records.Sum(item => (long)item.TotalTokens),
            records.Sum(item => item.WebSearchCalls), records);
    }
}
