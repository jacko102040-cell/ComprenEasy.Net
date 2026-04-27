using ReadingAdaptive.Application.Evaluations.Dtos;

namespace ReadingAdaptive.Application.Evaluations.Interfaces;

public interface IEvaluationService
{
    Task<IReadOnlyCollection<ActiveAssessmentDto>> GetActiveAssessmentsAsync(
        string assessmentType,
        int studentId,
        CancellationToken cancellationToken = default);

    Task<AssessmentDetailDto> GetAssessmentDetailAsync(
        int assessmentId,
        int studentId,
        CancellationToken cancellationToken = default);

    Task<StartAssessmentAttemptResponseDto> StartAttemptAsync(
        int assessmentId,
        int studentId,
        CancellationToken cancellationToken = default);

    Task<SaveAttemptAnswersResponseDto> SaveAnswersAsync(
        long attemptId,
        int studentId,
        SaveAttemptAnswersRequestDto request,
        CancellationToken cancellationToken = default);

    Task<AssessmentAttemptResultDto> FinishAttemptAsync(
        long attemptId,
        int studentId,
        CancellationToken cancellationToken = default);

    Task<AssessmentAttemptResultDto> GetAttemptResultAsync(
        long attemptId,
        int studentId,
        CancellationToken cancellationToken = default);

    Task<PrePostComparisonSummaryDto> GetLatestPrePostComparisonAsync(
        int studentId,
        CancellationToken cancellationToken = default);
}
