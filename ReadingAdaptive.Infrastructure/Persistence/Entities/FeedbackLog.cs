using System;
using System.Collections.Generic;

namespace ReadingAdaptive.Infrastructure.Persistence.Entities;

public partial class FeedbackLog
{
    public long FeedbackLogId { get; set; }

    public long AttemptId { get; set; }

    public byte? PhaseId { get; set; }

    public int? QuestionId { get; set; }

    public string FeedbackType { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string Severity { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual AssessmentAttempt Attempt { get; set; } = null!;

    public virtual Phase? Phase { get; set; }

    public virtual Question? Question { get; set; }
}
