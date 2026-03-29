using Cotizador.Application.Ports;
using Cotizador.Domain.Constants;
using Cotizador.Domain.Entities;
using Cotizador.Domain.Exceptions;
using Cotizador.Domain.ValueObjects;
using MongoDB.Driver;

namespace Cotizador.Infrastructure.Persistence;

public class QuoteRepository : IQuoteRepository
{
    private readonly IMongoCollection<PropertyQuote> _collection;

    public QuoteRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        _collection = database.GetCollection<PropertyQuote>(settings.QuotesCollectionName);
    }

    public async Task CreateAsync(PropertyQuote quote, CancellationToken ct = default)
    {
        await _collection.InsertOneAsync(quote, cancellationToken: ct);
    }

    public async Task<PropertyQuote?> GetByFolioNumberAsync(string folioNumber, CancellationToken ct = default)
    {
        FilterDefinition<PropertyQuote> filter = Builders<PropertyQuote>.Filter.Eq(q => q.FolioNumber, folioNumber);
        return await _collection.Find(filter).FirstOrDefaultAsync(ct);
    }

    public async Task<PropertyQuote?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct = default)
    {
        FilterDefinition<PropertyQuote> filter = Builders<PropertyQuote>.Filter.Eq("metadata.idempotencyKey", idempotencyKey);
        return await _collection.Find(filter).FirstOrDefaultAsync(ct);
    }

    public async Task UpdateGeneralInfoAsync(
        string folioNumber,
        int expectedVersion,
        InsuredData insuredData,
        ConductionData conductionData,
        string agentCode,
        string businessType,
        string riskClassification,
        CancellationToken ct = default)
    {
        FilterDefinition<PropertyQuote> filter = BuildVersionedFilter(folioNumber, expectedVersion);

        UpdateDefinition<PropertyQuote> update = Builders<PropertyQuote>.Update
            .Set(q => q.InsuredData, insuredData)
            .Set(q => q.ConductionData, conductionData)
            .Set(q => q.AgentCode, agentCode)
            .Set(q => q.BusinessType, businessType)
            .Set(q => q.RiskClassification, riskClassification)
            .Set(q => q.Version, expectedVersion + 1)
            .Set(q => q.Metadata.UpdatedAt, DateTime.UtcNow)
            .Set(q => q.Metadata.LastWizardStep, 1);

        await ExecuteUpdateAsync(folioNumber, expectedVersion, filter, update, ct);
    }

    public async Task UpdateLayoutAsync(
        string folioNumber,
        int expectedVersion,
        LayoutConfiguration layout,
        CancellationToken ct = default)
    {
        FilterDefinition<PropertyQuote> filter = BuildVersionedFilter(folioNumber, expectedVersion);

        UpdateDefinition<PropertyQuote> update = Builders<PropertyQuote>.Update
            .Set(q => q.LayoutConfiguration, layout)
            .Set(q => q.Version, expectedVersion + 1)
            .Set(q => q.Metadata.UpdatedAt, DateTime.UtcNow)
            .Set(q => q.Metadata.LastWizardStep, 2);

        await ExecuteUpdateAsync(folioNumber, expectedVersion, filter, update, ct);
    }

    public async Task UpdateLocationsAsync(
        string folioNumber,
        int expectedVersion,
        List<Location> locations,
        CancellationToken ct = default)
    {
        FilterDefinition<PropertyQuote> filter = BuildVersionedFilter(folioNumber, expectedVersion);

        UpdateDefinition<PropertyQuote> update = Builders<PropertyQuote>.Update
            .Set(q => q.Locations, locations)
            .Set(q => q.Version, expectedVersion + 1)
            .Set(q => q.Metadata.UpdatedAt, DateTime.UtcNow)
            .Set(q => q.Metadata.LastWizardStep, 2);

        await ExecuteUpdateAsync(folioNumber, expectedVersion, filter, update, ct);
    }

    public async Task PatchLocationAsync(
        string folioNumber,
        int expectedVersion,
        int locationIndex,
        Location patchData,
        CancellationToken ct = default)
    {
        FilterDefinition<PropertyQuote> filter = BuildVersionedFilter(folioNumber, expectedVersion);

        // locationIndex is 1-based; MongoDB array notation uses 0-based index
        int arrayIndex = locationIndex - 1;

        UpdateDefinition<PropertyQuote> update = Builders<PropertyQuote>.Update
            .Set($"locations.{arrayIndex}", patchData)
            .Set(q => q.Version, expectedVersion + 1)
            .Set(q => q.Metadata.UpdatedAt, DateTime.UtcNow)
            .Set(q => q.Metadata.LastWizardStep, 2);

        await ExecuteUpdateAsync(folioNumber, expectedVersion, filter, update, ct);
    }

    public async Task UpdateCoverageOptionsAsync(
        string folioNumber,
        int expectedVersion,
        CoverageOptions options,
        CancellationToken ct = default)
    {
        FilterDefinition<PropertyQuote> filter = BuildVersionedFilter(folioNumber, expectedVersion);

        UpdateDefinition<PropertyQuote> update = Builders<PropertyQuote>.Update
            .Set(q => q.CoverageOptions, options)
            .Set(q => q.Version, expectedVersion + 1)
            .Set(q => q.Metadata.UpdatedAt, DateTime.UtcNow)
            .Set(q => q.Metadata.LastWizardStep, 3);

        await ExecuteUpdateAsync(folioNumber, expectedVersion, filter, update, ct);
    }

    public async Task UpdateFinancialResultAsync(
        string folioNumber,
        int expectedVersion,
        decimal netPremium,
        decimal commercialPremium,
        List<LocationPremium> premiumsByLocation,
        CancellationToken ct = default)
    {
        FilterDefinition<PropertyQuote> filter = BuildVersionedFilter(folioNumber, expectedVersion);

        UpdateDefinition<PropertyQuote> update = Builders<PropertyQuote>.Update
            .Set(q => q.NetPremium, netPremium)
            .Set(q => q.CommercialPremium, commercialPremium)
            .Set(q => q.PremiumsByLocation, premiumsByLocation)
            .Set(q => q.QuoteStatus, QuoteStatus.Calculated)
            .Set(q => q.Version, expectedVersion + 1)
            .Set(q => q.Metadata.UpdatedAt, DateTime.UtcNow)
            .Set(q => q.Metadata.LastWizardStep, 4);

        await ExecuteUpdateAsync(folioNumber, expectedVersion, filter, update, ct);
    }

    private static FilterDefinition<PropertyQuote> BuildVersionedFilter(string folioNumber, int expectedVersion)
    {
        return Builders<PropertyQuote>.Filter.And(
            Builders<PropertyQuote>.Filter.Eq(q => q.FolioNumber, folioNumber),
            Builders<PropertyQuote>.Filter.Eq(q => q.Version, expectedVersion));
    }

    private async Task ExecuteUpdateAsync(
        string folioNumber,
        int expectedVersion,
        FilterDefinition<PropertyQuote> filter,
        UpdateDefinition<PropertyQuote> update,
        CancellationToken ct)
    {
        UpdateResult result = await _collection.UpdateOneAsync(filter, update, cancellationToken: ct);

        if (result.ModifiedCount == 0)
        {
            throw new VersionConflictException(folioNumber, expectedVersion);
        }
    }
}
