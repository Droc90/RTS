using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RTS.Application.CandidateDiscovery;
using RTS.Infrastructure.Persistence;

namespace RTS.Infrastructure.CandidateDiscovery;

public sealed class CandidateIdentificationSettingsService(RtsDbContext dbContext) : ICandidateIdentificationSettingsService
{
    public async Task<CandidateIdentificationSettingsDetails> GetOrCreateAsync(Guid userExternalId, CancellationToken cancellationToken = default)
    {
        var userId = await GetUserIdAsync(userExternalId, cancellationToken);
        var entity = await dbContext.CandidateIdentificationSettings.SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (entity is null)
        {
            entity = new CandidateIdentificationSettingsEntity { ExternalId = Guid.NewGuid(), UserId = userId, SettingsJson = Serialize(CandidateIdentificationSettings.Defaults), CreatedUtc = DateTime.UtcNow };
            dbContext.CandidateIdentificationSettings.Add(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        return ToDetails(entity);
    }

    public async Task<CandidateIdentificationSettingsDetails> SaveAsync(Guid userExternalId, CandidateIdentificationSettings settings, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        Validate(settings);
        if (rowVersion.Length == 0) throw new InvalidOperationException("Candidate settings must be reloaded before saving.");
        var userId = await GetUserIdAsync(userExternalId, cancellationToken);
        var entity = await dbContext.CandidateIdentificationSettings.SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken)
            ?? throw new InvalidOperationException("Candidate settings could not be found.");
        if (!entity.RowVersion.SequenceEqual(rowVersion)) throw new InvalidOperationException("Candidate settings changed elsewhere. Reload and try again.");
        entity.SettingsJson = Serialize(settings);
        entity.ModifiedUtc = DateTime.UtcNow;
        dbContext.Entry(entity).Property(item => item.RowVersion).OriginalValue = rowVersion;
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw new InvalidOperationException("Candidate settings changed elsewhere. Reload and try again."); }
        return ToDetails(entity);
    }

    private static void Validate(CandidateIdentificationSettings settings)
    {
        if (!settings.IncludeStocks && !settings.IncludeEtfs) throw new ArgumentException("At least one security type must be enabled.");
        if (settings.MinimumSharePrice < 0 || settings.MinimumAverageDailyVolume < 0 || settings.MinimumAverageDollarVolume < 0) throw new ArgumentException("Minimum price and liquidity values cannot be negative.");
        if (settings.PreferredBidAskSpreadPercent < 0 || settings.MaximumBidAskSpreadPercent < settings.PreferredBidAskSpreadPercent) throw new ArgumentException("The maximum spread must be at least the preferred spread.");
        var percentages = new[] { settings.MaximumInitialPositionPercent, settings.HardSingleNameCapPercent, settings.MaximumSectorExposurePercent, settings.MaximumCorrelatedClusterExposurePercent };
        if (percentages.Any(value => value is <= 0 or > 100) || settings.MaximumInitialPositionPercent > settings.HardSingleNameCapPercent) throw new ArgumentException("Portfolio caps must be between 0 and 100, and the initial position cannot exceed the hard single-name cap.");
    }

    private async Task<long> GetUserIdAsync(Guid externalId, CancellationToken cancellationToken) =>
        await dbContext.Users.Where(user => user.ExternalId == externalId && user.IsActive && !user.IsDeleted).Select(user => (long?)user.Id).SingleOrDefaultAsync(cancellationToken)
        ?? throw new InvalidOperationException("The active user account could not be found.");
    private static string Serialize(CandidateIdentificationSettings settings) => JsonSerializer.Serialize(settings);
    private static CandidateIdentificationSettingsDetails ToDetails(CandidateIdentificationSettingsEntity entity) =>
        new(JsonSerializer.Deserialize<CandidateIdentificationSettings>(entity.SettingsJson) ?? CandidateIdentificationSettings.Defaults, entity.RowVersion);
}
