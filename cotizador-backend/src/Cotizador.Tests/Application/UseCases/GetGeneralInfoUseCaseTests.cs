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

public class GetGeneralInfoUseCaseTests
{
    private readonly Mock<IQuoteRepository> _mockRepository = new();
    private readonly Mock<ILogger<GetGeneralInfoUseCase>> _mockLogger = new();

    private GetGeneralInfoUseCase Sut => new(_mockRepository.Object, _mockLogger.Object);

    // ─── Happy Paths ───────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_ExistingFolio_ReturnsGeneralInfoDto()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00001";
        var quote = BuildPropertyQuote(folioNumber);

        _mockRepository
            .Setup(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        // Act
        GeneralInfoDto result = await Sut.ExecuteAsync(folioNumber);

        // Assert
        result.Should().NotBeNull();
        result.AgentCode.Should().Be("AGT-001");
        result.BusinessType.Should().Be("commercial");
        result.RiskClassification.Should().Be("A");
        result.Version.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_ExistingFolio_MapsInsuredDataCorrectly()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00002";
        var quote = BuildPropertyQuote(folioNumber);

        _mockRepository
            .Setup(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        // Act
        GeneralInfoDto result = await Sut.ExecuteAsync(folioNumber);

        // Assert
        result.InsuredData.Name.Should().Be("Empresa Ejemplo S.A.");
        result.InsuredData.TaxId.Should().Be("EEJ010101ABC");
        result.InsuredData.Email.Should().Be("contacto@empresa.mx");
        result.InsuredData.Phone.Should().Be("+521234567890");
    }

    [Fact]
    public async Task ExecuteAsync_ExistingFolio_MapsConductionDataCorrectly()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00003";
        var quote = BuildPropertyQuote(folioNumber);

        _mockRepository
            .Setup(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        // Act
        GeneralInfoDto result = await Sut.ExecuteAsync(folioNumber);

        // Assert
        result.ConductionData.SubscriberCode.Should().Be("SUB-001");
        result.ConductionData.OfficeName.Should().Be("Oficina Central");
        result.ConductionData.BranchOffice.Should().Be("Sucursal Norte");
    }

    [Fact]
    public async Task ExecuteAsync_ExistingFolio_ReturnsCurrentVersion()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00004";
        var quote = BuildPropertyQuote(folioNumber);
        quote.Version = 5;

        _mockRepository
            .Setup(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        // Act
        GeneralInfoDto result = await Sut.ExecuteAsync(folioNumber);

        // Assert
        result.Version.Should().Be(5);
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
        await act.Should().ThrowAsync<FolioNotFoundException>()
            .Where(ex => ex.FolioNumber == folioNumber);
    }

    [Fact]
    public async Task ExecuteAsync_FolioNotFound_ExceptionMessageContainsFolioNumber()
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
            .WithMessage($"*{folioNumber}*");
    }

    // ─── Helpers ───────────────────────────────────────────────────────────────

    private static PropertyQuote BuildPropertyQuote(string folioNumber) =>
        new()
        {
            FolioNumber = folioNumber,
            QuoteStatus = QuoteStatus.Draft,
            AgentCode = "AGT-001",
            BusinessType = "commercial",
            RiskClassification = "A",
            Version = 1,
            InsuredData = new InsuredData
            {
                Name = "Empresa Ejemplo S.A.",
                TaxId = "EEJ010101ABC",
                Email = "contacto@empresa.mx",
                Phone = "+521234567890"
            },
            ConductionData = new ConductionData
            {
                SubscriberCode = "SUB-001",
                OfficeName = "Oficina Central",
                BranchOffice = "Sucursal Norte"
            },
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
