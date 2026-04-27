using System;
using System.Collections.Generic;

namespace ReadingAdaptive.Infrastructure.Persistence.Entities;

public partial class DifficultyLevel
{
    public byte DifficultyLevelId { get; set; }

    public string Name { get; set; } = null!;

    public byte RankOrder { get; set; }

    public virtual ICollection<AdaptiveRecommendation> AdaptiveRecommendationCurrentDifficultyLevels { get; set; } = new List<AdaptiveRecommendation>();

    public virtual ICollection<AdaptiveRecommendation> AdaptiveRecommendationRecommendedDifficultyLevels { get; set; } = new List<AdaptiveRecommendation>();

    public virtual ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

    public virtual ICollection<Reading> Readings { get; set; } = new List<Reading>();
}
