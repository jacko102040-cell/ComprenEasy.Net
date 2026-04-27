using System;
using System.Collections.Generic;

namespace ReadingAdaptive.Infrastructure.Persistence.Entities;

public partial class AssessmentQuestion
{
    public int AssessmentQuestionId { get; set; }

    public int AssessmentId { get; set; }

    public int QuestionId { get; set; }

    public byte? PhaseId { get; set; }

    public byte DisplayOrder { get; set; }

    public decimal Points { get; set; }

    public bool IsActive { get; set; }

    public virtual Assessment Assessment { get; set; } = null!;

    public virtual Phase? Phase { get; set; }

    public virtual Question Question { get; set; } = null!;
}
