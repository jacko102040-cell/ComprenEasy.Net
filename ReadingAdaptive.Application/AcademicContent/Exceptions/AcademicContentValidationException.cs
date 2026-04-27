namespace ReadingAdaptive.Application.AcademicContent.Exceptions;

public sealed class AcademicContentValidationException : Exception
{
    public AcademicContentValidationException(string message)
        : base(message)
    {
    }
}
