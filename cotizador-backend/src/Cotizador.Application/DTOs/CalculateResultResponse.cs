namespace Cotizador.Application.DTOs;

public record CalculateResultResponse(
    decimal NetPremium,
    decimal CommercialPremiumBeforeTax,
    decimal CommercialPremium,
    List<LocationPremiumDto> PremiumsByLocation,
    string QuoteStatus,
    int Version
);
