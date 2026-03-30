namespace Cotizador.Application.DTOs;

public record UpdateCoverageOptionsRequest(
    List<string> EnabledGuarantees,
    decimal DeductiblePercentage,
    decimal CoinsurancePercentage,
    int Version
);
