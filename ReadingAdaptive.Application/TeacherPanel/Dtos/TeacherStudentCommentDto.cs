namespace ReadingAdaptive.Application.TeacherPanel.Dtos;

public sealed record TeacherStudentCommentDto(
    long TeacherCommentId,
    string TeacherName,
    byte CommentTagId,
    string CommentTagName,
    long? AttemptId,
    string CommentText,
    DateTime CreatedAt);
