using Microsoft.Extensions.DependencyInjection;
using RTS.Application.Email;

namespace RTS.Web.Configuration;

public static class ProductionServiceValidator
{
    public static void Validate(
        IHostEnvironment environment,
        IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(services);

        if (environment.IsDevelopment())
        {
            return;
        }

        var serviceProviderIsService =
            services.GetRequiredService<
                IServiceProviderIsService>();

        if (!serviceProviderIsService.IsService(
            typeof(IEmailSender)))
        {
            throw new InvalidOperationException(
                "A production IEmailSender implementation must be registered outside Development.");
        }
    }
}