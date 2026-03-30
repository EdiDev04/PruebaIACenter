using Cotizador.API.Controllers;
using Cotizador.Application.DTOs;
using Cotizador.Application.Interfaces;
using Cotizador.Domain.Constants;
using Cotizador.Domain.Exceptions;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Cotizador.Tests.API.Controllers;

public class QuoteControllerLayoutTests
{
    private readonly Mock<IGetGeneralInfoUseCase> _mockGetGeneralInfoUseCase = new();
    private readonly Mock<IUpdateGeneralInfoUseCase> _mockUpdateGeneralInfoUseCase = new();
    private readonly Mock<IValidator<UpdateGeneralInfoRequest>> _mockGeneralInfoValidator = new();
    private readonly Mock<IGetLayoutUseCase> _mockGetLayoutUseCase = new();
    private readonly Mock<IUpdateLayoutUseCase> _mockUpdateLayoutUseCase = new();
    private readonly Mock<IValidator<UpdateLayoutRequest>> _mockLayoutValidator = new();
    private readonly Mock<IGetLocationsUseCase> _mockGetLocationsUseCase = new();
    private readonly Mock<IUpdateLocationsUseCase> _mockUpdateLocationsUseCase = new();
    private readonly Mock<IPatchLocationUseCase> _mockPatchLocationUseCase = new();
    private readonly Mock<IGetLocationsSummaryUseCase> _mockGetLocationsSummaryUseCase = new();
    private readonly Mock<IValidator<UpdateLocationsRequest>> _mockUpdateLocationsValidator = new();
    private readonly Mock<IValidator<PatchLocationRequest>> _mockPatchLocationValidator = new();
    private readonly Mock<IGetCoverageOptionsUseCase> _mockGetCoverageOptionsUseCase = new();
    private readonly Mock<IUpdateCoverageOptionsUseCase> _mockUpdateCoverageOptionsUseCase = new();
    private readonly Mock<IValidator<UpdateCoverageOptionsRequest>> _mockCoverageOptionsValidator = new();
    private readonly Mock<IGetQuoteStateUseCase> _mockGetQuoteStateUseCase = new();
    private readonly Mock<ICalculateQuoteUseCase> _mockCalculateQuoteUseCase = new();
    private readonly Mock<IValidator<CalculateRequest>> _mockCalculateValidator = new();

