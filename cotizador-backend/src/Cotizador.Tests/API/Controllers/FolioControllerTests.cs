using Cotizador.API.Controllers;
using Cotizador.Application.DTOs;
using Cotizador.Application.Interfaces;
using Cotizador.Domain.Constants;
using Cotizador.Domain.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Cotizador.Tests.API.Controllers;

public class FolioControllerTests
{
    private readonly Mock<ICreateFolioUseCase> _mockCreateFolioUseCase = new();
    private readonly Mock<IGetQuoteSummaryUseCase> _mockGetQuoteSummaryUseCase = new();

    private FolioController CreateController(HttpContext? httpContext = null)
    {
        var controller = new FolioController(
            _mockCreateFolioUseCase.Object,
            _mockGetQuoteSummaryUseCase.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext ?? new DefaultHttpContext()
        };

        return controller;
    }

    private static DefaultHttpContext BuildHttpContextWithIdempotencyKey(string idempotencyKey, string userName = "agent@company.mx")
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Idempotency-Key"] = idempotencyKey;
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, userName) }, "test");
        httpContext.User = new ClaimsPrincipal(identity);
        return httpContext;
    }

    private static QuoteSummaryDto BuildQuoteSummaryDto(string folioNumber) =>
        new(
            folioNumber,
            QuoteStatus.Draft,
            1,
            new QuoteMetadataDto(
                new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                "agent@company.mx",
                0
            )
        );

    // ─── POST /v1/folios ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateFolioAsync_MissingIdempotencyKeyHeader_Returns400()
    {
        // Arrange
        var controller = CreateController();

        // Act
        IActionResult result = await controller.CreateFolioAsync(CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task CreateFolioAsync_EmptyIdempotencyKeyHeader_Returns400()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Idempotency-Key"] = string.Empty;
        var controller = CreateController(httpContext);

        // Act
        IActionResult result = await controller.CreateFolioAsync(CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task CreateFolioAsync_WhitespaceIdempotencyKeyHeader_Returns400()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Idempotency-Key"] = "   ";
        var controller = CreateController(httpContext);

        // Act
        IActionResult result = await controller.CreateFolioAsync(CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task CreateFolioAsync_NewFolio_Returns201WithFolioData()
    {
        // Arrange
        const string idempotencyKey = "idem-key-new";
        const string folioNumber = "DAN-2024-00001";
        var dto = BuildQuoteSummaryDto(folioNumber);
        var httpContext = BuildHttpContextWithIdempotencyKey(idempotencyKey);

        _mockCreateFolioUseCase
            .Setup(uc => uc.ExecuteAsync(idempotencyKey, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((dto, IsNew: true));

        var controller = CreateController(httpContext);

        // Act
        IActionResult result = await controller.CreateFolioAsync(CancellationToken.None);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task CreateFolioAsync_ExistingFolio_Returns200WithFolioData()
    {
        // Arrange
        const string idempotencyKey = "idem-key-existing";
        const string folioNumber = "DAN-2024-00001";
        var dto = BuildQuoteSummaryDto(folioNumber);
        var httpContext = BuildHttpContextWithIdempotencyKey(idempotencyKey);

        _mockCreateFolioUseCase
            .Setup(uc => uc.ExecuteAsync(idempotencyKey, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((dto, IsNew: false));

        var controller = CreateController(httpContext);

        // Act
        IActionResult result = await controller.CreateFolioAsync(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task CreateFolioAsync_ValidRequest_PassesUserIdentityNameAsCreatedBy()
    {
        // Arrange
        const string idempotencyKey = "idem-key-user";
        const string userName = "specific.user@company.mx";
        var dto = BuildQuoteSummaryDto("DAN-2024-00001");
        var httpContext = BuildHttpContextWithIdempotencyKey(idempotencyKey, userName);

        _mockCreateFolioUseCase
            .Setup(uc => uc.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((dto, IsNew: true));

        var controller = CreateController(httpContext);

        // Act
        await controller.CreateFolioAsync(CancellationToken.None);

        // Assert
        _mockCreateFolioUseCase.Verify(
            uc => uc.ExecuteAsync(idempotencyKey, userName, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ─── GET /v1/quotes/{folio} ────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("DAN-202-00001")]
    [InlineData("DAM-2024-00001")]
    [InlineData("DAN-2024-0001")]
    [InlineData("DAN-2024-000001")]
    [InlineData("dan-2024-00001")]
    [InlineData("DAN_2024_00001")]
    public async Task GetQuoteSummaryAsync_InvalidFolioFormat_Returns400(string invalidFolio)
    {
        // Arrange
        var controller = CreateController();

        // Act
        IActionResult result = await controller.GetQuoteSummaryAsync(invalidFolio, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task GetQuoteSummaryAsync_ExistingFolio_Returns200WithDto()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00001";
        var dto = BuildQuoteSummaryDto(folioNumber);

        _mockGetQuoteSummaryUseCase
            .Setup(uc => uc.ExecuteAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var controller = CreateController();

        // Act
        IActionResult result = await controller.GetQuoteSummaryAsync(folioNumber, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task GetQuoteSummaryAsync_FolioNotFound_PropagatesFolioNotFoundException()
    {
        // Arrange
        // The ExceptionHandlingMiddleware converts FolioNotFoundException → HTTP 404.
        // In unit tests we verify the exception propagates from the controller.
        const string folioNumber = "DAN-2024-99999";

        _mockGetQuoteSummaryUseCase
            .Setup(uc => uc.ExecuteAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FolioNotFoundException(folioNumber));

        var controller = CreateController();

        // Act
        Func<Task> act = async () => await controller.GetQuoteSummaryAsync(folioNumber, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<FolioNotFoundException>()
            .Where(ex => ex.FolioNumber == folioNumber);
    }

    [Fact]
    public async Task GetQuoteSummaryAsync_ValidFolioFormat_CallsUseCaseWithFolioNumber()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00001";
        var dto = BuildQuoteSummaryDto(folioNumber);

        _mockGetQuoteSummaryUseCase
            .Setup(uc => uc.ExecuteAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var controller = CreateController();

        // Act
        await controller.GetQuoteSummaryAsync(folioNumber, CancellationToken.None);

        // Assert
        _mockGetQuoteSummaryUseCase.Verify(
            uc => uc.ExecuteAsync(folioNumber, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
