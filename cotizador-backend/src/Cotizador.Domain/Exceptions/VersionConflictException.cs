namespace Cotizador.Domain.Exceptions;

public class VersionConflictException : Exception
{
    public string FolioNumber { get; }
    public int ExpectedVersion { get; }

    public VersionConflictException(string folioNumber, int expectedVersion)
        : base($"Version conflict on folio '{folioNumber}'. Expected version: {expectedVersion}")
    {
        FolioNumber = folioNumber;
        ExpectedVersion = expectedVersion;
    }
}
