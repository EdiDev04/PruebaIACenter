namespace Cotizador.Domain.Exceptions;

public class InvalidQuoteStateException : Exception
{
    public string FolioNumber { get; }
    public string CurrentState { get; }

    public InvalidQuoteStateException(string folioNumber, string currentState, string message)
        : base(message)
    {
        FolioNumber = folioNumber;
        CurrentState = currentState;
    }
}
