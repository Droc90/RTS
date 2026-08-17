using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RTS.Domain.TradingModels;

namespace RTS.Infrastructure.Persistence.Configurations;

public sealed class TradingModelConfiguration
    : IEntityTypeConfiguration<TradingModel>
{
    public void Configure(
        EntityTypeBuilder<TradingModel> builder)
    {
        builder.ToTable("TradingModels", "Trading");

        builder.HasKey(model => model.Id);

        builder.Property(model => model.Id)
            .ValueGeneratedOnAdd();

        builder.Property(model => model.ExternalId)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(model => model.OwnerUserId);

        builder.Property(model => model.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(model => model.Description)
            .HasMaxLength(2000);

        builder.Property(model => model.IsActive)
            .IsRequired();

        builder.Property<DateTime>("CreatedUtc")
            .HasPrecision(3)
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .ValueGeneratedOnAdd();

        builder.Property<byte[]>("RowVersion")
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasMany(model => model.Versions)
            .WithOne()
            .HasForeignKey(version => version.TradingModelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(model => model.Versions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(model => model.ExternalId)
            .IsUnique()
            .HasDatabaseName("UX_TradingModels_ExternalId");

        builder.HasIndex(model => new
        {
            model.OwnerUserId,
            model.Name
        })
            .IsUnique()
            .HasFilter("[OwnerUserId] IS NOT NULL")
            .HasDatabaseName(
                "UX_TradingModels_OwnerUserId_Name");

        builder.HasIndex(model => model.Name)
            .IsUnique()
            .HasFilter("[OwnerUserId] IS NULL")
            .HasDatabaseName(
                "UX_TradingModels_SystemTemplate_Name");

        builder.HasIndex(model => new
        {
            model.OwnerUserId,
            model.IsActive
        })
            .HasDatabaseName(
                "IX_TradingModels_OwnerUserId_IsActive");
    }
}