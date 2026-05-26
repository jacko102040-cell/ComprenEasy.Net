using ReadingAdaptive.Application.TeacherPanel.Dtos;

namespace ReadingAdaptive.Application.TeacherPanel.Interfaces;

public interface ITeacherPanelService
{
    Task<IReadOnlyCollection<TeacherStudentListItemDto>> GetStudentsAsync(
        int teacherId,
        CancellationToken cancellationToken = default);

    Task<TeacherStudentDetailDto> GetStudentDetailAsync(
        int teacherId,
        int studentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<TeacherCommentTagDto>> GetCommentTagsAsync(
        int teacherId,
        CancellationToken cancellationToken = default);

    Task<TeacherPanelExportFileDto> ExportPosttestReadingsAsync(
        int teacherId,
        CancellationToken cancellationToken = default);

    Task<ResetStudentPasswordResponseDto> ResetStudentPasswordAsync(
        int teacherId,
        int studentId,
        CancellationToken cancellationToken = default);

    Task<TeacherStudentCommentDto> AddCommentAsync(
        int teacherId,
        int studentId,
        CreateTeacherCommentRequestDto request,
        CancellationToken cancellationToken = default);
}
