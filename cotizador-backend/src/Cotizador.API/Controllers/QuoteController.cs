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
    private readonly IValidator<UpdateGeneralInfoRequest> _validator;

    public QuoteController(
        IGetGeneralInfoUseCase getGeneralInfoUseCase,
        IUpdateGeneralInfoUseCase updateGeneralInfoUseCase,
        IValidator<UpdateGeneralInfoRequest> validator)
    {
        _getGeneralInfoUseCase = getGeneralInfoUseCase;
        _updateGeneralInfoUseCase = updateGeneralInfoUseCase;
        _validator = validator;
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

        await _validator.ValidateAndThrowAsync(request, ct);

        var dto = await _updateGeneralInfoUseCase.ExecuteAsync(folio, request, ct);
        return Ok(new { data = dto });
    }
}
