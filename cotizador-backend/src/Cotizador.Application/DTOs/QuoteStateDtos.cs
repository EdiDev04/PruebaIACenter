namespace Cotizador.Application.DTOs;

public record QuoteStateDto(
    string FolioNumber,
    string QuoteStatus,
    int Version,
    ProgressDto Progress,
    LocationsStateDto Locations,
    bool ReadyForCalculation,
    CalculationResultDto? CalculationResult
);

public record ProgressDto(
    bool GeneralInfo,
    bool LayoutConfiguration,
    bool Locations,
    bool CoverageOptions
);

public record LocationsStateDto(
    int Total,
    int Calculable,
    int Incomplete,
    List<LocationAlertDto> Alerts
);

public record LocationAlertDto(
    int Index,
    string LocationName,
    List<string> MissingFields
);

public record CalculationResultDto(
    decimal NetPremium,
    decimal CommercialPremiumBeforeTax,
    decimal CommercialPremium,
    List<LocationPremiumDto> PremiumsByLocation
);

public record LocationPremiumDto(
    int LocationIndex,
    string LocationName,
    decimal NetPremium,
    string ValidationStatus,
    List<CoveragePremiumDto> CoveragePremiums
);

public record CoveragePremiumDto(
    string GuaranteeKey,
    decimal InsuredAmount,
    decimal Rate,
    decimal Premium
);
