using Cotizador.Application.DTOs;
using Cotizador.Application.Interfaces;
using Cotizador.Application.Ports;
using Cotizador.Domain.Entities;
using Cotizador.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Cotizador.Application.UseCases;

public class GetLayoutUseCase : IGetLayoutUseCase
{
    private readonly IQuoteRepository _repository;
    private readonly ILogger<GetLayoutUseCase> _logger;

    public GetLayoutUseCase(IQuoteRepository repository, ILogger<GetLayoutUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<LayoutConfigurationDto> ExecuteAsync(string folioNumber, CancellationToken ct = default)
    {
        _logger.LogInformation("Ejecutando {UseCase} para folio {Folio}", nameof(GetLayoutUseCase), folioNumber);

        PropertyQuote? quote = await _repository.GetByFolioNumberAsync(folioNumber, ct);
        if (quote is null)
            throw new FolioNotFoundException(folioNumber);

        return new LayoutConfigurationDto(
            quote.LayoutConfiguration.DisplayMode,
            quote.LayoutConfiguration.VisibleColumns,
            quote.Version
        );
    }
}
