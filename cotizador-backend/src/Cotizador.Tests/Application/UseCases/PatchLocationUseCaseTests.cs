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

public class PatchLocationUseCaseTests
{
    private readonly Mock<IQuoteRepository> _mockRepository = new();
    private readonly Mock<ILogger<PatchLocationUseCase>> _mockLogger = new();

    private PatchLocationUseCase Sut => new(_mockRepository.Object, _mockLogger.Object);

    // ─── Builders ─────────────────────────────────────────────────────────────

    private static PropertyQuote BuildPropertyQuoteWithLocations(
        string folioNumber, int version, List<Location> locations) =>
        new()
        {
            FolioNumber = folioNumber,
            Version = version,
            Locations = locations
        };

    private static Location BuildCalculableLocation(int index = 1) =>
        new()
        {
            Index = index,
            LocationName = "Bodega Principal",
            Address = "Av. Industria 340",
            ZipCode = "06600",
            State = "Ciudad de México",
            Municipality = "Cuauhtémoc",
            Neighborhood = "Doctores",
            City = "Ciudad de México",
            ConstructionType = "Tipo 1 - Macizo",
            Level = 2,
            ConstructionYear = 1998,
            BusinessLine = new BusinessLine { Description = "Storage warehouse", FireKey = "B-03" },
            Guarantees = new List<LocationGuarantee>
            {
                new() { GuaranteeKey = GuaranteeKeys.BuildingFire, InsuredAmount = 5_000_000m }
            },
            CatZone = "A",
            ValidationStatus = ValidationStatus.Calculable,
            BlockingAlerts = new List<string>()
        };

    private static PatchLocationRequest BuildPatchRequest(
        string? locationName = null,
        string? zipCode = null,
        BusinessLineDto? businessLine = null,
        List<LocationGuaranteeDto>? guarantees = null,
        int version = 1) =>
        new(
            LocationName: locationName,
            Address: null, ZipCode: zipCode, State: null,
            Municipality: null, Neighborhood: null, City: null,
            ConstructionType: null, Level: null, ConstructionYear: null,
            LocationBusinessLine: businessLine,
            Guarantees: guarantees,
            CatZone: null, Version: version);

    // ─── Happy Paths ───────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_ValidPatch_ReturnsUpdatedSingleLocationResponse()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00001";
        const int index = 1;
        const int version = 3;
        var request = BuildPatchRequest(locationName: "Bodega Actualizada", version: version);

        var existingQuote = BuildPropertyQuoteWithLocations(folioNumber, version, new List<Location> { BuildCalculableLocation(index) });
        var updatedLocation = BuildCalculableLocation(index);
        updatedLocation.LocationName = "Bodega Actualizada";
        var updatedQuote = BuildPropertyQuoteWithLocations(folioNumber, version + 1, new List<Location> { updatedLocation });

