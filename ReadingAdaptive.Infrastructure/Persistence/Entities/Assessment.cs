using System;
using System.Collections.Generic;

namespace ReadingAdaptive.Infrastructure.Persistence.Entities;

public partial class Assessment
{
    public int AssessmentId { get; set; }

    public string AssessmentType { get; set; } = null!;

    public int? ReadingId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public byte? DifficultyLevelId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<AdaptiveRecommendation> AdaptiveRecommendations { get; set; } = new List<AdaptiveRecommendation>();

    public virtual ICollection<AssessmentAttempt> AssessmentAttempts { get; set; } = new List<AssessmentAttempt>();

    public virtual ICollection<AssessmentQuestion> AssessmentQuestions { get; set; } = new List<AssessmentQuestion>();

    public virtual DifficultyLevel? DifficultyLevel { get; set; }

    public virtual Reading? Reading { get; set; }
}
