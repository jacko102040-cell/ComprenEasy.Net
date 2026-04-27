namespace ReadingAdaptive.Application.TeacherPanel.Exceptions;

public sealed class TeacherPanelValidationException : Exception
{
    public TeacherPanelValidationException(string message)
        : base(message)
    {
    }
}
