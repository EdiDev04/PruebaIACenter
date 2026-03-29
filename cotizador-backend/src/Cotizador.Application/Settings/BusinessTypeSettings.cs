namespace Cotizador.Application.Settings;

public class BusinessTypeSettings
{
    public const string SectionName = "BusinessTypes";
    public List<string> AllowedValues { get; set; } = new() { "commercial", "industrial", "residential" };
}
