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

public class UpdateLayoutUseCaseTests
{
    private readonly Mock<IQuoteRepository> _mockRepository = new();
    private readonly Mock<ILogger<UpdateLayoutUseCase>> _mockLogger = new();

    private UpdateLayoutUseCase Sut => new(_mockRepository.Object, _mockLogger.Object);

    // ─── Happy Paths ───────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_Should_ReturnUpdatedLayout_WhenDataIsValid()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00001";
        var request = BuildValidRequest(version: 2);
        var updatedQuote = BuildPropertyQuote(folioNumber, displayMode: "list",
            visibleColumns: new List<string> { "index", "locationName", "zipCode" }, version: 3);

        _mockRepository
            .Setup(r => r.UpdateLayoutAsync(
                folioNumber, request.Version, It.IsAny<LayoutConfiguration>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedQuote);

        // Act
        LayoutConfigurationDto result = await Sut.ExecuteAsync(folioNumber, request);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnIncrementedVersion_WhenDataIsValid()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00002";
        var request = BuildValidRequest(version: 2);
        var updatedQuote = BuildPropertyQuote(folioNumber, displayMode: "grid",
            visibleColumns: new List<string> { "index", "locationName" }, version: 3);

        _mockRepository
            .Setup(r => r.UpdateLayoutAsync(
                folioNumber, request.Version, It.IsAny<LayoutConfiguration>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedQuote);

        // Act
        LayoutConfigurationDto result = await Sut.ExecuteAsync(folioNumber, request);

        // Assert
        result.Version.Should().Be(3);
    }

    [Fact]
    public async Task ExecuteAsync_Should_CallUpdateLayoutAsync_WithCorrectArguments()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00003";
        var request = BuildValidRequest(version: 1, displayMode: "list",
            visibleColumns: new List<string> { "index", "locationName", "zipCode" });
        var updatedQuote = BuildPropertyQuote(folioNumber, displayMode: "list",
            visibleColumns: request.VisibleColumns, version: 2);

        _mockRepository
            .Setup(r => r.UpdateLayoutAsync(
                folioNumber, It.IsAny<int>(), It.IsAny<LayoutConfiguration>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedQuote);

        // Act
        await Sut.ExecuteAsync(folioNumber, request);

        // Assert
        _mockRepository.Verify(r => r.UpdateLayoutAsync(
            folioNumber,
            request.Version,
            It.Is<LayoutConfiguration>(l =>
                l.DisplayMode == request.DisplayMode &&
                l.VisibleColumns.SequenceEqual(request.VisibleColumns)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnUpdatedDisplayMode_WhenDataIsValid()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00004";
        var request = BuildValidRequest(version: 1, displayMode: "list",
            visibleColumns: new List<string> { "index", "locationName" });
        var updatedQuote = BuildPropertyQuote(folioNumber, displayMode: "list",
            visibleColumns: request.VisibleColumns, version: 2);

        _mockRepository
            .Setup(r => r.UpdateLayoutAsync(
                folioNumber, It.IsAny<int>(), It.IsAny<LayoutConfiguration>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedQuote);

        // Act
        LayoutConfigurationDto result = await Sut.ExecuteAsync(folioNumber, request);

        // Assert
        result.DisplayMode.Should().Be("list");
    }

    // ─── Error Paths ───────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_Should_ThrowVersionConflictException_WhenVersionMismatch()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00005";
        var request = BuildValidRequest(version: 1);

        _mockRepository
            .Setup(r => r.UpdateLayoutAsync(
                folioNumber, request.Version, It.IsAny<LayoutConfiguration>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new VersionConflictException(folioNumber, request.Version));

        // Act
        Func<Task> act = async () => await Sut.ExecuteAsync(folioNumber, request);

        // Assert
        await act.Should().ThrowAsync<VersionConflictException>();
    }

    [Fact]
    public async Task ExecuteAsync_Should_PreserveVersionConflictDetails_WhenVersionMismatch()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00006";
        var request = BuildValidRequest(version: 1);

        _mockRepository
            .Setup(r => r.UpdateLayoutAsync(
                folioNumber, request.Version, It.IsAny<LayoutConfiguration>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new VersionConflictException(folioNumber, request.Version));

        // Act
        Func<Task> act = async () => await Sut.ExecuteAsync(folioNumber, request);

        // Assert
        await act.Should().ThrowAsync<VersionConflictException>()
            .Where(ex => ex.FolioNumber == folioNumber && ex.ExpectedVersion == request.Version);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_Should_ThrowFolioNotFoundException_WhenFolioDoesNotExist()
    {
        // Arrange
        const string folioNumber = "DAN-2024-99999";
        var request = BuildValidRequest(version: 1);

        _mockRepository
            .Setup(r => r.UpdateLayoutAsync(
                folioNumber, request.Version, It.IsAny<LayoutConfiguration>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FolioNotFoundException(folioNumber));

        // Act
        Func<Task> act = async () => await Sut.ExecuteAsync(folioNumber, request);

        // Assert
        await act.Should().ThrowAsync<FolioNotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_Should_PreserveFolioNumberInException_WhenFolioDoesNotExist()
    {
        // Arrange
        const string folioNumber = "DAN-2024-88888";
        var request = BuildValidRequest(version: 1);

        _mockRepository
            .Setup(r => r.UpdateLayoutAsync(
                folioNumber, request.Version, It.IsAny<LayoutConfiguration>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FolioNotFoundException(folioNumber));

        // Act
        Func<Task> act = async () => await Sut.ExecuteAsync(folioNumber, request);

        // Assert
        await act.Should().ThrowAsync<FolioNotFoundException>()
            .Where(ex => ex.FolioNumber == folioNumber);
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotCallGetByFolioNumber_WhenUpdateLayoutThrows()
    {
        // Arrange
        const string folioNumber = "DAN-2024-77777";
        var request = BuildValidRequest(version: 1);

        _mockRepository
            .Setup(r => r.UpdateLayoutAsync(
                folioNumber, request.Version, It.IsAny<LayoutConfiguration>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new VersionConflictException(folioNumber, request.Version));

        // Act
        Func<Task> act = async () => await Sut.ExecuteAsync(folioNumber, request);
        await act.Should().ThrowAsync<VersionConflictException>();

        // Assert
        _mockRepository.Verify(
            r => r.GetByFolioNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ─── Helpers ───────────────────────────────────────────────────────────────

    private static UpdateLayoutRequest BuildValidRequest(
        int version = 1,
        string displayMode = "grid",
        List<string>? visibleColumns = null) =>
        new(
            DisplayMode: displayMode,
            VisibleColumns: visibleColumns ?? new List<string> { "index", "locationName", "zipCode", "businessLine", "validationStatus" },
            Version: version
        );

    private static PropertyQuote BuildPropertyQuote(
        string folioNumber,
        string displayMode,
        List<string> visibleColumns,
        int version) =>
        new()
        {
            FolioNumber = folioNumber,
            QuoteStatus = QuoteStatus.Draft,
            AgentCode = "AGT-001",
            BusinessType = "commercial",
            RiskClassification = "A",
            Version = version,
            LayoutConfiguration = new LayoutConfiguration
            {
                DisplayMode = displayMode,
                VisibleColumns = visibleColumns
            },
            InsuredData = new InsuredData { Name = "Empresa Test S.A.", TaxId = "ETT010101ABC" },
            ConductionData = new ConductionData { SubscriberCode = "SUB-001" },
            Metadata = new QuoteMetadata
            {
                CreatedBy = "agent@company.mx",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IdempotencyKey = "key-001",
                LastWizardStep = 0
            }
        };
}
