using Cotizador.Application.DTOs;
using Cotizador.Application.Ports;
using Cotizador.Application.UseCases;
using Cotizador.Domain.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cotizador.Tests.Application.UseCases;

public class GetBusinessLinesUseCaseTests
{
    private readonly Mock<ICoreOhsClient> _mockCoreOhsClient = new();
    private readonly Mock<ILogger<GetBusinessLinesUseCase>> _mockLogger = new();

    private GetBusinessLinesUseCase Sut => new(_mockCoreOhsClient.Object, _mockLogger.Object);

    // ─── Happy Paths ───────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_ReturnsBusinessLinesFromCoreOhs()
    {
        // Arrange
        var businessLines = new List<BusinessLineDto>
        {
            new("BL-001", "Storage warehouse", "B-03", "bajo"),
            new("BL-002", "Office building", "A-01", "bajo"),
            new("BL-003", "Retail store", "C-05", "medio")
        };

        _mockCoreOhsClient
            .Setup(c => c.GetBusinessLinesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(businessLines);

        // Act
        var result = await Sut.ExecuteAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain(bl => bl.FireKey == "B-03");
    }

    [Fact]
    public async Task ExecuteAsync_DelegatesCallToCoreOhsClient()
    {
        // Arrange
        _mockCoreOhsClient
            .Setup(c => c.GetBusinessLinesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BusinessLineDto>());

        // Act
        await Sut.ExecuteAsync();

        // Assert
        _mockCoreOhsClient.Verify(
            c => c.GetBusinessLinesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyCatalog_ReturnsEmptyList()
    {
        // Arrange
        _mockCoreOhsClient
            .Setup(c => c.GetBusinessLinesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BusinessLineDto>());

        // Act
        var result = await Sut.ExecuteAsync();

        // Assert
        result.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_MapsAllBusinessLineFields()
    {
        // Arrange
        var expected = new BusinessLineDto("BL-010", "Industrial plant", "D-07", "alto");

        _mockCoreOhsClient
            .Setup(c => c.GetBusinessLinesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BusinessLineDto> { expected });

        // Act
        var result = await Sut.ExecuteAsync();

        // Assert
        result[0].Code.Should().Be("BL-010");
        result[0].Description.Should().Be("Industrial plant");
        result[0].FireKey.Should().Be("D-07");
        result[0].RiskLevel.Should().Be("alto");
    }

    // ─── Error Paths ───────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Regression")]
    public async Task ExecuteAsync_CoreOhsUnavailable_PropagatesException()
    {
        // Arrange
        _mockCoreOhsClient
            .Setup(c => c.GetBusinessLinesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CoreOhsUnavailableException("core-ohs no disponible"));

        // Act
        Func<Task> act = async () => await Sut.ExecuteAsync();

        // Assert
        await act.Should().ThrowAsync<CoreOhsUnavailableException>();
    }
}
