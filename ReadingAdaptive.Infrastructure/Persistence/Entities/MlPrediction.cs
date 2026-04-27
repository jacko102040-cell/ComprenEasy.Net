using System;
using System.Collections.Generic;

namespace ReadingAdaptive.Infrastructure.Persistence.Entities;

public partial class MlPrediction
{
    public long MlPredictionId { get; set; }

    public long RecommendationId { get; set; }

    public decimal LiteralScore { get; set; }

    public decimal InferentialScore { get; set; }

    public decimal CriticalScore { get; set; }

    public int ErrorCount { get; set; }

    public decimal AvgResponseTimeSeconds { get; set; }

    public byte CurrentDifficultyRank { get; set; }

    public decimal? PreviousProgressDelta { get; set; }

    public string? ModelVersion { get; set; }

    public string? RawOutput { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual AdaptiveRecommendation Recommendation { get; set; } = null!;
}
