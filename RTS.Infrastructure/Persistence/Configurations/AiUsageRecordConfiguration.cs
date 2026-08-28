using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RTS.Infrastructure.AiUsage;
using RTS.Infrastructure.Identity;

namespace RTS.Infrastructure.Persistence.Configurations;

public sealed class AiUsageRecordConfiguration : IEntityTypeConfiguration<AiUsageRecord>
{
    public void Configure(EntityTypeBuilder<AiUsageRecord> builder)
    {
        builder.ToTable("AiUsageRecords", "Audit");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedOnAdd();
        builder.Property(item => item.ExternalId).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
        builder.Property(item => item.OperationType).HasMaxLength(50).IsUnicode(false).IsRequired();
        builder.Property(item => item.Provider).HasMaxLength(50).IsRequired();
        builder.Property(item => item.Model).HasMaxLength(100).IsRequired();
        builder.Property(item => item.PromptVersion).HasMaxLength(50).IsRequired();
        builder.Property(item => item.SchemaVersion).HasMaxLength(50).IsRequired();
        builder.Property(item => item.RecordedUtc).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()").ValueGeneratedOnAdd();
        builder.Property(item => item.RowVersion).IsRowVersion();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(item => item.OwnerUserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(item => item.ExternalId).IsUnique().HasDatabaseName("UX_AiUsageRecords_ExternalId");
        builder.HasIndex(item => new { item.RecordedUtc, item.OperationType }).HasDatabaseName("IX_AiUsageRecords_RecordedUtc_OperationType");
        builder.HasIndex(item => new { item.OwnerUserId, item.RecordedUtc }).HasDatabaseName("IX_AiUsageRecords_OwnerUserId_RecordedUtc");
    }
}
