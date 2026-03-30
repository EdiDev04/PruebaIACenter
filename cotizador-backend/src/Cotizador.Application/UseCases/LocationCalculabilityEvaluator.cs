using Cotizador.Domain.Constants;
using Cotizador.Domain.Entities;

namespace Cotizador.Application.UseCases;

internal static class LocationCalculabilityEvaluator
{
    public static void Evaluate(Location location)
    {
        var alerts = new List<string>();

        if (string.IsNullOrWhiteSpace(location.ZipCode) ||
            !System.Text.RegularExpressions.Regex.IsMatch(location.ZipCode, @"^\d{5}$"))
        {
            alerts.Add("Código postal requerido");
        }

        if (location.BusinessLine is null || string.IsNullOrWhiteSpace(location.BusinessLine.FireKey))
        {
            alerts.Add("Giro comercial requerido");
        }

        if (location.Guarantees == null || location.Guarantees.Count == 0)
        {
            alerts.Add("Al menos una garantía es requerida");
        }
        else
        {
            foreach (var guarantee in location.Guarantees)
            {
                bool requiresInsuredAmount = !GuaranteeKeys.NotRequiringInsuredAmount.Contains(guarantee.GuaranteeKey);
                if (requiresInsuredAmount && guarantee.InsuredAmount <= 0)
                {
                    alerts.Add($"Suma asegurada requerida para {guarantee.GuaranteeKey}");
                }
            }
        }

        location.ValidationStatus = alerts.Count == 0
            ? ValidationStatus.Calculable
            : ValidationStatus.Incomplete;

        location.BlockingAlerts = alerts;
    }
}
