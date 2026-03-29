using Cotizador.Application.DTOs;
using Cotizador.Application.Interfaces;
using Cotizador.Application.Ports;
using Cotizador.Domain.Entities;
using Cotizador.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Cotizador.Application.UseCases;

public class GetQuoteSummaryUseCase : IGetQuoteSummaryUseCase
{
    private readonly IQuoteRepository _repository;
    private readonly ILogger<GetQuoteSummaryUseCase> _logger;

    public GetQuoteSummaryUseCase(IQuoteRepository repository, ILogger<GetQuoteSummaryUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<QuoteSummaryDto> ExecuteAsync(string folioNumber, CancellationToken ct = default)
    {
        _logger.LogInformation("Ejecutando {UseCase} para folio {Folio}", nameof(GetQuoteSummaryUseCase), folioNumber);

        PropertyQuote? quote = await _repository.GetByFolioNumberAsync(folioNumber, ct);
        if (quote is null)
            throw new FolioNotFoundException(folioNumber);

        return MapToDto(quote);
    }

    private static QuoteSummaryDto MapToDto(PropertyQuote quote) =>
        new(
            quote.FolioNumber,
            quote.QuoteStatus,
            quote.Version,
            new QuoteMetadataDto(
                quote.Metadata.CreatedAt,
                quote.Metadata.UpdatedAt,
                quote.Metadata.CreatedBy,
                quote.Metadata.LastWizardStep
            )
        );
}