        _mockRepository
            .SetupSequence(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingQuote)
            .ReturnsAsync(updatedQuote);
        _mockRepository
            .Setup(r => r.PatchLocationAsync(folioNumber, version, index, It.IsAny<Location>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await Sut.ExecuteAsync(folioNumber, index, request);

        // Assert
        result.Should().NotBeNull();
        result.LocationName.Should().Be("Bodega Actualizada");
        result.Index.Should().Be(index);
        result.Version.Should().Be(version + 1);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_ValidPatch_CallsPatchRepositoryOnlyForSpecifiedIndex()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00002";
        const int patchIndex = 2;
        const int version = 1;
        var request = BuildPatchRequest(locationName: "Nombre Nuevo", version: version);

        var existingQuote = BuildPropertyQuoteWithLocations(folioNumber, version,
            new List<Location> { BuildCalculableLocation(1), BuildCalculableLocation(2), BuildCalculableLocation(3) });
        var updatedLocation2 = BuildCalculableLocation(2);
        updatedLocation2.LocationName = "Nombre Nuevo";
        var updatedQuote = BuildPropertyQuoteWithLocations(folioNumber, version + 1,
            new List<Location> { BuildCalculableLocation(1), updatedLocation2, BuildCalculableLocation(3) });

        _mockRepository
            .SetupSequence(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingQuote)
            .ReturnsAsync(updatedQuote);
        _mockRepository
            .Setup(r => r.PatchLocationAsync(folioNumber, version, patchIndex, It.IsAny<Location>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await Sut.ExecuteAsync(folioNumber, patchIndex, request);

        // Assert — only the matching index is passed to repository
        _mockRepository.Verify(r => r.PatchLocationAsync(
            folioNumber, version, patchIndex, It.IsAny<Location>(), It.IsAny<CancellationToken>()),
            Times.Once);
        result.Index.Should().Be(patchIndex);
    }

    [Fact]
    public async Task ExecuteAsync_PatchWithNullFields_PreservesExistingLocationValues()
    {
        // Arrange — all optional patch fields are null, existing values must be preserved
        const string folioNumber = "DAN-2024-00003";
        const int index = 1;
        const int version = 1;
        var request = BuildPatchRequest(version: version); // all optional fields null

        var existingLocation = BuildCalculableLocation(index);
        var existingQuote = BuildPropertyQuoteWithLocations(folioNumber, version, new List<Location> { existingLocation });
        var updatedQuote = BuildPropertyQuoteWithLocations(folioNumber, version + 1,
            new List<Location> { BuildCalculableLocation(index) });

        Location? capturedPatchData = null;
        _mockRepository
            .SetupSequence(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingQuote)
            .ReturnsAsync(updatedQuote);
        _mockRepository
            .Setup(r => r.PatchLocationAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Location>(), It.IsAny<CancellationToken>()))
            .Callback<string, int, int, Location, CancellationToken>((_, _, _, loc, _) => capturedPatchData = loc)
            .Returns(Task.CompletedTask);

        // Act
        await Sut.ExecuteAsync(folioNumber, index, request);

        // Assert
        capturedPatchData!.LocationName.Should().Be(existingLocation.LocationName);
        capturedPatchData.Address.Should().Be(existingLocation.Address);
        capturedPatchData.ZipCode.Should().Be(existingLocation.ZipCode);
        capturedPatchData.BusinessLine.FireKey.Should().Be(existingLocation.BusinessLine.FireKey);
    }

    [Fact]
    public async Task ExecuteAsync_PatchClearingZipCode_RecalculatesValidationStatusToIncomplete()
    {
        // Arrange — patch clears zipCode; evaluator must set Incomplete
        const string folioNumber = "DAN-2024-00004";
        const int index = 1;
        const int version = 2;
        var request = BuildPatchRequest(zipCode: string.Empty, version: version);

        var existingLocation = BuildCalculableLocation(index); // currently calculable
        var existingQuote = BuildPropertyQuoteWithLocations(folioNumber, version, new List<Location> { existingLocation });

        var patchedLocation = BuildCalculableLocation(index);
        patchedLocation.ZipCode = string.Empty;
        patchedLocation.ValidationStatus = ValidationStatus.Incomplete;
        patchedLocation.BlockingAlerts = new List<string> { "Código postal requerido" };
        var updatedQuote = BuildPropertyQuoteWithLocations(folioNumber, version + 1, new List<Location> { patchedLocation });

        Location? capturedPatchData = null;
        _mockRepository
            .SetupSequence(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingQuote)
            .ReturnsAsync(updatedQuote);
        _mockRepository
            .Setup(r => r.PatchLocationAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Location>(), It.IsAny<CancellationToken>()))
            .Callback<string, int, int, Location, CancellationToken>((_, _, _, loc, _) => capturedPatchData = loc)
            .Returns(Task.CompletedTask);

        // Act
        await Sut.ExecuteAsync(folioNumber, index, request);

        // Assert — evaluator flags the location as Incomplete after zip is cleared
        capturedPatchData!.ValidationStatus.Should().Be(ValidationStatus.Incomplete);
        capturedPatchData.BlockingAlerts.Should().Contain("Código postal requerido");
    }

    [Fact]
    public async Task ExecuteAsync_ValidPatch_CallsRepositoryWithExpectedVersion()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00005";
        const int index = 1;
        const int version = 7;
        var request = BuildPatchRequest(locationName: "Test", version: version);

        var existingQuote = BuildPropertyQuoteWithLocations(folioNumber, version, new List<Location> { BuildCalculableLocation(index) });
        var updatedQuote = BuildPropertyQuoteWithLocations(folioNumber, version + 1, new List<Location> { BuildCalculableLocation(index) });

        _mockRepository
            .SetupSequence(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingQuote)
            .ReturnsAsync(updatedQuote);
        _mockRepository
            .Setup(r => r.PatchLocationAsync(folioNumber, version, index, It.IsAny<Location>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await Sut.ExecuteAsync(folioNumber, index, request);

        // Assert
        _mockRepository.Verify(r => r.PatchLocationAsync(
            folioNumber, version, index, It.IsAny<Location>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ─── Error Paths ───────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_FolioNotFound_ThrowsFolioNotFoundException()
    {
        // Arrange
        const string folioNumber = "DAN-2024-99999";
        var request = BuildPatchRequest(version: 1);

        _mockRepository
            .Setup(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyQuote?)null);

        // Act
        Func<Task> act = async () => await Sut.ExecuteAsync(folioNumber, 1, request);

        // Assert
        await act.Should().ThrowAsync<FolioNotFoundException>();
        _mockRepository.Verify(r => r.PatchLocationAsync(
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Location>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_LocationIndexNotFound_ThrowsFolioNotFoundException()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00006";
        const int nonExistentIndex = 99;
        var request = BuildPatchRequest(version: 1);

        var existingQuote = BuildPropertyQuoteWithLocations(folioNumber, 1,
            new List<Location> { BuildCalculableLocation(1), BuildCalculableLocation(2) });

        _mockRepository
            .Setup(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingQuote);

        // Act
        Func<Task> act = async () => await Sut.ExecuteAsync(folioNumber, nonExistentIndex, request);

        // Assert
        await act.Should().ThrowAsync<FolioNotFoundException>()
            .WithMessage($"*{nonExistentIndex}*");
        _mockRepository.Verify(r => r.PatchLocationAsync(
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Location>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_VersionConflict_PropagatesVersionConflictException()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00007";
        const int index = 1;
        const int version = 3;
        var request = BuildPatchRequest(locationName: "Test", version: version);

        var existingQuote = BuildPropertyQuoteWithLocations(folioNumber, version, new List<Location> { BuildCalculableLocation(index) });

        _mockRepository
            .Setup(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingQuote);
        _mockRepository
            .Setup(r => r.PatchLocationAsync(folioNumber, version, index, It.IsAny<Location>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new VersionConflictException(folioNumber, version));

        // Act
        Func<Task> act = async () => await Sut.ExecuteAsync(folioNumber, index, request);

        // Assert
        await act.Should().ThrowAsync<VersionConflictException>()
            .Where(ex => ex.FolioNumber == folioNumber && ex.ExpectedVersion == version);
    }
}
