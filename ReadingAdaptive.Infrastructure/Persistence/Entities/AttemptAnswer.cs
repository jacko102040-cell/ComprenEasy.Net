using System;
using System.Collections.Generic;

namespace ReadingAdaptive.Infrastructure.Persistence.Entities;

public partial class AttemptAnswer
{
    public long AttemptAnswerId { get; set; }

    public long AttemptId { get; set; }

    public int QuestionId { get; set; }

    public int? SelectedOptionId { get; set; }

    public bool? IsCorrect { get; set; }

    public decimal? ScoreObtained { get; set; }

    public int? AnswerTimeSeconds { get; set; }

    public DateTime? AnsweredAt { get; set; }

    public virtual AssessmentAttempt Attempt { get; set; } = null!;

    public virtual Question Question { get; set; } = null!;

    public virtual QuestionOption? SelectedOption { get; set; }
}
