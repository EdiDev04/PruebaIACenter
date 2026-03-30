namespace Cotizador.Domain.ValueObjects;

public class LayoutConfiguration
{
    public string DisplayMode { get; init; } = "grid";
    public List<string> VisibleColumns { get; init; } = new()
    {
        "index", "locationName", "zipCode", "businessLine", "validationStatus"
    };
}
