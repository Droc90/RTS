using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RTS.Domain.TradingModels;
using RTS.Domain.TradingModels.Indicators;
using RTS.Domain.TradingModels.Timeframes;
using RTS.Infrastructure.Auditing;
using RTS.Infrastructure.Identity;
using RTS.Infrastructure.Profiles;
using RTS.Domain.TradingModels.Criteria;
using RTS.Domain.ScreeningStrategies;
using RTS.Infrastructure.CandidateDiscovery;

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
    public DbSet<LoginHistory> LoginHistory =>
        Set<LoginHistory>();

    public DbSet<AuditLog> AuditLogs =>
        Set<AuditLog>();

    public DbSet<UserProfile> UserProfiles =>
        Set<UserProfile>();

    public DbSet<ApplicationError> ApplicationErrors =>
        Set<ApplicationError>();

    public DbSet<TradingModel> TradingModels =>
        Set<TradingModel>();

    public DbSet<TradingModelVersion> TradingModelVersions =>
        Set<TradingModelVersion>();

    public DbSet<TradingModelTimeframe> TradingModelTimeframes =>
        Set<TradingModelTimeframe>();

    public DbSet<TradingModelCriterion> TradingModelCriteria =>
    Set<TradingModelCriterion>();

    public DbSet<TradingModelIndicator> TradingModelIndicators =>
        Set<TradingModelIndicator>();

    public DbSet<TradingModelIndicatorParameter>
        TradingModelIndicatorParameters =>
            Set<TradingModelIndicatorParameter>();

    public DbSet<ScreeningStrategy> ScreeningStrategies =>
        Set<ScreeningStrategy>();

    public DbSet<ScreeningStrategyVersion> ScreeningStrategyVersions =>
        Set<ScreeningStrategyVersion>();

    public DbSet<ScreeningRule> ScreeningRules =>
        Set<ScreeningRule>();

    public DbSet<DiscoveryRun> DiscoveryRuns => Set<DiscoveryRun>();

    public DbSet<DiscoveryCandidate> DiscoveryCandidates => Set<DiscoveryCandidate>();

    public RtsDbContext(
        DbContextOptions<RtsDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(
        ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>()
            .ToTable("Users", "Identity");

        builder.Entity<ApplicationRole>()
            .ToTable("Roles", "Identity");

        builder.Entity<IdentityUserRole<long>>()
            .ToTable("UserRoles", "Identity");

        builder.Entity<IdentityUserClaim<long>>()
            .ToTable("UserClaims", "Identity");

        builder.Entity<IdentityRoleClaim<long>>()
            .ToTable("RoleClaims", "Identity");

        builder.Entity<IdentityUserLogin<long>>()
            .ToTable("UserLogins", "Identity");

        builder.Entity<IdentityUserToken<long>>()
            .ToTable("UserTokens", "Identity");

        builder.ApplyConfigurationsFromAssembly(
            typeof(RtsDbContext).Assembly);
    }
}
