namespace ReadingAdaptive.Application.Readings.Exceptions;

public sealed class ReadingNotFoundException : Exception
{
    public ReadingNotFoundException(string message)
        : base(message)
    {
    }
}
