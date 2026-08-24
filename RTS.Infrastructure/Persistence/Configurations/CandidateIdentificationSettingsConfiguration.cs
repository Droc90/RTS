using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RTS.Infrastructure.CandidateDiscovery;
using RTS.Infrastructure.Identity;

namespace RTS.Infrastructure.Persistence.Configurations;

public sealed class CandidateIdentificationSettingsConfiguration : IEntityTypeConfiguration<CandidateIdentificationSettingsEntity>
{
    public void Configure(EntityTypeBuilder<CandidateIdentificationSettingsEntity> builder)
    {
        builder.ToTable("CandidateIdentificationSettings", "Trading", table => table.HasCheckConstraint("CK_CandidateIdentificationSettings_SettingsJson", "ISJSON([SettingsJson]) = 1"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.ExternalId).ValueGeneratedNever();
        builder.Property(item => item.SettingsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(item => item.CreatedUtc).HasPrecision(3);
        builder.Property(item => item.ModifiedUtc).HasPrecision(3);
        builder.Property(item => item.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasOne<ApplicationUser>().WithOne().HasForeignKey<CandidateIdentificationSettingsEntity>(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(item => item.ExternalId).IsUnique();
        builder.HasIndex(item => item.UserId).IsUnique();
    }
}
