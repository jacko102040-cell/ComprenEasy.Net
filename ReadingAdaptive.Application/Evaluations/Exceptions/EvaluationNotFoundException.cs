namespace ReadingAdaptive.Application.Evaluations.Exceptions;

public sealed class EvaluationNotFoundException : Exception
{
    public EvaluationNotFoundException(string message)
        : base(message)
    {
    }
}
