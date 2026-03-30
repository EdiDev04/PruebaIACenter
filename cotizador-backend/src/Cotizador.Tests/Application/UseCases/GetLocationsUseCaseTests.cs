using Cotizador.Application.DTOs;
using Cotizador.Application.Ports;
using Cotizador.Application.UseCases;
using Cotizador.Domain.Constants;
using Cotizador.Domain.Entities;
using Cotizador.Domain.Exceptions;
using Cotizador.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cotizador.Tests.Application.UseCases;

public class GetLocationsUseCaseTests
{
    private readonly Mock<IQuoteRepository> _mockRepository = new();
    private readonly Mock<ILogger<GetLocationsUseCase>> _mockLogger = new();

    private GetLocationsUseCase Sut => new(_mockRepository.Object, _mockLogger.Object);

    // ─── Builders ─────────────────────────────────────────────────────────────

    private static PropertyQuote BuildPropertyQuote(string folioNumber, int version, List<Location> locations) =>
        new()
        {
            FolioNumber = folioNumber,
            Version = version,
            Locations = locations
        };

    private static Location BuildLocation(int index, string validationStatus = ValidationStatus.Calculable) =>
        new()
        {
            Index = index,
            LocationName = $"Ubicación {index}",
            Address = "Av. Test 100",
            ZipCode = "06600",
            BusinessLine = new BusinessLine { Description = "Storage", FireKey = "B-03" },
            Guarantees = new List<LocationGuarantee>
            {
                new() { GuaranteeKey = GuaranteeKeys.BuildingFire, InsuredAmount = 1_000_000m }
            },
            CatZone = "A",
            ValidationStatus = validationStatus,
            BlockingAlerts = new List<string>()
        };

    // ─── Happy Paths ───────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_FolioWithLocations_ReturnsAllLocationDtos()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00001";
        var quote = BuildPropertyQuote(folioNumber, version: 3,
            locations: new List<Location> { BuildLocation(1), BuildLocation(2) });

        _mockRepository
            .Setup(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        // Act
        var result = await Sut.ExecuteAsync(folioNumber);

        // Assert
        result.Should().NotBeNull();
        result.Locations.Should().HaveCount(2);
        result.Version.Should().Be(3);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_EmptyLocations_ReturnsEmptyList()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00002";
        var quote = BuildPropertyQuote(folioNumber, version: 1, locations: new List<Location>());

        _mockRepository
            .Setup(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        // Act
        var result = await Sut.ExecuteAsync(folioNumber);

        // Assert
        result.Locations.Should().BeEmpty();
        result.Version.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_FolioWithLocations_ReturnsCorrectVersion()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00003";
        const int expectedVersion = 99;
        var quote = BuildPropertyQuote(folioNumber, version: expectedVersion,
            locations: new List<Location> { BuildLocation(1) });

        _mockRepository
            .Setup(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        // Act
        var result = await Sut.ExecuteAsync(folioNumber);

        // Assert
        result.Version.Should().Be(expectedVersion);
    }

    [Fact]
    public async Task ExecuteAsync_FolioWithLocations_MapsAllLocationFields()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00004";
        var location = new Location
        {
            Index = 2,
            LocationName = "Bodega Norte",
            Address = "Av. Norte 500",
            ZipCode = "06600",
            State = "Ciudad de México",
            Municipality = "Cuauhtémoc",
            Neighborhood = "Centro",
            City = "CDMX",
            ConstructionType = "Tipo 2",
            Level = 5,
            ConstructionYear = 2005,
            CatZone = "B",
            ValidationStatus = ValidationStatus.Calculable,
            BlockingAlerts = new List<string>(),
            BusinessLine = new BusinessLine { Description = "Office", FireKey = "A-01" },
            Guarantees = new List<LocationGuarantee>
            {
                new() { GuaranteeKey = GuaranteeKeys.BuildingFire, InsuredAmount = 2_000_000m }
            }
        };
        var quote = BuildPropertyQuote(folioNumber, version: 4, locations: new List<Location> { location });

        _mockRepository
            .Setup(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        // Act
        var result = await Sut.ExecuteAsync(folioNumber);

        // Assert
        var dto = result.Locations[0];
        dto.Index.Should().Be(2);
        dto.LocationName.Should().Be("Bodega Norte");
        dto.ZipCode.Should().Be("06600");
        dto.State.Should().Be("Ciudad de México");
        dto.CatZone.Should().Be("B");
        dto.ValidationStatus.Should().Be(ValidationStatus.Calculable);
    }

    // ─── Error Paths ───────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_FolioNotFound_ThrowsFolioNotFoundException()
    {
        // Arrange
        const string folioNumber = "DAN-2024-99999";

        _mockRepository
            .Setup(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyQuote?)null);

        // Act
        Func<Task> act = async () => await Sut.ExecuteAsync(folioNumber);

        // Assert
        await act.Should().ThrowAsync<FolioNotFoundException>();
    }
}
