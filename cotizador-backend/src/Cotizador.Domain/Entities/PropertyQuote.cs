using Cotizador.Domain.ValueObjects;
using Cotizador.Domain.Constants;

namespace Cotizador.Domain.Entities;

public class PropertyQuote
{
    public string FolioNumber { get; set; } = string.Empty;
    public string QuoteStatus { get; set; } = Cotizador.Domain.Constants.QuoteStatus.Draft;
    public InsuredData InsuredData { get; set; } = new();
    public ConductionData ConductionData { get; set; } = new();
    public string AgentCode { get; set; } = string.Empty;
    public string RiskClassification { get; set; } = string.Empty;
    public string BusinessType { get; set; } = string.Empty;
    public LayoutConfiguration LayoutConfiguration { get; set; } = new();
    public CoverageOptions CoverageOptions { get; set; } = new();
    public List<Location> Locations { get; set; } = new();
    public decimal NetPremium { get; set; }
    public decimal CommercialPremiumBeforeTax { get; set; }
    public decimal CommercialPremium { get; set; }
    public List<LocationPremium> PremiumsByLocation { get; set; } = new();
    public int Version { get; set; } = 1;
    public QuoteMetadata Metadata { get; set; } = new();
}
