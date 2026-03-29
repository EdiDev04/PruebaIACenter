namespace Cotizador.Application.DTOs;

public record QuoteSummaryDto(
    string FolioNumber,
    string QuoteStatus,
    int Version,
    QuoteMetadataDto Metadata
);
