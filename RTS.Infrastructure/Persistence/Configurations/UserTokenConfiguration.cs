using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RTS.Infrastructure.Identity;

namespace RTS.Infrastructure.Persistence.Configurations;

public sealed class UserTokenConfiguration
    : IEntityTypeConfiguration<IdentityUserToken<long>>
{
    public void Configure(EntityTypeBuilder<IdentityUserToken<long>> builder)
    {
        builder.ToTable("UserTokens", "Identity");

        builder.HasKey(userToken => new
        {
            userToken.UserId,
            userToken.LoginProvider,
            userToken.Name
        })
            .HasName("PK_UserTokens");

        builder.Property(userToken => userToken.LoginProvider)
            .HasMaxLength(128);

        builder.Property(userToken => userToken.Name)
            .HasMaxLength(128);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(userToken => userToken.UserId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_UserTokens_User");
    }
}
