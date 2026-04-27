using System;
using System.Collections.Generic;

namespace ReadingAdaptive.Infrastructure.Persistence.Entities;

public partial class PasswordResetLog
{
    public long PasswordResetLogId { get; set; }

    public int StudentId { get; set; }

    public int ResetByUserId { get; set; }

    public string? Reason { get; set; }

    public DateTime ResetAt { get; set; }

    public virtual User ResetByUser { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
