namespace Cotizador.Application.DTOs;

public record GeneralInfoDto(
    InsuredDataDto InsuredData,
    ConductionDataDto ConductionData,
    string AgentCode,
    string BusinessType,
    string RiskClassification,
    int Version
);
