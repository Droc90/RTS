using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RTS.Application.Auditing;
using RTS.Application.Identity;
using RTS.Contracts.Auditing;
using RTS.Infrastructure.Identity;
using RTS.Infrastructure.Persistence;

namespace RTS.Infrastructure.Auditing;

public sealed class ApplicationErrorAdministrationService(
    RtsDbContext dbContext,
    UserManager<ApplicationUser> userManager)
    : IApplicationErrorAdministrationService
{
    private readonly RtsDbContext _dbContext =
        dbContext;

    private readonly UserManager<ApplicationUser> _userManager =
        userManager;

    public async Task<ApplicationErrorSearchResult> SearchAsync(
        ApplicationErrorSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pageNumber =
            Math.Max(1, request.PageNumber);

        var pageSize =
            Math.Clamp(request.PageSize, 1, 100);

        var searchTerm =
            Normalize(request.SearchTerm);

        var resolutionStatus =
            Normalize(request.ResolutionStatus);

        if (resolutionStatus is not null &&
            !ApplicationErrorResolutionStatuses.IsValid(
                resolutionStatus))
        {
            throw new ArgumentException(
                "The resolution status is invalid.",
                nameof(request));
        }

        var query =
            _dbContext.ApplicationErrors
                .AsNoTracking();

        if (resolutionStatus is not null)
        {
            query = query.Where(
                error =>
                    error.ResolutionStatus ==
                    resolutionStatus);
        }

        if (searchTerm is not null)
        {
            var isGuid =
                Guid.TryParse(
                    searchTerm,
                    out var searchGuid);

            query = query.Where(
                error =>
                    error.ErrorType.Contains(searchTerm) ||
                    (error.ErrorCode != null &&
                     error.ErrorCode.Contains(searchTerm)) ||
                    (error.Source != null &&
                     error.Source.Contains(searchTerm)) ||
                    (error.RequestPath != null &&
                     error.RequestPath.Contains(searchTerm)) ||
                    (isGuid &&
                     (error.ExternalId == searchGuid ||
                      error.CorrelationId == searchGuid)));
        }

        var totalCount =
            await query.CountAsync(
                cancellationToken);

        var errors =
            await query
                .OrderByDescending(
                    error => error.OccurredUtc)
                .Skip(
                    (pageNumber - 1) *
                    pageSize)
                .Take(pageSize)
                .Select(
                    error =>
                        new ApplicationErrorSummary(
                            error.ExternalId,
                            error.CorrelationId,
                            error.ErrorType,
                            error.ErrorCode,
                            error.SafeMessage,
                            error.RequestPath,
                            error.HttpMethod,
                            error.StatusCode,
                            _dbContext.Users
                                .Where(
                                    user =>
                                        user.Id ==
                                        error.UserId)
                                .Select(
                                    user => user.Email)
                                .FirstOrDefault(),
                            error.OccurredUtc,
                            error.ResolutionStatus))
                .ToArrayAsync(
                    cancellationToken);

        return new ApplicationErrorSearchResult(
            errors,
            totalCount,
            pageNumber,
            pageSize);
    }

    public async Task<ApplicationErrorDetails?> GetAsync(
        Guid externalId,
        CancellationToken cancellationToken = default)
    {
        if (externalId == Guid.Empty)
        {
            return null;
        }

        return await _dbContext.ApplicationErrors
            .AsNoTracking()
            .Where(
                error =>
                    error.ExternalId ==
                    externalId)
            .Select(
                error =>
                    new ApplicationErrorDetails(
                        error.ExternalId,
                        error.CorrelationId,
                        error.ErrorType,
                        error.ErrorCode,
                        error.SafeMessage,
                        error.DiagnosticDetails,
                        error.Source,
                        error.RequestPath,
                        error.HttpMethod,
                        error.StatusCode,
                        _dbContext.Users
                            .Where(
                                user =>
                                    user.Id ==
                                    error.UserId)
                            .Select(
                                user => user.Email)
                            .FirstOrDefault(),
                        error.OccurredUtc,
                        error.ResolutionStatus,
                        error.ResolvedUtc,
                        _dbContext.Users
                            .Where(
                                user =>
                                    user.Id ==
                                    error.ResolvedByUserId)
                            .Select(
                                user => user.Email)
                            .FirstOrDefault(),
                        error.ResolutionNotes,
                        error.RowVersion))
            .SingleOrDefaultAsync(
                cancellationToken);
    }

    public async Task<UpdateApplicationErrorResult> UpdateAsync(
        Guid externalId,
        Guid actingUserExternalId,
        UpdateApplicationErrorRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (externalId == Guid.Empty)
        {
            return UpdateApplicationErrorResult.Failure(
                "ErrorNotFound");
        }

        if (request.RowVersion is null ||
            request.RowVersion.Length == 0)
        {
            return UpdateApplicationErrorResult.Failure(
                "InvalidRowVersion");
        }

        var resolutionStatus =
            Normalize(request.ResolutionStatus);

        if (!ApplicationErrorResolutionStatuses.IsValid(
                resolutionStatus))
        {
            return UpdateApplicationErrorResult.Failure(
                "InvalidResolutionStatus");
        }

        var resolutionNotes =
            Normalize(request.ResolutionNotes);

        if (resolutionNotes?.Length > 2000)
        {
            return UpdateApplicationErrorResult.Failure(
                "ResolutionNotesTooLong");
        }

        var actingUser =
            await _dbContext.Users
                .SingleOrDefaultAsync(
                    user =>
                        user.ExternalId ==
                        actingUserExternalId &&
                        user.IsActive &&
                        !user.IsDeleted,
                    cancellationToken);

        if (actingUser is null ||
            !await _userManager.IsInRoleAsync(
                actingUser,
                SystemRoleNames.Administrator))
        {
            return UpdateApplicationErrorResult.Failure(
                "InvalidAdministrator");
        }

        var applicationError =
            await _dbContext.ApplicationErrors
                .SingleOrDefaultAsync(
                    error =>
                        error.ExternalId ==
                        externalId,
                    cancellationToken);

        if (applicationError is null)
        {
            return UpdateApplicationErrorResult.Failure(
                "ErrorNotFound");
        }

        if (!applicationError.RowVersion.SequenceEqual(
                request.RowVersion))
        {
            return UpdateApplicationErrorResult.Failure(
                "ConcurrencyConflict");
        }

        applicationError.ResolutionStatus =
            resolutionStatus!;

        applicationError.ResolutionNotes =
            resolutionNotes;

        if (resolutionStatus is
            ApplicationErrorResolutionStatuses.Resolved or
            ApplicationErrorResolutionStatuses.Ignored)
        {
            applicationError.ResolvedUtc =
                DateTime.UtcNow;

            applicationError.ResolvedByUserId =
                actingUser.Id;
        }
        else
        {
            applicationError.ResolvedUtc = null;
            applicationError.ResolvedByUserId = null;
        }

        _dbContext.Entry(applicationError)
            .Property(
                error => error.RowVersion)
            .OriginalValue =
                request.RowVersion;

        try
        {
            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return UpdateApplicationErrorResult.Failure(
                "ConcurrencyConflict");
        }

        var updatedError =
            await GetAsync(
                applicationError.ExternalId,
                cancellationToken);

        if (updatedError is null)
        {
            return UpdateApplicationErrorResult.Failure(
                "ErrorNotFound");
        }

        return UpdateApplicationErrorResult.Success(
            updatedError);
    }

    private static string? Normalize(
        string? value)
    {
        var normalized =
            value?.Trim();

        return string.IsNullOrWhiteSpace(
            normalized)
                ? null
                : normalized;
    }
}