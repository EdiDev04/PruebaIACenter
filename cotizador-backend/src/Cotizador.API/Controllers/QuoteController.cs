using System.Text.RegularExpressions;
using Cotizador.Application.DTOs;
using Cotizador.Application.Interfaces;
using Cotizador.Domain.Constants;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cotizador.API.Controllers;

[ApiController]
[Authorize]
[Route("v1/quotes")]
public class QuoteController : ControllerBase
{

    private readonly IGetGeneralInfoUseCase _getGeneralInfoUseCase;
    private readonly IUpdateGeneralInfoUseCase _updateGeneralInfoUseCase;
    private readonly IValidator<UpdateGeneralInfoRequest> _generalInfoValidator;
    private readonly IGetLayoutUseCase _getLayoutUseCase;
    private readonly IUpdateLayoutUseCase _updateLayoutUseCase;
    private readonly IValidator<UpdateLayoutRequest> _layoutValidator;
    private readonly IGetLocationsUseCase _getLocationsUseCase;
    private readonly IUpdateLocationsUseCase _updateLocationsUseCase;
    private readonly IPatchLocationUseCase _patchLocationUseCase;
    private readonly IGetLocationsSummaryUseCase _getLocationsSummaryUseCase;
    private readonly IValidator<UpdateLocationsRequest> _updateLocationsValidator;
    private readonly IValidator<PatchLocationRequest> _patchLocationValidator;
    private readonly IGetCoverageOptionsUseCase _getCoverageOptionsUseCase;
    private readonly IUpdateCoverageOptionsUseCase _updateCoverageOptionsUseCase;
    private readonly IValidator<UpdateCoverageOptionsRequest> _coverageOptionsValidator;
    private readonly IGetQuoteStateUseCase _getQuoteStateUseCase;

    public QuoteController(
        IGetGeneralInfoUseCase getGeneralInfoUseCase,
        IUpdateGeneralInfoUseCase updateGeneralInfoUseCase,
        IValidator<UpdateGeneralInfoRequest> generalInfoValidator,
        IGetLayoutUseCase getLayoutUseCase,
        IUpdateLayoutUseCase updateLayoutUseCase,
        IValidator<UpdateLayoutRequest> layoutValidator,
        IGetLocationsUseCase getLocationsUseCase,
        IUpdateLocationsUseCase updateLocationsUseCase,
        IPatchLocationUseCase patchLocationUseCase,
        IGetLocationsSummaryUseCase getLocationsSummaryUseCase,
        IValidator<UpdateLocationsRequest> updateLocationsValidator,
        IValidator<PatchLocationRequest> patchLocationValidator,
        IGetCoverageOptionsUseCase getCoverageOptionsUseCase,
        IUpdateCoverageOptionsUseCase updateCoverageOptionsUseCase,
        IValidator<UpdateCoverageOptionsRequest> coverageOptionsValidator,
        IGetQuoteStateUseCase getQuoteStateUseCase)
    {
        _getGeneralInfoUseCase = getGeneralInfoUseCase;
        _updateGeneralInfoUseCase = updateGeneralInfoUseCase;
        _generalInfoValidator = generalInfoValidator;
        _getLayoutUseCase = getLayoutUseCase;
        _updateLayoutUseCase = updateLayoutUseCase;
        _layoutValidator = layoutValidator;
        _getLocationsUseCase = getLocationsUseCase;
        _updateLocationsUseCase = updateLocationsUseCase;
        _patchLocationUseCase = patchLocationUseCase;
        _getLocationsSummaryUseCase = getLocationsSummaryUseCase;
        _updateLocationsValidator = updateLocationsValidator;
        _patchLocationValidator = patchLocationValidator;
        _getCoverageOptionsUseCase = getCoverageOptionsUseCase;
        _updateCoverageOptionsUseCase = updateCoverageOptionsUseCase;
        _coverageOptionsValidator = coverageOptionsValidator;
        _getQuoteStateUseCase = getQuoteStateUseCase;
    }

