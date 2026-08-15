using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RTS.Infrastructure.Identity;
using RTS.Infrastructure.Auditing;
using RTS.Infrastructure.Profiles;

namespace RTS.Infrastructure.Persistence;

public sealed class RtsDbContext : IdentityDbContext<
    ApplicationUser,
    ApplicationRole,
    long,
    IdentityUserClaim<long>,
    IdentityUserRole<long>,
    IdentityUserLogin<long>,
    IdentityRoleClaim<long>,
    IdentityUserToken<long>>
{
    public DbSet<LoginHistory> LoginHistory => Set<LoginHistory>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<UserProfile> UserProfiles =>
    Set<UserProfile>();
    public DbSet<ApplicationError> ApplicationErrors =>
    Set<ApplicationError>();

    public RtsDbContext(DbContextOptions<RtsDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>().ToTable("Users", "Identity");
        builder.Entity<ApplicationRole>().ToTable("Roles", "Identity");
        builder.Entity<IdentityUserRole<long>>().ToTable("UserRoles", "Identity");
        builder.Entity<IdentityUserClaim<long>>().ToTable("UserClaims", "Identity");
        builder.Entity<IdentityRoleClaim<long>>().ToTable("RoleClaims", "Identity");
        builder.Entity<IdentityUserLogin<long>>().ToTable("UserLogins", "Identity");
        builder.Entity<IdentityUserToken<long>>().ToTable("UserTokens", "Identity");
        builder.ApplyConfigurationsFromAssembly(typeof(RtsDbContext).Assembly);
    }
}
