using System;
using System.Collections.Generic;

namespace ReadingAdaptive.Infrastructure.Persistence.Entities;

public partial class Reading
{
    public int ReadingId { get; set; }

    public string Title { get; set; } = null!;

    public string? Summary { get; set; }

    public string Content { get; set; } = null!;

    public string? ImageUrl { get; set; }

    public byte DifficultyLevelId { get; set; }

    public int? EstimatedMinutes { get; set; }

    public bool IsActive { get; set; }

    public int CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual DifficultyLevel DifficultyLevel { get; set; } = null!;

    public virtual ICollection<ReadingPhase> ReadingPhases { get; set; } = new List<ReadingPhase>();
}
