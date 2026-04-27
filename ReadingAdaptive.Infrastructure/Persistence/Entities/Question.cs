using System;
using System.Collections.Generic;

namespace ReadingAdaptive.Infrastructure.Persistence.Entities;

public partial class Question
{
    public int QuestionId { get; set; }

    public byte DimensionId { get; set; }

    public string Stem { get; set; } = null!;

    public string QuestionType { get; set; } = null!;

    public string? Explanation { get; set; }

    public byte? DifficultyLevelId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<AssessmentQuestion> AssessmentQuestions { get; set; } = new List<AssessmentQuestion>();

    public virtual ICollection<AttemptAnswer> AttemptAnswers { get; set; } = new List<AttemptAnswer>();

    public virtual DifficultyLevel? DifficultyLevel { get; set; }

    public virtual Dimension Dimension { get; set; } = null!;

    public virtual ICollection<FeedbackLog> FeedbackLogs { get; set; } = new List<FeedbackLog>();

    public virtual ICollection<QuestionOption> QuestionOptions { get; set; } = new List<QuestionOption>();
}
