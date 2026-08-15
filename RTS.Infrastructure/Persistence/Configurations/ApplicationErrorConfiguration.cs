using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RTS.Infrastructure.Auditing;
using RTS.Infrastructure.Identity;

namespace RTS.Infrastructure.Persistence.Configurations;

public sealed class ApplicationErrorConfiguration
    : IEntityTypeConfiguration<ApplicationError>
{
    public void Configure(
        EntityTypeBuilder<ApplicationError> builder)
    {
        builder.ToTable("ApplicationErrors", "Audit");

        builder.HasKey(error => error.Id);

        builder.Property(error => error.Id)
            .ValueGeneratedOnAdd();

        builder.Property(error => error.ExternalId)
            .HasDefaultValueSql("NEWID()")
            .ValueGeneratedOnAdd();

        builder.Property(error => error.ErrorType)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(error => error.ErrorCode)
            .HasMaxLength(100);

        builder.Property(error => error.SafeMessage)
            .HasMaxLength(1000);

        builder.Property(error => error.Source)
            .HasMaxLength(256);

        builder.Property(error => error.RequestPath)
            .HasMaxLength(2048);

        builder.Property(error => error.HttpMethod)
            .HasMaxLength(10)
            .IsUnicode(false);

        builder.Property(error => error.OccurredUtc)
            .HasPrecision(3)
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .ValueGeneratedOnAdd();

        builder.Property(error => error.ResolutionStatus)
            .HasMaxLength(30)
            .IsUnicode(false)
            .HasDefaultValue("New")
            .IsRequired();

        builder.Property(error => error.ResolvedUtc)
            .HasPrecision(3);

        builder.Property(error => error.ResolutionNotes)
            .HasMaxLength(2000);

        builder.Property(error => error.RowVersion)
            .IsRowVersion();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(error => error.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(error =>
                error.ResolvedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(error => error.ExternalId)
            .IsUnique()
            .HasDatabaseName(
                "UX_ApplicationErrors_ExternalId");

        builder.HasIndex(error => error.CorrelationId)
            .HasFilter("[CorrelationId] IS NOT NULL")
            .HasDatabaseName(
                "IX_ApplicationErrors_CorrelationId");

        builder.HasIndex(error => new
        {
            error.UserId,
            error.OccurredUtc
        })
            .HasDatabaseName(
                "IX_ApplicationErrors_UserId_OccurredUtc");

        builder.HasIndex(error => new
        {
            error.ResolutionStatus,
            error.OccurredUtc
        })
            .HasDatabaseName(
                "IX_ApplicationErrors_ResolutionStatus_OccurredUtc");

        builder.HasIndex(error => new
        {
            error.ErrorCode,
            error.OccurredUtc
        })
            .HasFilter("[ErrorCode] IS NOT NULL")
            .HasDatabaseName(
                "IX_ApplicationErrors_ErrorCode_OccurredUtc");
    }
}