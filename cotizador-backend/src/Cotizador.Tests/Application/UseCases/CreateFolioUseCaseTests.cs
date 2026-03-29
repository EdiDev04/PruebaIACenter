using Cotizador.Application.DTOs;
using Cotizador.Application.Ports;
using Cotizador.Application.UseCases;
using Cotizador.Domain.Constants;
using Cotizador.Domain.Entities;
using Cotizador.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cotizador.Tests.Application.UseCases;

public class CreateFolioUseCaseTests
{
    private readonly Mock<IQuoteRepository> _mockRepository = new();
    private readonly Mock<ICoreOhsClient> _mockCoreOhsClient = new();
    private readonly Mock<ILogger<CreateFolioUseCase>> _mockLogger = new();

    private CreateFolioUseCase Sut => new(
        _mockRepository.Object,
        _mockCoreOhsClient.Object,
        _mockLogger.Object);

    // ─── Happy Paths ───────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_NewFolio_CallsApiClientAndPersistsReturningIsNewTrue()
    {
        // Arrange
        const string idempotencyKey = "idem-key-001";
        const string createdBy = "agent@company.mx";
        const string folioNumber = "DAN-2024-00001";

        _mockRepository
            .Setup(r => r.GetByIdempotencyKeyAsync(idempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyQuote?)null);

        _mockCoreOhsClient
            .Setup(c => c.GenerateFolioAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FolioDto(folioNumber));

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<PropertyQuote>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var (dto, isNew) = await Sut.ExecuteAsync(idempotencyKey, createdBy);

        // Assert
        isNew.Should().BeTrue();
        dto.FolioNumber.Should().Be(folioNumber);
        dto.QuoteStatus.Should().Be(QuoteStatus.Draft);
        dto.Version.Should().Be(1);

        _mockCoreOhsClient.Verify(c => c.GenerateFolioAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<PropertyQuote>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_ExistingIdempotencyKey_ReturnsExistingDtoWithIsNewFalseWithoutCallingApiClient()
    {
        // Arrange
        const string idempotencyKey = "idem-key-existing";
        var existingQuote = BuildPropertyQuote("DAN-2024-00099", idempotencyKey);

        _mockRepository
            .Setup(r => r.GetByIdempotencyKeyAsync(idempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingQuote);

        // Act
        var (dto, isNew) = await Sut.ExecuteAsync(idempotencyKey, "agent@company.mx");

        // Assert
        isNew.Should().BeFalse();
        dto.FolioNumber.Should().Be("DAN-2024-00099");

        _mockCoreOhsClient.Verify(c => c.GenerateFolioAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<PropertyQuote>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_NewFolio_PersistsCorrectMetadataFields()
    {
        // Arrange
        const string idempotencyKey = "idem-key-003";
        const string createdBy = "agent@company.mx";

        _mockRepository
            .Setup(r => r.GetByIdempotencyKeyAsync(idempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyQuote?)null);

        _mockCoreOhsClient
            .Setup(c => c.GenerateFolioAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FolioDto("DAN-2024-00003"));

        PropertyQuote? captured = null;
        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<PropertyQuote>(), It.IsAny<CancellationToken>()))
            .Callback<PropertyQuote, CancellationToken>((q, _) => captured = q)
            .Returns(Task.CompletedTask);

        // Act
        await Sut.ExecuteAsync(idempotencyKey, createdBy);

        // Assert
        captured.Should().NotBeNull();
        captured!.Metadata.IdempotencyKey.Should().Be(idempotencyKey);
        captured.Metadata.CreatedBy.Should().Be(createdBy);
        captured.Metadata.LastWizardStep.Should().Be(0);
        captured.QuoteStatus.Should().Be(QuoteStatus.Draft);
        captured.Version.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_ExistingIdempotencyKey_ReturnsSameFolioStatusAndVersion()
    {
        // Arrange
        const string idempotencyKey = "idem-key-v2";
        var existingQuote = BuildPropertyQuote("DAN-2024-00050", idempotencyKey);
        existingQuote.QuoteStatus = QuoteStatus.InProgress;
        existingQuote.Version = 3;

        _mockRepository
            .Setup(r => r.GetByIdempotencyKeyAsync(idempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingQuote);

        // Act
        var (dto, isNew) = await Sut.ExecuteAsync(idempotencyKey, "agent@company.mx");

        // Assert
        isNew.Should().BeFalse();
        dto.QuoteStatus.Should().Be(QuoteStatus.InProgress);
        dto.Version.Should().Be(3);
    }

    // ─── Error Paths ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_RepositoryCreateThrows_PropagatesException()
    {
        // Arrange
        const string idempotencyKey = "idem-key-002";

        _mockRepository
            .Setup(r => r.GetByIdempotencyKeyAsync(idempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyQuote?)null);

        _mockCoreOhsClient
            .Setup(c => c.GenerateFolioAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FolioDto("DAN-2024-00002"));

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<PropertyQuote>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Folio already exists"));

        // Act
        Func<Task> act = async () => await Sut.ExecuteAsync(idempotencyKey, "agent@company.mx");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Folio already exists");
    }

    [Fact]
    public async Task ExecuteAsync_ApiClientGenerateFolioThrows_PropagatesException()
    {
        // Arrange
        const string idempotencyKey = "idem-key-004";

        _mockRepository
            .Setup(r => r.GetByIdempotencyKeyAsync(idempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyQuote?)null);

        _mockCoreOhsClient
            .Setup(c => c.GenerateFolioAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("core-ohs unavailable"));

        // Act
        Func<Task> act = async () => await Sut.ExecuteAsync(idempotencyKey, "agent@company.mx");

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();

        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<PropertyQuote>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── Helpers ───────────────────────────────────────────────────────────────

    private static PropertyQuote BuildPropertyQuote(string folioNumber, string idempotencyKey) =>
        new()
        {
            FolioNumber = folioNumber,
            QuoteStatus = QuoteStatus.Draft,
            Version = 1,
            Metadata = new QuoteMetadata
            {
                IdempotencyKey = idempotencyKey,
                CreatedBy = "agent@company.mx",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                LastWizardStep = 0
            }
        };
}
