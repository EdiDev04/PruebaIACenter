using Cotizador.Application.DTOs;
using Cotizador.Application.Ports;
using Cotizador.Application.Settings;
using Cotizador.Application.UseCases;
using Cotizador.Domain.Constants;
using Cotizador.Domain.Entities;
using Cotizador.Domain.Exceptions;
using Cotizador.Domain.ValueObjects;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Cotizador.Tests.Application.UseCases;

public class UpdateGeneralInfoUseCaseTests
{
    private readonly Mock<IQuoteRepository> _mockRepository = new();
    private readonly Mock<ICoreOhsClient> _mockCoreOhsClient = new();
    private readonly Mock<ILogger<UpdateGeneralInfoUseCase>> _mockLogger = new();
    private readonly IOptions<BusinessTypeSettings> _defaultSettings = Options.Create(new BusinessTypeSettings
    {
        AllowedValues = new List<string> { "commercial", "industrial", "residential" }
    });

    private UpdateGeneralInfoUseCase Sut => new(
        _mockRepository.Object,
        _mockCoreOhsClient.Object,
        _defaultSettings,
        _mockLogger.Object);

    // ─── Happy Paths ───────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_ValidRequest_PersistsAndReturnsUpdatedDto()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00001";
        var request = BuildValidRequest(version: 1);
        var existingQuote = BuildPropertyQuote(folioNumber, QuoteStatus.Draft, version: 1);
        var updatedQuote = BuildPropertyQuote(folioNumber, QuoteStatus.InProgress, version: 2);

