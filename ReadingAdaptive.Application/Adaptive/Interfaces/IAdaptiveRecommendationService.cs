using ReadingAdaptive.Application.Adaptive.Dtos;

namespace ReadingAdaptive.Application.Adaptive.Interfaces;

public interface IAdaptiveRecommendationService
{
    Task<AdaptiveRecommendationDto> GetLatestRecommendationAsync(
        int studentId,
        CancellationToken cancellationToken = default);

    Task<AdaptiveRecommendationDto> GenerateRecommendationAsync(
        long attemptId,
        int studentId,
        CancellationToken cancellationToken = default);
}
