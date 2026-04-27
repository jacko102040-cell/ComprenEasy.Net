namespace ReadingAdaptive.Application.Adaptive.Exceptions;

public sealed class AdaptiveNotFoundException : Exception
{
    public AdaptiveNotFoundException(string message)
        : base(message)
    {
    }
}
