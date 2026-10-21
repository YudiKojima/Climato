namespace Climato.Gateway.Domain.Exceptions;

public sealed class UserNotFoundException : Exception
{
    public UserNotFoundException() : base("O usuário do token não existe mais.")
    {
    }
}
