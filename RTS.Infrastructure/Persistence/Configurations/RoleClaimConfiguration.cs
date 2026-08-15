using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RTS.Infrastructure.Identity;

namespace RTS.Infrastructure.Persistence.Configurations;

public sealed class RoleClaimConfiguration
    : IEntityTypeConfiguration<IdentityRoleClaim<long>>
{
    public void Configure(EntityTypeBuilder<IdentityRoleClaim<long>> builder)
    {
        builder.ToTable("RoleClaims", "Identity");

        builder.HasKey(roleClaim => roleClaim.Id)
            .HasName("PK_RoleClaims");

        builder.Property(roleClaim => roleClaim.Id)
            .UseIdentityColumn();

        builder.HasOne<ApplicationRole>()
            .WithMany()
            .HasForeignKey(roleClaim => roleClaim.RoleId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_RoleClaims_Role");

        builder.HasIndex(roleClaim => roleClaim.RoleId)
            .HasDatabaseName("IX_RoleClaims_RoleId");
    }
}