    private QuoteController CreateController()
    {
        var controller = new QuoteController(
            _mockGetGeneralInfoUseCase.Object,
            _mockUpdateGeneralInfoUseCase.Object,
            _mockGeneralInfoValidator.Object,
            _mockGetLayoutUseCase.Object,
            _mockUpdateLayoutUseCase.Object,
            _mockLayoutValidator.Object,
            _mockGetLocationsUseCase.Object,
            _mockUpdateLocationsUseCase.Object,
            _mockPatchLocationUseCase.Object,
            _mockGetLocationsSummaryUseCase.Object,
            _mockUpdateLocationsValidator.Object,
            _mockPatchLocationValidator.Object,
            _mockGetCoverageOptionsUseCase.Object,
            _mockUpdateCoverageOptionsUseCase.Object,
            _mockCoverageOptionsValidator.Object,
            _mockGetQuoteStateUseCase.Object,
            _mockCalculateQuoteUseCase.Object,
            _mockCalculateValidator.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }

    private static LayoutConfigurationDto BuildLayoutDto(int version = 2) =>
        new(
            DisplayMode: "grid",
            VisibleColumns: new List<string> { "index", "locationName", "zipCode", "businessLine", "validationStatus" },
            Version: version
        );

    private static UpdateLayoutRequest BuildValidLayoutRequest(int version = 1) =>
        new(
            DisplayMode: "grid",
            VisibleColumns: new List<string> { "index", "locationName", "zipCode" },
            Version: version
        );

    private void SetupLayoutValidatorSuccess()
    {
        _mockLayoutValidator
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateLayoutRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    private void SetupLayoutValidatorFailure(string propertyName = "DisplayMode", string errorMessage = "Modo inválido")
    {
        _mockLayoutValidator
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateLayoutRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure(propertyName, errorMessage) }));
    }

    // ─── GET /v1/quotes/{folio}/locations/layout ───────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task GetLayout_Should_Return200_WhenFolioIsValid()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00001";
        _mockGetLayoutUseCase
            .Setup(uc => uc.ExecuteAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildLayoutDto());

        var controller = CreateController();

        // Act
        IActionResult result = await controller.GetLayoutAsync(folioNumber, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task GetLayout_Should_ReturnLayoutData_WhenFolioIsValid()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00002";
        var layoutDto = BuildLayoutDto(version: 3);

        _mockGetLayoutUseCase
            .Setup(uc => uc.ExecuteAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(layoutDto);

        var controller = CreateController();

        // Act
        IActionResult result = await controller.GetLayoutAsync(folioNumber, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value as dynamic;
        ((object)response!).Should().NotBeNull();
    }

    [Trait("Category", "Smoke")]
    [Theory]
    [InlineData("")]
    [InlineData("DAN-202-00001")]
    [InlineData("DAM-2024-00001")]
    [InlineData("DAN-2024-0001")]
    [InlineData("DAN-2024-000001")]
    [InlineData("dan-2024-00001")]
    public async Task GetLayout_Should_Return400_WhenFolioFormatIsInvalid(string invalidFolio)
    {
        // Arrange
        var controller = CreateController();

        // Act
        IActionResult result = await controller.GetLayoutAsync(invalidFolio, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task GetLayout_Should_PropagatesFolioNotFoundException_WhenFolioNotFound()
    {
        // Arrange
        // The ExceptionHandlingMiddleware converts FolioNotFoundException → HTTP 404.
        // In unit tests we verify the exception propagates from the controller.
        const string folioNumber = "DAN-2024-99999";

        _mockGetLayoutUseCase
            .Setup(uc => uc.ExecuteAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FolioNotFoundException(folioNumber));

        var controller = CreateController();

        // Act
        Func<Task> act = async () => await controller.GetLayoutAsync(folioNumber, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<FolioNotFoundException>()
            .Where(ex => ex.FolioNumber == folioNumber);
    }

    [Fact]
    public async Task GetLayout_Should_CallUseCaseWithFolioNumber_WhenFolioIsValid()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00003";
        _mockGetLayoutUseCase
            .Setup(uc => uc.ExecuteAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildLayoutDto());

        var controller = CreateController();

        // Act
        await controller.GetLayoutAsync(folioNumber, CancellationToken.None);

        // Assert
        _mockGetLayoutUseCase.Verify(
            uc => uc.ExecuteAsync(folioNumber, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetLayout_Should_NotCallUseCase_WhenFolioFormatIsInvalid()
    {
        // Arrange
        const string invalidFolio = "INVALID";
        var controller = CreateController();

        // Act
        await controller.GetLayoutAsync(invalidFolio, CancellationToken.None);

        // Assert
        _mockGetLayoutUseCase.Verify(
            uc => uc.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ─── PUT /v1/quotes/{folio}/locations/layout ───────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task PutLayout_Should_Return200_WhenRequestIsValid()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00001";
        var request = BuildValidLayoutRequest(version: 1);
        var updatedDto = BuildLayoutDto(version: 2);

        SetupLayoutValidatorSuccess();
        _mockUpdateLayoutUseCase
            .Setup(uc => uc.ExecuteAsync(folioNumber, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDto);

        var controller = CreateController();

        // Act
        IActionResult result = await controller.UpdateLayoutAsync(folioNumber, request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task PutLayout_Should_ReturnUpdatedLayoutData_WhenRequestIsValid()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00002";
        var request = BuildValidLayoutRequest(version: 1);
        var updatedDto = BuildLayoutDto(version: 2);

        SetupLayoutValidatorSuccess();
        _mockUpdateLayoutUseCase
            .Setup(uc => uc.ExecuteAsync(folioNumber, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDto);

        var controller = CreateController();

        // Act
        IActionResult result = await controller.UpdateLayoutAsync(folioNumber, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("DAN-202-00001")]
    [InlineData("DAM-2024-00001")]
    [InlineData("DAN-2024-0001")]
    [InlineData("DAN-2024-000001")]
    public async Task PutLayout_Should_Return400_WhenFolioFormatIsInvalid(string invalidFolio)
    {
        // Arrange
        var request = BuildValidLayoutRequest();
        var controller = CreateController();

        // Act
        IActionResult result = await controller.UpdateLayoutAsync(invalidFolio, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task PutLayout_Should_ThrowValidationException_WhenValidationFails()
    {
        // Arrange
        // The ExceptionHandlingMiddleware converts ValidationException → HTTP 400.
        // In unit tests we verify the exception propagates from the controller.
        const string folioNumber = "DAN-2024-00001";
        var request = BuildValidLayoutRequest();

        SetupLayoutValidatorFailure("DisplayMode", "Modo de visualización inválido");

        var controller = CreateController();

        // Act
        Func<Task> act = async () => await controller.UpdateLayoutAsync(folioNumber, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();

        _mockUpdateLayoutUseCase.Verify(
            uc => uc.ExecuteAsync(It.IsAny<string>(), It.IsAny<UpdateLayoutRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task PutLayout_Should_PropagateVersionConflictException_WhenVersionMismatch()
    {
        // Arrange
        // The ExceptionHandlingMiddleware converts VersionConflictException → HTTP 409.
        // In unit tests we verify the exception propagates from the controller.
        const string folioNumber = "DAN-2024-00001";
        var request = BuildValidLayoutRequest(version: 1);

        SetupLayoutValidatorSuccess();
        _mockUpdateLayoutUseCase
            .Setup(uc => uc.ExecuteAsync(folioNumber, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new VersionConflictException(folioNumber, request.Version));

        var controller = CreateController();

        // Act
        Func<Task> act = async () => await controller.UpdateLayoutAsync(folioNumber, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<VersionConflictException>()
            .Where(ex => ex.FolioNumber == folioNumber && ex.ExpectedVersion == request.Version);
    }

    [Fact]
    public async Task PutLayout_Should_PropagateFolioNotFoundException_WhenFolioNotFound()
    {
        // Arrange
        const string folioNumber = "DAN-2024-99999";
        var request = BuildValidLayoutRequest(version: 1);

        SetupLayoutValidatorSuccess();
        _mockUpdateLayoutUseCase
            .Setup(uc => uc.ExecuteAsync(folioNumber, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FolioNotFoundException(folioNumber));

        var controller = CreateController();

        // Act
        Func<Task> act = async () => await controller.UpdateLayoutAsync(folioNumber, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<FolioNotFoundException>()
            .Where(ex => ex.FolioNumber == folioNumber);
    }

    [Fact]
    public async Task PutLayout_Should_CallUseCaseWithCorrectArguments_WhenRequestIsValid()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00003";
        var request = BuildValidLayoutRequest(version: 2);

        SetupLayoutValidatorSuccess();
        _mockUpdateLayoutUseCase
            .Setup(uc => uc.ExecuteAsync(folioNumber, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildLayoutDto(version: 3));

        var controller = CreateController();

        // Act
        await controller.UpdateLayoutAsync(folioNumber, request, CancellationToken.None);

        // Assert
        _mockUpdateLayoutUseCase.Verify(
            uc => uc.ExecuteAsync(folioNumber, request, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PutLayout_Should_NotCallUseCase_WhenFolioFormatIsInvalid()
    {
        // Arrange
        const string invalidFolio = "INVALID-FOLIO";
        var request = BuildValidLayoutRequest();
        var controller = CreateController();

        // Act
        await controller.UpdateLayoutAsync(invalidFolio, request, CancellationToken.None);

        // Assert
        _mockUpdateLayoutUseCase.Verify(
            uc => uc.ExecuteAsync(It.IsAny<string>(), It.IsAny<UpdateLayoutRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
