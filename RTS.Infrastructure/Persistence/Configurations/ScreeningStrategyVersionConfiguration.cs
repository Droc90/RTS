using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RTS.Domain.ScreeningStrategies;

namespace RTS.Infrastructure.Persistence.Configurations;

public sealed class ScreeningStrategyVersionConfiguration
    : IEntityTypeConfiguration<ScreeningStrategyVersion>
{
    public void Configure(
        EntityTypeBuilder<ScreeningStrategyVersion> builder)
    {
        builder.ToTable(
            "ScreeningStrategyVersions",
            "Trading");

        builder.HasKey(version => version.Id);

        builder.Property(version => version.Id)
            .ValueGeneratedOnAdd();

        builder.Property(version => version.ExternalId)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(version => version.ScreeningStrategyId)
            .IsRequired();

        builder.Property(version => version.VersionNumber)
            .IsRequired();

        builder.Property(version => version.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(version => version.CreatedUtc)
            .HasConversion(
                value => value,
                value => DateTime.SpecifyKind(
                    value,
                    DateTimeKind.Utc))
            .HasPrecision(3)
            .IsRequired();

        builder.Property(version => version.PublishedUtc)
            .HasConversion(
                value => value,
                value => value.HasValue
                    ? DateTime.SpecifyKind(
                        value.Value,
                        DateTimeKind.Utc)
                    : (DateTime?)null)
            .HasPrecision(3);

        builder.Property<byte[]>("RowVersion")
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasMany(version => version.Rules)
            .WithOne()
            .HasForeignKey(
                rule =>
                    rule.ScreeningStrategyVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(version => version.Rules)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(version => version.ExternalId)
            .IsUnique()
            .HasDatabaseName(
                "UX_ScreeningStrategyVersions_ExternalId");

        builder.HasIndex(version => new
        {
            version.ScreeningStrategyId,
            version.VersionNumber
        })
            .IsUnique()
            .HasDatabaseName(
                "UX_ScreeningStrategyVersions_StrategyId_VersionNumber");

        builder.HasIndex(version => version.ScreeningStrategyId)
            .IsUnique()
            .HasFilter("[Status] = 1")
            .HasDatabaseName(
                "UX_ScreeningStrategyVersions_OneDraftPerStrategy");

        builder.HasIndex(version => new
        {
            version.ScreeningStrategyId,
            version.Status
        })
            .HasDatabaseName(
                "IX_ScreeningStrategyVersions_StrategyId_Status");
    }
}