using Microsoft.EntityFrameworkCore;
using RTS.Domain.Rules;
using RTS.Domain.ScreeningStrategies;
using RTS.Infrastructure.Persistence;

namespace RTS.Integration.Tests.Persistence;

public sealed class ScreeningStrategyPersistenceTests
{
    private const string DefaultConnectionString =
        "Server=localhost;Database=RTSIntegrationTests;" +
        "Trusted_Connection=True;TrustServerCertificate=True;" +
        "MultipleActiveResultSets=True";

    [Fact]
    public async Task ScreeningStrategy_CanSavePublishAndReloadAggregate()
    {
        var uniqueName =
            $"Integration Strategy {Guid.NewGuid():N}";

        var strategy = ScreeningStrategy.Create(
            uniqueName,
            "Persistence integration-test strategy.",
            ownerUserId: null);

        var initialDraft = Assert.Single(
            strategy.Versions);

        initialDraft.AddRule(
            "Maximum P/E",
            "Fundamental.PriceToEarningsRatio",
            RulePurpose.Filter,
            ComparisonOperator.LessThan,
            RuleValue.FromDecimal(15m));

        initialDraft.AddRule(
            "Revenue growth ranking",
            "Fundamental.RevenueGrowth",
            RulePurpose.Score,
            ComparisonOperator.GreaterThan,
            RuleValue.FromPercentage(10m),
            weight: 20m);

        initialDraft.AddRule(
            "Low liquidity warning",
            "Market.AverageDailyDollarVolume",
            RulePurpose.Warning,
            ComparisonOperator.LessThan,
            RuleValue.FromDecimal(5_000_000m));

        var strategyExternalId =
            strategy.ExternalId;

        try
        {
            await using (var context = CreateDbContext())
            {
                context.ScreeningStrategies.Add(strategy);

                await context.SaveChangesAsync();
            }

            await using (var context = CreateDbContext())
            {
                var savedStrategy =
                    await LoadStrategyAsync(
                        context,
                        strategyExternalId,
                        asNoTracking: false);

                Assert.True(savedStrategy.Id > 0);
                Assert.Equal(
                    uniqueName,
                    savedStrategy.Name);
                Assert.True(savedStrategy.IsActive);

                var savedDraft = Assert.Single(
                    savedStrategy.Versions);

                Assert.True(savedDraft.Id > 0);
                Assert.Equal(
                    savedStrategy.Id,
                    savedDraft.ScreeningStrategyId);
                Assert.Equal(
                    ScreeningStrategyVersionStatus.Draft,
                    savedDraft.Status);

                AssertRules(savedDraft);

                savedStrategy.PublishDraftVersion(
                    savedDraft.ExternalId,
                    savedDraft.CreatedUtc.AddMinutes(1));

                await context.SaveChangesAsync();
            }

            await using (var context = CreateDbContext())
            {
                var reloadedStrategy =
                    await LoadStrategyAsync(
                        context,
                        strategyExternalId,
                        asNoTracking: true);

                var publishedVersion = Assert.Single(
                    reloadedStrategy.Versions);

                Assert.Equal(
                    ScreeningStrategyVersionStatus.Published,
                    publishedVersion.Status);
                Assert.NotNull(
                    publishedVersion.PublishedUtc);

                AssertRules(publishedVersion);
            }
        }
        finally
        {
            await DeleteTestStrategyAsync(
                strategyExternalId);
        }
    }

    private static void AssertRules(
        ScreeningStrategyVersion version)
    {
        var rules = version.Rules
            .OrderBy(rule => rule.DisplayOrder)
            .ToArray();

        Assert.Equal(3, rules.Length);

        Assert.Equal("Maximum P/E", rules[0].Name);
        Assert.Equal(
            RulePurpose.Filter,
            rules[0].Purpose);
        Assert.Equal(
            ComparisonOperator.LessThan,
            rules[0].Operator);
        Assert.Equal(
            15m,
            rules[0].PrimaryValue.AsDecimal());
        Assert.Null(rules[0].Weight);

        Assert.Equal(
            RulePurpose.Score,
            rules[1].Purpose);
        Assert.Equal(20m, rules[1].Weight);
        Assert.Equal(
            RuleValueType.Percentage,
            rules[1].PrimaryValue.ValueType);

        Assert.Equal(
            RulePurpose.Warning,
            rules[2].Purpose);
    }

    private static async Task<ScreeningStrategy>
        LoadStrategyAsync(
            RtsDbContext context,
            Guid strategyExternalId,
            bool asNoTracking)
    {
        IQueryable<ScreeningStrategy> query =
            context.ScreeningStrategies;

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query
            .AsSplitQuery()
            .Include(strategy => strategy.Versions)
            .ThenInclude(version => version.Rules)
            .SingleAsync(
                strategy =>
                    strategy.ExternalId ==
                    strategyExternalId);
    }

    private static async Task DeleteTestStrategyAsync(
        Guid strategyExternalId)
    {
        await using var context = CreateDbContext();

        var strategyIds = context.ScreeningStrategies
            .Where(
                strategy =>
                    strategy.ExternalId ==
                    strategyExternalId)
            .Select(strategy => strategy.Id);

        var versionIds = context.ScreeningStrategyVersions
            .Where(
                version =>
                    strategyIds.Contains(
                        version.ScreeningStrategyId))
            .Select(version => version.Id);

        await context.ScreeningRules
            .Where(
                rule =>
                    versionIds.Contains(
                        rule.ScreeningStrategyVersionId))
            .ExecuteDeleteAsync();

        await context.ScreeningStrategyVersions
            .Where(
                version =>
                    strategyIds.Contains(
                        version.ScreeningStrategyId))
            .ExecuteDeleteAsync();

        await context.ScreeningStrategies
            .Where(
                strategy =>
                    strategy.ExternalId ==
                    strategyExternalId)
            .ExecuteDeleteAsync();
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