using Cotizador.Application.DTOs;
using Cotizador.Application.Ports;
using Cotizador.Application.UseCases;
using Cotizador.Domain.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cotizador.Tests.Application.UseCases;

public class GetRiskClassificationsUseCaseTests
{
    private readonly Mock<ICoreOhsClient> _mockCoreOhsClient = new();
    private readonly Mock<ILogger<GetRiskClassificationsUseCase>> _mockLogger = new();

    private GetRiskClassificationsUseCase Sut => new(_mockCoreOhsClient.Object, _mockLogger.Object);

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_Should_ReturnClassificationList_WhenCoreOhsResponds()
    {
        // Arrange
        var classifications = new List<RiskClassificationDto>
        {
            new("A", "Riesgo Bajo", 1.0m),
            new("B", "Riesgo Medio", 1.5m),
            new("C", "Riesgo Alto", 2.0m)
        };

        _mockCoreOhsClient
            .Setup(c => c.GetRiskClassificationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(classifications);

        // Act
        List<RiskClassificationDto> result = await Sut.ExecuteAsync();

        // Assert
        result.Should().HaveCount(3);
        result[0].Code.Should().Be("A");
        result[0].Description.Should().Be("Riesgo Bajo");
        result[0].Factor.Should().Be(1.0m);
        result[2].Code.Should().Be("C");
        result[2].Description.Should().Be("Riesgo Alto");
        result[2].Factor.Should().Be(2.0m);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task ExecuteAsync_Should_ThrowCoreOhsUnavailableException_WhenClientThrows()
    {
        // Arrange
        _mockCoreOhsClient
            .Setup(c => c.GetRiskClassificationsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("core-ohs no disponible"));

        // Act
        Func<Task> act = async () => await Sut.ExecuteAsync();

        // Assert
        await act.Should().ThrowAsync<CoreOhsUnavailableException>()
            .WithMessage("*clasificaciones de riesgo*");
    }
}
