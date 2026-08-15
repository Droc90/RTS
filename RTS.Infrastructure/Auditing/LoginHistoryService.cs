using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RTS.Application.Auditing;
using RTS.Infrastructure.Persistence;

namespace RTS.Infrastructure.Auditing;

public sealed class LoginHistoryService
    : ILoginHistoryService
{
    private readonly RtsDbContext _dbContext;

    public LoginHistoryService(
        RtsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task RecordAsync(
        RecordLoginHistoryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        long? userId = null;

        if (request.UserExternalId.HasValue)
        {
            userId = await _dbContext.Users
                .Where(user =>
                    user.ExternalId ==
                    request.UserExternalId.Value)
                .Select(user => (long?)user.Id)
                .SingleOrDefaultAsync(cancellationToken);
        }

        var history = new LoginHistory
        {
            UserId = userId,
            IdentifierHash = HashIdentifier(
                request.Identifier),
            EventType = request.EventType.ToString(),
            IsSuccess = request.IsSuccess,
            FailureCode = request.FailureCode?.ToString(),
            IpAddress = Truncate(request.IpAddress, 45),
            UserAgent = Truncate(request.UserAgent, 500),
            CorrelationId = request.CorrelationId
        };

        _dbContext.LoginHistory.Add(history);

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private static byte[]? HashIdentifier(
        string? identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return null;
        }

        var normalizedIdentifier =
            identifier.Trim().ToUpperInvariant();

        var identifierBytes =
            Encoding.UTF8.GetBytes(normalizedIdentifier);

        return SHA256.HashData(identifierBytes);
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