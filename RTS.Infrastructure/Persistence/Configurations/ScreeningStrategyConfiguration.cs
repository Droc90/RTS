using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RTS.Domain.ScreeningStrategies;

namespace RTS.Infrastructure.Persistence.Configurations;

public sealed class ScreeningStrategyConfiguration
    : IEntityTypeConfiguration<ScreeningStrategy>
{
    public void Configure(
        EntityTypeBuilder<ScreeningStrategy> builder)
    {
        builder.ToTable(
            "ScreeningStrategies",
            "Trading");

        builder.HasKey(strategy => strategy.Id);

        builder.Property(strategy => strategy.Id)
            .ValueGeneratedOnAdd();

        builder.Property(strategy => strategy.ExternalId)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(strategy => strategy.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(strategy => strategy.Description)
            .HasMaxLength(2000);

        builder.Property(strategy => strategy.IsActive)
            .IsRequired();

        builder.Property<DateTime>("CreatedUtc")
            .HasPrecision(3)
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .ValueGeneratedOnAdd();

        builder.Property<byte[]>("RowVersion")
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasMany(strategy => strategy.Versions)
            .WithOne()
            .HasForeignKey(
                version =>
                    version.ScreeningStrategyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(strategy => strategy.Versions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(strategy => strategy.ExternalId)
            .IsUnique()
            .HasDatabaseName(
                "UX_ScreeningStrategies_ExternalId");

        builder.HasIndex(strategy => new
        {
            strategy.OwnerUserId,
            strategy.Name
        })
            .IsUnique()
            .HasFilter("[OwnerUserId] IS NOT NULL")
            .HasDatabaseName(
                "UX_ScreeningStrategies_OwnerUserId_Name");

        builder.HasIndex(strategy => strategy.Name)
            .IsUnique()
            .HasFilter("[OwnerUserId] IS NULL")
            .HasDatabaseName(
                "UX_ScreeningStrategies_SystemTemplate_Name");

        builder.HasIndex(strategy => new
        {
            strategy.OwnerUserId,
            strategy.IsActive
        })
            .HasDatabaseName(
                "IX_ScreeningStrategies_OwnerUserId_IsActive");
    }
}