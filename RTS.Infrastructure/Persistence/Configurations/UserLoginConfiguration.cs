using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RTS.Infrastructure.Identity;

namespace RTS.Infrastructure.Persistence.Configurations;

public sealed class UserLoginConfiguration
    : IEntityTypeConfiguration<IdentityUserLogin<long>>
{
    public void Configure(EntityTypeBuilder<IdentityUserLogin<long>> builder)
    {
        builder.ToTable("UserLogins", "Identity");

        builder.HasKey(userLogin => new
        {
            userLogin.LoginProvider,
            userLogin.ProviderKey
        })
            .HasName("PK_UserLogins");

        builder.Property(userLogin => userLogin.LoginProvider)
            .HasMaxLength(128);

        builder.Property(userLogin => userLogin.ProviderKey)
            .HasMaxLength(128);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(userLogin => userLogin.UserId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_UserLogins_User");

        builder.HasIndex(userLogin => userLogin.UserId)
            .HasDatabaseName("IX_UserLogins_UserId");
    }
}