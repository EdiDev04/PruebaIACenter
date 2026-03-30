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

public class GetLocationsSummaryUseCaseTests
{
    private readonly Mock<IQuoteRepository> _mockRepository = new();
    private readonly Mock<ILogger<GetLocationsSummaryUseCase>> _mockLogger = new();

    private GetLocationsSummaryUseCase Sut => new(_mockRepository.Object, _mockLogger.Object);

    // ─── Builders ─────────────────────────────────────────────────────────────

    private static PropertyQuote BuildPropertyQuote(string folioNumber, int version, List<Location> locations) =>
        new()
        {
            FolioNumber = folioNumber,
            Version = version,
            Locations = locations
        };

    private static Location BuildLocation(int index, string validationStatus, List<string>? alerts = null) =>
        new()
        {
            Index = index,
            LocationName = $"Ubicación {index}",
            Address = "Av. Test 100",
            ZipCode = validationStatus == ValidationStatus.Calculable ? "06600" : string.Empty,
            BusinessLine = new BusinessLine { FireKey = validationStatus == ValidationStatus.Calculable ? "B-03" : string.Empty },
            Guarantees = new List<LocationGuarantee>(),
            ValidationStatus = validationStatus,
            BlockingAlerts = alerts ?? new List<string>()
        };

    // ─── Happy Paths ───────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_MixedValidationStatuses_ReturnsCorrectTotals()
    {
        // Arrange — 2 calculable, 1 incomplete
        const string folioNumber = "DAN-2024-00001";
        var quote = BuildPropertyQuote(folioNumber, version: 4, locations: new List<Location>
        {
            BuildLocation(1, ValidationStatus.Calculable),
            BuildLocation(2, ValidationStatus.Calculable),
            BuildLocation(3, ValidationStatus.Incomplete, new List<string> { "Código postal requerido" })
        });

        _mockRepository
            .Setup(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        // Act
        var result = await Sut.ExecuteAsync(folioNumber);

        // Assert
        result.TotalCalculable.Should().Be(2);
        result.TotalIncomplete.Should().Be(1);
        result.Locations.Should().HaveCount(3);
        result.Version.Should().Be(4);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_NoLocations_ReturnsZeroCounts()
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
        result.TotalCalculable.Should().Be(0);
        result.TotalIncomplete.Should().Be(0);
        result.Locations.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_AllCalculable_ReturnsTotalCalculableEqualToCount()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00003";
        var quote = BuildPropertyQuote(folioNumber, version: 2, locations: new List<Location>
        {
            BuildLocation(1, ValidationStatus.Calculable),
            BuildLocation(2, ValidationStatus.Calculable),
            BuildLocation(3, ValidationStatus.Calculable)
        });

        _mockRepository
            .Setup(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        // Act
        var result = await Sut.ExecuteAsync(folioNumber);

        // Assert
        result.TotalCalculable.Should().Be(3);
        result.TotalIncomplete.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_AllIncomplete_ReturnsTotalIncompleteEqualToCount()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00004";
        var quote = BuildPropertyQuote(folioNumber, version: 1, locations: new List<Location>
        {
            BuildLocation(1, ValidationStatus.Incomplete, new List<string> { "Código postal requerido" }),
            BuildLocation(2, ValidationStatus.Incomplete, new List<string> { "Giro comercial requerido" })
        });

        _mockRepository
            .Setup(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        // Act
        var result = await Sut.ExecuteAsync(folioNumber);

        // Assert
        result.TotalCalculable.Should().Be(0);
        result.TotalIncomplete.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_IncompleteLocation_ReturnsBlockingAlertsInSummary()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00005";
        var alerts = new List<string> { "Código postal requerido", "Giro comercial requerido" };
        var quote = BuildPropertyQuote(folioNumber, version: 1, locations: new List<Location>
        {
            BuildLocation(1, ValidationStatus.Incomplete, alerts)
        });

        _mockRepository
            .Setup(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        // Act
        var result = await Sut.ExecuteAsync(folioNumber);

        // Assert
        result.Locations[0].BlockingAlerts.Should().HaveCount(2);
        result.Locations[0].BlockingAlerts.Should().Contain("Código postal requerido");
        result.Locations[0].BlockingAlerts.Should().Contain("Giro comercial requerido");
    }

    [Fact]
    public async Task ExecuteAsync_FolioWithLocations_MapsLocationNameAndIndexInSummary()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00006";
        var location = new Location
        {
            Index = 5,
            LocationName = "Sucursal Norte",
            ValidationStatus = ValidationStatus.Calculable,
            BlockingAlerts = new List<string>()
        };
        var quote = BuildPropertyQuote(folioNumber, version: 2, locations: new List<Location> { location });

        _mockRepository
            .Setup(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        // Act
        var result = await Sut.ExecuteAsync(folioNumber);

        // Assert
        result.Locations[0].Index.Should().Be(5);
        result.Locations[0].LocationName.Should().Be("Sucursal Norte");
        result.Locations[0].ValidationStatus.Should().Be(ValidationStatus.Calculable);
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
