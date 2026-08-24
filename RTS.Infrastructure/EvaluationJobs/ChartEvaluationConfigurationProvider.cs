using Microsoft.EntityFrameworkCore;
using RTS.Application.Charting;
using RTS.Domain.Rules;
using RTS.Domain.TradingModels;
using RTS.Domain.TradingModels.Criteria;
using RTS.Infrastructure.Persistence;

namespace RTS.Infrastructure.EvaluationJobs;

public sealed class ChartEvaluationConfigurationProvider(RtsDbContext dbContext)
    : IChartEvaluationConfigurationProvider
{
    public async Task<ChartEvaluationConfiguration> GetAsync(Guid userExternalId,
        CancellationToken cancellationToken = default)
    {
        var ownerId = await dbContext.Users.AsNoTracking()
            .Where(user => user.ExternalId == userExternalId && user.IsActive && !user.IsDeleted)
            .Select(user => (long?)user.Id)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("The active user could not be found.");

        var models = await dbContext.TradingModels.AsNoTracking()
            .Where(model => model.IsActive && (model.OwnerUserId == ownerId || model.OwnerUserId == null))
            .Include(model => model.Versions).ThenInclude(version => version.Criteria)
            .Include(model => model.Versions).ThenInclude(version => version.Timeframes)
            .ToArrayAsync(cancellationToken);

        var selected = models
            .SelectMany(model => model.Versions
                .Where(version => version.Status == TradingModelVersionStatus.Published)
                .Select(version => new { Model = model, Version = version }))
            .OrderByDescending(item => item.Model.OwnerUserId == ownerId)
            .ThenByDescending(item => item.Version.PublishedUtc)
            .FirstOrDefault();

        return selected is null ? InitialChartEvaluationConfiguration.Create() : Map(selected.Model.Name, selected.Version);
    }

    private static ChartEvaluationConfiguration Map(string modelName, TradingModelVersion version)
    {
        var rules = version.Criteria.Where(criterion => criterion.IsEnabled)
            .OrderBy(criterion => criterion.DisplayOrder)
            .Select(MapRule).ToArray();
        var initialWeights = InitialChartEvaluationConfiguration.Create().TimeframeWeights;
        var weights = version.Timeframes.ToDictionary(
            timeframe => ResolveTimeframeKey(timeframe.Name),
            timeframe => initialWeights.GetValueOrDefault(ResolveTimeframeKey(timeframe.Name), 1m),
            StringComparer.OrdinalIgnoreCase);
        return new(modelName, version.VersionNumber.ToString(), weights, rules);
    }

    private static ChartEvaluationRule MapRule(TradingModelCriterion criterion) => new(
        criterion.Name, criterion.MetricKey, criterion.Purpose, criterion.Operator,
        Numeric(criterion.PrimaryValue),
        criterion.SecondaryValue is null ? null : Numeric(criterion.SecondaryValue),
        criterion.Weight);

    private static decimal Numeric(RuleValue value) => value.ValueType switch
    {
        RuleValueType.Integer => value.AsInteger(),
        RuleValueType.Decimal or RuleValueType.Percentage => value.AsDecimal(),
        _ => throw new InvalidOperationException("Technical evaluation criteria require numeric values.")
    };

    private static string ResolveTimeframeKey(string name)
    {
        var definition = InitialChartSpecification.Views.FirstOrDefault(view =>
            string.Equals(view.Name, name, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(view.Key, name, StringComparison.OrdinalIgnoreCase));
        return definition?.Key ?? name;
    }
}
