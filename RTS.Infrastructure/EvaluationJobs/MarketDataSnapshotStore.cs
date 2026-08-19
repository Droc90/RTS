using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RTS.Application.MarketData;
using RTS.Infrastructure.Persistence;

namespace RTS.Infrastructure.EvaluationJobs;

public sealed class MarketDataSnapshotStore(RtsDbContext dbContext) : IMarketDataSnapshotStore
{
    public async Task<Guid> SaveAsync(Guid evaluationJobExternalId, NormalizedMarketData data, CancellationToken cancellationToken = default)
    {
        var jobId = await dbContext.EvaluationJobs.Where(job => job.ExternalId == evaluationJobExternalId)
            .Select(job => (long?)job.Id).SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("The evaluation job could not be found.");
        var snapshot = new MarketDataSnapshot
        {
            ExternalId = Guid.NewGuid(), EvaluationJobId = jobId, Symbol = data.Request.Symbol,
            ProviderKey = data.ProviderKey, RetrievedUtc = data.RetrievedUtc,
            RequestJson = JsonSerializer.Serialize(data.Request), BarsJson = JsonSerializer.Serialize(data.Bars),
            MissingBarsJson = JsonSerializer.Serialize(data.MissingBarTimestampsUtc)
        };
        dbContext.MarketDataSnapshots.Add(snapshot);
        await dbContext.SaveChangesAsync(cancellationToken);
        return snapshot.ExternalId;
    }

    public async Task<NormalizedMarketData?> GetAsync(Guid snapshotExternalId, CancellationToken cancellationToken = default)
    {
        var snapshot = await dbContext.MarketDataSnapshots.AsNoTracking().SingleOrDefaultAsync(item => item.ExternalId == snapshotExternalId, cancellationToken);
        if (snapshot is null) return null;
        var request = JsonSerializer.Deserialize<MarketDataRequest>(snapshot.RequestJson) ?? throw new InvalidOperationException("The saved market-data request is invalid.");
        var bars = JsonSerializer.Deserialize<PriceBar[]>(snapshot.BarsJson) ?? [];
        var missing = JsonSerializer.Deserialize<DateTime[]>(snapshot.MissingBarsJson) ?? [];
        return new NormalizedMarketData(request, bars, missing, snapshot.RetrievedUtc, snapshot.ProviderKey);
    }
}
