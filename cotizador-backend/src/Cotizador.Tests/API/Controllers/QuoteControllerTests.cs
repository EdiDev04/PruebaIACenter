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

public class QuoteControllerTests
{
    private readonly Mock<IGetGeneralInfoUseCase> _mockGetGeneralInfoUseCase = new();
    private readonly Mock<IUpdateGeneralInfoUseCase> _mockUpdateGeneralInfoUseCase = new();
    private readonly Mock<IValidator<UpdateGeneralInfoRequest>> _mockValidator = new();

    private QuoteController CreateController()
    {
        var controller = new QuoteController(
            _mockGetGeneralInfoUseCase.Object,
            _mockUpdateGeneralInfoUseCase.Object,
            _mockValidator.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }

    private static GeneralInfoDto BuildGeneralInfoDto() =>
        new(
            InsuredData: new InsuredDataDto("Empresa Test S.A.", "ETT010101XYZ", "test@empresa.mx", "5512345678"),
            ConductionData: new ConductionDataDto("SUB-001", "Oficina Central", "Sucursal A"),
            AgentCode: "AGT-001",
            BusinessType: "commercial",
            RiskClassification: "A",
            Version: 2
        );

    private static UpdateGeneralInfoRequest BuildValidRequest(int version = 1) =>
        new(
            InsuredData: new InsuredDataDto("Empresa Test S.A.", "ETT010101XYZ", "test@empresa.mx", "5512345678"),
            ConductionData: new ConductionDataDto("SUB-001", "Oficina Central", "Sucursal A"),
            AgentCode: "AGT-001",
            BusinessType: "commercial",
            RiskClassification: "A",
            Version: version
        );

    private void SetupValidatorSuccess()
    {
        _mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateGeneralInfoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    private void SetupValidatorFailure(string propertyName = "AgentCode", string errorMessage = "Campo requerido")
    {
        _mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateGeneralInfoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure(propertyName, errorMessage) }));
    }

    // ─── GET /v1/quotes/{folio}/general-info ──────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("DAN-202-00001")]
    [InlineData("DAM-2024-00001")]
    [InlineData("DAN-2024-0001")]
    [InlineData("DAN-2024-000001")]
    [InlineData("dan-2024-00001")]
    public async Task GetGeneralInfoAsync_InvalidFolioFormat_Returns400(string invalidFolio)
    {
        // Arrange
        var controller = CreateController();

        // Act
        IActionResult result = await controller.GetGeneralInfoAsync(invalidFolio, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task GetGeneralInfoAsync_ValidFolio_Returns200WithDto()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00001";
        _mockGetGeneralInfoUseCase
            .Setup(uc => uc.ExecuteAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildGeneralInfoDto());

        var controller = CreateController();

        // Act
        IActionResult result = await controller.GetGeneralInfoAsync(folioNumber, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task GetGeneralInfoAsync_FolioNotFound_PropagatesFolioNotFoundException()
    {
        // Arrange
        // The ExceptionHandlingMiddleware converts FolioNotFoundException → HTTP 404.
        // In unit tests we verify the exception propagates from the controller.
        const string folioNumber = "DAN-2024-99999";

        _mockGetGeneralInfoUseCase
            .Setup(uc => uc.ExecuteAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FolioNotFoundException(folioNumber));

        var controller = CreateController();

        // Act
        Func<Task> act = async () => await controller.GetGeneralInfoAsync(folioNumber, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<FolioNotFoundException>()
            .Where(ex => ex.FolioNumber == folioNumber);
    }

    [Fact]
    public async Task GetGeneralInfoAsync_ValidFolio_CallsUseCaseWithFolioNumber()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00001";

        _mockGetGeneralInfoUseCase
            .Setup(uc => uc.ExecuteAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildGeneralInfoDto());

        var controller = CreateController();

        // Act
        await controller.GetGeneralInfoAsync(folioNumber, CancellationToken.None);

        // Assert
        _mockGetGeneralInfoUseCase.Verify(
            uc => uc.ExecuteAsync(folioNumber, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ─── PUT /v1/quotes/{folio}/general-info ──────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("DAN-202-00001")]
    [InlineData("DAM-2024-00001")]
    [InlineData("DAN-2024-0001")]
    [InlineData("DAN-2024-000001")]
    public async Task UpdateGeneralInfoAsync_InvalidFolioFormat_Returns400(string invalidFolio)
    {
        // Arrange
        var request = BuildValidRequest();
        var controller = CreateController();

        // Act
        IActionResult result = await controller.UpdateGeneralInfoAsync(invalidFolio, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task UpdateGeneralInfoAsync_ValidRequestAndFolio_Returns200WithUpdatedDto()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00001";
        var request = BuildValidRequest(version: 1);
        var updatedDto = BuildGeneralInfoDto();

        SetupValidatorSuccess();
        _mockUpdateGeneralInfoUseCase
            .Setup(uc => uc.ExecuteAsync(folioNumber, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDto);

        var controller = CreateController();

        // Act
        IActionResult result = await controller.UpdateGeneralInfoAsync(folioNumber, request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task UpdateGeneralInfoAsync_InvalidRequest_ThrowsValidationException()
    {
        // Arrange
        // The ExceptionHandlingMiddleware converts ValidationException → HTTP 400.
        // In unit tests we verify the exception propagates from the controller.
        const string folioNumber = "DAN-2024-00001";
        var request = BuildValidRequest();

        SetupValidatorFailure("InsuredData.Name", "El nombre es requerido");

        var controller = CreateController();

        // Act
        Func<Task> act = async () => await controller.UpdateGeneralInfoAsync(folioNumber, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();

        _mockUpdateGeneralInfoUseCase.Verify(
            uc => uc.ExecuteAsync(It.IsAny<string>(), It.IsAny<UpdateGeneralInfoRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task UpdateGeneralInfoAsync_VersionConflict_PropagatesVersionConflictException()
    {
        // Arrange
        // The ExceptionHandlingMiddleware converts VersionConflictException → HTTP 409.
        // In unit tests we verify the exception propagates from the controller.
        const string folioNumber = "DAN-2024-00001";
        var request = BuildValidRequest(version: 1);

        SetupValidatorSuccess();
        _mockUpdateGeneralInfoUseCase
            .Setup(uc => uc.ExecuteAsync(folioNumber, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new VersionConflictException(folioNumber, request.Version));

        var controller = CreateController();

        // Act
        Func<Task> act = async () => await controller.UpdateGeneralInfoAsync(folioNumber, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<VersionConflictException>()
            .Where(ex => ex.FolioNumber == folioNumber && ex.ExpectedVersion == request.Version);
    }

    [Fact]
    public async Task UpdateGeneralInfoAsync_ValidRequest_CallsUseCaseWithCorrectArguments()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00001";
        var request = BuildValidRequest(version: 2);

        SetupValidatorSuccess();
        _mockUpdateGeneralInfoUseCase
            .Setup(uc => uc.ExecuteAsync(folioNumber, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildGeneralInfoDto());

        var controller = CreateController();

        // Act
        await controller.UpdateGeneralInfoAsync(folioNumber, request, CancellationToken.None);

        // Assert
        _mockUpdateGeneralInfoUseCase.Verify(
            uc => uc.ExecuteAsync(folioNumber, request, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateGeneralInfoAsync_FolioNotFound_PropagatesFolioNotFoundException()
    {
        // Arrange
        const string folioNumber = "DAN-2024-99999";
        var request = BuildValidRequest();

        SetupValidatorSuccess();
        _mockUpdateGeneralInfoUseCase
            .Setup(uc => uc.ExecuteAsync(folioNumber, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FolioNotFoundException(folioNumber));

        var controller = CreateController();

        // Act
        Func<Task> act = async () => await controller.UpdateGeneralInfoAsync(folioNumber, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<FolioNotFoundException>()
            .Where(ex => ex.FolioNumber == folioNumber);
    }

    [Fact]
    public async Task UpdateGeneralInfoAsync_InvalidFolioFormat_DoesNotCallValidator()
    {
        // Arrange
        const string invalidFolio = "INVALID-FOLIO";
        var request = BuildValidRequest();
        var controller = CreateController();

        // Act
        await controller.UpdateGeneralInfoAsync(invalidFolio, request, CancellationToken.None);

        // Assert
        _mockValidator.Verify(
            v => v.ValidateAsync(It.IsAny<UpdateGeneralInfoRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
