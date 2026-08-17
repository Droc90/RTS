using Microsoft.EntityFrameworkCore;
using RTS.Domain.Rules;
using RTS.Domain.TradingModels;
using RTS.Domain.TradingModels.Indicators;
using RTS.Domain.TradingModels.Timeframes;
using RTS.Infrastructure.Persistence;

namespace RTS.Integration.Tests.Persistence;

public sealed class TradingModelPersistenceTests
{
    private const string DefaultConnectionString =
        "Server=localhost;Database=RTSIntegrationTests;" +
        "Trusted_Connection=True;TrustServerCertificate=True;" +
        "MultipleActiveResultSets=True";

    [Fact]
    public async Task TradingModel_CanSavePublishAndReloadAggregate()
    {
        var uniqueName =
            $"Integration Model {Guid.NewGuid():N}";

        var model = TradingModel.Create(
            uniqueName,
            "Persistence integration-test model.",
            ownerUserId: null);

        var initialDraft = Assert.Single(model.Versions);

        var fiveDay = initialDraft.AddTimeframe(
            "5 Day",
            5,
            TimeframeLookbackUnit.TradingDays,
            5,
            BarIntervalUnit.Minutes,
            MarketSessionMode.RegularHoursOnly);

        var oneMonth = initialDraft.AddTimeframe(
            "1 Month",
            1,
            TimeframeLookbackUnit.Months,
            1,
            BarIntervalUnit.Days,
            MarketSessionMode.RegularHoursOnly);

        var threeMonth = initialDraft.AddTimeframe(
            "3 Month",
            3,
            TimeframeLookbackUnit.Months,
            1,
            BarIntervalUnit.Days,
            MarketSessionMode.RegularHoursOnly);

        AddInitialIndicators(initialDraft, fiveDay);
        AddInitialIndicators(initialDraft, oneMonth);
        AddInitialIndicators(initialDraft, threeMonth);

        AddInitialCriteria(initialDraft);

        var modelExternalId = model.ExternalId;

        try
        {
            await using (var context = CreateDbContext())
            {
                context.TradingModels.Add(model);

                await context.SaveChangesAsync();
            }

            await using (var context = CreateDbContext())
            {
                var savedModel = await LoadModelAsync(
                    context,
                    modelExternalId,
                    asNoTracking: false);

                Assert.True(savedModel.Id > 0);
                Assert.Equal(uniqueName, savedModel.Name);
                Assert.True(savedModel.IsActive);

                var savedDraft = Assert.Single(
                    savedModel.Versions);

                Assert.True(savedDraft.Id > 0);
                Assert.Equal(
                    savedModel.Id,
                    savedDraft.TradingModelId);
                Assert.Equal(1, savedDraft.VersionNumber);
                Assert.Equal(
                    TradingModelVersionStatus.Draft,
                    savedDraft.Status);

                AssertTimeframesAndIndicators(savedDraft);
                AssertCriteria(savedDraft);

                savedModel.PublishDraftVersion(
                    savedDraft.ExternalId,
                    savedDraft.CreatedUtc.AddMinutes(1));

                await context.SaveChangesAsync();
            }

            await using (var context = CreateDbContext())
            {
                var reloadedModel = await LoadModelAsync(
                    context,
                    modelExternalId,
                    asNoTracking: true);

                var publishedVersion = Assert.Single(
                    reloadedModel.Versions);

                Assert.Equal(
                    TradingModelVersionStatus.Published,
                    publishedVersion.Status);
                Assert.NotNull(
                    publishedVersion.PublishedUtc);

                AssertTimeframesAndIndicators(
                    publishedVersion);

                AssertCriteria(
                    publishedVersion);
            }
        }
        finally
        {
            await DeleteTestModelAsync(
                modelExternalId);
        }
    }

