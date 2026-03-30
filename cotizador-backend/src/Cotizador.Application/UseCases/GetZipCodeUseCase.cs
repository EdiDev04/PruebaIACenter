using Cotizador.Application.DTOs;
using Cotizador.Application.Interfaces;
using Cotizador.Application.Ports;
using Microsoft.Extensions.Logging;

namespace Cotizador.Application.UseCases;

public class GetZipCodeUseCase : IGetZipCodeUseCase
{
    private readonly ICoreOhsClient _coreOhsClient;
    private readonly ILogger<GetZipCodeUseCase> _logger;

    public GetZipCodeUseCase(ICoreOhsClient coreOhsClient, ILogger<GetZipCodeUseCase> logger)
    {
        _coreOhsClient = coreOhsClient;
        _logger = logger;
    }

    public async Task<ZipCodeDto?> ExecuteAsync(string zipCode, CancellationToken ct = default)
    {
        _logger.LogDebug("Consultando código postal {ZipCode} en core-ohs", zipCode);
        return await _coreOhsClient.GetZipCodeAsync(zipCode, ct);
    }
}
