using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RTS.Infrastructure.Identity;

namespace RTS.Infrastructure.Persistence.Configurations;

public sealed class ApplicationRoleConfiguration
    : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.ToTable("Roles", "Identity");

        builder.HasKey(role => role.Id)
            .HasName("PK_Roles");

        builder.Property(role => role.Id)
            .UseIdentityColumn();

        builder.Property(role => role.ExternalId)
            .HasDefaultValueSql("(NEWID())")
            .ValueGeneratedOnAdd();

        builder.Property(role => role.Name)
            .HasMaxLength(256);

        builder.Property(role => role.NormalizedName)
            .HasMaxLength(256);

        builder.Property(role => role.Description)
            .HasMaxLength(500);

        builder.Property(role => role.IsActive)
            .HasDefaultValue(true);

        builder.Property(role => role.CreatedUtc)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("(SYSUTCDATETIME())")
            .ValueGeneratedOnAdd();

        builder.Property(role => role.ModifiedUtc)
            .HasColumnType("datetime2(3)");

        builder.Property(role => role.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasIndex(role => role.ExternalId)
            .IsUnique()
            .HasDatabaseName("UX_Roles_ExternalId");

        builder.HasIndex(role => role.NormalizedName)
            .IsUnique()
            .HasFilter("[NormalizedName] IS NOT NULL")
            .HasDatabaseName("UX_Roles_NormalizedName");

        builder.HasIndex(role => role.IsActive)
            .HasDatabaseName("IX_Roles_IsActive");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(role => role.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Roles_CreatedByUser");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(role => role.ModifiedByUserId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Roles_ModifiedByUser");
    }
}
