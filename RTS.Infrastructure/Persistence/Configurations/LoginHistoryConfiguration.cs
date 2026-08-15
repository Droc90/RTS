using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RTS.Infrastructure.Auditing;
using RTS.Infrastructure.Identity;

namespace RTS.Infrastructure.Persistence.Configurations;

public sealed class LoginHistoryConfiguration
    : IEntityTypeConfiguration<LoginHistory>
{
    public void Configure(
        EntityTypeBuilder<LoginHistory> builder)
    {
        builder.ToTable("LoginHistory", "Audit");

        builder.HasKey(history => history.Id);

        builder.Property(history => history.Id)
            .ValueGeneratedOnAdd();

        builder.Property(history => history.ExternalId)
            .HasDefaultValueSql("NEWID()")
            .ValueGeneratedOnAdd();

        builder.Property(history => history.IdentifierHash)
            .HasMaxLength(32)
            .IsFixedLength();

        builder.Property(history => history.EventType)
            .HasMaxLength(50)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(history => history.FailureCode)
            .HasMaxLength(100)
            .IsUnicode(false);

        builder.Property(history => history.IpAddress)
            .HasMaxLength(45)
            .IsUnicode(false);

        builder.Property(history => history.UserAgent)
            .HasMaxLength(500);

        builder.Property(history => history.CreatedUtc)
            .HasPrecision(3)
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .ValueGeneratedOnAdd();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(history => history.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(history => history.ExternalId)
            .IsUnique()
            .HasDatabaseName("UX_LoginHistory_ExternalId");

        builder.HasIndex(history => new
        {
            history.UserId,
            history.CreatedUtc
        })
            .HasDatabaseName(
                "IX_LoginHistory_UserId_CreatedUtc");

        builder.HasIndex(history => history.IdentifierHash)
            .HasDatabaseName(
                "IX_LoginHistory_IdentifierHash");

        builder.HasIndex(history => history.IpAddress)
            .HasDatabaseName(
                "IX_LoginHistory_IpAddress");

        builder.HasIndex(history => history.EventType)
            .HasDatabaseName(
                "IX_LoginHistory_EventType");

        builder.HasIndex(history => history.CorrelationId)
            .HasDatabaseName(
                "IX_LoginHistory_CorrelationId");
    }
}