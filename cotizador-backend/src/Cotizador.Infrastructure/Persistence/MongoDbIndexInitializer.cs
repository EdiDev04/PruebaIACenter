using Cotizador.Domain.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Cotizador.Infrastructure.Persistence;

internal sealed class MongoDbIndexInitializer : BackgroundService
{
    private readonly IMongoDatabase _database;
    private readonly MongoDbSettings _settings;
    private readonly ILogger<MongoDbIndexInitializer> _logger;

    public MongoDbIndexInitializer(
        IMongoDatabase database,
        MongoDbSettings settings,
        ILogger<MongoDbIndexInitializer> logger)
    {
        _database = database;
        _settings = settings;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            IMongoCollection<PropertyQuote> collection =
                _database.GetCollection<PropertyQuote>(_settings.QuotesCollectionName);

            CreateIndexModel<PropertyQuote> folioIndex = new(
                Builders<PropertyQuote>.IndexKeys.Ascending(q => q.FolioNumber),
                new CreateIndexOptions { Unique = true, Name = "idx_folioNumber_unique" });

            CreateIndexModel<PropertyQuote> idempotencyIndex = new(
                Builders<PropertyQuote>.IndexKeys.Ascending("metadata.idempotencyKey"),
                new CreateIndexOptions { Unique = true, Sparse = true, Name = "idx_idempotencyKey_unique_sparse" });

            await collection.Indexes.CreateManyAsync(
                new[] { folioIndex, idempotencyIndex },
                cancellationToken: stoppingToken);

            _logger.LogInformation("MongoDB indexes created successfully on collection '{Collection}'", _settings.QuotesCollectionName);
        }
        catch (OperationCanceledException)
        {
            // App shutdown before indexes could be created — safe to ignore
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create MongoDB indexes on collection '{Collection}'", _settings.QuotesCollectionName);
        }
    }
}
