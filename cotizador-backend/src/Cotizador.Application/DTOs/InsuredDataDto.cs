namespace Cotizador.Application.DTOs;

public record InsuredDataDto(
    string Name,
    string TaxId,
    string? Email,
    string? Phone
);
