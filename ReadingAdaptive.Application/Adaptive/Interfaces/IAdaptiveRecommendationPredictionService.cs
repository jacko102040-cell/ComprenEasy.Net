using ReadingAdaptive.Application.Adaptive.Dtos;

namespace ReadingAdaptive.Application.Adaptive.Interfaces;

public interface IAdaptiveRecommendationPredictionService
{
    AdaptiveMlPredictionResult Predict(AdaptiveMlPredictionRequest request);
}
