using Cotizador.Domain.Constants;

namespace Cotizador.Domain.ValueObjects;

public class CoverageOptions
{
    public List<string> EnabledGuarantees { get; set; } = new List<string>();
    public decimal DeductiblePercentage { get; set; } = 0;
    public decimal CoinsurancePercentage { get; set; } = 0;
}
