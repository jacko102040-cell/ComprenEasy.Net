using System;
using System.Collections.Generic;

namespace ReadingAdaptive.Infrastructure.Persistence.Entities;

public partial class ReadingPhase
{
    public int ReadingPhaseId { get; set; }

    public int ReadingId { get; set; }

    public byte PhaseId { get; set; }

    public byte DisplayOrder { get; set; }

    public bool IsEnabled { get; set; }

    public bool IsRequired { get; set; }

    public string? GuidanceText { get; set; }

    public byte? MinQuestionsToUnlockNext { get; set; }

    public virtual Phase Phase { get; set; } = null!;

    public virtual Reading Reading { get; set; } = null!;
}
