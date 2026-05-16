using ReadingAdaptive.Application.Readings.Dtos;

namespace ReadingAdaptive.Application.Readings.Interfaces;

public interface IReadingService
{
    Task<IReadOnlyCollection<ActiveReadingDto>> GetActiveReadingsAsync(
        int studentId,
        CancellationToken cancellationToken = default);

    Task<ReadingProgressSummaryDto> GetReadingProgressAsync(
        int studentId,
        CancellationToken cancellationToken = default);

    Task<ReadingDetailDto> GetReadingDetailAsync(
        int readingId,
        int studentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ReadingPhaseDto>> GetReadingPhasesAsync(
        int readingId,
        int studentId,
        CancellationToken cancellationToken = default);

    Task<ReadingSessionProgressDto> StartReadingSessionAsync(
        int readingId,
        int studentId,
        CancellationToken cancellationToken = default);

    Task<ReadingSessionProgressDto> SavePhaseProgressAsync(
        long attemptId,
        byte phaseId,
        int studentId,
        SaveReadingPhaseProgressRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ReadingSessionProgressDto> CompletePhaseAsync(
        long attemptId,
        byte phaseId,
        int studentId,
        SaveReadingPhaseProgressRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ReadingSessionProgressDto> GetReadingSessionProgressAsync(
        long attemptId,
        int studentId,
        CancellationToken cancellationToken = default);

    Task<ReadingSessionProgressDto> FinishReadingSessionAsync(
        long attemptId,
        int studentId,
        CancellationToken cancellationToken = default);
}
