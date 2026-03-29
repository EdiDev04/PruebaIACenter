using System.Text.RegularExpressions;
using Cotizador.Application.DTOs;
using Cotizador.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cotizador.API.Controllers;

[ApiController]
[Authorize]
[Route("v1")]
public class CatalogController : ControllerBase
{
    private static readonly Regex AgentCodeRegex = new(@"^AGT-\d{3}$", RegexOptions.Compiled);

    private readonly IGetSubscribersUseCase _getSubscribersUseCase;
    private readonly IGetAgentByCodeUseCase _getAgentByCodeUseCase;
    private readonly IGetRiskClassificationsUseCase _getRiskClassificationsUseCase;

    public CatalogController(
        IGetSubscribersUseCase getSubscribersUseCase,
        IGetAgentByCodeUseCase getAgentByCodeUseCase,
        IGetRiskClassificationsUseCase getRiskClassificationsUseCase)
    {
        _getSubscribersUseCase = getSubscribersUseCase;
        _getAgentByCodeUseCase = getAgentByCodeUseCase;
        _getRiskClassificationsUseCase = getRiskClassificationsUseCase;
    }

    /// <summary>GET /v1/subscribers — Obtiene el catálogo de suscriptores desde core-ohs.</summary>
    [HttpGet("subscribers")]
    public async Task<IActionResult> GetSubscribersAsync(CancellationToken ct)
    {
        List<SubscriberDto> subscribers = await _getSubscribersUseCase.ExecuteAsync(ct);
        return Ok(new { data = subscribers });
    }

    /// <summary>GET /v1/agents?code=AGT-001 — Obtiene un agente por código desde core-ohs.</summary>
    [HttpGet("agents")]
    public async Task<IActionResult> GetAgentByCodeAsync([FromQuery] string code, CancellationToken ct)
    {
        if (!AgentCodeRegex.IsMatch(code))
        {
            return BadRequest(new
            {
                type = "validationError",
                message = "Código de agente inválido",
                field = "code"
            });
        }

        AgentDto? agent = await _getAgentByCodeUseCase.ExecuteAsync(code, ct);

        if (agent is null)
        {
            return NotFound(new
            {
                type = "agentNotFound",
                message = $"El agente {code} no está registrado en el catálogo",
                field = (string?)null
            });
        }

        return Ok(new { data = agent });
    }

    /// <summary>GET /v1/catalogs/risk-classification — Obtiene el catálogo de clasificaciones de riesgo desde core-ohs.</summary>
    [HttpGet("catalogs/risk-classification")]
    public async Task<IActionResult> GetRiskClassificationsAsync(CancellationToken ct)
    {
        List<RiskClassificationDto> classifications = await _getRiskClassificationsUseCase.ExecuteAsync(ct);
        return Ok(new { data = classifications });
    }
}
