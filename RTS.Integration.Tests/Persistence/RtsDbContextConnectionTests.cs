using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RTS.Infrastructure.Persistence;

namespace RTS.Integration.Tests.Persistence;

public sealed class RtsDbContextConnectionTests
{
    private const string DefaultConnectionString =
        "Server=localhost;Database=RTSIntegrationTests;" +
        "Trusted_Connection=True;TrustServerCertificate=True;" +
        "MultipleActiveResultSets=True";

    [Fact]
    public async Task CanConnectAsync_ReturnsTrue_ForConfiguredDatabase()
    {
        await using var context = CreateDbContext();

        var canConnect =
            await context.Database.CanConnectAsync();

        Assert.True(canConnect);
    }

    [Fact]
    public async Task IdentityMappings_CanQueryAllIdentityTables()
    {
        await using var context = CreateDbContext();

        _ = await context.Users
            .AsNoTracking()
            .CountAsync();

        _ = await context.Roles
            .AsNoTracking()
            .CountAsync();

        _ = await context.Set<IdentityUserRole<long>>()
            .AsNoTracking()
            .CountAsync();

        _ = await context.Set<IdentityUserClaim<long>>()
            .AsNoTracking()
            .CountAsync();

        _ = await context.Set<IdentityRoleClaim<long>>()
            .AsNoTracking()
            .CountAsync();

        _ = await context.Set<IdentityUserLogin<long>>()
            .AsNoTracking()
            .CountAsync();

        _ = await context.Set<IdentityUserToken<long>>()
            .AsNoTracking()
            .CountAsync();

        _ = await context.LoginHistory
            .AsNoTracking()
            .CountAsync();

        _ = await context.AuditLogs
            .AsNoTracking()
            .CountAsync();

        _ = await context.ApplicationErrors
            .AsNoTracking()
            .CountAsync();
    }

    private static RtsDbContext CreateDbContext()
    {
        var configuredConnectionString =
            Environment.GetEnvironmentVariable(
                "RTS_TEST_CONNECTION_STRING");

        var connectionString =
            string.IsNullOrWhiteSpace(configuredConnectionString)
                ? DefaultConnectionString
                : configuredConnectionString;

        var options =
            new DbContextOptionsBuilder<RtsDbContext>()
                .UseSqlServer(connectionString)
                .Options;

        return new RtsDbContext(options);
    }
}