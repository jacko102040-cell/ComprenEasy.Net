namespace ReadingAdaptive.Application.TeacherPanel.Dtos;

public sealed record ResetStudentPasswordResponseDto(
    int StudentId,
    string StudentFullName,
    string TemporaryPassword,
    DateTime ResetAt);
