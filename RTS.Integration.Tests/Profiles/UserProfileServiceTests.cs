using Microsoft.EntityFrameworkCore;
using RTS.Contracts.Profiles;
using RTS.Infrastructure.Identity;
using RTS.Infrastructure.Persistence;
using RTS.Infrastructure.Profiles;

namespace RTS.Integration.Tests.Profiles;

public sealed class UserProfileServiceTests
{
    [Fact]
    public async Task GetOrCreateAsync_CreatesAndReturnsProfile()
    {
        await using var context = CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var user = CreateUser();

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service =
            new UserProfileService(context);

        var profile =
            await service.GetOrCreateAsync(
                user.ExternalId);

        Assert.NotEqual(
            Guid.Empty,
            profile.ExternalId);

        Assert.NotEmpty(profile.RowVersion);

        var persistedProfile =
            await context.UserProfiles
                .AsNoTracking()
                .SingleAsync(
                    candidate =>
                        candidate.UserId == user.Id);

        Assert.Equal(
            user.Id,
            persistedProfile.CreatedByUserId);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesProfileAndRowVersion()
    {
        await using var context = CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var user = CreateUser();

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service =
            new UserProfileService(context);

        var original =
            await service.GetOrCreateAsync(
                user.ExternalId);

        var originalRowVersion =
            original.RowVersion.ToArray();

        var result =
            await service.UpdateAsync(
                user.ExternalId,
                new UpdateUserProfileRequest(
                    " Dan ",
                    " Daniel ",
                    " O'Connell ",
                    " Eastern Standard Time ",
                    " en-US ",
                    " MM/dd/yyyy ",
                    original.RowVersion));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Profile);

        Assert.Equal(
            "Dan",
            result.Profile.DisplayName);

        Assert.Equal(
            "Daniel",
            result.Profile.FirstName);

        Assert.Equal(
            "O'Connell",
            result.Profile.LastName);

        Assert.Equal(
            "Eastern Standard Time",
            result.Profile.TimeZoneId);

        Assert.Equal(
            "en-US",
            result.Profile.Locale);

        Assert.Equal(
            "MM/dd/yyyy",
            result.Profile.PreferredDateFormat);

        Assert.False(
            originalRowVersion.SequenceEqual(
                result.Profile.RowVersion));

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task UpdateAsync_RejectsStaleRowVersion()
    {
        await using var context = CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var user = CreateUser();

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service =
            new UserProfileService(context);

        var original =
            await service.GetOrCreateAsync(
                user.ExternalId);

        var staleRowVersion =
            original.RowVersion.ToArray();

        var firstUpdate =
            await service.UpdateAsync(
                user.ExternalId,
                new UpdateUserProfileRequest(
                    "First update",
                    null,
                    null,
                    null,
                    null,
                    null,
                    original.RowVersion));

        Assert.True(firstUpdate.Succeeded);

        var staleUpdate =
            await service.UpdateAsync(
                user.ExternalId,
                new UpdateUserProfileRequest(
                    "Stale update",
                    null,
                    null,
                    null,
                    null,
                    null,
                    staleRowVersion));

        Assert.False(staleUpdate.Succeeded);
        Assert.Equal(
            "ConcurrencyConflict",
            staleUpdate.ErrorCode);

        await transaction.RollbackAsync();
    }

    private static ApplicationUser CreateUser()
    {
        var email =
            $"profile-{Guid.NewGuid():N}@example.test";

        return new ApplicationUser
        {
            UserName = email,
            NormalizedUserName =
                email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail =
                email.ToUpperInvariant(),
            EmailConfirmed = true,
            IsActive = true
        };
    }

    private static RtsDbContext CreateDbContext()
    {
        var configuredConnectionString =
            Environment.GetEnvironmentVariable(
                "RTS_TEST_CONNECTION_STRING");

        var connectionString =
            string.IsNullOrWhiteSpace(
                configuredConnectionString)
                ? "Server=localhost;Database=RTSIntegrationTests;" +
                  "Trusted_Connection=True;" +
                  "TrustServerCertificate=True;" +
                  "MultipleActiveResultSets=True"
                : configuredConnectionString;

        var options =
            new DbContextOptionsBuilder<RtsDbContext>()
                .UseSqlServer(connectionString)
                .Options;

        return new RtsDbContext(options);
    }
}