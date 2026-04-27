namespace ReadingAdaptive.Application.TeacherPanel.Exceptions;

public sealed class TeacherPanelAccessDeniedException : Exception
{
    public TeacherPanelAccessDeniedException(string message)
        : base(message)
    {
    }
}
