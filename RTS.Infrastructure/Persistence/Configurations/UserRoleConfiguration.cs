using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RTS.Infrastructure.Identity;

namespace RTS.Infrastructure.Persistence.Configurations;

public sealed class UserRoleConfiguration
    : IEntityTypeConfiguration<IdentityUserRole<long>>
{
    public void Configure(EntityTypeBuilder<IdentityUserRole<long>> builder)
    {
        builder.ToTable("UserRoles", "Identity");

        builder.HasKey(userRole => new
        {
            userRole.UserId,
            userRole.RoleId
        })
            .HasName("PK_UserRoles");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(userRole => userRole.UserId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_UserRoles_User");

        builder.HasOne<ApplicationRole>()
            .WithMany()
            .HasForeignKey(userRole => userRole.RoleId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_UserRoles_Role");

        builder.HasIndex(userRole => userRole.RoleId)
            .HasDatabaseName("IX_UserRoles_RoleId");
    }
}
