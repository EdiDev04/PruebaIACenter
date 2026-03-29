using System.Text.RegularExpressions;
using Cotizador.Application.Interfaces;
using Cotizador.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cotizador.API.Controllers;

[ApiController]
[Authorize]
[Route("v1")]
public class FolioController : ControllerBase
{

    private readonly ICreateFolioUseCase _createFolioUseCase;
    private readonly IGetQuoteSummaryUseCase _getQuoteSummaryUseCase;

    public FolioController(
        ICreateFolioUseCase createFolioUseCase,
        IGetQuoteSummaryUseCase getQuoteSummaryUseCase)
    {
        _createFolioUseCase = createFolioUseCase;
        _getQuoteSummaryUseCase = getQuoteSummaryUseCase;
    }

    /// <summary>POST /v1/folios — Genera un nuevo folio de cotización.</summary>
    [HttpPost("folios")]
    public async Task<IActionResult> CreateFolioAsync(CancellationToken ct)
    {
        if (!Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKeyValues)
            || string.IsNullOrWhiteSpace(idempotencyKeyValues.FirstOrDefault()))
        {
            return BadRequest(new
            {
                type = "validationError",
                message = "El header Idempotency-Key es obligatorio",
                field = "Idempotency-Key"
            });
        }

        string idempotencyKey = idempotencyKeyValues.First()!;
        string createdBy = HttpContext.User.Identity?.Name ?? string.Empty;

        (var dto, bool isNew) = await _createFolioUseCase.ExecuteAsync(idempotencyKey, createdBy, ct);

        if (isNew)
            return StatusCode(StatusCodes.Status201Created, new { data = dto });

        return Ok(new { data = dto });
    }

    /// <summary>GET /v1/quotes/{folio} — Obtiene el resumen de una cotización.</summary>
    [HttpGet("quotes/{folio}")]
    public async Task<IActionResult> GetQuoteSummaryAsync(string folio, CancellationToken ct)
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

        var dto = await _getQuoteSummaryUseCase.ExecuteAsync(folio, ct);
        return Ok(new { data = dto });
    }
}
