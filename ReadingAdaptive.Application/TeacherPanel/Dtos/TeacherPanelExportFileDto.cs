namespace ReadingAdaptive.Application.TeacherPanel.Dtos;

public sealed record TeacherPanelExportFileDto(
    byte[] Content,
    string FileName,
    string ContentType);
