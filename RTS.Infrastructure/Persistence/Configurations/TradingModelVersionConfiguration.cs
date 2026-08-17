using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RTS.Domain.TradingModels;

namespace RTS.Infrastructure.Persistence.Configurations;

public sealed class TradingModelVersionConfiguration
    : IEntityTypeConfiguration<TradingModelVersion>
{
    public void Configure(
        EntityTypeBuilder<TradingModelVersion> builder)
    {
        builder.ToTable(
            "TradingModelVersions",
            "Trading");

        builder.HasKey(version => version.Id);

        builder.Property(version => version.Id)
            .ValueGeneratedOnAdd();

        builder.Property(version => version.ExternalId)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(version => version.TradingModelId)
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

        builder.HasMany(version => version.Timeframes)
            .WithOne()
            .HasForeignKey(
                timeframe =>
                    timeframe.TradingModelVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(version => version.Timeframes)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(version => version.Criteria)
            .WithOne()
            .HasForeignKey(
                criterion =>
                    criterion.TradingModelVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(version => version.Criteria)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(version => version.ExternalId)
            .IsUnique()
            .HasDatabaseName(
                "UX_TradingModelVersions_ExternalId");

        builder.HasIndex(version => new
        {
            version.TradingModelId,
            version.VersionNumber
        })
            .IsUnique()
            .HasDatabaseName(
                "UX_TradingModelVersions_ModelId_VersionNumber");

        builder.HasIndex(version => version.TradingModelId)
            .IsUnique()
            .HasFilter("[Status] = 1")
            .HasDatabaseName(
                "UX_TradingModelVersions_OneDraftPerModel");

        builder.HasIndex(version => new
        {
            version.TradingModelId,
            version.Status
        })
            .HasDatabaseName(
                "IX_TradingModelVersions_ModelId_Status");
    }
}