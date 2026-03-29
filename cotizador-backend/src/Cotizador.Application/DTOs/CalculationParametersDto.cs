namespace Cotizador.Application.DTOs;

public record CalculationParametersDto(
    decimal ExpeditionExpenses,
    decimal AgentCommission,
    decimal IssuingRights,
    decimal Iva,
    decimal Surcharges,
    string EffectiveDate);
