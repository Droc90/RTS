using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RTS.Domain.TradingModels.Timeframes;

namespace RTS.Infrastructure.Persistence.Configurations;

public sealed class TradingModelTimeframeConfiguration
    : IEntityTypeConfiguration<TradingModelTimeframe>
{
    public void Configure(
        EntityTypeBuilder<TradingModelTimeframe> builder)
    {
        builder.ToTable(
            "TradingModelTimeframes",
            "Trading");

        builder.HasKey(timeframe => timeframe.Id);

        builder.Property(timeframe => timeframe.Id)
            .ValueGeneratedOnAdd();

        builder.Property(timeframe => timeframe.ExternalId)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(
                timeframe =>
                    timeframe.TradingModelVersionId)
            .IsRequired();

        builder.Property(timeframe => timeframe.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(timeframe => timeframe.DisplayOrder)
            .IsRequired();

        builder.Property(timeframe => timeframe.LookbackValue)
            .IsRequired();

        builder.Property(timeframe => timeframe.LookbackUnit)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(timeframe => timeframe.BarIntervalValue)
            .IsRequired();

        builder.Property(timeframe => timeframe.BarIntervalUnit)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(timeframe => timeframe.MarketSessionMode)
            .HasConversion<int>()
            .IsRequired();

        builder.Property<byte[]>("RowVersion")
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasMany(timeframe => timeframe.Indicators)
            .WithOne()
            .HasForeignKey(
                indicator =>
                    indicator.TradingModelTimeframeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(timeframe => timeframe.Indicators)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(timeframe => timeframe.ExternalId)
            .IsUnique()
            .HasDatabaseName(
                "UX_TradingModelTimeframes_ExternalId");

        builder.HasIndex(timeframe => new
        {
            timeframe.TradingModelVersionId,
            timeframe.Name
        })
            .IsUnique()
            .HasDatabaseName(
                "UX_TradingModelTimeframes_VersionId_Name");

        builder.HasIndex(timeframe => new
        {
            timeframe.TradingModelVersionId,
            timeframe.DisplayOrder
        })
            .IsUnique()
            .HasDatabaseName(
                "UX_TradingModelTimeframes_VersionId_DisplayOrder");
    }
}