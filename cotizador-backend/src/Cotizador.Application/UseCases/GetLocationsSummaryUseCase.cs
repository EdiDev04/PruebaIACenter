using Cotizador.Application.DTOs;
using Cotizador.Application.Interfaces;
using Cotizador.Application.Ports;
using Cotizador.Domain.Constants;
using Cotizador.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Cotizador.Application.UseCases;

public class GetLocationsSummaryUseCase : IGetLocationsSummaryUseCase
{
    private readonly IQuoteRepository _repository;
    private readonly ILogger<GetLocationsSummaryUseCase> _logger;

    public GetLocationsSummaryUseCase(IQuoteRepository repository, ILogger<GetLocationsSummaryUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<LocationsSummaryResponse> ExecuteAsync(string folioNumber, CancellationToken ct = default)
    {
        _logger.LogInformation("Ejecutando {UseCase} para folio {Folio}", nameof(GetLocationsSummaryUseCase), folioNumber);

        var quote = await _repository.GetByFolioNumberAsync(folioNumber, ct);
        if (quote is null)
            throw new FolioNotFoundException(folioNumber);

        var summaries = quote.Locations
            .Select(l => new LocationSummaryDto(
                l.Index,
                l.LocationName,
                l.ValidationStatus,
                l.BlockingAlerts))
            .ToList();

        int totalCalculable = summaries.Count(s => s.ValidationStatus == ValidationStatus.Calculable);
        int totalIncomplete = summaries.Count(s => s.ValidationStatus == ValidationStatus.Incomplete);

        return new LocationsSummaryResponse(summaries, totalCalculable, totalIncomplete, quote.Version);
    }
}
