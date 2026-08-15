using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RTS.Infrastructure.Identity;

namespace RTS.Infrastructure.Persistence.Configurations;

public sealed class UserClaimConfiguration
    : IEntityTypeConfiguration<IdentityUserClaim<long>>
{
    public void Configure(EntityTypeBuilder<IdentityUserClaim<long>> builder)
    {
        builder.ToTable("UserClaims", "Identity");

        builder.HasKey(userClaim => userClaim.Id)
            .HasName("PK_UserClaims");

        builder.Property(userClaim => userClaim.Id)
            .UseIdentityColumn();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(userClaim => userClaim.UserId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_UserClaims_User");

        builder.HasIndex(userClaim => userClaim.UserId)
            .HasDatabaseName("IX_UserClaims_UserId");
    }
}
