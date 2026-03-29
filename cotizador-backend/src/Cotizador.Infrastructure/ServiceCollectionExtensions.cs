using Cotizador.Application.Ports;
using Cotizador.Infrastructure.ExternalServices;
using Cotizador.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cotizador.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddExternalServices(configuration);
        return services;
    }

    private static IServiceCollection AddExternalServices(this IServiceCollection services, IConfiguration configuration)
    {
        CoreOhsSettings coreOhsSettings = new();
        configuration.GetSection("CoreOhs").Bind(coreOhsSettings);
        services.AddSingleton(coreOhsSettings);

        services.AddHttpClient<ICoreOhsClient, CoreOhsClient>(client =>
        {
            client.BaseAddress = new Uri(coreOhsSettings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        return services;
    }
}
