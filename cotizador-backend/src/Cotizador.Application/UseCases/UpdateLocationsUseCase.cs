using Cotizador.Application.DTOs;
using Cotizador.Application.Interfaces;
using Cotizador.Application.Ports;
using Cotizador.Domain.Entities;
using Cotizador.Domain.Exceptions;
using Cotizador.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Cotizador.Application.UseCases;

public class UpdateLocationsUseCase : IUpdateLocationsUseCase
{
    private readonly IQuoteRepository _repository;
    private readonly ILogger<UpdateLocationsUseCase> _logger;

    public UpdateLocationsUseCase(IQuoteRepository repository, ILogger<UpdateLocationsUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<LocationsResponse> ExecuteAsync(
        string folioNumber,
        UpdateLocationsRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Ejecutando {UseCase} para folio {Folio}", nameof(UpdateLocationsUseCase), folioNumber);

        // 1. Verify folio exists
        var quote = await _repository.GetByFolioNumberAsync(folioNumber, ct);
        if (quote is null)
            throw new FolioNotFoundException(folioNumber);

        // 2. Map DTOs → entities and evaluate calculability
        var locations = (request.Locations ?? new List<LocationDto>())
            .Select(dto =>
            {
                var entity = LocationMapper.ToEntity(dto);
                LocationCalculabilityEvaluator.Evaluate(entity);
                return entity;
            })
            .ToList();

        // 3. Persist (throws VersionConflictException on mismatch)
        await _repository.UpdateLocationsAsync(folioNumber, request.Version, locations, ct);

        // 4. Re-read to get updated version
        var updated = await _repository.GetByFolioNumberAsync(folioNumber, ct);
        if (updated is null)
            throw new FolioNotFoundException(folioNumber);

        var locationDtos = updated.Locations.Select(LocationMapper.ToDto).ToList();
        return new LocationsResponse(locationDtos, updated.Version);
    }
}
