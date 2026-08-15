using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RTS.Infrastructure.Identity;

namespace RTS.Infrastructure.Profiles;

public sealed class UserProfileConfiguration
    : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(
        EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable(
            "UserProfiles",
            "Profile");

        builder.HasKey(profile => profile.Id)
            .HasName("PK_UserProfiles");

        builder.Property(profile => profile.Id)
            .ValueGeneratedOnAdd();

        builder.Property(profile => profile.ExternalId)
            .IsRequired()
            .HasDefaultValueSql("NEWID()")
            .ValueGeneratedOnAdd();

        builder.Property(profile => profile.UserId)
            .IsRequired();

        builder.Property(profile => profile.DisplayName)
            .HasMaxLength(150);

        builder.Property(profile => profile.FirstName)
            .HasMaxLength(100);

        builder.Property(profile => profile.LastName)
            .HasMaxLength(100);

        builder.Property(profile => profile.TimeZoneId)
            .HasMaxLength(100);

        builder.Property(profile => profile.Locale)
            .HasMaxLength(20);

        builder.Property(profile => profile.PreferredDateFormat)
            .HasMaxLength(50);

        builder.Property(
                profile => profile.IsOnboardingComplete)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(profile => profile.CreatedUtc)
            .IsRequired()
            .HasPrecision(3)
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .ValueGeneratedOnAdd();

        builder.Property(profile => profile.ModifiedUtc)
            .HasPrecision(3);

        builder.Property(profile => profile.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(profile => profile.DeletedUtc)
            .HasPrecision(3);

        builder.Property(profile => profile.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<UserProfile>(
                profile => profile.UserId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_UserProfiles_User");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(
                profile => profile.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName(
                "FK_UserProfiles_CreatedByUser");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(
                profile => profile.ModifiedByUserId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName(
                "FK_UserProfiles_ModifiedByUser");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(
                profile => profile.DeletedByUserId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName(
                "FK_UserProfiles_DeletedByUser");

        builder.HasIndex(profile => profile.ExternalId)
            .IsUnique()
            .HasDatabaseName(
                "UX_UserProfiles_ExternalId");

        builder.HasIndex(profile => profile.UserId)
            .IsUnique()
            .HasDatabaseName(
                "UX_UserProfiles_UserId");
    }
}