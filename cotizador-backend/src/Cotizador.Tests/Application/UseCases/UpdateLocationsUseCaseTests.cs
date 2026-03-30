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

public class UpdateLocationsUseCaseTests
{
    private readonly Mock<IQuoteRepository> _mockRepository = new();
    private readonly Mock<ILogger<UpdateLocationsUseCase>> _mockLogger = new();

    private UpdateLocationsUseCase Sut => new(_mockRepository.Object, _mockLogger.Object);

    // ─── Builders ─────────────────────────────────────────────────────────────

    private static PropertyQuote BuildPropertyQuote(string folioNumber, int version = 1) =>
        new()
        {
            FolioNumber = folioNumber,
            Version = version,
            Locations = new List<Location>()
        };

    private static PropertyQuote BuildPropertyQuote(string folioNumber, int version, List<Location> locations) =>
        new()
        {
            FolioNumber = folioNumber,
            Version = version,
            Locations = locations
        };

    private static LocationDto BuildCalculableLocationDto(int index = 1) =>
        new(
            Index: index,
            LocationName: "Bodega Principal",
            Address: "Av. Industria 340",
            ZipCode: "06600",
            State: "Ciudad de México",
            Municipality: "Cuauhtémoc",
            Neighborhood: "Doctores",
            City: "Ciudad de México",
            ConstructionType: "Tipo 1 - Macizo",
            Level: 2,
            ConstructionYear: 1998,
            LocationBusinessLine: new BusinessLineDto("BL-001", "Storage warehouse", "B-03", "bajo"),
            Guarantees: new List<LocationGuaranteeDto>
            {
                new(GuaranteeKeys.BuildingFire, 5_000_000m)
            },
            CatZone: "A",
            BlockingAlerts: new List<string>(),
            ValidationStatus: ValidationStatus.Incomplete);

    private static LocationDto BuildLocationDtoNoZipCode(int index = 1) =>
        new(
            Index: index,
            LocationName: "Ubicación Sin CP",
            Address: "Calle Ficticia 123",
            ZipCode: string.Empty,
            State: string.Empty, Municipality: string.Empty,
            Neighborhood: string.Empty, City: string.Empty,
            ConstructionType: string.Empty, Level: 0, ConstructionYear: 0,
            LocationBusinessLine: new BusinessLineDto("BL-001", "Storage warehouse", "B-03", "bajo"),
            Guarantees: new List<LocationGuaranteeDto>
            {
                new(GuaranteeKeys.BuildingFire, 5_000_000m)
            },
            CatZone: string.Empty,
            BlockingAlerts: new List<string>(),
            ValidationStatus: ValidationStatus.Incomplete);

