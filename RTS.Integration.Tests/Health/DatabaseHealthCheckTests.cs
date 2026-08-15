using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RTS.Infrastructure.Persistence;

namespace RTS.Integration.Tests.Health;

public sealed class DatabaseHealthCheckTests
{
    private const string DefaultConnectionString =
        "Server=localhost;Database=RTSIntegrationTests;" +
        "Trusted_Connection=True;" +
        "TrustServerCertificate=True;" +
        "MultipleActiveResultSets=True";

    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthyForRTSDatabase()
    {
        await using var dbContext =
            CreateDbContext();

        var healthCheck =
            new DatabaseHealthCheck(dbContext);

        var result =
            await healthCheck.CheckHealthAsync(
                new HealthCheckContext());

        Assert.Equal(
            HealthStatus.Healthy,
            result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthyWhenDatabaseIsUnavailable()
    {
        const string unavailableConnectionString =
            "Server=127.0.0.1,1;Database=RTSIntegrationTests;" +
            "User Id=invalid;Password=invalid;" +
            "Encrypt=False;Connect Timeout=1;" +
            "ConnectRetryCount=0";

        var options =
            new DbContextOptionsBuilder<RtsDbContext>()
                .UseSqlServer(
                    unavailableConnectionString)
                .Options;

        await using var dbContext =
            new RtsDbContext(options);

        var healthCheck =
            new DatabaseHealthCheck(dbContext);

        var result =
            await healthCheck.CheckHealthAsync(
                new HealthCheckContext());

        Assert.Equal(
            HealthStatus.Unhealthy,
            result.Status);

        Assert.Equal(
            "The database is unavailable.",
            result.Description);
    }
    private static RtsDbContext CreateDbContext()
    {
        var configuredConnectionString =
            Environment.GetEnvironmentVariable(
                "RTS_TEST_CONNECTION_STRING");

        var connectionString =
            string.IsNullOrWhiteSpace(
                configuredConnectionString)
                ? DefaultConnectionString
                : configuredConnectionString;

        var options =
            new DbContextOptionsBuilder<RtsDbContext>()
                .UseSqlServer(connectionString)
                .Options;

        return new RtsDbContext(options);
    }
}