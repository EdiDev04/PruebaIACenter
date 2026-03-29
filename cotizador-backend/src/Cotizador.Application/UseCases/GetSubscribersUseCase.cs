using Cotizador.Application.DTOs;
using Cotizador.Application.Interfaces;
using Cotizador.Application.Ports;
using Cotizador.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Cotizador.Application.UseCases;

public class GetSubscribersUseCase : IGetSubscribersUseCase
{
    private readonly ICoreOhsClient _coreOhsClient;
    private readonly ILogger<GetSubscribersUseCase> _logger;

    public GetSubscribersUseCase(ICoreOhsClient coreOhsClient, ILogger<GetSubscribersUseCase> logger)
    {
        _coreOhsClient = coreOhsClient;
        _logger = logger;
    }

    public async Task<List<SubscriberDto>> ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Ejecutando {UseCase}", nameof(GetSubscribersUseCase));

        try
        {
            return await _coreOhsClient.GetSubscribersAsync(ct);
        }
        catch (HttpRequestException ex)
        {
            throw new CoreOhsUnavailableException("No se pudo obtener el catálogo de suscriptores desde core-ohs.", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            throw new CoreOhsUnavailableException("Timeout al obtener el catálogo de suscriptores desde core-ohs.", ex);
        }
    }
}
