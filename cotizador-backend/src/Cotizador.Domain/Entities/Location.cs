using Cotizador.Domain.ValueObjects;

namespace Cotizador.Domain.Entities;

public class Location
{
    public int Index { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Municipality { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string ConstructionType { get; set; } = string.Empty;
    public int Level { get; set; }
    public int ConstructionYear { get; set; }
    public BusinessLine BusinessLine { get; set; } = new();
    public List<string> Guarantees { get; set; } = new();
    public string CatZone { get; set; } = string.Empty;
    public List<string> BlockingAlerts { get; set; } = new();
    public string ValidationStatus { get; set; } = Constants.ValidationStatus.Incomplete;
}
