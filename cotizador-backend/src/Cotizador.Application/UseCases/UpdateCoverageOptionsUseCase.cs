using Cotizador.Application.DTOs;
using Cotizador.Application.Interfaces;
using Cotizador.Application.Ports;
using Cotizador.Domain.Exceptions;
using Cotizador.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Cotizador.Application.UseCases;

public class UpdateCoverageOptionsUseCase : IUpdateCoverageOptionsUseCase
{
    private readonly IQuoteRepository _repository;
    private readonly ILogger<UpdateCoverageOptionsUseCase> _logger;

    public UpdateCoverageOptionsUseCase(IQuoteRepository repository, ILogger<UpdateCoverageOptionsUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<CoverageOptionsDto> ExecuteAsync(
        string folioNumber,
        UpdateCoverageOptionsRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Ejecutando {UseCase} para folio {Folio}", nameof(UpdateCoverageOptionsUseCase), folioNumber);

        // Verify folio exists
        var existing = await _repository.GetByFolioNumberAsync(folioNumber, ct);
        if (existing is null)
            throw new FolioNotFoundException(folioNumber);

        // Build value object from request
        var coverageOptions = new CoverageOptions
        {
            EnabledGuarantees = request.EnabledGuarantees,
            DeductiblePercentage = request.DeductiblePercentage,
            CoinsurancePercentage = request.CoinsurancePercentage
        };

        // Persist — throws VersionConflictException if version mismatch
        await _repository.UpdateCoverageOptionsAsync(folioNumber, request.Version, coverageOptions, ct);

        // Re-read to get updated version
        var updated = await _repository.GetByFolioNumberAsync(folioNumber, ct);
        if (updated is null)
            throw new FolioNotFoundException(folioNumber);

        return new CoverageOptionsDto(
            updated.CoverageOptions.EnabledGuarantees,
            updated.CoverageOptions.DeductiblePercentage,
            updated.CoverageOptions.CoinsurancePercentage,
            updated.Version
        );
    }
}
