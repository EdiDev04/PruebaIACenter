using Cotizador.Application.DTOs;
using Cotizador.Application.Ports;
using Cotizador.Application.UseCases;
using Cotizador.Domain.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cotizador.Tests.Application.UseCases;

public class GetSubscribersUseCaseTests
{
    private readonly Mock<ICoreOhsClient> _mockCoreOhsClient = new();
    private readonly Mock<ILogger<GetSubscribersUseCase>> _mockLogger = new();

    private GetSubscribersUseCase Sut => new(_mockCoreOhsClient.Object, _mockLogger.Object);

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_Should_ReturnSubscriberList_WhenCoreOhsResponds()
    {
        // Arrange
        var subscribers = new List<SubscriberDto>
        {
            new("SUB-001", "Suscriptor Uno", "Oficina A", true),
            new("SUB-002", "Suscriptor Dos", "Oficina B", false)
        };

        _mockCoreOhsClient
            .Setup(c => c.GetSubscribersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscribers);

        // Act
        List<SubscriberDto> result = await Sut.ExecuteAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].Code.Should().Be("SUB-001");
        result[0].Name.Should().Be("Suscriptor Uno");
        result[0].Office.Should().Be("Oficina A");
        result[0].Active.Should().BeTrue();
        result[1].Code.Should().Be("SUB-002");
        result[1].Active.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task ExecuteAsync_Should_ThrowCoreOhsUnavailableException_WhenClientThrows()
    {
        // Arrange
        _mockCoreOhsClient
            .Setup(c => c.GetSubscribersAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("core-ohs no disponible"));

        // Act
        Func<Task> act = async () => await Sut.ExecuteAsync();

        // Assert
        await act.Should().ThrowAsync<CoreOhsUnavailableException>()
            .WithMessage("*suscriptores*");
    }
}
