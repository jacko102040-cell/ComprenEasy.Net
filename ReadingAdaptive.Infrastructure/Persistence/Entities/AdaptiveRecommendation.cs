using System;
using System.Collections.Generic;

namespace ReadingAdaptive.Infrastructure.Persistence.Entities;

public partial class AdaptiveRecommendation
{
    public long RecommendationId { get; set; }

    public int StudentId { get; set; }

    public long SourceAttemptId { get; set; }

    public byte CurrentDifficultyLevelId { get; set; }

    public byte RecommendedDifficultyLevelId { get; set; }

    public string PredictedAction { get; set; } = null!;

    public int? RecommendedAssessmentId { get; set; }

    public string EngineType { get; set; } = null!;

    public decimal? ConfidenceScore { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual DifficultyLevel CurrentDifficultyLevel { get; set; } = null!;

    public virtual MlPrediction? MlPrediction { get; set; }

    public virtual Assessment? RecommendedAssessment { get; set; }

    public virtual DifficultyLevel RecommendedDifficultyLevel { get; set; } = null!;

    public virtual AssessmentAttempt SourceAttempt { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
