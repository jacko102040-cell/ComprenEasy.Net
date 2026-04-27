using System;
using System.Collections.Generic;

namespace ReadingAdaptive.Infrastructure.Persistence.Entities;

public partial class StudentBadge
{
    public long StudentBadgeId { get; set; }

    public int StudentId { get; set; }

    public int BadgeId { get; set; }

    public long? AttemptId { get; set; }

    public DateTime EarnedAt { get; set; }

    public virtual AssessmentAttempt? Attempt { get; set; }

    public virtual Badge Badge { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
