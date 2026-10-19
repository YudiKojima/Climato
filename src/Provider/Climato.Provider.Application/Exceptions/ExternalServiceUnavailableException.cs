namespace Climato.Provider.Application.Exceptions;

public sealed class ExternalServiceUnavailableException : Exception
{
    public ExternalServiceUnavailableException(string serviceName, string reason, Exception? innerException = null)
        : base($"{serviceName} indisponível: {reason}.", innerException)
    {
    }
}
