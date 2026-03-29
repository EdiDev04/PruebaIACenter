using Cotizador.API.Controllers;
using Cotizador.Application.DTOs;
using Cotizador.Application.Interfaces;
using Cotizador.Domain.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Cotizador.Tests.API.Controllers;

public class CatalogControllerTests
{
    private readonly Mock<IGetSubscribersUseCase> _mockGetSubscribersUseCase = new();
    private readonly Mock<IGetAgentByCodeUseCase> _mockGetAgentByCodeUseCase = new();
    private readonly Mock<IGetRiskClassificationsUseCase> _mockGetRiskClassificationsUseCase = new();

    private CatalogController CreateController()
    {
        var controller = new CatalogController(
            _mockGetSubscribersUseCase.Object,
            _mockGetAgentByCodeUseCase.Object,
            _mockGetRiskClassificationsUseCase.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }

    // ─── GET /v1/subscribers ──────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task GetSubscribers_Should_Return200_WhenUseCaseSucceeds()
    {
        // Arrange
        var subscribers = new List<SubscriberDto>
        {
            new("SUB-001", "Suscriptor Uno", "Oficina A", true),
            new("SUB-002", "Suscriptor Dos", "Oficina B", false)
        };

        _mockGetSubscribersUseCase
            .Setup(uc => uc.ExecuteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscribers);

        var controller = CreateController();

        // Act
        IActionResult result = await controller.GetSubscribersAsync(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Which;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        dynamic value = okResult.Value!;
        IEnumerable<SubscriberDto> data = value.data;
        data.Should().HaveCount(2);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task GetSubscribers_Should_PropagateException_WhenCoreOhsUnavailable()
    {
        // Arrange
        _mockGetSubscribersUseCase
            .Setup(uc => uc.ExecuteAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CoreOhsUnavailableException("core-ohs no disponible"));

        var controller = CreateController();

        // Act
        Func<Task> act = async () => await controller.GetSubscribersAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<CoreOhsUnavailableException>();
    }

    // ─── GET /v1/agents?code= ─────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task GetAgentByCode_Should_Return200_WhenAgentExists()
    {
        // Arrange
        const string code = "AGT-001";
        var agent = new AgentDto(code, "Agente Ejemplo", "Norte", true);

        _mockGetAgentByCodeUseCase
            .Setup(uc => uc.ExecuteAsync(code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        var controller = CreateController();

        // Act
        IActionResult result = await controller.GetAgentByCodeAsync(code, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Which;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        dynamic value = okResult.Value!;
        AgentDto data = value.data;
        data.Code.Should().Be(code);
        data.Name.Should().Be("Agente Ejemplo");
    }

    [Fact]
    public async Task GetAgentByCode_Should_Return404_WhenAgentNotFound()
    {
        // Arrange
        const string code = "AGT-999";

        _mockGetAgentByCodeUseCase
            .Setup(uc => uc.ExecuteAsync(code, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AgentDto?)null);

        var controller = CreateController();

        // Act
        IActionResult result = await controller.GetAgentByCodeAsync(code, CancellationToken.None);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Which;
        notFoundResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);

        dynamic value = notFoundResult.Value!;
        ((string)value.type).Should().Be("agentNotFound");
    }

    [Fact]
    public async Task GetAgentByCode_Should_Return400_WhenCodeFormatInvalid()
    {
        // Arrange
        const string invalidCode = "INVALID";
        var controller = CreateController();

        // Act
        IActionResult result = await controller.GetAgentByCodeAsync(invalidCode, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Which;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        dynamic value = badRequestResult.Value!;
        ((string)value.type).Should().Be("validationError");
        ((string)value.field).Should().Be("code");

        _mockGetAgentByCodeUseCase.Verify(
            uc => uc.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ─── GET /v1/catalogs/risk-classification ─────────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task GetRiskClassifications_Should_Return200_WhenUseCaseSucceeds()
    {
        // Arrange
        var classifications = new List<RiskClassificationDto>
        {
            new("A", "Riesgo Bajo", 1.0m),
            new("B", "Riesgo Medio", 1.5m)
        };

        _mockGetRiskClassificationsUseCase
            .Setup(uc => uc.ExecuteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(classifications);

        var controller = CreateController();

        // Act
        IActionResult result = await controller.GetRiskClassificationsAsync(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Which;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        dynamic value = okResult.Value!;
        IEnumerable<RiskClassificationDto> data = value.data;
        data.Should().HaveCount(2);
    }
}
