using Microsoft.EntityFrameworkCore;
using RTS.Application.Charting;
using RTS.Application.EvaluationJobs;
using RTS.Application.MarketData;
using RTS.Infrastructure.Persistence;

namespace RTS.Infrastructure.EvaluationJobs;

public sealed class MarketDataEvaluationJobProcessor(IMarketDataService marketDataService, IMarketDataSnapshotStore snapshotStore,
    ICanonicalEvaluationService canonicalEvaluationService, RtsDbContext dbContext) : IEvaluationJobProcessor
{
    public async Task ProcessAsync(Guid jobExternalId, string symbol, CancellationToken cancellationToken = default)
    {
        var requests = InitialChartSpecification.CreateRequests(symbol, DateTime.UtcNow).ToArray();
        await UpdateProgressAsync(jobExternalId, 10, "Retrieving current quote and corporate actions.", cancellationToken);
        _ = await marketDataService.GetQuoteAsync(symbol, cancellationToken);
        _ = await marketDataService.GetCorporateActionsAsync(symbol, requests.Min(item => item.FromUtc), requests.Max(item => item.ToUtc), cancellationToken);
        for (var index = 0; index < requests.Length; index++)
        {
            var request = ExpandForIndicatorWarmup(requests[index]);
            await UpdateProgressAsync(jobExternalId, 20 + index * 25, $"Retrieving {InitialChartSpecification.Views.ElementAt(index).Name} market data.", cancellationToken);
            var data = await marketDataService.GetNormalizedBarsAsync(request, cancellationToken);
            await snapshotStore.SaveAsync(jobExternalId, data, cancellationToken);
        }
        await UpdateProgressAsync(jobExternalId, 90, "Market data normalized and chart views prepared.", cancellationToken);
        await UpdateProgressAsync(jobExternalId, 92, "Researching current fundamentals, catalysts, and risks.", cancellationToken);
        await canonicalEvaluationService.GenerateAsync(jobExternalId, cancellationToken);
        await UpdateProgressAsync(jobExternalId, 97, "Canonical comprehensive evaluation saved.", cancellationToken);
    }

    private static MarketDataRequest ExpandForIndicatorWarmup(MarketDataRequest request) =>
        request.IntervalUnit == RTS.Domain.TradingModels.Timeframes.BarIntervalUnit.Days
            ? request with { FromUtc = request.FromUtc.AddDays(-120) }
            : request;

    private async Task UpdateProgressAsync(Guid externalId, int percent, string message, CancellationToken cancellationToken)
    {
        var job = await dbContext.EvaluationJobs.SingleAsync(item => item.ExternalId == externalId, cancellationToken);
        if (job.Status == EvaluationJobStatus.Cancelled) throw new OperationCanceledException("The evaluation was cancelled.");
        job.ProgressPercent = percent; job.ProgressMessage = message;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
