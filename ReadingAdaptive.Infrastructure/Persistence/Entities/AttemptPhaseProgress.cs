using System;
using System.Collections.Generic;

namespace ReadingAdaptive.Infrastructure.Persistence.Entities;

public partial class AttemptPhaseProgress
{
    public long AttemptPhaseProgressId { get; set; }

    public long AttemptId { get; set; }

    public byte PhaseId { get; set; }

    public byte SequenceOrder { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public int? TimeSpentSeconds { get; set; }

    public string? UnlockReason { get; set; }

    public virtual AssessmentAttempt Attempt { get; set; } = null!;

    public virtual Phase Phase { get; set; } = null!;
}
