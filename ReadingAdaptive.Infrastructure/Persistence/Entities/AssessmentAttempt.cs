using System;
using System.Collections.Generic;

namespace ReadingAdaptive.Infrastructure.Persistence.Entities;

public partial class AssessmentAttempt
{
    public long AttemptId { get; set; }

    public int AssessmentId { get; set; }

    public int StudentId { get; set; }

    public byte AttemptNumber { get; set; }

    public string Status { get; set; } = null!;

    public DateTime StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public decimal? TotalScore { get; set; }

    public decimal? LiteralScore { get; set; }

    public decimal? InferentialScore { get; set; }

    public decimal? CriticalScore { get; set; }

    public int? TotalCorrect { get; set; }

    public int? TotalErrors { get; set; }

    public int? TotalTimeSeconds { get; set; }

    public decimal? CompletionPercentage { get; set; }

    public virtual ICollection<AdaptiveRecommendation> AdaptiveRecommendations { get; set; } = new List<AdaptiveRecommendation>();

    public virtual Assessment Assessment { get; set; } = null!;

    public virtual ICollection<AttemptAnswer> AttemptAnswers { get; set; } = new List<AttemptAnswer>();

    public virtual ICollection<AttemptPhaseProgress> AttemptPhaseProgresses { get; set; } = new List<AttemptPhaseProgress>();

    public virtual ICollection<FeedbackLog> FeedbackLogs { get; set; } = new List<FeedbackLog>();

    public virtual Student Student { get; set; } = null!;

    public virtual ICollection<StudentBadge> StudentBadges { get; set; } = new List<StudentBadge>();

    public virtual ICollection<TeacherComment> TeacherComments { get; set; } = new List<TeacherComment>();
}
