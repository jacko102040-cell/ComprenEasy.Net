namespace ReadingAdaptive.Application.AcademicContent.Exceptions;

public sealed class AcademicContentNotFoundException : Exception
{
    public AcademicContentNotFoundException(string message)
        : base(message)
    {
    }
}
