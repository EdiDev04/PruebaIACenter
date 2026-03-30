using Cotizador.Application.DTOs;
using Cotizador.Application.Interfaces;
using Cotizador.Application.Ports;
using Cotizador.Domain.Exceptions;
using Cotizador.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Cotizador.Application.UseCases;

public class PatchLocationUseCase : IPatchLocationUseCase
{
    private readonly IQuoteRepository _repository;
    private readonly ILogger<PatchLocationUseCase> _logger;

    public PatchLocationUseCase(IQuoteRepository repository, ILogger<PatchLocationUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<SingleLocationResponse> ExecuteAsync(
        string folioNumber,
        int index,
        PatchLocationRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Ejecutando {UseCase} para folio {Folio}, índice {Index}",
            nameof(PatchLocationUseCase), folioNumber, index);

        // 1. Verify folio exists
        var quote = await _repository.GetByFolioNumberAsync(folioNumber, ct);
        if (quote is null)
            throw new FolioNotFoundException(folioNumber);

        // 2. Find existing location by index (1-based)
        var existing = quote.Locations.FirstOrDefault(l => l.Index == index);
        if (existing is null)
            throw new FolioNotFoundException($"La ubicación con índice {index} no existe en el folio");

        // 3. Apply non-null patch fields to a copy of the existing location
        if (request.LocationName is not null) existing.LocationName = request.LocationName;
        if (request.Address is not null) existing.Address = request.Address;
        if (request.ZipCode is not null) existing.ZipCode = request.ZipCode;
        if (request.State is not null) existing.State = request.State;
        if (request.Municipality is not null) existing.Municipality = request.Municipality;
        if (request.Neighborhood is not null) existing.Neighborhood = request.Neighborhood;
        if (request.City is not null) existing.City = request.City;
        if (request.ConstructionType is not null) existing.ConstructionType = request.ConstructionType;
        if (request.Level.HasValue) existing.Level = request.Level.Value;
        if (request.ConstructionYear.HasValue) existing.ConstructionYear = request.ConstructionYear.Value;
        if (request.CatZone is not null) existing.CatZone = request.CatZone;

        if (request.LocationBusinessLine is not null)
        {
            existing.BusinessLine = new BusinessLine
            {
                Description = request.LocationBusinessLine.Description ?? string.Empty,
                FireKey = request.LocationBusinessLine.FireKey ?? string.Empty
            };
        }

        if (request.Guarantees is not null)
        {
            existing.Guarantees = request.Guarantees
                .Select(g => new LocationGuarantee { GuaranteeKey = g.GuaranteeKey, InsuredAmount = g.InsuredAmount })
                .ToList();
        }

        // 4. Re-evaluate calculability on the merged location
        LocationCalculabilityEvaluator.Evaluate(existing);

        // 5. Persist the patched location (throws VersionConflictException on mismatch)
        await _repository.PatchLocationAsync(folioNumber, request.Version, index, existing, ct);

        // 6. Re-read to get updated version
        var updated = await _repository.GetByFolioNumberAsync(folioNumber, ct);
        if (updated is null)
            throw new FolioNotFoundException(folioNumber);

        var updatedLocation = updated.Locations.FirstOrDefault(l => l.Index == index);
        if (updatedLocation is null)
            throw new FolioNotFoundException($"La ubicación con índice {index} no existe en el folio");

        return LocationMapper.ToSingleResponse(updatedLocation, updated.Version);
    }
}
