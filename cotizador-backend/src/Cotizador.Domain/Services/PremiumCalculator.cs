using Cotizador.Domain.Constants;
using Cotizador.Domain.ValueObjects;

namespace Cotizador.Domain.Services;

/// <summary>
/// Motor de cálculo de primas. Funciones puras sin I/O — 100% unit-testable sin mocks.
/// Implementa fórmulas simplificadas documentadas (S-04 del reto).
/// </summary>
public static class PremiumCalculator
{
    /// <summary>
    /// Calcula la prima de una cobertura individual.
    /// Para coberturas de tarifa plana (glass, illuminated_signs): premium = SimplifiedTariffRates.FlatPremium, rate = 0.
    /// Para el resto: premium = insuredAmount × rate.
    /// </summary>
    public static CoveragePremium CalculateCoveragePremium(
        string guaranteeKey,
        decimal insuredAmount,
        decimal rate)
    {
        bool isFlat = guaranteeKey == GuaranteeKeys.Glass || guaranteeKey == GuaranteeKeys.IlluminatedSigns;

        decimal premium = isFlat
            ? SimplifiedTariffRates.FlatPremium
            : Math.Round(insuredAmount * rate, 2);

        return new CoveragePremium
        {
            GuaranteeKey = guaranteeKey,
            InsuredAmount = insuredAmount,
            Rate = isFlat ? 0m : rate,
            Premium = premium,
        };
    }

    /// <summary>
    /// Calcula la prima neta de una ubicación como la suma de primas de sus coberturas.
    /// </summary>
    public static decimal CalculateLocationNetPremium(List<CoveragePremium> coveragePremiums)
    {
        return Math.Round(coveragePremiums.Sum(c => c.Premium), 2);
    }

    /// <summary>
    /// Deriva la prima comercial a partir de la prima neta y los parámetros globales.
    ///
    /// Fórmula:
    ///   commercialPremiumBeforeTax = netPremium × (1 + expeditionExpenses + agentCommission + issuingRights + surcharges)
    ///   commercialPremium          = commercialPremiumBeforeTax × (1 + iva)
    ///
    /// Con parámetros por defecto del fixture (calculation-parameters):
    ///   loadingFactor = 1 + 0.05 + 0.10 + 0.03 + 0.02 = 1.20
    ///   ivaBefore = beforeTax × 1.16
    /// </summary>
    public static (decimal BeforeTax, decimal WithTax) CalculateCommercialPremium(
        decimal netPremium,
        decimal expeditionExpenses,
        decimal agentCommission,
        decimal issuingRights,
        decimal surcharges,
        decimal iva)
    {
        decimal loadingFactor = 1m + expeditionExpenses + agentCommission + issuingRights + surcharges;
        decimal beforeTax = Math.Round(netPremium * loadingFactor, 2);
        decimal withTax = Math.Round(beforeTax * (1m + iva), 2);
        return (beforeTax, withTax);
    }
}
