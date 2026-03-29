using Cotizador.Application.DTOs;
using Cotizador.Application.Interfaces;
using Cotizador.Application.Ports;
using Cotizador.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Cotizador.Application.UseCases;

public class GetRiskClassificationsUseCase : IGetRiskClassificationsUseCase
{
    private readonly ICoreOhsClient _coreOhsClient;
    private readonly ILogger<GetRiskClassificationsUseCase> _logger;

    public GetRiskClassificationsUseCase(ICoreOhsClient coreOhsClient, ILogger<GetRiskClassificationsUseCase> logger)
    {
        _coreOhsClient = coreOhsClient;
        _logger = logger;
    }

    public async Task<List<RiskClassificationDto>> ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Ejecutando {UseCase}", nameof(GetRiskClassificationsUseCase));

        try
        {
            return await _coreOhsClient.GetRiskClassificationsAsync(ct);
        }
        catch (HttpRequestException ex)
        {
            throw new CoreOhsUnavailableException("No se pudo obtener el catálogo de clasificaciones de riesgo desde core-ohs.", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            throw new CoreOhsUnavailableException("Timeout al obtener el catálogo de clasificaciones de riesgo desde core-ohs.", ex);
        }
    }
}
