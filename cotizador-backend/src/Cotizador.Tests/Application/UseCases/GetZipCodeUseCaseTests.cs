using Cotizador.Application.DTOs;
using Cotizador.Application.Ports;
using Cotizador.Application.UseCases;
using Cotizador.Domain.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cotizador.Tests.Application.UseCases;

public class GetZipCodeUseCaseTests
{
    private readonly Mock<ICoreOhsClient> _mockCoreOhsClient = new();
    private readonly Mock<ILogger<GetZipCodeUseCase>> _mockLogger = new();

    private GetZipCodeUseCase Sut => new(_mockCoreOhsClient.Object, _mockLogger.Object);

    // ─── Happy Paths ───────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_ValidZipCode_ReturnsZipCodeDto()
    {
        // Arrange
        const string zipCode = "06600";
        var expected = new ZipCodeDto(zipCode, "Ciudad de México", "Cuauhtémoc", "Doctores", "Ciudad de México", "A", 1);

        _mockCoreOhsClient
            .Setup(c => c.GetZipCodeAsync(zipCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await Sut.ExecuteAsync(zipCode);

        // Assert
        result.Should().NotBeNull();
        result!.ZipCode.Should().Be(zipCode);
        result.State.Should().Be("Ciudad de México");
        result.Municipality.Should().Be("Cuauhtémoc");
        result.Neighborhood.Should().Be("Doctores");
        result.CatZone.Should().Be("A");
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_ZipCodeNotFound_ReturnsNull()
    {
        // Arrange — core-ohs returns null when CP does not exist
        const string zipCode = "99999";

        _mockCoreOhsClient
            .Setup(c => c.GetZipCodeAsync(zipCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ZipCodeDto?)null);

        // Act
        var result = await Sut.ExecuteAsync(zipCode);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_DelegatesCallToCoreOhsClient()
    {
        // Arrange
        const string zipCode = "01000";

        _mockCoreOhsClient
            .Setup(c => c.GetZipCodeAsync(zipCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ZipCodeDto?)null);

        // Act
        await Sut.ExecuteAsync(zipCode);

        // Assert
        _mockCoreOhsClient.Verify(
            c => c.GetZipCodeAsync(zipCode, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ZipCodeResolvesAllGeoFields()
    {
        // Arrange
        const string zipCode = "64000";
        var dto = new ZipCodeDto(zipCode, "Nuevo León", "Monterrey", "Centro", "Monterrey", "B", 2);

        _mockCoreOhsClient
            .Setup(c => c.GetZipCodeAsync(zipCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await Sut.ExecuteAsync(zipCode);

        // Assert — all geographic fields resolved correctly (RN-006-07)
        result!.State.Should().Be("Nuevo León");
        result.Municipality.Should().Be("Monterrey");
        result.Neighborhood.Should().Be("Centro");
        result.City.Should().Be("Monterrey");
        result.CatZone.Should().Be("B");
        result.TechnicalLevel.Should().Be(2);
    }

    // ─── Error Paths ───────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Regression")]
    public async Task ExecuteAsync_CoreOhsUnavailable_PropagatesException()
    {
        // Arrange
        const string zipCode = "06600";

        _mockCoreOhsClient
            .Setup(c => c.GetZipCodeAsync(zipCode, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CoreOhsUnavailableException("core-ohs no disponible"));

        // Act
        Func<Task> act = async () => await Sut.ExecuteAsync(zipCode);

        // Assert
        await act.Should().ThrowAsync<CoreOhsUnavailableException>();
    }
}