    private void SetupRepositorySequence(string folioNumber, int requestVersion,
        PropertyQuote firstReturn, PropertyQuote secondReturn)
    {
        _mockRepository
            .SetupSequence(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstReturn)
            .ReturnsAsync(secondReturn);

        _mockRepository
            .Setup(r => r.UpdateLocationsAsync(folioNumber, requestVersion, It.IsAny<List<Location>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    // ─── Happy Paths ───────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_AllLocationsCalculable_ReturnsLocationsResponseWithCalculableStatus()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00001";
        var request = new UpdateLocationsRequest(
            Locations: new List<LocationDto> { BuildCalculableLocationDto() },
            Version: 2);

        var existingQuote = BuildPropertyQuote(folioNumber, version: 2);
        var updatedLocationEntity = new Location
        {
            Index = 1,
            LocationName = "Bodega Principal",
            ZipCode = "06600",
            BusinessLine = new BusinessLine { Description = "Storage warehouse", FireKey = "B-03" },
            Guarantees = new List<LocationGuarantee>
            {
                new() { GuaranteeKey = GuaranteeKeys.BuildingFire, InsuredAmount = 5_000_000m }
            },
            ValidationStatus = ValidationStatus.Calculable,
            BlockingAlerts = new List<string>()
        };
        var updatedQuote = BuildPropertyQuote(folioNumber, version: 3, locations: new List<Location> { updatedLocationEntity });

        SetupRepositorySequence(folioNumber, 2, existingQuote, updatedQuote);

        // Act
        var result = await Sut.ExecuteAsync(folioNumber, request);

        // Assert
        result.Should().NotBeNull();
        result.Locations.Should().HaveCount(1);
        result.Locations[0].ValidationStatus.Should().Be(ValidationStatus.Calculable);
        result.Version.Should().Be(3);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_LocationWithoutZipCode_PersistsWithIncompleteStatusAndAlert()
    {
        // Arrange — evaluator sets Incomplete + alert when ZipCode is missing/invalid
        const string folioNumber = "DAN-2024-00002";
        var request = new UpdateLocationsRequest(
            Locations: new List<LocationDto> { BuildLocationDtoNoZipCode() },
            Version: 1);

        var existingQuote = BuildPropertyQuote(folioNumber, version: 1);
        var updatedQuote = BuildPropertyQuote(folioNumber, version: 2, locations: new List<Location>
        {
            new()
            {
                Index = 1, ValidationStatus = ValidationStatus.Incomplete,
                BlockingAlerts = new List<string> { "Código postal requerido" }
            }
        });

        List<Location>? capturedLocations = null;
        _mockRepository
            .SetupSequence(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingQuote)
            .ReturnsAsync(updatedQuote);
        _mockRepository
            .Setup(r => r.UpdateLocationsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<List<Location>>(), It.IsAny<CancellationToken>()))
            .Callback<string, int, List<Location>, CancellationToken>((_, _, locs, _) => capturedLocations = locs)
            .Returns(Task.CompletedTask);

        // Act
        await Sut.ExecuteAsync(folioNumber, request);

        // Assert
        capturedLocations.Should().NotBeNull();
        capturedLocations![0].ValidationStatus.Should().Be(ValidationStatus.Incomplete);
        capturedLocations[0].BlockingAlerts.Should().Contain("Código postal requerido");
    }

    [Fact]
    public async Task ExecuteAsync_LocationWithoutBusinessLine_PersistsWithIncompleteStatusAndAlert()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00003";
        var locationDto = new LocationDto(
            Index: 1, LocationName: "Oficina Sin Giro", Address: "Calle Real 456",
            ZipCode: "06600", State: string.Empty, Municipality: string.Empty,
            Neighborhood: string.Empty, City: string.Empty, ConstructionType: string.Empty,
            Level: 0, ConstructionYear: 0,
            LocationBusinessLine: null, // businessLine missing
            Guarantees: new List<LocationGuaranteeDto> { new(GuaranteeKeys.BuildingFire, 3_000_000m) },
            CatZone: string.Empty, BlockingAlerts: new List<string>(), ValidationStatus: ValidationStatus.Incomplete);

        var request = new UpdateLocationsRequest(Locations: new List<LocationDto> { locationDto }, Version: 1);
        var existingQuote = BuildPropertyQuote(folioNumber, version: 1);
        var updatedQuote = BuildPropertyQuote(folioNumber, version: 2, locations: new List<Location>
        {
            new() { Index = 1, ValidationStatus = ValidationStatus.Incomplete, BlockingAlerts = new List<string> { "Giro comercial requerido" } }
        });

        List<Location>? capturedLocations = null;
        _mockRepository
            .SetupSequence(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingQuote)
            .ReturnsAsync(updatedQuote);
        _mockRepository
            .Setup(r => r.UpdateLocationsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<List<Location>>(), It.IsAny<CancellationToken>()))
            .Callback<string, int, List<Location>, CancellationToken>((_, _, locs, _) => capturedLocations = locs)
            .Returns(Task.CompletedTask);

        // Act
        await Sut.ExecuteAsync(folioNumber, request);

        // Assert
        capturedLocations![0].ValidationStatus.Should().Be(ValidationStatus.Incomplete);
        capturedLocations[0].BlockingAlerts.Should().Contain("Giro comercial requerido");
    }

    [Fact]
    public async Task ExecuteAsync_LocationWithNoGuarantees_PersistsWithIncompleteStatusAndAlert()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00004";
        var locationDto = new LocationDto(
            Index: 1, LocationName: "Bodega", Address: "Calle 1",
            ZipCode: "06600", State: string.Empty, Municipality: string.Empty,
            Neighborhood: string.Empty, City: string.Empty, ConstructionType: string.Empty,
            Level: 0, ConstructionYear: 0,
            LocationBusinessLine: new BusinessLineDto("BL-001", "Storage", "B-03", "bajo"),
            Guarantees: new List<LocationGuaranteeDto>(), // no guarantees
            CatZone: string.Empty, BlockingAlerts: new List<string>(), ValidationStatus: ValidationStatus.Incomplete);

        var request = new UpdateLocationsRequest(Locations: new List<LocationDto> { locationDto }, Version: 1);
        var existingQuote = BuildPropertyQuote(folioNumber, version: 1);
        var updatedQuote = BuildPropertyQuote(folioNumber, version: 2, locations: new List<Location>
        {
            new() { Index = 1, ValidationStatus = ValidationStatus.Incomplete, BlockingAlerts = new List<string> { "Al menos una garantía es requerida" } }
        });

