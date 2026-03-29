namespace Cotizador.Domain.ValueObjects;

public class ConductionData
{
    public string SubscriberCode { get; set; } = string.Empty;
    public string OfficeName { get; set; } = string.Empty;
    public string? BranchOffice { get; set; }
}
