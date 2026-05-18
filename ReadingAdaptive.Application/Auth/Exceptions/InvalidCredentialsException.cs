namespace ReadingAdaptive.Application.Auth.Exceptions;

public sealed class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException()
        : base("Nombre de usuario o contrasena invalidos.")
    {
    }
}
