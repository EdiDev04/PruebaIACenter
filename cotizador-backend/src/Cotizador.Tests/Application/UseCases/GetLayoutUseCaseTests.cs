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

public class GetLayoutUseCaseTests
{
    private readonly Mock<IQuoteRepository> _mockRepository = new();
    private readonly Mock<ILogger<GetLayoutUseCase>> _mockLogger = new();

    private GetLayoutUseCase Sut => new(_mockRepository.Object, _mockLogger.Object);

    // ─── Happy Paths ───────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_Should_ReturnLayoutDto_WhenFolioExistsWithLayout()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00001";
        var quote = BuildPropertyQuote(folioNumber, displayMode: "list", visibleColumns: new List<string>
        {
            "index", "locationName", "address", "zipCode", "state"
        }, version: 3);

        _mockRepository
            .Setup(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        // Act
        LayoutConfigurationDto result = await Sut.ExecuteAsync(folioNumber);

        // Assert
        result.DisplayMode.Should().Be("list");
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnCorrectVisibleColumns_WhenFolioExistsWithLayout()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00002";
        var expectedColumns = new List<string> { "index", "locationName", "address", "zipCode", "state" };
        var quote = BuildPropertyQuote(folioNumber, displayMode: "list", visibleColumns: expectedColumns, version: 3);

        _mockRepository
            .Setup(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        // Act
        LayoutConfigurationDto result = await Sut.ExecuteAsync(folioNumber);

        // Assert
        result.VisibleColumns.Should().BeEquivalentTo(expectedColumns);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnCorrectVersion_WhenFolioExistsWithLayout()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00003";
        var quote = BuildPropertyQuote(folioNumber, displayMode: "list",
            visibleColumns: new List<string> { "index" }, version: 3);

        _mockRepository
            .Setup(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        // Act
        LayoutConfigurationDto result = await Sut.ExecuteAsync(folioNumber);

        // Assert
        result.Version.Should().Be(3);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnDefaultLayout_WhenFolioHasNoCustomLayout()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00004";
        var quote = BuildPropertyQuoteWithDefaultLayout(folioNumber);

        _mockRepository
            .Setup(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        // Act
        LayoutConfigurationDto result = await Sut.ExecuteAsync(folioNumber);

        // Assert
        result.DisplayMode.Should().Be("grid");
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnDefaultVisibleColumns_WhenFolioHasNoCustomLayout()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00005";
        var quote = BuildPropertyQuoteWithDefaultLayout(folioNumber);

        _mockRepository
            .Setup(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        // Act
        LayoutConfigurationDto result = await Sut.ExecuteAsync(folioNumber);

        // Assert
        result.VisibleColumns.Should().BeEquivalentTo(new List<string>
        {
            "index", "locationName", "zipCode", "businessLine", "validationStatus"
        });
    }

    // ─── Error Paths ───────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_Should_ThrowFolioNotFoundException_WhenFolioDoesNotExist()
    {
        // Arrange
        const string folioNumber = "DAN-2024-99999";

        _mockRepository
            .Setup(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyQuote?)null);

        // Act
        Func<Task> act = async () => await Sut.ExecuteAsync(folioNumber);

        // Assert
        await act.Should().ThrowAsync<FolioNotFoundException>()
            .WithMessage($"*{folioNumber}*");
    }

    [Fact]
    public async Task ExecuteAsync_Should_IncludeFolioNumberInException_WhenFolioDoesNotExist()
    {
        // Arrange
        const string folioNumber = "DAN-2024-88888";

        _mockRepository
            .Setup(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyQuote?)null);

        // Act
        Func<Task> act = async () => await Sut.ExecuteAsync(folioNumber);

        // Assert
        await act.Should().ThrowAsync<FolioNotFoundException>()
            .Where(ex => ex.FolioNumber == folioNumber);
    }

    [Fact]
    public async Task ExecuteAsync_Should_CallRepositoryOnce_WhenFolioExists()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00006";
        var quote = BuildPropertyQuoteWithDefaultLayout(folioNumber);

        _mockRepository
            .Setup(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        // Act
        await Sut.ExecuteAsync(folioNumber);

        // Assert
        _mockRepository.Verify(
            r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ─── Helpers ───────────────────────────────────────────────────────────────

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

    private static PropertyQuote BuildPropertyQuoteWithDefaultLayout(string folioNumber) =>
        new()
        {
            FolioNumber = folioNumber,
            QuoteStatus = QuoteStatus.Draft,
            AgentCode = "AGT-001",
            BusinessType = "commercial",
            RiskClassification = "A",
            Version = 1,
            LayoutConfiguration = new LayoutConfiguration(), // default values
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