    private static void AddInitialIndicators(
        TradingModelVersion draft,
        TradingModelTimeframe timeframe)
    {
        draft.AddSimpleMovingAverage(
            timeframe.ExternalId,
            period: 50);

        draft.AddBollingerBands(
            timeframe.ExternalId,
            period: 20,
            standardDeviations: 2m);

        draft.AddMacd(
            timeframe.ExternalId,
            fastPeriod: 12,
            slowPeriod: 26,
            signalPeriod: 9);

        draft.AddRelativeStrengthIndex(
            timeframe.ExternalId,
            period: 14);

        draft.AddVolume(
            timeframe.ExternalId);
    }

    private static void AddInitialCriteria(
        TradingModelVersion draft)
    {
        draft.AddCriterion(
            "Maximum P/E",
            "Fundamental.PriceToEarningsRatio",
            RulePurpose.Filter,
            ComparisonOperator.LessThan,
            RuleValue.FromDecimal(15m));

        draft.AddCriterion(
            "RSI score",
            "Technical.Rsi",
            RulePurpose.Score,
            ComparisonOperator.Between,
            RuleValue.FromDecimal(40m),
            RuleValue.FromDecimal(60m),
            weight: 15m);

        draft.AddCriterion(
            "Negative earnings growth warning",
            "Fundamental.EarningsGrowth",
            RulePurpose.Warning,
            ComparisonOperator.LessThan,
            RuleValue.FromPercentage(0m));
    }

    private static void AssertTimeframesAndIndicators(
        TradingModelVersion version)
    {
        var timeframes = version.Timeframes
            .OrderBy(
                timeframe =>
                    timeframe.DisplayOrder)
            .ToArray();

        Assert.Equal(3, timeframes.Length);

        Assert.Equal("5 Day", timeframes[0].Name);
        Assert.Equal(
            TimeframeLookbackUnit.TradingDays,
            timeframes[0].LookbackUnit);
        Assert.Equal(5, timeframes[0].BarIntervalValue);
        Assert.Equal(
            BarIntervalUnit.Minutes,
            timeframes[0].BarIntervalUnit);

        Assert.Equal("1 Month", timeframes[1].Name);
        Assert.Equal("3 Month", timeframes[2].Name);

        Assert.All(
            timeframes,
            timeframe =>
            {
                Assert.Equal(
                    MarketSessionMode.RegularHoursOnly,
                    timeframe.MarketSessionMode);

                Assert.Equal(
                    5,
                    timeframe.Indicators.Count);
            });

        var fiveDayIndicators = timeframes[0].Indicators
            .OrderBy(
                indicator =>
                    indicator.DisplayOrder)
            .ToArray();

        Assert.Collection(
            fiveDayIndicators,
            indicator => Assert.Equal(
                TradingIndicatorType.SimpleMovingAverage,
                indicator.IndicatorType),
            indicator => Assert.Equal(
                TradingIndicatorType.BollingerBands,
                indicator.IndicatorType),
            indicator => Assert.Equal(
                TradingIndicatorType.Macd,
                indicator.IndicatorType),
            indicator => Assert.Equal(
                TradingIndicatorType.RelativeStrengthIndex,
                indicator.IndicatorType),
            indicator => Assert.Equal(
                TradingIndicatorType.Volume,
                indicator.IndicatorType));

        var macd = fiveDayIndicators.Single(
            indicator =>
                indicator.IndicatorType ==
                TradingIndicatorType.Macd);

        AssertParameter(
            macd,
            "FastPeriod",
            IndicatorParameterValueType.Integer,
            "12");

        AssertParameter(
            macd,
            "SlowPeriod",
            IndicatorParameterValueType.Integer,
            "26");

        AssertParameter(
            macd,
            "SignalPeriod",
            IndicatorParameterValueType.Integer,
            "9");
    }