        _mockCoreOhsClient
            .Setup(c => c.GetAgentByCodeAsync(request.AgentCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentDto("AGT-001", "Agente Test", "Norte", true));

        _mockRepository
            .SetupSequence(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingQuote)
            .ReturnsAsync(updatedQuote);

        _mockRepository
            .Setup(r => r.UpdateGeneralInfoAsync(
                folioNumber, It.IsAny<int>(), It.IsAny<InsuredData>(), It.IsAny<ConductionData>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        GeneralInfoDto result = await Sut.ExecuteAsync(folioNumber, request);

        // Assert
        result.Should().NotBeNull();
        result.Version.Should().Be(2);

        _mockRepository.Verify(r => r.UpdateGeneralInfoAsync(
            folioNumber, request.Version, It.IsAny<InsuredData>(), It.IsAny<ConductionData>(),
            request.AgentCode, request.BusinessType, request.RiskClassification,
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_DraftStatus_SetsStatusToInProgress()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00002";
        var request = BuildValidRequest(version: 1);
        var draftQuote = BuildPropertyQuote(folioNumber, QuoteStatus.Draft, version: 1);
        var updatedQuote = BuildPropertyQuote(folioNumber, QuoteStatus.InProgress, version: 2);

        _mockCoreOhsClient
            .Setup(c => c.GetAgentByCodeAsync(request.AgentCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentDto("AGT-001", "Agente Test", "Norte", true));

        _mockRepository
            .SetupSequence(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(draftQuote)
            .ReturnsAsync(updatedQuote);

        _mockRepository
            .Setup(r => r.UpdateGeneralInfoAsync(
                folioNumber, It.IsAny<int>(), It.IsAny<InsuredData>(), It.IsAny<ConductionData>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                QuoteStatus.InProgress, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await Sut.ExecuteAsync(folioNumber, request);

        // Assert — status transition draft → in_progress must be persisted
        _mockRepository.Verify(r => r.UpdateGeneralInfoAsync(
            folioNumber, It.IsAny<int>(), It.IsAny<InsuredData>(), It.IsAny<ConductionData>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            QuoteStatus.InProgress, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_InProgressStatus_DoesNotChangeStatus()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00003";
        var request = BuildValidRequest(version: 2);
        var inProgressQuote = BuildPropertyQuote(folioNumber, QuoteStatus.InProgress, version: 2);
        var updatedQuote = BuildPropertyQuote(folioNumber, QuoteStatus.InProgress, version: 3);

        _mockCoreOhsClient
            .Setup(c => c.GetAgentByCodeAsync(request.AgentCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentDto("AGT-001", "Agente Test", "Norte", true));

        _mockRepository
            .SetupSequence(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inProgressQuote)
            .ReturnsAsync(updatedQuote);

        _mockRepository
            .Setup(r => r.UpdateGeneralInfoAsync(
                folioNumber, It.IsAny<int>(), It.IsAny<InsuredData>(), It.IsAny<ConductionData>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await Sut.ExecuteAsync(folioNumber, request);

        // Assert — newStatus must be null when already in_progress
        _mockRepository.Verify(r => r.UpdateGeneralInfoAsync(
            folioNumber, It.IsAny<int>(), It.IsAny<InsuredData>(), It.IsAny<ConductionData>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ValidRequest_MapsInsuredDataToValueObject()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00004";
        var request = BuildValidRequest(version: 1);
        var existingQuote = BuildPropertyQuote(folioNumber, QuoteStatus.Draft, version: 1);
        var updatedQuote = BuildPropertyQuote(folioNumber, QuoteStatus.InProgress, version: 2);

        _mockCoreOhsClient
            .Setup(c => c.GetAgentByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentDto("AGT-001", "Agente Test", "Norte", true));

        _mockRepository
            .SetupSequence(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingQuote)
            .ReturnsAsync(updatedQuote);

        InsuredData? capturedInsuredData = null;
        _mockRepository
            .Setup(r => r.UpdateGeneralInfoAsync(
                It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<InsuredData>(), It.IsAny<ConductionData>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, int, InsuredData, ConductionData, string, string, string, string?, CancellationToken>(
                (_, _, insured, _, _, _, _, _, _) => capturedInsuredData = insured)
            .Returns(Task.CompletedTask);

        // Act
        await Sut.ExecuteAsync(folioNumber, request);

        // Assert
        capturedInsuredData.Should().NotBeNull();
        capturedInsuredData!.Name.Should().Be(request.InsuredData.Name);
        capturedInsuredData.TaxId.Should().Be(request.InsuredData.TaxId);
        capturedInsuredData.Email.Should().Be(request.InsuredData.Email);
        capturedInsuredData.Phone.Should().Be(request.InsuredData.Phone);
    }

    // ─── Error Paths ───────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_FolioNotFound_ThrowsFolioNotFoundException()
    {
        // Arrange
        const string folioNumber = "DAN-2024-99999";
        var request = BuildValidRequest(version: 1);

        _mockCoreOhsClient
            .Setup(c => c.GetAgentByCodeAsync(request.AgentCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentDto("AGT-001", "Agente Test", "Norte", true));

        _mockRepository
            .Setup(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyQuote?)null);

        // Act
        Func<Task> act = async () => await Sut.ExecuteAsync(folioNumber, request);

        // Assert
        await act.Should().ThrowAsync<FolioNotFoundException>()
            .Where(ex => ex.FolioNumber == folioNumber);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidBusinessType_ThrowsValidationException()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00001";
        var request = BuildValidRequest(version: 1) with { BusinessType = "unknown_type" };

        // Act
        Func<Task> act = async () => await Sut.ExecuteAsync(folioNumber, request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.Any(e => e.PropertyName == "BusinessType"));

        _mockCoreOhsClient.Verify(c => c.GetAgentByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.GetByFolioNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("commercial")]
    [InlineData("COMMERCIAL")]
    [InlineData("Industrial")]
    [InlineData("residential")]
    public async Task ExecuteAsync_ValidBusinessTypeVariousCase_DoesNotThrowValidationException(string businessType)
    {
        // Arrange
        const string folioNumber = "DAN-2024-00001";
        var request = BuildValidRequest(version: 1) with { BusinessType = businessType };
        var existingQuote = BuildPropertyQuote(folioNumber, QuoteStatus.Draft, version: 1);
        var updatedQuote = BuildPropertyQuote(folioNumber, QuoteStatus.InProgress, version: 2);

        _mockCoreOhsClient
            .Setup(c => c.GetAgentByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentDto("AGT-001", "Agente Test", "Norte", true));

        _mockRepository
            .SetupSequence(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingQuote)
            .ReturnsAsync(updatedQuote);

        _mockRepository
            .Setup(r => r.UpdateGeneralInfoAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<InsuredData>(), It.IsAny<ConductionData>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        Func<Task> act = async () => await Sut.ExecuteAsync(folioNumber, request);

        // Assert
        await act.Should().NotThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task ExecuteAsync_AgentNotFoundInCatalog_ThrowsInvalidQuoteStateException()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00001";
        var request = BuildValidRequest(version: 1);
        var existingQuote = BuildPropertyQuote(folioNumber, QuoteStatus.Draft, version: 1);

        _mockRepository
            .Setup(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingQuote);

        _mockCoreOhsClient
            .Setup(c => c.GetAgentByCodeAsync(request.AgentCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AgentDto?)null);

        // Act
        Func<Task> act = async () => await Sut.ExecuteAsync(folioNumber, request);

        // Assert
        await act.Should().ThrowAsync<InvalidQuoteStateException>();

        _mockRepository.Verify(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_RepositoryThrowsVersionConflict_PropagatesVersionConflictException()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00001";
        var request = BuildValidRequest(version: 1);
        var existingQuote = BuildPropertyQuote(folioNumber, QuoteStatus.Draft, version: 5);

        _mockCoreOhsClient
            .Setup(c => c.GetAgentByCodeAsync(request.AgentCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentDto("AGT-001", "Agente Test", "Norte", true));

        _mockRepository
            .Setup(r => r.GetByFolioNumberAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingQuote);

        _mockRepository
            .Setup(r => r.UpdateGeneralInfoAsync(
                folioNumber, It.IsAny<int>(), It.IsAny<InsuredData>(), It.IsAny<ConductionData>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new VersionConflictException(folioNumber, request.Version));

        // Act
        Func<Task> act = async () => await Sut.ExecuteAsync(folioNumber, request);

        // Assert
        await act.Should().ThrowAsync<VersionConflictException>()
            .Where(ex => ex.FolioNumber == folioNumber && ex.ExpectedVersion == request.Version);
    }

    // ─── Helpers ───────────────────────────────────────────────────────────────

    private static UpdateGeneralInfoRequest BuildValidRequest(int version) =>
        new(
            InsuredData: new InsuredDataDto("Empresa Test S.A.", "ETT010101XYZ", "test@empresa.mx", "5512345678"),
            ConductionData: new ConductionDataDto("SUB-001", "Oficina Central", "Sucursal A"),
            AgentCode: "AGT-001",
            BusinessType: "commercial",
            RiskClassification: "A",
            Version: version
        );

    private static PropertyQuote BuildPropertyQuote(string folioNumber, string status, int version) =>
        new()
        {
            FolioNumber = folioNumber,
            QuoteStatus = status,
            Version = version,
            AgentCode = "AGT-001",
            BusinessType = "commercial",
            RiskClassification = "A",
            InsuredData = new InsuredData
            {
                Name = "Empresa Test S.A.",
                TaxId = "ETT010101XYZ",
                Email = "test@empresa.mx",
                Phone = "5512345678"
            },
            ConductionData = new ConductionData
            {
                SubscriberCode = "SUB-001",
                OfficeName = "Oficina Central",
                BranchOffice = "Sucursal A"
            },
            Metadata = new QuoteMetadata
            {
                CreatedBy = "agent@company.mx",
                IdempotencyKey = "key-001",
                LastWizardStep = 1
            }
        };
}