    /// <summary>GET /v1/quotes/{folio}/general-info — Obtiene la información general de una cotización.</summary>
    [HttpGet("{folio}/general-info")]
    public async Task<IActionResult> GetGeneralInfoAsync(string folio, CancellationToken ct)
    {
        if (!Regex.IsMatch(folio, FolioConstants.FolioPattern))
        {
            return BadRequest(new
            {
                type = "validationError",
                message = "Formato de folio inválido. Use DAN-YYYY-NNNNN",
                field = "folio"
            });
        }

        var dto = await _getGeneralInfoUseCase.ExecuteAsync(folio, ct);
        return Ok(new { data = dto });
    }

    /// <summary>PUT /v1/quotes/{folio}/general-info — Actualiza la información general de una cotización.</summary>
    [HttpPut("{folio}/general-info")]
    public async Task<IActionResult> UpdateGeneralInfoAsync(
        string folio,
        [FromBody] UpdateGeneralInfoRequest request,
        CancellationToken ct)
    {
        if (!Regex.IsMatch(folio, FolioConstants.FolioPattern))
        {
            return BadRequest(new
            {
                type = "validationError",
                message = "Formato de folio inválido. Use DAN-YYYY-NNNNN",
                field = "folio"
            });
        }

        await _generalInfoValidator.ValidateAndThrowAsync(request, ct);

        var dto = await _updateGeneralInfoUseCase.ExecuteAsync(folio, request, ct);
        return Ok(new { data = dto });
    }

    /// <summary>GET /v1/quotes/{folio}/locations/layout — Obtiene la configuración de layout de ubicaciones.</summary>
    [HttpGet("{folio}/locations/layout")]
    public async Task<IActionResult> GetLayoutAsync(string folio, CancellationToken ct)
    {
        if (!Regex.IsMatch(folio, FolioConstants.FolioPattern))
        {
            return BadRequest(new
            {
                type = "validationError",
                message = "Formato de folio inválido. Use DAN-YYYY-NNNNN",
                field = "folio"
            });
        }

        var dto = await _getLayoutUseCase.ExecuteAsync(folio, ct);
        return Ok(new { data = dto });
    }

    /// <summary>PUT /v1/quotes/{folio}/locations/layout — Actualiza la configuración de layout de ubicaciones.</summary>
    [HttpPut("{folio}/locations/layout")]
    public async Task<IActionResult> UpdateLayoutAsync(
        string folio,
        [FromBody] UpdateLayoutRequest request,
        CancellationToken ct)
    {
        if (!Regex.IsMatch(folio, FolioConstants.FolioPattern))
        {
            return BadRequest(new
            {
                type = "validationError",
                message = "Formato de folio inválido. Use DAN-YYYY-NNNNN",
                field = "folio"
            });
        }

        await _layoutValidator.ValidateAndThrowAsync(request, ct);

        var dto = await _updateLayoutUseCase.ExecuteAsync(folio, request, ct);
        return Ok(new { data = dto });
    }

    /// <summary>GET /v1/quotes/{folio}/locations — Obtiene las ubicaciones de riesgo de una cotización.</summary>
    [HttpGet("{folio}/locations")]
    public async Task<IActionResult> GetLocationsAsync(string folio, CancellationToken ct)
    {
        if (!Regex.IsMatch(folio, FolioConstants.FolioPattern))
        {
            return BadRequest(new
            {
                type = "validationError",
                message = "Formato de folio inválido. Use DAN-YYYY-NNNNN",
                field = "folio"
            });
        }

        var result = await _getLocationsUseCase.ExecuteAsync(folio, ct);
        return Ok(new { data = result });
    }

    /// <summary>PUT /v1/quotes/{folio}/locations — Reemplaza el array completo de ubicaciones del folio.</summary>
    [HttpPut("{folio}/locations")]
    public async Task<IActionResult> UpdateLocationsAsync(
        string folio,
        [FromBody] UpdateLocationsRequest request,
        CancellationToken ct)
    {
        if (!Regex.IsMatch(folio, FolioConstants.FolioPattern))
        {
            return BadRequest(new
            {
                type = "validationError",
                message = "Formato de folio inválido. Use DAN-YYYY-NNNNN",
                field = "folio"
            });
        }

        await _updateLocationsValidator.ValidateAndThrowAsync(request, ct);

        var result = await _updateLocationsUseCase.ExecuteAsync(folio, request, ct);
        return Ok(new { data = result });
    }

