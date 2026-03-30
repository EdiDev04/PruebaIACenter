namespace Cotizador.Domain.ValueObjects;

public class LocationGuarantee
{
    public string GuaranteeKey { get; set; } = string.Empty;
    public decimal InsuredAmount { get; set; }
}
