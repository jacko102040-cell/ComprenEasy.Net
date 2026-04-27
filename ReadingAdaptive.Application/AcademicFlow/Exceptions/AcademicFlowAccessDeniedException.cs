namespace ReadingAdaptive.Application.AcademicFlow.Exceptions;

public sealed class AcademicFlowAccessDeniedException : Exception
{
    public AcademicFlowAccessDeniedException(string message)
        : base(message)
    {
    }
}
