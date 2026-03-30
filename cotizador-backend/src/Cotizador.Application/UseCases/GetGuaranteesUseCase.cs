using Cotizador.Application.DTOs;
using Cotizador.Application.Interfaces;
using Cotizador.Application.Ports;
using Cotizador.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Cotizador.Application.UseCases;

public class GetGuaranteesUseCase : IGetGuaranteesUseCase
{
    private readonly ICoreOhsClient _coreOhsClient;
    private readonly ILogger<GetGuaranteesUseCase> _logger;

    public GetGuaranteesUseCase(ICoreOhsClient coreOhsClient, ILogger<GetGuaranteesUseCase> logger)
    {
        _coreOhsClient = coreOhsClient;
        _logger = logger;
    }

    public async Task<List<GuaranteeDto>> ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Ejecutando {UseCase}", nameof(GetGuaranteesUseCase));

        try
        {
            return await _coreOhsClient.GetGuaranteesAsync(ct);
        }
        catch (HttpRequestException ex)
        {
            throw new CoreOhsUnavailableException("No se pudo obtener el catálogo de garantías desde core-ohs.", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            throw new CoreOhsUnavailableException("Timeout al obtener el catálogo de garantías desde core-ohs.", ex);
        }
    }
}
