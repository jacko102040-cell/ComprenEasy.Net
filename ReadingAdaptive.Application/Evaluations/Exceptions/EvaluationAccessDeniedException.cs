namespace ReadingAdaptive.Application.Evaluations.Exceptions;

public sealed class EvaluationAccessDeniedException : Exception
{
    public EvaluationAccessDeniedException(string message)
        : base(message)
    {
    }
}
