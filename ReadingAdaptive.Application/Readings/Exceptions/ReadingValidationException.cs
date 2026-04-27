namespace ReadingAdaptive.Application.Readings.Exceptions;

public sealed class ReadingValidationException : Exception
{
    public ReadingValidationException(string message)
        : base(message)
    {
    }
}
