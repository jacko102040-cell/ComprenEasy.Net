namespace ReadingAdaptive.Application.Auth.Exceptions;

public sealed class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException()
        : base("Invalid username or password.")
    {
    }
}
