namespace Cotizador.Infrastructure.Persistence;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string QuotesCollectionName { get; set; } = "property_quotes";
}
