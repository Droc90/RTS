using Microsoft.EntityFrameworkCore;
using RTS.Application.Auditing;
using RTS.Infrastructure.Persistence;

namespace RTS.Infrastructure.Auditing;

public sealed class ApplicationErrorService
    : IApplicationErrorService
{
    private readonly RtsDbContext _dbContext;

    public ApplicationErrorService(
        RtsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task RecordAsync(
        RecordApplicationErrorRequest request,
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

        var applicationError = new ApplicationError
        {
            UserId = userId,
            CorrelationId = request.CorrelationId,
            ErrorType = Truncate(
                    request.ErrorType,
                    256)
                ?? "UnknownError",
            ErrorCode = Truncate(
                request.ErrorCode,
                100),
            SafeMessage = Truncate(
                request.SafeMessage,
                1000),
            DiagnosticDetails =
                request.DiagnosticDetails,
            Source = Truncate(
                request.Source,
                256),
            RequestPath = Truncate(
                request.RequestPath,
                2048),
            HttpMethod = Truncate(
                request.HttpMethod,
                10),
            StatusCode = IsValidStatusCode(
                request.StatusCode)
                    ? request.StatusCode
                    : null,
            ResolutionStatus = "New"
        };

        _dbContext.ApplicationErrors.Add(
            applicationError);

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private static bool IsValidStatusCode(
        int? statusCode)
    {
        return !statusCode.HasValue ||
            statusCode is >= 100 and <= 599;
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