    private static void AssertCriteria(
        TradingModelVersion version)
    {
        var criteria = version.Criteria
            .OrderBy(
                criterion =>
                    criterion.DisplayOrder)
            .ToArray();

        Assert.Equal(3, criteria.Length);

        var maximumPe = criteria[0];

        Assert.Equal("Maximum P/E", maximumPe.Name);
        Assert.Equal(
            "Fundamental.PriceToEarningsRatio",
            maximumPe.MetricKey);
        Assert.Equal(
            RulePurpose.Filter,
            maximumPe.Purpose);
        Assert.Equal(
            ComparisonOperator.LessThan,
            maximumPe.Operator);
        Assert.Equal(
            15m,
            maximumPe.PrimaryValue.AsDecimal());
        Assert.Null(maximumPe.SecondaryValue);
        Assert.Null(maximumPe.Weight);

        var rsiScore = criteria[1];

        Assert.Equal(
            RulePurpose.Score,
            rsiScore.Purpose);
        Assert.Equal(
            ComparisonOperator.Between,
            rsiScore.Operator);
        Assert.Equal(
            40m,
            rsiScore.PrimaryValue.AsDecimal());
        Assert.Equal(
            60m,
            rsiScore.SecondaryValue!.AsDecimal());
        Assert.Equal(15m, rsiScore.Weight);

        Assert.Equal(
            RulePurpose.Warning,
            criteria[2].Purpose);
    }

    private static void AssertParameter(
        TradingModelIndicator indicator,
        string key,
        IndicatorParameterValueType valueType,
        string value)
    {
        var parameter = Assert.Single(
            indicator.Parameters,
            item => item.Key == key);

        Assert.Equal(valueType, parameter.ValueType);
        Assert.Equal(value, parameter.Value);
    }

    private static async Task<TradingModel> LoadModelAsync(
        RtsDbContext context,
        Guid modelExternalId,
        bool asNoTracking)
    {
        IQueryable<TradingModel> query =
            context.TradingModels;

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query
            .AsSplitQuery()
            .Include(model => model.Versions)
            .ThenInclude(version => version.Timeframes)
            .ThenInclude(timeframe => timeframe.Indicators)
            .ThenInclude(indicator => indicator.Parameters)
            .Include(model => model.Versions)
            .ThenInclude(version => version.Criteria)
            .SingleAsync(
                model =>
                    model.ExternalId ==
                    modelExternalId);
    }

    private static async Task DeleteTestModelAsync(
        Guid modelExternalId)
    {
        await using var context = CreateDbContext();

        var modelIds = context.TradingModels
            .Where(
                model =>
                    model.ExternalId ==
                    modelExternalId)
            .Select(model => model.Id);

        var versionIds = context.TradingModelVersions
            .Where(
                version =>
                    modelIds.Contains(
                        version.TradingModelId))
            .Select(version => version.Id);

        var timeframeIds = context.TradingModelTimeframes
            .Where(
                timeframe =>
                    versionIds.Contains(
                        timeframe.TradingModelVersionId))
            .Select(timeframe => timeframe.Id);

        var indicatorIds = context.TradingModelIndicators
            .Where(
                indicator =>
                    timeframeIds.Contains(
                        indicator.TradingModelTimeframeId))
            .Select(indicator => indicator.Id);

        await context.TradingModelIndicatorParameters
            .Where(
                parameter =>
                    indicatorIds.Contains(
                        parameter.TradingModelIndicatorId))
            .ExecuteDeleteAsync();

        await context.TradingModelIndicators
            .Where(
                indicator =>
                    timeframeIds.Contains(
                        indicator.TradingModelTimeframeId))
            .ExecuteDeleteAsync();

        await context.TradingModelCriteria
            .Where(
                criterion =>
                    versionIds.Contains(
                        criterion.TradingModelVersionId))
            .ExecuteDeleteAsync();

        await context.TradingModelTimeframes
            .Where(
                timeframe =>
                    versionIds.Contains(
                        timeframe.TradingModelVersionId))
            .ExecuteDeleteAsync();

        await context.TradingModelVersions
            .Where(
                version =>
                    modelIds.Contains(
                        version.TradingModelId))
            .ExecuteDeleteAsync();

        await context.TradingModels
            .Where(
                model =>
                    model.ExternalId ==
                    modelExternalId)
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