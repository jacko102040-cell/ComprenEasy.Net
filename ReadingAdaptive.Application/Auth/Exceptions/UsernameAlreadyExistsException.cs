namespace ReadingAdaptive.Application.Auth.Exceptions;

public sealed class UsernameAlreadyExistsException : Exception
{
    public UsernameAlreadyExistsException(string username)
        : base($"El nombre de usuario '{username}' ya esta en uso.")
    {
    }
}
