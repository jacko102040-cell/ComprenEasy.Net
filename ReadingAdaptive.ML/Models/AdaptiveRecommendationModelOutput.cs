using Microsoft.ML.Data;

namespace ReadingAdaptive.ML.Models;

public sealed class AdaptiveRecommendationModelOutput
{
    [ColumnName("PredictedAction")]
    public string PredictedAction { get; set; } = string.Empty;

    public float[] Score { get; set; } = Array.Empty<float>();
}
