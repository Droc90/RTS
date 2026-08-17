using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RTS.Domain.ScreeningStrategies;

namespace RTS.Infrastructure.Persistence.Configurations;

public sealed class ScreeningRuleConfiguration
    : IEntityTypeConfiguration<ScreeningRule>
{
    public void Configure(
        EntityTypeBuilder<ScreeningRule> builder)
    {
        builder.ToTable(
            "ScreeningRules",
            "Trading");

        builder.HasKey(rule => rule.Id);

        builder.Property(rule => rule.Id)
            .ValueGeneratedOnAdd();

        builder.Property(rule => rule.ExternalId)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(
                rule =>
                    rule.ScreeningStrategyVersionId)
            .IsRequired();

        builder.Property(rule => rule.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(rule => rule.MetricKey)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(rule => rule.Purpose)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(rule => rule.Operator)
            .HasColumnName("ComparisonOperator")
            .HasConversion<int>()
            .IsRequired();

        builder.OwnsOne(
            rule => rule.PrimaryValue,
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

        builder.Navigation(rule => rule.PrimaryValue)
            .IsRequired();

        builder.OwnsOne(
            rule => rule.SecondaryValue,
            secondaryValue =>
            {
                secondaryValue.Property(value => value.ValueType)
                    .HasColumnName("SecondaryValueType")
                    .HasConversion<int>();

                secondaryValue.Property(value => value.Value)
                    .HasColumnName("SecondaryValue")
                    .HasMaxLength(500);
            });

        builder.Property(rule => rule.Weight)
            .HasPrecision(9, 4);

        builder.Property(rule => rule.DisplayOrder)
            .IsRequired();

        builder.Property(rule => rule.IsEnabled)
            .IsRequired();

        builder.Property<byte[]>("RowVersion")
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasIndex(rule => rule.ExternalId)
            .IsUnique()
            .HasDatabaseName(
                "UX_ScreeningRules_ExternalId");

        builder.HasIndex(rule => new
        {
            rule.ScreeningStrategyVersionId,
            rule.Name
        })
            .IsUnique()
            .HasDatabaseName(
                "UX_ScreeningRules_VersionId_Name");

        builder.HasIndex(rule => new
        {
            rule.ScreeningStrategyVersionId,
            rule.DisplayOrder
        })
            .IsUnique()
            .HasDatabaseName(
                "UX_ScreeningRules_VersionId_DisplayOrder");

        builder.HasIndex(rule => new
        {
            rule.ScreeningStrategyVersionId,
            rule.Purpose
        })
            .HasDatabaseName(
                "IX_ScreeningRules_VersionId_Purpose");
    }
}