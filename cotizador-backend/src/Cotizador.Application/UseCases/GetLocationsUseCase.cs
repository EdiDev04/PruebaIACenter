using Cotizador.Application.DTOs;
using Cotizador.Application.Interfaces;
using Cotizador.Application.Ports;
using Cotizador.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Cotizador.Application.UseCases;

public class GetLocationsUseCase : IGetLocationsUseCase
{
    private readonly IQuoteRepository _repository;
    private readonly ILogger<GetLocationsUseCase> _logger;

    public GetLocationsUseCase(IQuoteRepository repository, ILogger<GetLocationsUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<LocationsResponse> ExecuteAsync(string folioNumber, CancellationToken ct = default)
    {
        _logger.LogInformation("Ejecutando {UseCase} para folio {Folio}", nameof(GetLocationsUseCase), folioNumber);

        var quote = await _repository.GetByFolioNumberAsync(folioNumber, ct);
        if (quote is null)
            throw new FolioNotFoundException(folioNumber);

        var locationDtos = quote.Locations
            .Select(LocationMapper.ToDto)
            .ToList();

        return new LocationsResponse(locationDtos, quote.Version);
    }
}
