namespace Cotizador.Application.DTOs;

public record UpdateGeneralInfoRequest(
    InsuredDataDto InsuredData,
    ConductionDataDto ConductionData,
    string AgentCode,
    string BusinessType,
    string RiskClassification,
    int Version
);
