using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RTS.Infrastructure.CandidateDiscovery;
using RTS.Infrastructure.EvaluationJobs;
using RTS.Infrastructure.Identity;

namespace RTS.Infrastructure.Persistence.Configurations;

public sealed class EvaluationJobConfiguration : IEntityTypeConfiguration<EvaluationJob>
{
    public void Configure(EntityTypeBuilder<EvaluationJob> builder)
    {
        builder.ToTable("EvaluationJobs", "Trading", table =>
        {
            table.HasCheckConstraint("CK_EvaluationJobs_Status", "[Status] BETWEEN 1 AND 5");
            table.HasCheckConstraint("CK_EvaluationJobs_Progress", "[ProgressPercent] BETWEEN 0 AND 100");
            table.HasCheckConstraint("CK_EvaluationJobs_Attempts", "[AttemptCount] >= 0");
        });
        builder.HasKey(job => job.Id);
        builder.Property(job => job.ExternalId).ValueGeneratedNever();
        builder.Property(job => job.Symbol).HasMaxLength(15).IsRequired();
        builder.Property(job => job.ProgressMessage).HasMaxLength(500);
        builder.Property(job => job.ErrorMessage).HasMaxLength(4000);
        builder.Property(job => job.CreatedUtc).HasPrecision(3);
        builder.Property(job => job.StartedUtc).HasPrecision(3);
        builder.Property(job => job.CompletedUtc).HasPrecision(3);
        builder.Property(job => job.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(job => job.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DiscoveryCandidate>().WithMany().HasForeignKey(job => job.DiscoveryCandidateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(job => job.ExternalId).IsUnique();
        builder.HasIndex(job => new { job.OwnerUserId, job.Status, job.CreatedUtc });
    }
}

public sealed class MarketDataSnapshotConfiguration : IEntityTypeConfiguration<MarketDataSnapshot>
{
    public void Configure(EntityTypeBuilder<MarketDataSnapshot> builder)
    {
        builder.ToTable("MarketDataSnapshots", "Trading");
        builder.HasKey(snapshot => snapshot.Id);
        builder.Property(snapshot => snapshot.ExternalId).ValueGeneratedNever();
        builder.Property(snapshot => snapshot.Symbol).HasMaxLength(15).IsRequired();
        builder.Property(snapshot => snapshot.ProviderKey).HasMaxLength(100).IsRequired();
        builder.Property(snapshot => snapshot.RetrievedUtc).HasPrecision(3);
        builder.Property(snapshot => snapshot.RequestJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(snapshot => snapshot.BarsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(snapshot => snapshot.MissingBarsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.HasOne<EvaluationJob>().WithMany().HasForeignKey(snapshot => snapshot.EvaluationJobId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(snapshot => snapshot.ExternalId).IsUnique();
        builder.HasIndex(snapshot => new { snapshot.EvaluationJobId, snapshot.RetrievedUtc });
    }
}

public sealed class EvaluationResultConfiguration : IEntityTypeConfiguration<EvaluationResult>
{
    public void Configure(EntityTypeBuilder<EvaluationResult> builder)
    {
        builder.ToTable("EvaluationResults", "Trading", table =>
        {
            table.HasCheckConstraint("CK_EvaluationResults_ConfigurationJson", "ISJSON([ConfigurationJson]) = 1");
            table.HasCheckConstraint("CK_EvaluationResults_ResultJson", "ISJSON([ResultJson]) = 1");
            table.HasCheckConstraint("CK_EvaluationResults_MarketDataSnapshotExternalIdsJson", "ISJSON([MarketDataSnapshotExternalIdsJson]) = 1");
        });
        builder.HasKey(result => result.Id);
        builder.Property(result => result.ExternalId).ValueGeneratedNever();
        builder.Property(result => result.CreatedUtc).HasPrecision(3);
        builder.Property(result => result.CalculationVersion).HasMaxLength(50).IsRequired();
        builder.Property(result => result.ConfigurationJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(result => result.ResultJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(result => result.MarketDataSnapshotExternalIdsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(result => result.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasOne<EvaluationJob>().WithOne().HasForeignKey<EvaluationResult>(result => result.EvaluationJobId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(result => result.ExternalId).IsUnique();
        builder.HasIndex(result => result.EvaluationJobId).IsUnique();
    }
}
