using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RTS.Domain.TradingModels.Criteria;

namespace RTS.Infrastructure.Persistence.Configurations;

public sealed class TradingModelCriterionConfiguration
    : IEntityTypeConfiguration<TradingModelCriterion>
{
    public void Configure(
        EntityTypeBuilder<TradingModelCriterion> builder)
    {
        builder.ToTable(
            "TradingModelCriteria",
            "Trading");

        builder.HasKey(criterion => criterion.Id);

        builder.Property(criterion => criterion.Id)
            .ValueGeneratedOnAdd();

        builder.Property(criterion => criterion.ExternalId)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(
                criterion =>
                    criterion.TradingModelVersionId)
            .IsRequired();

        builder.Property(criterion => criterion.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(criterion => criterion.MetricKey)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(criterion => criterion.Purpose)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(criterion => criterion.Operator)
            .HasColumnName("ComparisonOperator")
            .HasConversion<int>()
            .IsRequired();

        builder.OwnsOne(
            criterion => criterion.PrimaryValue,
            primaryValue =>
            {
                primaryValue.Property(value => value.ValueType)
                    .HasColumnName("PrimaryValueType")
                    .HasConversion<int>()
                    .IsRequired();

                primaryValue.Property(value => value.Value)
                    .HasColumnName("PrimaryValue")
                    .HasMaxLength(500)
                    .IsRequired();
            });

        builder.Navigation(criterion => criterion.PrimaryValue)
            .IsRequired();

        builder.OwnsOne(
            criterion => criterion.SecondaryValue,
            secondaryValue =>
            {
                secondaryValue.Property(value => value.ValueType)
                    .HasColumnName("SecondaryValueType")
                    .HasConversion<int>();

                secondaryValue.Property(value => value.Value)
                    .HasColumnName("SecondaryValue")
                    .HasMaxLength(500);
            });

        builder.Property(criterion => criterion.Weight)
            .HasPrecision(9, 4);

        builder.Property(criterion => criterion.DisplayOrder)
            .IsRequired();

        builder.Property(criterion => criterion.IsEnabled)
            .IsRequired();

        builder.Property<byte[]>("RowVersion")
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasIndex(criterion => criterion.ExternalId)
            .IsUnique()
            .HasDatabaseName(
                "UX_TradingModelCriteria_ExternalId");

        builder.HasIndex(criterion => new
        {
            criterion.TradingModelVersionId,
            criterion.Name
        })
            .IsUnique()
            .HasDatabaseName(
                "UX_TradingModelCriteria_VersionId_Name");

        builder.HasIndex(criterion => new
        {
            criterion.TradingModelVersionId,
            criterion.DisplayOrder
        })
            .IsUnique()
            .HasDatabaseName(
                "UX_TradingModelCriteria_VersionId_DisplayOrder");

        builder.HasIndex(criterion => new
        {
            criterion.TradingModelVersionId,
            criterion.Purpose
        })
            .HasDatabaseName(
                "IX_TradingModelCriteria_VersionId_Purpose");
    }
}