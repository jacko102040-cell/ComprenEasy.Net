namespace ReadingAdaptive.Application.Adaptive.Exceptions;

public sealed class AdaptiveValidationException : Exception
{
    public AdaptiveValidationException(string message)
        : base(message)
    {
    }
}
