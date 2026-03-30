using Cotizador.Application.DTOs;
using Cotizador.Application.Interfaces;
using Cotizador.Application.Ports;
using Cotizador.Domain.Entities;
using Cotizador.Domain.Exceptions;
using Cotizador.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Cotizador.Application.UseCases;

public class UpdateLayoutUseCase : IUpdateLayoutUseCase
{
    private readonly IQuoteRepository _repository;
    private readonly ILogger<UpdateLayoutUseCase> _logger;

    public UpdateLayoutUseCase(IQuoteRepository repository, ILogger<UpdateLayoutUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<LayoutConfigurationDto> ExecuteAsync(
        string folioNumber,
        UpdateLayoutRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Ejecutando {UseCase} para folio {Folio}", nameof(UpdateLayoutUseCase), folioNumber);

        LayoutConfiguration layout = new()
        {
            DisplayMode = request.DisplayMode,
            VisibleColumns = request.VisibleColumns
        };

        // VersionConflictException and FolioNotFoundException are re-thrown to middleware
        await _repository.UpdateLayoutAsync(folioNumber, request.Version, layout, ct);

        PropertyQuote? updated = await _repository.GetByFolioNumberAsync(folioNumber, ct);
        if (updated is null)
            throw new FolioNotFoundException(folioNumber);

        return new LayoutConfigurationDto(
            updated.LayoutConfiguration.DisplayMode,
            updated.LayoutConfiguration.VisibleColumns,
            updated.Version
        );
    }
}
