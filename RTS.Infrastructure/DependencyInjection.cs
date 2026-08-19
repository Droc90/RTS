using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RTS.Infrastructure.Persistence;
using RTS.Application.Identity;
using RTS.Infrastructure.Identity;
using RTS.Application.Administration;
using RTS.Infrastructure.Administration;
using RTS.Application.Auditing;
using RTS.Infrastructure.Auditing;
using RTS.Application.Profiles;
using RTS.Infrastructure.Profiles;
using RTS.Application.CandidateDiscovery;
using RTS.Infrastructure.CandidateDiscovery;
using RTS.Application.EvaluationJobs;
using RTS.Application.MarketData;
using RTS.Infrastructure.EvaluationJobs;

namespace RTS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<RtsDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddMemoryCache();

        services
            .AddOptions<AccountRequestLimitOptions>()
            .Bind(
                configuration.GetSection(
                    AccountRequestLimitOptions.SectionName))
            .Validate(
                options => options.PermitLimit > 0,
                "Account request permit limit must be greater than zero.")
            .Validate(
                options => options.WindowMinutes > 0,
                "Account request window must be greater than zero.")
            .ValidateOnStart();

        services.AddSingleton<
            IAccountRequestLimiter,
            MemoryAccountRequestLimiter>();

        services.AddScoped<
            IUserRegistrationService,
            UserRegistrationService>();

        services.AddScoped<
            IEmailConfirmationService,
            EmailConfirmationService>();

        services.AddScoped<
            IPasswordRecoveryService,
            PasswordRecoveryService>();

        services.AddScoped<
            IRoleInitializationService,
            RoleInitializationService>();

        services.AddScoped<
            IInitialAdministratorService,
            InitialAdministratorService>();

        services.AddScoped<
            IUserAdministrationService,
            UserAdministrationService>();

        services.AddScoped<
            ILoginHistoryService,
            LoginHistoryService>();

        services.AddScoped<
            IAuditService,
            AuditService>();

        services.AddScoped<
            IApplicationErrorService,
            ApplicationErrorService>();

        services.AddScoped<
            IUserProfileService,
            UserProfileService>();

        services.AddScoped<
            IApplicationErrorAdministrationService,
            ApplicationErrorAdministrationService>();

        services.AddScoped<ICandidateInboxService, CandidateInboxService>();
        services.AddScoped<IEvaluationJobService, EvaluationJobService>();
        services.AddScoped<IMarketDataSnapshotStore, MarketDataSnapshotStore>();

        return services;
    }
}
