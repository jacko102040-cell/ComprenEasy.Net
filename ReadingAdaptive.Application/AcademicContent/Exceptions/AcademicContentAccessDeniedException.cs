namespace ReadingAdaptive.Application.AcademicContent.Exceptions;

public sealed class AcademicContentAccessDeniedException : Exception
{
    public AcademicContentAccessDeniedException(string message)
        : base(message)
    {
    }
}