    /// <summary>PATCH /v1/quotes/{folio}/locations/{index} — Actualiza una ubicación específica del folio.</summary>
    [HttpPatch("{folio}/locations/{index:int}")]
    public async Task<IActionResult> PatchLocationAsync(
        string folio,
        int index,
        [FromBody] PatchLocationRequest request,
        CancellationToken ct)
    {
        if (!Regex.IsMatch(folio, FolioConstants.FolioPattern))
        {
            return BadRequest(new
            {
                type = "validationError",
                message = "Formato de folio inválido. Use DAN-YYYY-NNNNN",
                field = "folio"
            });
        }

        if (index < 1)
        {
            return BadRequest(new
            {
                type = "validationError",
                message = "El índice de ubicación debe ser mayor o igual a 1",
                field = "index"
            });
        }

        await _patchLocationValidator.ValidateAndThrowAsync(request, ct);

        var result = await _patchLocationUseCase.ExecuteAsync(folio, index, request, ct);
        return Ok(new { data = result });
    }

    /// <summary>GET /v1/quotes/{folio}/locations/summary — Obtiene el resumen de validación de ubicaciones.</summary>
    [HttpGet("{folio}/locations/summary")]
    public async Task<IActionResult> GetLocationsSummaryAsync(string folio, CancellationToken ct)
    {
        if (!Regex.IsMatch(folio, FolioConstants.FolioPattern))
        {
            return BadRequest(new
            {
                type = "validationError",
                message = "Formato de folio inválido. Use DAN-YYYY-NNNNN",
                field = "folio"
            });
        }

        var result = await _getLocationsSummaryUseCase.ExecuteAsync(folio, ct);
        return Ok(new { data = result });
    }

    /// <summary>GET /v1/quotes/{folio}/coverage-options — Obtiene las opciones de cobertura de una cotización.</summary>
    [HttpGet("{folio}/coverage-options")]
    public async Task<IActionResult> GetCoverageOptionsAsync(string folio, CancellationToken ct)
    {
        if (!Regex.IsMatch(folio, FolioConstants.FolioPattern))
        {
            return BadRequest(new
            {
                type = "validationError",
                message = "Formato de folio inválido. Use DAN-YYYY-NNNNN",
                field = "folio"
            });
        }

        var dto = await _getCoverageOptionsUseCase.ExecuteAsync(folio, ct);
        return Ok(new { data = dto });
    }

    /// <summary>GET /v1/quotes/{folio}/state — Obtiene el estado completo del folio: progreso por sección, ubicaciones, flag de calculabilidad y resultado financiero.</summary>
    [HttpGet("{folio}/state")]
    public async Task<IActionResult> GetQuoteStateAsync(string folio, CancellationToken ct)
    {
        if (!Regex.IsMatch(folio, FolioConstants.FolioPattern))
        {
            return BadRequest(new
            {
                type = "validationError",
                message = "Formato de folio inválido. Use DAN-YYYY-NNNNN",
                field = "folio"
            });
        }

        var dto = await _getQuoteStateUseCase.ExecuteAsync(folio, ct);
        return Ok(new { data = dto });
    }

    /// <summary>PUT /v1/quotes/{folio}/coverage-options — Actualiza las opciones de cobertura de una cotización.</summary>
    [HttpPut("{folio}/coverage-options")]
    public async Task<IActionResult> UpdateCoverageOptionsAsync(
        string folio,
        [FromBody] UpdateCoverageOptionsRequest request,
        CancellationToken ct)
    {
        if (!Regex.IsMatch(folio, FolioConstants.FolioPattern))
        {
            return BadRequest(new
            {
                type = "validationError",
                message = "Formato de folio inválido. Use DAN-YYYY-NNNNN",
                field = "folio"
            });
        }

        await _coverageOptionsValidator.ValidateAndThrowAsync(request, ct);

        var dto = await _updateCoverageOptionsUseCase.ExecuteAsync(folio, request, ct);
        return Ok(new { data = dto });
    }
}
