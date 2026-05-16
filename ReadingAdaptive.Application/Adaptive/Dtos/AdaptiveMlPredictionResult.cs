namespace ReadingAdaptive.Application.Adaptive.Dtos;

public sealed record AdaptiveMlPredictionResult(
    bool IsAvailable,
    bool IsAccepted,
    string? PredictedAction,
    float Confidence,
    IReadOnlyList<float> Scores,
    string ModelVersion,
    string? FallbackReason,
    string? RawOutput)
{
    public static AdaptiveMlPredictionResult Unavailable(
        string modelVersion,
        string fallbackReason,
        string? rawOutput = null)
    {
        return new AdaptiveMlPredictionResult(
            false,
            false,
            null,
            0f,
            Array.Empty<float>(),
            modelVersion,
            fallbackReason,
            rawOutput);
    }
}
