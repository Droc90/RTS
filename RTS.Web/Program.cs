using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Radzen;
using RTS.Application.Identity;
using RTS.Infrastructure;
using RTS.Infrastructure.Identity;
using RTS.Infrastructure.Persistence;
using RTS.Web.Components;
using RTS.Web.Components.Account;
using RTS.Web.Configuration;
using RTS.Web.Endpoints;
using RTS.Web.Identity;
using RTS.Application.Email;
using RTS.Infrastructure.Email;
using RTS.Web.Middleware;
using RTS.Web.Authorization;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using RTS.Web.Health;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<
    IValidateOptions<ApplicationOptions>,
    ApplicationOptionsValidator>();

builder.Services
    .AddOptions<ApplicationOptions>()
    .Bind(builder.Configuration.GetSection(
        ApplicationOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var applicationSection = builder.Configuration.GetSection(
    ApplicationOptions.SectionName);

var applicationOptions =
    applicationSection.Get<ApplicationOptions>()
    ?? throw new InvalidOperationException(
        "Application configuration is missing.");

if (string.IsNullOrWhiteSpace(
    applicationOptions.DataProtectionName))
{
    throw new InvalidOperationException(
        "Data-protection application name is not configured.");
}

var dataProtectionBuilder = builder.Services
    .AddDataProtection()
    .SetApplicationName(
        applicationOptions.DataProtectionName);

if (!string.IsNullOrWhiteSpace(
    applicationOptions.DataProtectionKeysPath))
{
    dataProtectionBuilder.PersistKeysToFileSystem(
        new DirectoryInfo(
            applicationOptions.DataProtectionKeysPath));
}


if (!builder.Environment.IsDevelopment())
{
    if (!string.IsNullOrWhiteSpace(
        applicationOptions.DataProtectionCertificatePath))
    {
        if (!File.Exists(
            applicationOptions.DataProtectionCertificatePath))
        {
            throw new InvalidOperationException(
                "The configured Data Protection certificate file was not found.");
        }

        var dataProtectionCertificate =
            X509CertificateLoader.LoadPkcs12FromFile(
                applicationOptions.DataProtectionCertificatePath,
                applicationOptions
                    .DataProtectionCertificatePassword);

        dataProtectionBuilder.ProtectKeysWithCertificate(
            dataProtectionCertificate);
    }
    else if (OperatingSystem.IsWindows())
    {
        dataProtectionBuilder.ProtectKeysWithDpapi();
    }
}
builder.Services
    .AddOptions<SecurityOptions>()
    .Bind(builder.Configuration.GetSection(
        SecurityOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var securityOptions = builder.Configuration
    .GetSection(SecurityOptions.SectionName)
    .Get<SecurityOptions>()
    ?? new SecurityOptions();

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(
        securityOptions.HstsMaxAgeDays);

    options.IncludeSubDomains =
        securityOptions.HstsIncludeSubDomains;

    options.Preload =
        securityOptions.HstsPreload;
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddRadzenComponents();
builder.Services.AddRadzenCookieThemeService(options =>
{
    options.Name = "RTS.Theme";
    options.Duration = TimeSpan.FromDays(365);
});

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services
    .AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy(),
        tags: ["live"])
    .AddCheck<DatabaseHealthCheck>(
        "database",
        tags: ["ready"]);

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<
        IEmailSender,
        DevelopmentEmailSender>();
}
else
{
    builder.Services.AddSingleton<
        IValidateOptions<SmtpEmailOptions>,
        SmtpEmailOptionsValidator>();

    builder.Services
        .AddOptions<SmtpEmailOptions>()
        .Bind(builder.Configuration.GetSection(
            SmtpEmailOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services.AddScoped<
        IEmailSender,
        SmtpEmailSender>();
}

builder.Services.AddIdentityCore<ApplicationUser>()
    .AddRoles<ApplicationRole>()
    .AddSignInManager()
    .AddDefaultTokenProviders()
    .AddEntityFrameworkStores<RtsDbContext>()
    .AddClaimsPrincipalFactory<
        ApplicationUserClaimsPrincipalFactory>();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.User.RequireUniqueEmail = true;

    options.Password.RequiredLength = 10;
    options.Password.RequiredUniqueChars = 4;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;

    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

    options.SignIn.RequireConfirmedEmail = true;
});

builder.Services.Configure<SecurityStampValidatorOptions>(
    options =>
    {
        options.ValidationInterval =
            TimeSpan.FromMinutes(5);
    });

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
        IdentityConstants.ApplicationScheme;

    options.DefaultChallengeScheme =
        IdentityConstants.ApplicationScheme;

    options.DefaultSignInScheme =
        IdentityConstants.ExternalScheme;
})
    .AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options =>
{
    var cookieName = builder.Configuration[
        $"{ApplicationOptions.SectionName}:CookieName"];

    if (string.IsNullOrWhiteSpace(cookieName))
    {
        throw new InvalidOperationException(
            "Application cookie name is not configured.");
    }

    options.Cookie.Name = cookieName;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

    options.LoginPath = "/account/login";
    options.AccessDeniedPath = "/account/access-denied";

    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

builder.Services.AddAuthorization(
    options => options.AddApplicationPolicies());

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<
    AuthenticationStateProvider,
    AuthenticationStateValidator>();

var app = builder.Build();

_ = app.Services
    .GetRequiredService<
        IOptions<ApplicationOptions>>()
    .Value;

_ = app.Services
    .GetRequiredService<
        IOptions<SecurityOptions>>()
    .Value;

if (!app.Environment.IsDevelopment())
{
    _ = app.Services
        .GetRequiredService<
            IOptions<SmtpEmailOptions>>()
        .Value;
}

ProductionServiceValidator.Validate(
    app.Environment,
    app.Services);

using (var scope = app.Services.CreateScope())
{
    var roleInitializer = scope.ServiceProvider
        .GetRequiredService<IRoleInitializationService>();

    await roleInitializer.InitializeAsync();

    var initialAdministratorEmail =
        app.Configuration["InitialAdministrator:Email"];

    if (!string.IsNullOrWhiteSpace(
        initialAdministratorEmail))
    {
        var administratorService = scope.ServiceProvider
            .GetRequiredService<
                IInitialAdministratorService>();

        await administratorService.PromoteAsync(
            initialAdministratorEmail);
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseMiddleware<
    ApplicationErrorLoggingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = registration =>
            registration.Tags.Contains("live"),
        ResponseWriter =
            HealthCheckResponseWriter.WriteAsync
    })
    .AllowAnonymous();

app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = registration =>
            registration.Tags.Contains("ready"),
        ResponseWriter =
            HealthCheckResponseWriter.WriteAsync
    })
    .AllowAnonymous();

app.MapAccountEndpoints();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

//if (app.Environment.IsDevelopment())
//{
//    app.MapGet(
//        "/development/test-error",
//        static IResult () =>
//            throw new InvalidOperationException(
//                "Application error logging test."));
//}

app.Run();
