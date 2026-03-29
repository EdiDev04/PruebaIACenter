namespace Cotizador.Domain.ValueObjects;

public class LocationPremium
{
    public int LocationIndex { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public decimal NetPremium { get; set; }
    public string ValidationStatus { get; set; } = string.Empty;
    public List<CoveragePremium> CoveragePremiums { get; set; } = new();
}
