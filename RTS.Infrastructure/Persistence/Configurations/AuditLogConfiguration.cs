using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RTS.Infrastructure.Auditing;
using RTS.Infrastructure.Identity;

namespace RTS.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration
    : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(
        EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs", "Audit");

        builder.HasKey(auditLog => auditLog.Id);

        builder.Property(auditLog => auditLog.Id)
            .ValueGeneratedOnAdd();

        builder.Property(auditLog => auditLog.ExternalId)
            .HasDefaultValueSql("NEWID()")
            .ValueGeneratedOnAdd();

        builder.Property(auditLog => auditLog.EventCategory)
            .HasMaxLength(100);

        builder.Property(auditLog => auditLog.Action)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(auditLog => auditLog.EntityType)
            .HasMaxLength(150);

        builder.Property(auditLog => auditLog.IpAddress)
            .HasMaxLength(45)
            .IsUnicode(false);

        builder.Property(auditLog => auditLog.UserAgent)
            .HasMaxLength(500);

        builder.Property(auditLog => auditLog.IsSuccess)
            .HasDefaultValue(true);

        builder.Property(auditLog => auditLog.FailureReason)
            .HasMaxLength(1000);

        builder.Property(auditLog => auditLog.CreatedUtc)
            .HasPrecision(3)
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .ValueGeneratedOnAdd();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(auditLog => auditLog.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(auditLog => auditLog.ExternalId)
            .IsUnique()
            .HasDatabaseName("UX_AuditLogs_ExternalId");

        builder.HasIndex(auditLog => new
        {
            auditLog.UserId,
            auditLog.CreatedUtc
        })
            .HasDatabaseName(
                "IX_AuditLogs_UserId_CreatedUtc");

        builder.HasIndex(auditLog => new
        {
            auditLog.EntityType,
            auditLog.EntityId,
            auditLog.CreatedUtc
        })
            .HasDatabaseName(
                "IX_AuditLogs_EntityType_EntityId_CreatedUtc");

        builder.HasIndex(auditLog =>
                auditLog.EntityExternalId)
            .HasDatabaseName(
                "IX_AuditLogs_EntityExternalId");

        builder.HasIndex(auditLog =>
                auditLog.CorrelationId)
            .HasDatabaseName(
                "IX_AuditLogs_CorrelationId");

        builder.HasIndex(auditLog => new
        {
            auditLog.Action,
            auditLog.CreatedUtc
        })
            .HasDatabaseName(
                "IX_AuditLogs_Action_CreatedUtc");
    }
}