        List<Location>? capturedLocations = null;
        _mockRepository
            .SetupSequence(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingQuote)
            .ReturnsAsync(updatedQuote);
        _mockRepository
            .Setup(r => r.UpdateLocationsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<List<Location>>(), It.IsAny<CancellationToken>()))
            .Callback<string, int, List<Location>, CancellationToken>((_, _, locs, _) => capturedLocations = locs)
            .Returns(Task.CompletedTask);

        // Act
        await Sut.ExecuteAsync(folioNumber, request);

        // Assert
        capturedLocations![0].ValidationStatus.Should().Be(ValidationStatus.Incomplete);
        capturedLocations[0].BlockingAlerts.Should().Contain("Al menos una garantía es requerida");
    }

    [Fact]
    public async Task ExecuteAsync_GuaranteeRequiringInsuredAmountButZero_PersistsWithIncompleteStatus()
    {
        // Arrange — building_fire requires insuredAmount > 0 (RN-006-14)
        const string folioNumber = "DAN-2024-00005";
        var locationDto = new LocationDto(
            Index: 1, LocationName: "Bodega", Address: "Av. Principal 1",
            ZipCode: "06600", State: "CDMX", Municipality: "Cuauhtémoc",
            Neighborhood: "Centro", City: "CDMX", ConstructionType: "Tipo 1",
            Level: 1, ConstructionYear: 2000,
            LocationBusinessLine: new BusinessLineDto("BL-001", "Storage", "B-03", "bajo"),
            Guarantees: new List<LocationGuaranteeDto>
            {
                new(GuaranteeKeys.BuildingFire, 0m) // 0 is invalid for building_fire
            },
            CatZone: "A", BlockingAlerts: new List<string>(), ValidationStatus: ValidationStatus.Incomplete);

        var request = new UpdateLocationsRequest(Locations: new List<LocationDto> { locationDto }, Version: 1);
        var existingQuote = BuildPropertyQuote(folioNumber, version: 1);
        var updatedQuote = BuildPropertyQuote(folioNumber, version: 2, locations: new List<Location>
        {
            new() { Index = 1, ValidationStatus = ValidationStatus.Incomplete, BlockingAlerts = new List<string> { $"Suma asegurada requerida para {GuaranteeKeys.BuildingFire}" } }
        });

        List<Location>? capturedLocations = null;
        _mockRepository
            .SetupSequence(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingQuote)
            .ReturnsAsync(updatedQuote);
        _mockRepository
            .Setup(r => r.UpdateLocationsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<List<Location>>(), It.IsAny<CancellationToken>()))
            .Callback<string, int, List<Location>, CancellationToken>((_, _, locs, _) => capturedLocations = locs)
            .Returns(Task.CompletedTask);

        // Act
        await Sut.ExecuteAsync(folioNumber, request);

        // Assert
        capturedLocations![0].ValidationStatus.Should().Be(ValidationStatus.Incomplete);
        capturedLocations[0].BlockingAlerts.Should().Contain(a => a.Contains(GuaranteeKeys.BuildingFire));
    }

    [Fact]
    public async Task ExecuteAsync_OptionalGuaranteeWithZeroInsuredAmount_IsStillCalculable()
    {
        // Arrange — glass (RN-006-13) does NOT require insuredAmount > 0
        const string folioNumber = "DAN-2024-00006";
        var locationDto = new LocationDto(
            Index: 1, LocationName: "Bodega", Address: "Av. Industria 340",
            ZipCode: "06600", State: "CDMX", Municipality: "Cuauhtémoc",
            Neighborhood: "Doctores", City: "CDMX", ConstructionType: "Tipo 1",
            Level: 1, ConstructionYear: 2000,
            LocationBusinessLine: new BusinessLineDto("BL-001", "Storage", "B-03", "bajo"),
            Guarantees: new List<LocationGuaranteeDto>
            {
                new(GuaranteeKeys.BuildingFire, 5_000_000m), // requires > 0, satisfied
                new(GuaranteeKeys.Glass, 0m)                 // does NOT require > 0
            },
            CatZone: "A", BlockingAlerts: new List<string>(), ValidationStatus: ValidationStatus.Incomplete);

        var request = new UpdateLocationsRequest(Locations: new List<LocationDto> { locationDto }, Version: 1);
        var existingQuote = BuildPropertyQuote(folioNumber, version: 1);
        var updatedQuote = BuildPropertyQuote(folioNumber, version: 2, locations: new List<Location>
        {
            new() { Index = 1, ValidationStatus = ValidationStatus.Calculable, BlockingAlerts = new List<string>() }
        });

        List<Location>? capturedLocations = null;
        _mockRepository
            .SetupSequence(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingQuote)
            .ReturnsAsync(updatedQuote);
        _mockRepository
            .Setup(r => r.UpdateLocationsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<List<Location>>(), It.IsAny<CancellationToken>()))
            .Callback<string, int, List<Location>, CancellationToken>((_, _, locs, _) => capturedLocations = locs)
            .Returns(Task.CompletedTask);

        // Act
        await Sut.ExecuteAsync(folioNumber, request);

        // Assert — glass with 0 must NOT generate a missing-amount alert
        capturedLocations![0].ValidationStatus.Should().Be(ValidationStatus.Calculable);
        capturedLocations[0].BlockingAlerts.Should().NotContain(a => a.Contains(GuaranteeKeys.Glass));
    }

    [Fact]
    public async Task ExecuteAsync_EmptyLocationsArray_PersistsEmptyListSuccessfully()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00007";
        var request = new UpdateLocationsRequest(Locations: new List<LocationDto>(), Version: 1);

        var existingQuote = BuildPropertyQuote(folioNumber, version: 1);
        var updatedQuote = BuildPropertyQuote(folioNumber, version: 2, locations: new List<Location>());

        List<Location>? capturedLocations = null;
        _mockRepository
            .SetupSequence(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingQuote)
            .ReturnsAsync(updatedQuote);
        _mockRepository
            .Setup(r => r.UpdateLocationsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<List<Location>>(), It.IsAny<CancellationToken>()))
            .Callback<string, int, List<Location>, CancellationToken>((_, _, locs, _) => capturedLocations = locs)
            .Returns(Task.CompletedTask);

        // Act
        var result = await Sut.ExecuteAsync(folioNumber, request);

        // Assert
        capturedLocations.Should().NotBeNull().And.BeEmpty();
        result.Locations.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_ValidRequest_CallsRepositoryWithExpectedVersion()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00008";
        const int version = 5;
        var request = new UpdateLocationsRequest(Locations: new List<LocationDto> { BuildCalculableLocationDto() }, Version: version);

        SetupRepositorySequence(folioNumber, version,
            BuildPropertyQuote(folioNumber, version),
            BuildPropertyQuote(folioNumber, version + 1));

        // Act
        await Sut.ExecuteAsync(folioNumber, request);

        // Assert
        _mockRepository.Verify(r => r.UpdateLocationsAsync(
            folioNumber, version, It.IsAny<List<Location>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_MultipleLocations_PersistsAllAtomically()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00009";
        var request = new UpdateLocationsRequest(
            Locations: new List<LocationDto>
            {
                BuildCalculableLocationDto(1),
                BuildCalculableLocationDto(2),
                BuildCalculableLocationDto(3)
            },
            Version: 1);

        var existingQuote = BuildPropertyQuote(folioNumber, version: 1);
        var updatedQuote = BuildPropertyQuote(folioNumber, version: 2, locations: new List<Location>
        {
            new() { Index = 1, ValidationStatus = ValidationStatus.Calculable },
            new() { Index = 2, ValidationStatus = ValidationStatus.Calculable },
            new() { Index = 3, ValidationStatus = ValidationStatus.Calculable }
        });

        List<Location>? capturedLocations = null;
        _mockRepository
            .SetupSequence(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingQuote)
            .ReturnsAsync(updatedQuote);
        _mockRepository
            .Setup(r => r.UpdateLocationsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<List<Location>>(), It.IsAny<CancellationToken>()))
            .Callback<string, int, List<Location>, CancellationToken>((_, _, locs, _) => capturedLocations = locs)
            .Returns(Task.CompletedTask);

        // Act
        var result = await Sut.ExecuteAsync(folioNumber, request);

        // Assert — single UpdateLocationsAsync call with all 3 locations
        _mockRepository.Verify(r => r.UpdateLocationsAsync(
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<List<Location>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        capturedLocations.Should().HaveCount(3);
        result.Locations.Should().HaveCount(3);
    }

    // ─── Error Paths ───────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_FolioNotFound_ThrowsFolioNotFoundException()
    {
        // Arrange
        const string folioNumber = "DAN-2024-99999";
        var request = new UpdateLocationsRequest(Locations: new List<LocationDto>(), Version: 1);

        _mockRepository
            .Setup(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyQuote?)null);

        // Act
        Func<Task> act = async () => await Sut.ExecuteAsync(folioNumber, request);

        // Assert
        await act.Should().ThrowAsync<FolioNotFoundException>();
        _mockRepository.Verify(r => r.UpdateLocationsAsync(
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<List<Location>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_VersionConflict_PropagatesVersionConflictException()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00010";
        const int version = 2;
        var request = new UpdateLocationsRequest(Locations: new List<LocationDto>(), Version: version);

        _mockRepository
            .Setup(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildPropertyQuote(folioNumber, version));
        _mockRepository
            .Setup(r => r.UpdateLocationsAsync(folioNumber, version, It.IsAny<List<Location>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new VersionConflictException(folioNumber, version));

        // Act
        Func<Task> act = async () => await Sut.ExecuteAsync(folioNumber, request);

        // Assert
        await act.Should().ThrowAsync<VersionConflictException>()
            .Where(ex => ex.FolioNumber == folioNumber && ex.ExpectedVersion == version);
    }
}
