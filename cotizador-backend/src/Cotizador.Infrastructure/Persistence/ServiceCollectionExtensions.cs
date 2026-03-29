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

        // Register camelCase BSON convention
        ConventionPack conventionPack = new()
        {
            new CamelCaseElementNameConvention()
        };
        ConventionRegistry.Register("camelCase", conventionPack, _ => true);

        // Register MongoDB client + database
        MongoClient mongoClient = new(settings.ConnectionString);
        IMongoDatabase database = mongoClient.GetDatabase(settings.DatabaseName);
        services.AddSingleton<IMongoClient>(mongoClient);
        services.AddSingleton(database);

        // Create indexes on startup
        CreateIndexes(database, settings);

        services.AddScoped<IQuoteRepository, QuoteRepository>();

        return services;
    }

    private static void CreateIndexes(IMongoDatabase database, MongoDbSettings settings)
    {
        IMongoCollection<Domain.Entities.PropertyQuote> collection =
            database.GetCollection<Domain.Entities.PropertyQuote>(settings.QuotesCollectionName);

        // Unique index on folioNumber
        CreateIndexModel<Domain.Entities.PropertyQuote> folioIndex = new(
            Builders<Domain.Entities.PropertyQuote>.IndexKeys.Ascending(q => q.FolioNumber),
            new CreateIndexOptions { Unique = true, Name = "idx_folioNumber_unique" });

        // Unique sparse index on metadata.idempotencyKey
        CreateIndexModel<Domain.Entities.PropertyQuote> idempotencyIndex = new(
            Builders<Domain.Entities.PropertyQuote>.IndexKeys.Ascending("metadata.idempotencyKey"),
            new CreateIndexOptions { Unique = true, Sparse = true, Name = "idx_idempotencyKey_unique_sparse" });

        collection.Indexes.CreateMany(new[] { folioIndex, idempotencyIndex });
    }
}
