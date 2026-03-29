namespace Cotizador.Application.DTOs;

public record QuoteMetadataDto(
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string CreatedBy,
    int LastWizardStep
);
