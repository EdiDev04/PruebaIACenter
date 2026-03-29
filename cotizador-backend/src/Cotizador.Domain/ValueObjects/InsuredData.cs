namespace Cotizador.Domain.ValueObjects;

public class InsuredData
{
    public string Name { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
}
