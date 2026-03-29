using Cotizador.Application.Ports;
using Cotizador.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace Cotizador.Infrastructure.Persistence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        MongoDbSettings settings = new();
        configuration.GetSection("MongoDB").Bind(settings);
        services.AddSingleton(settings);

        // Register camelCase BSON convention + ignore unmapped fields (e.g. _id when no Id property exists)
        ConventionPack conventionPack = new()
        {
            new CamelCaseElementNameConvention(),
            new IgnoreExtraElementsConvention(true)
        };
        ConventionRegistry.Register("camelCase", conventionPack, _ => true);

        // Register MongoDB client + database
        MongoClient mongoClient = new(settings.ConnectionString);
        IMongoDatabase database = mongoClient.GetDatabase(settings.DatabaseName);
        services.AddSingleton<IMongoClient>(mongoClient);
        services.AddSingleton(database);

        services.AddScoped<IQuoteRepository, QuoteRepository>();
        services.AddHostedService<MongoDbIndexInitializer>();

        return services;
    }
}
