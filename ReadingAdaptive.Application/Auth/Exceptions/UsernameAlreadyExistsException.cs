namespace ReadingAdaptive.Application.Auth.Exceptions;

public sealed class UsernameAlreadyExistsException : Exception
{
    public UsernameAlreadyExistsException(string username)
        : base($"The username '{username}' is already in use.")
    {
    }
}
