namespace Cotizador.Application.DTOs;

public record CoverageOptionsDto(
    List<string> EnabledGuarantees,
    decimal DeductiblePercentage,
    decimal CoinsurancePercentage,
    int Version
);
