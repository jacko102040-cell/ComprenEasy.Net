using ReadingAdaptive.Application.AcademicContent.Dtos;

namespace ReadingAdaptive.Application.AcademicContent.Interfaces;

public interface IAcademicContentService
{
    Task<ContentLookupsDto> GetLookupsAsync(
        int teacherId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ContentReadingListItemDto>> GetReadingsAsync(
        int teacherId,
        CancellationToken cancellationToken = default);

    Task<ContentReadingDetailDto> GetReadingAsync(
        int teacherId,
        int readingId,
        CancellationToken cancellationToken = default);

    Task<ContentReadingDetailDto> CreateReadingAsync(
        int teacherId,
        SaveContentReadingRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ContentReadingDetailDto> UpdateReadingAsync(
        int teacherId,
        int readingId,
        SaveContentReadingRequestDto request,
        CancellationToken cancellationToken = default);

    Task ArchiveReadingAsync(
        int teacherId,
        int readingId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ContentAssessmentListItemDto>> GetAssessmentsAsync(
        int teacherId,
        CancellationToken cancellationToken = default);

    Task<ContentAssessmentDetailDto> GetAssessmentAsync(
        int teacherId,
        int assessmentId,
        CancellationToken cancellationToken = default);

    Task<ContentAssessmentDetailDto> CreateAssessmentAsync(
        int teacherId,
        SaveContentAssessmentRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ContentAssessmentDetailDto> UpdateAssessmentAsync(
        int teacherId,
        int assessmentId,
        SaveContentAssessmentRequestDto request,
        CancellationToken cancellationToken = default);

    Task ArchiveAssessmentAsync(
        int teacherId,
        int assessmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ContentQuestionListItemDto>> GetQuestionsAsync(
        int teacherId,
        CancellationToken cancellationToken = default);

    Task<ContentQuestionDetailDto> GetQuestionAsync(
        int teacherId,
        int questionId,
        CancellationToken cancellationToken = default);

    Task<ContentQuestionDetailDto> CreateQuestionAsync(
        int teacherId,
        SaveContentQuestionRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ContentQuestionDetailDto> UpdateQuestionAsync(
        int teacherId,
        int questionId,
        SaveContentQuestionRequestDto request,
        CancellationToken cancellationToken = default);

    Task ArchiveQuestionAsync(
        int teacherId,
        int questionId,
        CancellationToken cancellationToken = default);
}
