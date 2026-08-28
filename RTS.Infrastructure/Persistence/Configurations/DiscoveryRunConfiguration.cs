using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RTS.Infrastructure.CandidateDiscovery;
using RTS.Infrastructure.Identity;
using RTS.Domain.ScreeningStrategies;

namespace RTS.Infrastructure.Persistence.Configurations;

public sealed class DiscoveryRunConfiguration : IEntityTypeConfiguration<DiscoveryRun>
{
    public void Configure(EntityTypeBuilder<DiscoveryRun> builder)
    {
        builder.ToTable("DiscoveryRuns", "Trading", table =>
            table.HasCheckConstraint("CK_DiscoveryRuns_Timestamps", "[CompletedUtc] >= [StartedUtc]"));
        builder.HasKey(run => run.Id);
        builder.Property(run => run.ExternalId).IsRequired().ValueGeneratedNever();
        builder.Property(run => run.UniverseKey).HasMaxLength(100).IsRequired();
        builder.Property(run => run.StartedUtc).HasPrecision(3);
        builder.Property(run => run.CompletedUtc).HasPrecision(3);
        builder.Property(run => run.CriteriaSnapshot).HasColumnType("nvarchar(max)").IsRequired();
        builder.HasMany(run => run.Candidates).WithOne().HasForeignKey(candidate => candidate.DiscoveryRunId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(run => run.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ScreeningStrategyVersion>().WithMany().HasForeignKey(run => run.ScreeningStrategyVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(run => run.ExternalId).IsUnique();
        builder.HasIndex(run => new { run.OwnerUserId, run.CompletedUtc });
    }
}

public sealed class DiscoveryCandidateConfiguration : IEntityTypeConfiguration<DiscoveryCandidate>
{
    public void Configure(EntityTypeBuilder<DiscoveryCandidate> builder)
    {
        builder.ToTable("DiscoveryCandidates", "Trading", table =>
        {
            table.HasCheckConstraint("CK_DiscoveryCandidates_Status", "[Status] BETWEEN 1 AND 6");
            table.HasCheckConstraint("CK_DiscoveryCandidates_Rank", "[Rank] > 0");
        });
        builder.HasKey(candidate => candidate.Id);
        builder.Property(candidate => candidate.ExternalId).IsRequired().ValueGeneratedNever();
        builder.Property(candidate => candidate.Symbol).HasMaxLength(15).IsRequired();
        builder.Property(candidate => candidate.Score).HasPrecision(9, 4);
        builder.Property(candidate => candidate.DataTimestampUtc).HasPrecision(3);
        builder.Property(candidate => candidate.FactorsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(candidate => candidate.EvidenceJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(candidate => candidate.CreatedUtc).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(candidate => candidate.ModifiedUtc).HasPrecision(3);
        builder.Property(candidate => candidate.IsWatchlisted).HasDefaultValue(false);
        builder.Property(candidate => candidate.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(candidate => candidate.ExternalId).IsUnique();
        builder.HasIndex(candidate => new { candidate.DiscoveryRunId, candidate.Symbol }).IsUnique();
        builder.HasIndex(candidate => new { candidate.Status, candidate.Rank });
    }
}
