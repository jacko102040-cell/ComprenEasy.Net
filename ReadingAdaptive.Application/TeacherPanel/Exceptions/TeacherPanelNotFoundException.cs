namespace ReadingAdaptive.Application.TeacherPanel.Exceptions;

public sealed class TeacherPanelNotFoundException : Exception
{
    public TeacherPanelNotFoundException(string message)
        : base(message)
    {
    }
}
