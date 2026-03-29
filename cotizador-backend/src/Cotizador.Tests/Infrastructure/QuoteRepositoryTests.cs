using Cotizador.Domain.Entities;
using Cotizador.Domain.Exceptions;
using Cotizador.Domain.ValueObjects;
using Cotizador.Infrastructure.Persistence;
using FluentAssertions;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Cotizador.Tests.Infrastructure;

public class QuoteRepositoryTests
{
    private readonly Mock<IMongoCollection<PropertyQuote>> _mockCollection = new();
    private readonly Mock<IMongoDatabase> _mockDatabase = new();
    private readonly MongoDbSettings _settings = new()
    {
        ConnectionString = "mongodb://localhost:27017",
        DatabaseName = "cotizador_test",
        QuotesCollectionName = "property_quotes"
    };

    private QuoteRepository Sut
    {
        get
        {
            _mockDatabase
                .Setup(db => db.GetCollection<PropertyQuote>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings>()))
                .Returns(_mockCollection.Object);
            return new QuoteRepository(_mockDatabase.Object, _settings);
        }
    }

    [Fact]
    public async Task CreateAsync_Should_InsertDocument_WhenQuoteIsValid()
    {
        // Arrange
        PropertyQuote quote = BuildSampleQuote();
        _mockCollection
            .Setup(c => c.InsertOneAsync(
                It.IsAny<PropertyQuote>(),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await Sut.CreateAsync(quote);

        // Assert
        _mockCollection.Verify(
            c => c.InsertOneAsync(
                It.Is<PropertyQuote>(q => q.FolioNumber == quote.FolioNumber),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateGeneralInfoAsync_Should_ThrowVersionConflictException_WhenModifiedCountIsZero()
    {
        // Arrange
        const string folioNumber = "DAN-2026-00001";
        const int expectedVersion = 3;
        InsuredData insuredData = new() { Name = "Test Company", TaxId = "TST850101ABC" };
        ConductionData conductionData = new() { SubscriberCode = "SUB-001", OfficeName = "CDMX Central" };

        UpdateResult updateResult = new UpdateResult.Acknowledged(matchedCount: 0, modifiedCount: 0, upsertedId: null);

        _mockCollection
            .Setup(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<PropertyQuote>>(),
                It.IsAny<UpdateDefinition<PropertyQuote>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updateResult);

        // Act
        Func<Task> act = async () => await Sut.UpdateGeneralInfoAsync(
            folioNumber, expectedVersion, insuredData, conductionData, "AGT-001", "commercial", "standard");

        // Assert
        await act.Should().ThrowAsync<VersionConflictException>()
            .Where(ex => ex.FolioNumber == folioNumber && ex.ExpectedVersion == expectedVersion);
    }

    [Fact]
    public async Task UpdateGeneralInfoAsync_Should_Succeed_WhenVersionMatches()
    {
        // Arrange
        const string folioNumber = "DAN-2026-00001";
        const int expectedVersion = 3;
        InsuredData insuredData = new() { Name = "Test Company", TaxId = "TST850101ABC" };
        ConductionData conductionData = new() { SubscriberCode = "SUB-001", OfficeName = "CDMX Central" };

        UpdateResult updateResult = new UpdateResult.Acknowledged(matchedCount: 1, modifiedCount: 1, upsertedId: null);

        _mockCollection
            .Setup(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<PropertyQuote>>(),
                It.IsAny<UpdateDefinition<PropertyQuote>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updateResult);

        // Act
        Func<Task> act = async () => await Sut.UpdateGeneralInfoAsync(
            folioNumber, expectedVersion, insuredData, conductionData, "AGT-001", "commercial", "standard");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UpdateFinancialResultAsync_Should_ThrowVersionConflictException_WhenModifiedCountIsZero()
    {
        // Arrange
        const string folioNumber = "DAN-2026-00001";
        const int expectedVersion = 5;

        UpdateResult updateResult = new UpdateResult.Acknowledged(matchedCount: 0, modifiedCount: 0, upsertedId: null);

        _mockCollection
            .Setup(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<PropertyQuote>>(),
                It.IsAny<UpdateDefinition<PropertyQuote>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updateResult);

        // Act
        Func<Task> act = async () => await Sut.UpdateFinancialResultAsync(
            folioNumber, expectedVersion, 1500m, 1800m, new List<LocationPremium>());

        // Assert
        await act.Should().ThrowAsync<VersionConflictException>()
            .Where(ex => ex.FolioNumber == folioNumber);
    }

    [Fact]
    public async Task UpdateFinancialResultAsync_Should_Succeed_WhenVersionMatches()
    {
        // Arrange
        const string folioNumber = "DAN-2026-00001";
        const int expectedVersion = 2;

        UpdateResult updateResult = new UpdateResult.Acknowledged(matchedCount: 1, modifiedCount: 1, upsertedId: null);

        _mockCollection
            .Setup(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<PropertyQuote>>(),
                It.IsAny<UpdateDefinition<PropertyQuote>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updateResult);

        // Act
        Func<Task> act = async () => await Sut.UpdateFinancialResultAsync(
            folioNumber, expectedVersion, 5000m, 6000m, new List<LocationPremium>());

        // Assert
        await act.Should().NotThrowAsync();
    }

    private static PropertyQuote BuildSampleQuote() => new()
    {
        FolioNumber = "DAN-2026-00001",
        QuoteStatus = "draft",
        AgentCode = "AGT-001",
        RiskClassification = "standard",
        BusinessType = "commercial",
        Version = 1,
        InsuredData = new InsuredData { Name = "Distribuidora del Norte S.A.", TaxId = "DNO850101ABC" },
        ConductionData = new ConductionData { SubscriberCode = "SUB-001", OfficeName = "CDMX Central" },
        Metadata = new QuoteMetadata
        {
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "AGT-001",
            IdempotencyKey = Guid.NewGuid().ToString()
        }
    };
}
