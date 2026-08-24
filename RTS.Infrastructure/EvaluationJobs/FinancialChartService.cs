using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RTS.Application.Charting;
using RTS.Application.MarketData;
using RTS.Infrastructure.Persistence;

namespace RTS.Infrastructure.EvaluationJobs;

public sealed class FinancialChartService(RtsDbContext dbContext) : IFinancialChartService
{
    public async Task<IReadOnlyCollection<FinancialChartView>> GetViewsAsync(Guid userExternalId, Guid evaluationJobExternalId, CancellationToken cancellationToken = default)
    {
        var job = await dbContext.EvaluationJobs.AsNoTracking()
            .Where(item => item.ExternalId == evaluationJobExternalId &&
                dbContext.Users.Any(user => user.Id == item.OwnerUserId && user.ExternalId == userExternalId && user.IsActive && !user.IsDeleted))
            .Select(item => new { item.Id, item.Symbol })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("The evaluation job could not be found.");

        var snapshots = await dbContext.MarketDataSnapshots.AsNoTracking()
            .Where(snapshot => snapshot.EvaluationJobId == job.Id)
            .OrderByDescending(snapshot => snapshot.RetrievedUtc)
            .ToArrayAsync(cancellationToken);

        var results = new List<FinancialChartView>();
        foreach (var definition in InitialChartSpecification.Views)
        {
            var match = snapshots.Select(snapshot => new
                {
                    Snapshot = snapshot,
                    Request = JsonSerializer.Deserialize<MarketDataRequest>(snapshot.RequestJson)
                })
                .FirstOrDefault(item => item.Request is not null &&
                    item.Request.IntervalValue == definition.BarIntervalValue &&
                    item.Request.IntervalUnit == definition.BarIntervalUnit);
            if (match?.Request is null) continue;
            var bars = JsonSerializer.Deserialize<PriceBar[]>(match.Snapshot.BarsJson) ?? [];
            var missing = JsonSerializer.Deserialize<DateTime[]>(match.Snapshot.MissingBarsJson) ?? [];
            var calculated = InitialIndicatorCalculator.Calculate(bars.Select(bar =>
                new FinancialChartPoint(bar.TimestampUtc, bar.Open, bar.High, bar.Low, bar.Close, bar.Volume)));
            var visibleFromUtc = match.Request.ToUtc - definition.Lookback;
            results.Add(new FinancialChartView(definition,
                calculated.Where(point => point.TimestampUtc >= visibleFromUtc).ToArray(),
                match.Snapshot.ProviderKey, match.Snapshot.RetrievedUtc, missing));
        }
        return results;
    }
}
