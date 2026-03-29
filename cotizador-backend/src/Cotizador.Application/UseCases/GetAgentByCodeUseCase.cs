using Cotizador.Application.DTOs;
using Cotizador.Application.Interfaces;
using Cotizador.Application.Ports;
using Cotizador.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Cotizador.Application.UseCases;

public class GetAgentByCodeUseCase : IGetAgentByCodeUseCase
{
    private readonly ICoreOhsClient _coreOhsClient;
    private readonly ILogger<GetAgentByCodeUseCase> _logger;

    public GetAgentByCodeUseCase(ICoreOhsClient coreOhsClient, ILogger<GetAgentByCodeUseCase> logger)
    {
        _coreOhsClient = coreOhsClient;
        _logger = logger;
    }

    public async Task<AgentDto?> ExecuteAsync(string code, CancellationToken ct = default)
    {
        _logger.LogInformation("Ejecutando {UseCase} para agente {Code}", nameof(GetAgentByCodeUseCase), code);

        try
        {
            return await _coreOhsClient.GetAgentByCodeAsync(code, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new CoreOhsUnavailableException($"No se pudo obtener el agente {code} desde core-ohs.", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            throw new CoreOhsUnavailableException($"Timeout al obtener el agente {code} desde core-ohs.", ex);
        }
    }
}
