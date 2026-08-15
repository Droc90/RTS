using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RTS.Application.Auditing;
using RTS.Infrastructure.Persistence;

namespace RTS.Infrastructure.Auditing;

public sealed class AuditService : IAuditService
{
    private readonly RtsDbContext _dbContext;

    public AuditService(
        RtsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task RecordAsync(
        RecordAuditRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        long? actingUserId = null;

        if (request.ActingUserExternalId.HasValue)
        {
            actingUserId = await _dbContext.Users
                .Where(user =>
                    user.ExternalId ==
                    request.ActingUserExternalId.Value)
                .Select(user => (long?)user.Id)
                .SingleOrDefaultAsync(cancellationToken);
        }

        var auditLog = new AuditLog
        {
            UserId = actingUserId,
            EventCategory = Truncate(
                request.EventCategory,
                100),
            Action = Truncate(
                    request.Action.ToString(),
                    150)
                ?? throw new InvalidOperationException(
                    "An audit action is required."),
            EntityType = Truncate(
                request.EntityType,
                150),
            EntityExternalId = request.EntityExternalId,
            OldValues = SerializeValues(
                request.OldValues),
            NewValues = SerializeValues(
                request.NewValues),
            IpAddress = Truncate(
                request.IpAddress,
                45),
            UserAgent = Truncate(
                request.UserAgent,
                500),
            CorrelationId = request.CorrelationId,
            IsSuccess = request.IsSuccess,
            FailureReason = Truncate(
                request.FailureReason,
                1000)
        };

        _dbContext.AuditLogs.Add(auditLog);

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private static string? SerializeValues(
        IReadOnlyDictionary<string, object?>? values)
    {
        return values is null
            ? null
            : JsonSerializer.Serialize(values);
    }

    private static string? Truncate(
        string? value,
        int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmedValue = value.Trim();

        return trimmedValue.Length <= maximumLength
            ? trimmedValue
            : trimmedValue[..maximumLength];
    }
}