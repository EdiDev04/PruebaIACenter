using Cotizador.Application.DTOs;
using Cotizador.Application.Interfaces;
using Cotizador.Application.Ports;
using Cotizador.Domain.Constants;
using Cotizador.Domain.Entities;
using Cotizador.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Cotizador.Application.UseCases;

public class CreateFolioUseCase : ICreateFolioUseCase
{
    private readonly IQuoteRepository _repository;
    private readonly ICoreOhsClient _coreOhsClient;
    private readonly ILogger<CreateFolioUseCase> _logger;

    public CreateFolioUseCase(
        IQuoteRepository repository,
        ICoreOhsClient coreOhsClient,
        ILogger<CreateFolioUseCase> logger)
    {
        _repository = repository;
        _coreOhsClient = coreOhsClient;
        _logger = logger;
    }

    public async Task<(QuoteSummaryDto Dto, bool IsNew)> ExecuteAsync(
        string idempotencyKey,
        string createdBy,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Ejecutando {UseCase} con idempotencyKey {Key}", nameof(CreateFolioUseCase), idempotencyKey);

        PropertyQuote? existing = await _repository.GetByIdempotencyKeyAsync(idempotencyKey, ct);
        if (existing is not null)
        {
            _logger.LogInformation("Folio existente encontrado para idempotencyKey {Key}: {Folio}", idempotencyKey, existing.FolioNumber);
            return (MapToDto(existing), IsNew: false);
        }

        FolioDto folioDto = await _coreOhsClient.GenerateFolioAsync(ct);

        DateTime now = DateTime.UtcNow;
        PropertyQuote newQuote = new()
        {
            FolioNumber = folioDto.FolioNumber,
            QuoteStatus = QuoteStatus.Draft,
            Version = 1,
            Metadata = new QuoteMetadata
            {
                IdempotencyKey = idempotencyKey,
                CreatedBy = createdBy,
                CreatedAt = now,
                UpdatedAt = now,
                LastWizardStep = 0
            }
        };

        await _repository.CreateAsync(newQuote, ct);

        _logger.LogInformation("Folio creado: {Folio}", newQuote.FolioNumber);
        return (MapToDto(newQuote), IsNew: true);
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
