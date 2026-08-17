using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RTS.Domain.TradingModels.Indicators;

namespace RTS.Infrastructure.Persistence.Configurations;

public sealed class TradingModelIndicatorParameterConfiguration
    : IEntityTypeConfiguration<TradingModelIndicatorParameter>
{
    public void Configure(
        EntityTypeBuilder<TradingModelIndicatorParameter> builder)
    {
        builder.ToTable(
            "TradingModelIndicatorParameters",
            "Trading");

        builder.HasKey(parameter => parameter.Id);

        builder.Property(parameter => parameter.Id)
            .ValueGeneratedOnAdd();

        builder.Property(parameter => parameter.ExternalId)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(
                parameter =>
                    parameter.TradingModelIndicatorId)
            .IsRequired();

        builder.Property(parameter => parameter.Key)
            .HasColumnName("ParameterKey")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(parameter => parameter.ValueType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(parameter => parameter.Value)
            .HasColumnName("ParameterValue")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property<byte[]>("RowVersion")
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasIndex(parameter => parameter.ExternalId)
            .IsUnique()
            .HasDatabaseName(
                "UX_TradingModelIndicatorParameters_ExternalId");

        builder.HasIndex(parameter => new
        {
            parameter.TradingModelIndicatorId,
            parameter.Key
        })
            .IsUnique()
            .HasDatabaseName(
                "UX_TradingModelIndicatorParameters_IndicatorId_Key");
    }
}