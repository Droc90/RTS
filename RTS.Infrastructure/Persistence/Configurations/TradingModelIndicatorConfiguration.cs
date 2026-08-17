using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RTS.Domain.TradingModels.Indicators;

namespace RTS.Infrastructure.Persistence.Configurations;

public sealed class TradingModelIndicatorConfiguration
    : IEntityTypeConfiguration<TradingModelIndicator>
{
    public void Configure(
        EntityTypeBuilder<TradingModelIndicator> builder)
    {
        builder.ToTable(
            "TradingModelIndicators",
            "Trading");

        builder.HasKey(indicator => indicator.Id);

        builder.Property(indicator => indicator.Id)
            .ValueGeneratedOnAdd();

        builder.Property(indicator => indicator.ExternalId)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(
                indicator =>
                    indicator.TradingModelTimeframeId)
            .IsRequired();

        builder.Property(indicator => indicator.IndicatorType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(indicator => indicator.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(indicator => indicator.Pane)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(indicator => indicator.DisplayOrder)
            .IsRequired();

        builder.Property(indicator => indicator.IsEnabled)
            .IsRequired();

        builder.Property<byte[]>("RowVersion")
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasMany(indicator => indicator.Parameters)
            .WithOne()
            .HasForeignKey(
                parameter =>
                    parameter.TradingModelIndicatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(indicator => indicator.Parameters)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(indicator => indicator.ExternalId)
            .IsUnique()
            .HasDatabaseName(
                "UX_TradingModelIndicators_ExternalId");

        builder.HasIndex(indicator => new
        {
            indicator.TradingModelTimeframeId,
            indicator.Name
        })
            .IsUnique()
            .HasDatabaseName(
                "UX_TradingModelIndicators_TimeframeId_Name");

        builder.HasIndex(indicator => new
        {
            indicator.TradingModelTimeframeId,
            indicator.DisplayOrder
        })
            .IsUnique()
            .HasDatabaseName(
                "UX_TradingModelIndicators_TimeframeId_DisplayOrder");

        builder.HasIndex(indicator => new
        {
            indicator.TradingModelTimeframeId,
            indicator.IndicatorType
        })
            .HasDatabaseName(
                "IX_TradingModelIndicators_TimeframeId_IndicatorType");
    }
}