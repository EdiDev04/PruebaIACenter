using Cotizador.Application.DTOs;
using Cotizador.Application.Interfaces;
using Cotizador.Application.Ports;
using Microsoft.Extensions.Logging;

namespace Cotizador.Application.UseCases;

public class GetBusinessLinesUseCase : IGetBusinessLinesUseCase
{
    private readonly ICoreOhsClient _coreOhsClient;
    private readonly ILogger<GetBusinessLinesUseCase> _logger;

    public GetBusinessLinesUseCase(ICoreOhsClient coreOhsClient, ILogger<GetBusinessLinesUseCase> logger)
    {
        _coreOhsClient = coreOhsClient;
        _logger = logger;
    }

    public async Task<List<BusinessLineDto>> ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Consultando catálogo de giros en core-ohs");
        return await _coreOhsClient.GetBusinessLinesAsync(ct);
    }
}
