namespace Cotizador.Domain.ValueObjects;

public class CoveragePremium
{
    public string GuaranteeKey { get; set; } = string.Empty;
    public decimal InsuredAmount { get; set; }
    public decimal Rate { get; set; }
    public decimal Premium { get; set; }
}
