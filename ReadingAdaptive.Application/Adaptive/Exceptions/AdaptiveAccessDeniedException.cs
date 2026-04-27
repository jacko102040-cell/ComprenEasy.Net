namespace ReadingAdaptive.Application.Adaptive.Exceptions;

public sealed class AdaptiveAccessDeniedException : Exception
{
    public AdaptiveAccessDeniedException(string message)
        : base(message)
    {
    }
}
