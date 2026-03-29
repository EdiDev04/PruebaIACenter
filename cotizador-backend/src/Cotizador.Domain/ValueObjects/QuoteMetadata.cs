namespace Cotizador.Domain.ValueObjects;

public class QuoteMetadata
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public int LastWizardStep { get; set; }
}
