using Microsoft.EntityFrameworkCore;
using RTS.Application.Profiles;
using RTS.Contracts.Profiles;
using RTS.Infrastructure.Persistence;

namespace RTS.Infrastructure.Profiles;

public sealed class UserProfileService(
    RtsDbContext dbContext)
    : IUserProfileService
{
    private readonly RtsDbContext _dbContext =
        dbContext;

    public async Task<UserProfileDetails> GetOrCreateAsync(
        Guid userExternalId,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.ExternalId == userExternalId &&
                    candidate.IsActive &&
                    !candidate.IsDeleted,
                cancellationToken);

        if (user is null)
        {
            throw new InvalidOperationException(
                "The active user account could not be found.");
        }

        var profile = await _dbContext.UserProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.UserId == user.Id &&
                    !candidate.IsDeleted,
                cancellationToken);

        if (profile is null)
        {
            profile = new UserProfile
            {
                UserId = user.Id,
                CreatedByUserId = user.Id
            };

            _dbContext.UserProfiles.Add(profile);

            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }

        return ToDetails(profile);
    }

    public async Task<UpdateUserProfileResult> UpdateAsync(
        Guid userExternalId,
        UpdateUserProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.RowVersion.Length == 0)
        {
            return UpdateUserProfileResult.Failure(
                "InvalidRowVersion");
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.ExternalId == userExternalId &&
                    candidate.IsActive &&
                    !candidate.IsDeleted,
                cancellationToken);

        if (user is null)
        {
            return UpdateUserProfileResult.Failure(
                "UserNotFound");
        }

        var profile = await _dbContext.UserProfiles
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.UserId == user.Id &&
                    !candidate.IsDeleted,
                cancellationToken);

        if (profile is null)
        {
            return UpdateUserProfileResult.Failure(
                "ProfileNotFound");
        }

        if (!profile.RowVersion.SequenceEqual(
                request.RowVersion))
        {
            return UpdateUserProfileResult.Failure(
                "ConcurrencyConflict");
        }

        profile.DisplayName =
            Normalize(request.DisplayName);

        profile.FirstName =
            Normalize(request.FirstName);

        profile.LastName =
            Normalize(request.LastName);

        profile.TimeZoneId =
            Normalize(request.TimeZoneId);

        profile.Locale =
            Normalize(request.Locale);

        profile.PreferredDateFormat =
            Normalize(request.PreferredDateFormat);

        profile.ModifiedUtc = DateTime.UtcNow;
        profile.ModifiedByUserId = user.Id;

        _dbContext.Entry(profile)
            .Property(candidate => candidate.RowVersion)
            .OriginalValue = request.RowVersion;

        try
        {
            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return UpdateUserProfileResult.Failure(
                "ConcurrencyConflict");
        }

        return UpdateUserProfileResult.Success(
            ToDetails(profile));
    }

    private static string? Normalize(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized;
    }

    private static UserProfileDetails ToDetails(
        UserProfile profile) =>
        new(
            profile.ExternalId,
            profile.DisplayName,
            profile.FirstName,
            profile.LastName,
            profile.TimeZoneId,
            profile.Locale,
            profile.PreferredDateFormat,
            profile.IsOnboardingComplete,
            profile.RowVersion);
}