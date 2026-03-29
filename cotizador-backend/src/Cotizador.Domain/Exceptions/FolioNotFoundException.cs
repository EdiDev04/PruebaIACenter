namespace Cotizador.Domain.Exceptions;

public class FolioNotFoundException : Exception
{
    public string FolioNumber { get; }

    public FolioNotFoundException(string folioNumber)
        : base($"Folio '{folioNumber}' not found")
    {
        FolioNumber = folioNumber;
    }
}
