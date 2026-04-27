namespace ReadingAdaptive.Application.Readings.Exceptions;

public sealed class ReadingAccessDeniedException : Exception
{
    public ReadingAccessDeniedException(string message)
        : base(message)
    {
    }
}
