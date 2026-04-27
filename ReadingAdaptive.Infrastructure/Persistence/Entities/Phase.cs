using System;
using System.Collections.Generic;

namespace ReadingAdaptive.Infrastructure.Persistence.Entities;

public partial class Phase
{
    public byte PhaseId { get; set; }

    public string Code { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public byte DefaultOrder { get; set; }

    public virtual ICollection<AssessmentQuestion> AssessmentQuestions { get; set; } = new List<AssessmentQuestion>();

    public virtual ICollection<AttemptPhaseProgress> AttemptPhaseProgresses { get; set; } = new List<AttemptPhaseProgress>();

    public virtual ICollection<FeedbackLog> FeedbackLogs { get; set; } = new List<FeedbackLog>();

    public virtual ICollection<ReadingPhase> ReadingPhases { get; set; } = new List<ReadingPhase>();
}
