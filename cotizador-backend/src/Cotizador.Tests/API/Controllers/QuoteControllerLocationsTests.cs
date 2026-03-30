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

/// <summary>
/// Tests for the four location action methods added to QuoteController in SPEC-006:
/// GET /locations, PUT /locations, PATCH /locations/{index}, GET /locations/summary.
/// </summary>
public class QuoteControllerLocationsTests
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
            _mockPatchLocationValidator.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static LocationsResponse BuildLocationsResponse(int version = 2) =>
        new(
            Locations: new List<LocationDto>
            {
                new(
                    Index: 1, LocationName: "Bodega Principal", Address: "Av. Test 1",
                    ZipCode: "06600", State: "CDMX", Municipality: "Cuauhtémoc",
                    Neighborhood: "Doctores", City: "CDMX", ConstructionType: "Tipo 1",
                    Level: 2, ConstructionYear: 1998,
                    LocationBusinessLine: new BusinessLineDto("BL-001", "Storage", "B-03", "bajo"),
                    Guarantees: new List<LocationGuaranteeDto> { new(GuaranteeKeys.BuildingFire, 5_000_000m) },
                    CatZone: "A", BlockingAlerts: new List<string>(),
                    ValidationStatus: ValidationStatus.Calculable)
            },
            Version: version);

    private static UpdateLocationsRequest BuildValidUpdateRequest(int version = 1) =>
        new(
            Locations: new List<LocationDto>
            {
                new(
                    Index: 1, LocationName: "Bodega", Address: "Av. Test 1",
                    ZipCode: "06600", State: "CDMX", Municipality: "Cuauhtémoc",
                    Neighborhood: "Doctores", City: "CDMX", ConstructionType: "Tipo 1",
                    Level: 2, ConstructionYear: 1998,
                    LocationBusinessLine: new BusinessLineDto("BL-001", "Storage", "B-03", "bajo"),
                    Guarantees: new List<LocationGuaranteeDto> { new(GuaranteeKeys.BuildingFire, 5_000_000m) },
                    CatZone: "A", BlockingAlerts: new List<string>(),
                    ValidationStatus: ValidationStatus.Incomplete)
            },
            Version: version);

    private static PatchLocationRequest BuildValidPatchRequest(int version = 1) =>
        new(
            LocationName: "Bodega Actualizada",
            Address: null, ZipCode: null, State: null, Municipality: null,
            Neighborhood: null, City: null, ConstructionType: null,
            Level: null, ConstructionYear: null, LocationBusinessLine: null,
            Guarantees: null, CatZone: null, Version: version);

    private static SingleLocationResponse BuildSingleLocationResponse(int version = 2) =>
        new(
            Index: 1, LocationName: "Bodega Actualizada", Address: "Av. Test 1",
            ZipCode: "06600", State: "CDMX", Municipality: "Cuauhtémoc",
            Neighborhood: "Doctores", City: "CDMX", ConstructionType: "Tipo 1",
            Level: 2, ConstructionYear: 1998,
            LocationBusinessLine: new BusinessLineDto("BL-001", "Storage", "B-03", "bajo"),
            Guarantees: new List<LocationGuaranteeDto> { new(GuaranteeKeys.BuildingFire, 5_000_000m) },
            CatZone: "A", BlockingAlerts: new List<string>(),
            ValidationStatus: ValidationStatus.Calculable,
            Version: version);

    private static LocationsSummaryResponse BuildSummaryResponse(int version = 3) =>
        new(
            Locations: new List<LocationSummaryDto>
            {
                new(1, "Bodega Principal", ValidationStatus.Calculable, new List<string>()),
                new(2, "Oficina Sur", ValidationStatus.Incomplete, new List<string> { "Código postal requerido" })
            },
            TotalCalculable: 1,
            TotalIncomplete: 1,
            Version: version);

    private void SetupUpdateLocationsValidatorSuccess()
    {
        _mockUpdateLocationsValidator
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateLocationsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    private void SetupUpdateLocationsValidatorFailure()
    {
        _mockUpdateLocationsValidator
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateLocationsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(
                new[] { new ValidationFailure("Locations[0].Index", "El índice de ubicación es obligatorio") }));
    }

    private void SetupPatchLocationValidatorSuccess()
    {
        _mockPatchLocationValidator
            .Setup(v => v.ValidateAsync(It.IsAny<PatchLocationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    private void SetupPatchLocationValidatorFailure()
    {
        _mockPatchLocationValidator
            .Setup(v => v.ValidateAsync(It.IsAny<PatchLocationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(
                new[] { new ValidationFailure("Version", "Conflicto de versión") }));
    }

    // ─── GET /v1/quotes/{folio}/locations ─────────────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task GetLocationsAsync_ValidFolio_Returns200WithLocationsResponse()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00001";
        var response = BuildLocationsResponse(version: 5);

        _mockGetLocationsUseCase
            .Setup(uc => uc.ExecuteAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var controller = CreateController();

        // Act
        IActionResult result = await controller.GetLocationsAsync(folioNumber, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Which;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        dynamic value = okResult.Value!;
        LocationsResponse data = value.data;
        data.Locations.Should().HaveCount(1);
        data.Version.Should().Be(5);
    }

    [Theory]
    [InlineData("")]
    [InlineData("DAN-202-00001")]
    [InlineData("DAM-2024-00001")]
    [InlineData("DAN-2024-0001")]
    [InlineData("DAN-2024-000001")]
    public async Task GetLocationsAsync_InvalidFolioFormat_Returns400(string invalidFolio)
    {
        // Arrange
        var controller = CreateController();

        // Act
        IActionResult result = await controller.GetLocationsAsync(invalidFolio, CancellationToken.None);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Which;
        badRequest.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        _mockGetLocationsUseCase.Verify(
            uc => uc.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task GetLocationsAsync_FolioNotFound_PropagatesFolioNotFoundException()
    {
        // Arrange
        const string folioNumber = "DAN-2024-99999";

        _mockGetLocationsUseCase
            .Setup(uc => uc.ExecuteAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FolioNotFoundException(folioNumber));

        var controller = CreateController();

        // Act
        Func<Task> act = async () => await controller.GetLocationsAsync(folioNumber, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<FolioNotFoundException>()
            .Where(ex => ex.FolioNumber == folioNumber);
    }

    // ─── PUT /v1/quotes/{folio}/locations ─────────────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task UpdateLocationsAsync_ValidRequest_Returns200WithLocationsResponse()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00001";
        var request = BuildValidUpdateRequest(version: 3);
        var response = BuildLocationsResponse(version: 4);

        SetupUpdateLocationsValidatorSuccess();
        _mockUpdateLocationsUseCase
            .Setup(uc => uc.ExecuteAsync(folioNumber, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var controller = CreateController();

        // Act
        IActionResult result = await controller.UpdateLocationsAsync(folioNumber, request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Which;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Theory]
    [InlineData("")]
    [InlineData("DAN-202-00001")]
    [InlineData("DAM-2024-00001")]
    public async Task UpdateLocationsAsync_InvalidFolioFormat_Returns400(string invalidFolio)
    {
        // Arrange
        var request = BuildValidUpdateRequest();
        var controller = CreateController();

        // Act
        IActionResult result = await controller.UpdateLocationsAsync(invalidFolio, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        _mockUpdateLocationsUseCase.Verify(
            uc => uc.ExecuteAsync(It.IsAny<string>(), It.IsAny<UpdateLocationsRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task UpdateLocationsAsync_InvalidRequest_ThrowsValidationException()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00001";
        var request = BuildValidUpdateRequest();

        SetupUpdateLocationsValidatorFailure();

        var controller = CreateController();

        // Act
        Func<Task> act = async () => await controller.UpdateLocationsAsync(folioNumber, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();

        _mockUpdateLocationsUseCase.Verify(
            uc => uc.ExecuteAsync(It.IsAny<string>(), It.IsAny<UpdateLocationsRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateLocationsAsync_FolioNotFound_PropagatesFolioNotFoundException()
    {
        // Arrange
        const string folioNumber = "DAN-2024-99999";
        var request = BuildValidUpdateRequest();

        SetupUpdateLocationsValidatorSuccess();
        _mockUpdateLocationsUseCase
            .Setup(uc => uc.ExecuteAsync(folioNumber, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FolioNotFoundException(folioNumber));

        var controller = CreateController();

        // Act
        Func<Task> act = async () => await controller.UpdateLocationsAsync(folioNumber, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<FolioNotFoundException>()
            .Where(ex => ex.FolioNumber == folioNumber);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task UpdateLocationsAsync_VersionConflict_PropagatesVersionConflictException()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00001";
        var request = BuildValidUpdateRequest(version: 1);

        SetupUpdateLocationsValidatorSuccess();
        _mockUpdateLocationsUseCase
            .Setup(uc => uc.ExecuteAsync(folioNumber, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new VersionConflictException(folioNumber, request.Version));

        var controller = CreateController();

        // Act
        Func<Task> act = async () => await controller.UpdateLocationsAsync(folioNumber, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<VersionConflictException>()
            .Where(ex => ex.FolioNumber == folioNumber && ex.ExpectedVersion == request.Version);
    }

    [Fact]
    public async Task UpdateLocationsAsync_ValidRequest_CallsUseCaseWithCorrectArguments()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00001";
        var request = BuildValidUpdateRequest(version: 7);

        SetupUpdateLocationsValidatorSuccess();
        _mockUpdateLocationsUseCase
            .Setup(uc => uc.ExecuteAsync(folioNumber, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildLocationsResponse());

        var controller = CreateController();

        // Act
        await controller.UpdateLocationsAsync(folioNumber, request, CancellationToken.None);

        // Assert
        _mockUpdateLocationsUseCase.Verify(
            uc => uc.ExecuteAsync(folioNumber, request, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ─── PATCH /v1/quotes/{folio}/locations/{index} ───────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task PatchLocationAsync_ValidRequest_Returns200WithSingleLocationResponse()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00001";
        const int index = 1;
        var request = BuildValidPatchRequest(version: 2);
        var response = BuildSingleLocationResponse(version: 3);

        SetupPatchLocationValidatorSuccess();
        _mockPatchLocationUseCase
            .Setup(uc => uc.ExecuteAsync(folioNumber, index, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var controller = CreateController();

        // Act
        IActionResult result = await controller.PatchLocationAsync(folioNumber, index, request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Which;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Theory]
    [InlineData("")]
    [InlineData("DAN-202-00001")]
    [InlineData("DAM-2024-00001")]
    public async Task PatchLocationAsync_InvalidFolioFormat_Returns400(string invalidFolio)
    {
        // Arrange
        var request = BuildValidPatchRequest();
        var controller = CreateController();

        // Act
        IActionResult result = await controller.PatchLocationAsync(invalidFolio, 1, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        _mockPatchLocationUseCase.Verify(
            uc => uc.ExecuteAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<PatchLocationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-99)]
    public async Task PatchLocationAsync_IndexLessThanOne_Returns400(int invalidIndex)
    {
        // Arrange
        const string folioNumber = "DAN-2024-00001";
        var request = BuildValidPatchRequest();
        var controller = CreateController();

        // Act
        IActionResult result = await controller.PatchLocationAsync(folioNumber, invalidIndex, request, CancellationToken.None);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Which;
        badRequest.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        dynamic value = badRequest.Value!;
        ((string)value.type).Should().Be("validationError");
        ((string)value.field).Should().Be("index");
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task PatchLocationAsync_InvalidRequest_ThrowsValidationException()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00001";
        var request = BuildValidPatchRequest();

        SetupPatchLocationValidatorFailure();

        var controller = CreateController();

        // Act
        Func<Task> act = async () => await controller.PatchLocationAsync(folioNumber, 1, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();

        _mockPatchLocationUseCase.Verify(
            uc => uc.ExecuteAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<PatchLocationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PatchLocationAsync_FolioNotFound_PropagatesFolioNotFoundException()
    {
        // Arrange
        const string folioNumber = "DAN-2024-99999";
        const int index = 1;
        var request = BuildValidPatchRequest();

        SetupPatchLocationValidatorSuccess();
        _mockPatchLocationUseCase
            .Setup(uc => uc.ExecuteAsync(folioNumber, index, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FolioNotFoundException(folioNumber));

        var controller = CreateController();

        // Act
        Func<Task> act = async () => await controller.PatchLocationAsync(folioNumber, index, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<FolioNotFoundException>()
            .Where(ex => ex.FolioNumber == folioNumber);
    }

    [Fact]
    public async Task PatchLocationAsync_VersionConflict_PropagatesVersionConflictException()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00001";
        const int index = 1;
        var request = BuildValidPatchRequest(version: 2);

        SetupPatchLocationValidatorSuccess();
        _mockPatchLocationUseCase
            .Setup(uc => uc.ExecuteAsync(folioNumber, index, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new VersionConflictException(folioNumber, request.Version));

        var controller = CreateController();

        // Act
        Func<Task> act = async () => await controller.PatchLocationAsync(folioNumber, index, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<VersionConflictException>()
            .Where(ex => ex.FolioNumber == folioNumber && ex.ExpectedVersion == request.Version);
    }

    // ─── GET /v1/quotes/{folio}/locations/summary ─────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task GetLocationsSummaryAsync_ValidFolio_Returns200WithSummary()
    {
        // Arrange
        const string folioNumber = "DAN-2024-00001";
        var summary = BuildSummaryResponse(version: 5);

        _mockGetLocationsSummaryUseCase
            .Setup(uc => uc.ExecuteAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        var controller = CreateController();

        // Act
        IActionResult result = await controller.GetLocationsSummaryAsync(folioNumber, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Which;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        dynamic value = okResult.Value!;
        LocationsSummaryResponse data = value.data;
        data.TotalCalculable.Should().Be(1);
        data.TotalIncomplete.Should().Be(1);
        data.Version.Should().Be(5);
    }

    [Theory]
    [InlineData("")]
    [InlineData("DAN-202-00001")]
    [InlineData("DAM-2024-00001")]
    public async Task GetLocationsSummaryAsync_InvalidFolioFormat_Returns400(string invalidFolio)
    {
        // Arrange
        var controller = CreateController();

        // Act
        IActionResult result = await controller.GetLocationsSummaryAsync(invalidFolio, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        _mockGetLocationsSummaryUseCase.Verify(
            uc => uc.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetLocationsSummaryAsync_FolioNotFound_PropagatesFolioNotFoundException()
    {
        // Arrange
        const string folioNumber = "DAN-2024-99999";

        _mockGetLocationsSummaryUseCase
            .Setup(uc => uc.ExecuteAsync(folioNumber, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FolioNotFoundException(folioNumber));

        var controller = CreateController();

        // Act
        Func<Task> act = async () => await controller.GetLocationsSummaryAsync(folioNumber, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<FolioNotFoundException>()
            .Where(ex => ex.FolioNumber == folioNumber);
    }
}
