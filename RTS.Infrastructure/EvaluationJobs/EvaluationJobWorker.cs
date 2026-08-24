using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RTS.Application.EvaluationJobs;
using RTS.Infrastructure.Persistence;

namespace RTS.Infrastructure.EvaluationJobs;

public sealed class EvaluationJobWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<EvaluationJobWorker> logger) : BackgroundService
{
    private DateTime nextRetentionCleanupUtc = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (DateTime.UtcNow >= nextRetentionCleanupUtc)
                {
                    await DeleteExpiredCancelledJobsAsync(stoppingToken);
                    nextRetentionCleanupUtc = DateTime.UtcNow.AddHours(1);
                }
                var processed = await ProcessNextAsync(stoppingToken);
                if (!processed) await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception exception)
            {
                logger.LogError(exception, "The evaluation-job worker loop failed.");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task DeleteExpiredCancelledJobsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RtsDbContext>();
        var deleteCutoffUtc = DateTime.UtcNow - EvaluationJobRetentionPolicy.CancelledDeleteAfter;
        var expiredJobs = await dbContext.EvaluationJobs
            .Where(job => job.Status == EvaluationJobStatus.Cancelled && job.CompletedUtc < deleteCutoffUtc)
            .ToArrayAsync(cancellationToken);
        if (expiredJobs.Length == 0) return;

        dbContext.EvaluationJobs.RemoveRange(expiredJobs);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Deleted {Count} cancelled evaluation jobs older than {RetentionDays} days.",
            expiredJobs.Length, EvaluationJobRetentionPolicy.CancelledDeleteAfter.TotalDays);
    }

    private async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var processor = scope.ServiceProvider.GetService<IEvaluationJobProcessor>();
        if (processor is null) return false;
        var dbContext = scope.ServiceProvider.GetRequiredService<RtsDbContext>();
        var job = await dbContext.EvaluationJobs
            .Where(item => item.Status == EvaluationJobStatus.Queued)
            .OrderBy(item => item.CreatedUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (job is null) return false;

        job.Status = EvaluationJobStatus.Running;
        job.ProgressPercent = 5;
        job.ProgressMessage = "Market-data preparation started.";
        job.AttemptCount++;
        job.StartedUtc = DateTime.UtcNow;
        job.CompletedUtc = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await processor.ProcessAsync(job.ExternalId, job.Symbol, cancellationToken);
            await dbContext.Entry(job).ReloadAsync(cancellationToken);
            if (job.Status == EvaluationJobStatus.Cancelled) return true;
            job.Status = EvaluationJobStatus.Succeeded;
            job.ProgressPercent = 100;
            job.ProgressMessage = "Market data and chart preparation completed.";
            job.CompletedUtc = DateTime.UtcNow;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Evaluation job {JobExternalId} failed.", job.ExternalId);
            await dbContext.Entry(job).ReloadAsync(cancellationToken);
            if (job.Status == EvaluationJobStatus.Cancelled) return true;
            job.Status = EvaluationJobStatus.Failed;
            job.ProgressMessage = "Market-data preparation failed.";
            job.ErrorMessage = exception.Message.Length <= 4000 ? exception.Message : exception.Message[..4000];
            job.CompletedUtc = DateTime.UtcNow;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
