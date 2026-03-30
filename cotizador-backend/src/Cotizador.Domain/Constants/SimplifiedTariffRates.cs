namespace Cotizador.Domain.Constants;

/// <summary>
/// Tasas simplificadas documentadas (S-04 del reto).
/// No representan tarifas actuariales reales — son aproximaciones aceptables para el reto.
/// </summary>
public static class SimplifiedTariffRates
{
    /// <summary>
    /// Tasa para coberturas complementarias: debris_removal, extraordinary_expenses.
    /// Prima = suma_asegurada × 0.0010
    /// </summary>
    public const decimal SupplementaryRate = 0.0010m;

    /// <summary>
    /// Tasa para coberturas de pérdida de ingresos: rent_loss, business_interruption.
    /// Prima = suma_asegurada × 0.0015
    /// </summary>
    public const decimal IncomeRate = 0.0015m;

    /// <summary>
    /// Tasa para coberturas de robo: theft, cash_and_securities.
    /// Prima = suma_asegurada × 0.0020
    /// </summary>
    public const decimal SpecialRate = 0.0020m;

    /// <summary>
    /// Prima fija para coberturas de tarifa plana: glass, illuminated_signs.
    /// No depende de suma asegurada. Rate = 0 en el CoveragePremium.
    /// </summary>
    public const decimal FlatPremium = 500.00m;

    /// <summary>
    /// Clase de equipo electrónico por defecto para lookup en electronic_equipment factors.
    /// La entidad Location no expone equipmentClass — se usa "A" como simplificación documentada (SUP-009-05).
    /// </summary>
    public const string DefaultEquipmentClass = "A";
}
