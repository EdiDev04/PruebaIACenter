using Cotizador.Application.DTOs;
using Cotizador.Application.Ports;
using Cotizador.Application.UseCases;
using Cotizador.Domain.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cotizador.Tests.Application.UseCases;

public class GetAgentByCodeUseCaseTests
{
    private readonly Mock<ICoreOhsClient> _mockCoreOhsClient = new();
    private readonly Mock<ILogger<GetAgentByCodeUseCase>> _mockLogger = new();

    private GetAgentByCodeUseCase Sut => new(_mockCoreOhsClient.Object, _mockLogger.Object);

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_Should_ReturnAgent_WhenAgentExists()
    {
        // Arrange
        const string code = "AGT-001";
        var expectedAgent = new AgentDto(code, "Agente Ejemplo", "Norte", true);

        _mockCoreOhsClient
            .Setup(c => c.GetAgentByCodeAsync(code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedAgent);

        // Act
        AgentDto? result = await Sut.ExecuteAsync(code);

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be(code);
        result.Name.Should().Be("Agente Ejemplo");
        result.Region.Should().Be("Norte");
        result.Active.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnNull_WhenAgentDoesNotExist()
    {
        // Arrange
        const string code = "AGT-999";

        _mockCoreOhsClient
            .Setup(c => c.GetAgentByCodeAsync(code, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AgentDto?)null);

        // Act
        AgentDto? result = await Sut.ExecuteAsync(code);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task ExecuteAsync_Should_ThrowCoreOhsUnavailableException_WhenClientThrows()
    {
        // Arrange
        const string code = "AGT-001";

        _mockCoreOhsClient
            .Setup(c => c.GetAgentByCodeAsync(code, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("core-ohs no disponible"));

        // Act
        Func<Task> act = async () => await Sut.ExecuteAsync(code);

        // Assert
        await act.Should().ThrowAsync<CoreOhsUnavailableException>()
            .WithMessage($"*{code}*");
    }
}
