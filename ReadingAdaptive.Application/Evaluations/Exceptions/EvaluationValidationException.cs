namespace ReadingAdaptive.Application.Evaluations.Exceptions;

public sealed class EvaluationValidationException : Exception
{
    public EvaluationValidationException(string message)
        : base(message)
    {
    }
}
