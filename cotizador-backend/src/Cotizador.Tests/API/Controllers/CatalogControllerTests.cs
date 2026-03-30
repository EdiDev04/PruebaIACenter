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
    private readonly Mock<IGetZipCodeUseCase> _mockGetZipCodeUseCase = new();
    private readonly Mock<IGetBusinessLinesUseCase> _mockGetBusinessLinesUseCase = new();

    private CatalogController CreateController()
    {
        var controller = new CatalogController(
            _mockGetSubscribersUseCase.Object,
            _mockGetAgentByCodeUseCase.Object,
            _mockGetRiskClassificationsUseCase.Object,
            _mockGetZipCodeUseCase.Object,
            _mockGetBusinessLinesUseCase.Object);

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

    // ─── GET /v1/zip-codes/{zipCode} ──────────────────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task GetZipCode_Should_Return200_WhenZipCodeFound()
    {
        // Arrange
        const string zipCode = "06600";
        var dto = new ZipCodeDto(zipCode, "Ciudad de México", "Cuauhtémoc", "Doctores", "Ciudad de México", "A", 1);

        _mockGetZipCodeUseCase
            .Setup(uc => uc.ExecuteAsync(zipCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var controller = CreateController();

        // Act
        IActionResult result = await controller.GetZipCodeAsync(zipCode, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Which;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        dynamic value = okResult.Value!;
        ZipCodeDto data = value.data;
        data.ZipCode.Should().Be(zipCode);
        data.State.Should().Be("Ciudad de México");
        data.CatZone.Should().Be("A");
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task GetZipCode_Should_Return404_WhenZipCodeNotFound()
    {
        // Arrange
        const string zipCode = "99999";

        _mockGetZipCodeUseCase
            .Setup(uc => uc.ExecuteAsync(zipCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ZipCodeDto?)null);

        var controller = CreateController();

        // Act
        IActionResult result = await controller.GetZipCodeAsync(zipCode, CancellationToken.None);

        // Assert
        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Which;
        notFound.StatusCode.Should().Be(StatusCodes.Status404NotFound);

        dynamic value = notFound.Value!;
        ((string)value.type).Should().Be("zipCodeNotFound");
    }

    [Fact]
    public async Task GetZipCode_Should_Return400_WhenZipCodeFormatInvalid()
    {
        // Arrange
        var controller = CreateController();

        // Act
        IActionResult result = await controller.GetZipCodeAsync("ABCDE", CancellationToken.None);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Which;
        badRequest.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        dynamic value = badRequest.Value!;
        ((string)value.type).Should().Be("validationError");
        ((string)value.field).Should().Be("zipCode");

        _mockGetZipCodeUseCase.Verify(
            uc => uc.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("1234")]   // 4 digits
    [InlineData("123456")] // 6 digits
    [InlineData("ABCDE")]  // non-numeric
    [InlineData("")]       // empty
    public async Task GetZipCode_Should_Return400_WhenZipCodeDoesNotMatchPattern(string invalidZipCode)
    {
        // Arrange
        var controller = CreateController();

        // Act
        IActionResult result = await controller.GetZipCodeAsync(invalidZipCode, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    // ─── GET /v1/business-lines ───────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task GetBusinessLines_Should_Return200_WhenUseCaseSucceeds()
    {
        // Arrange
        var businessLines = new List<BusinessLineDto>
        {
            new("BL-001", "Storage warehouse", "B-03", "bajo"),
            new("BL-002", "Office building", "A-01", "bajo"),
            new("BL-003", "Retail store", "C-05", "medio")
        };

        _mockGetBusinessLinesUseCase
            .Setup(uc => uc.ExecuteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(businessLines);

        var controller = CreateController();

        // Act
        IActionResult result = await controller.GetBusinessLinesAsync(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Which;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        dynamic value = okResult.Value!;
        IEnumerable<BusinessLineDto> data = value.data;
        data.Should().HaveCount(3);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task GetBusinessLines_Should_PropagateException_WhenCoreOhsUnavailable()
    {
        // Arrange
        _mockGetBusinessLinesUseCase
            .Setup(uc => uc.ExecuteAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CoreOhsUnavailableException("core-ohs no disponible"));

        var controller = CreateController();

        // Act
        Func<Task> act = async () => await controller.GetBusinessLinesAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<CoreOhsUnavailableException>();
    }

    [Fact]
    public async Task GetBusinessLines_Should_ReturnEmptyList_WhenCatalogIsEmpty()
    {
        // Arrange
        _mockGetBusinessLinesUseCase
            .Setup(uc => uc.ExecuteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BusinessLineDto>());

        var controller = CreateController();

        // Act
        IActionResult result = await controller.GetBusinessLinesAsync(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Which;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        dynamic value = okResult.Value!;
        IEnumerable<BusinessLineDto> data = value.data;
        data.Should().BeEmpty();
    }
}
