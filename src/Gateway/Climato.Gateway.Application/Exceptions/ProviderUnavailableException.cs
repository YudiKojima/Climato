namespace Climato.Gateway.Application.Exceptions;

public sealed class ProviderUnavailableException : Exception
{
    public ProviderUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
