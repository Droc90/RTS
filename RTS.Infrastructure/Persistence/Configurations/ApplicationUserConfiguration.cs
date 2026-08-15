using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RTS.Infrastructure.Identity;

namespace RTS.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration
    : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("Users", "Identity");

        builder.HasKey(user => user.Id)
            .HasName("PK_Users");

        builder.Property(user => user.Id)
            .UseIdentityColumn();

        builder.Property(user => user.ExternalId)
            .HasDefaultValueSql("(NEWID())")
            .ValueGeneratedOnAdd();

        builder.Property(user => user.UserName)
            .HasMaxLength(256);

        builder.Property(user => user.NormalizedUserName)
            .HasMaxLength(256);

        builder.Property(user => user.Email)
            .HasMaxLength(256);

        builder.Property(user => user.NormalizedEmail)
            .HasMaxLength(256);

        builder.Property(user => user.LockoutEnd)
            .HasColumnType("datetimeoffset(7)");

        builder.Property(user => user.IsActive)
            .HasDefaultValue(true);

        builder.Property(user => user.CreatedUtc)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("(SYSUTCDATETIME())")
            .ValueGeneratedOnAdd();

        builder.Property(user => user.ModifiedUtc)
            .HasColumnType("datetime2(3)");

        builder.Property(user => user.LastLoginUtc)
            .HasColumnType("datetime2(3)");

        builder.Property(user => user.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(user => user.DeletedUtc)
            .HasColumnType("datetime2(3)");

        builder.Property(user => user.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasIndex(user => user.ExternalId)
            .IsUnique()
            .HasDatabaseName("UX_Users_ExternalId");

        builder.HasIndex(user => user.NormalizedUserName)
            .IsUnique()
            .HasFilter("[NormalizedUserName] IS NOT NULL")
            .HasDatabaseName("UX_Users_NormalizedUserName");

        builder.HasIndex(user => user.NormalizedEmail)
            .HasDatabaseName("IX_Users_NormalizedEmail");

        builder.HasIndex(user => new
        {
            user.IsActive,
            user.IsDeleted
        })
            .HasDatabaseName("IX_Users_IsActive_IsDeleted");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(user => user.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Users_CreatedByUser");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(user => user.ModifiedByUserId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Users_ModifiedByUser");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(user => user.DeletedByUserId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Users_DeletedByUser");
    }
}
