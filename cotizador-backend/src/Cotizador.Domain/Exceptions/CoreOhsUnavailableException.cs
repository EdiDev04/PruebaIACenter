namespace Cotizador.Domain.Exceptions;

public class CoreOhsUnavailableException : Exception
{
    public CoreOhsUnavailableException(string message) : base(message) { }

    public CoreOhsUnavailableException(string message, Exception inner)
        : base(message, inner) { }
}
