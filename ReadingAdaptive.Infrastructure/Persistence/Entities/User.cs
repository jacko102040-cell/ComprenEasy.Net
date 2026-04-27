using System;
using System.Collections.Generic;

namespace ReadingAdaptive.Infrastructure.Persistence.Entities;

public partial class User
{
    public int UserId { get; set; }

    public short RoleId { get; set; }

    public string FullName { get; set; } = null!;

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<PasswordResetLog> PasswordResetLogs { get; set; } = new List<PasswordResetLog>();

    public virtual ICollection<Reading> Readings { get; set; } = new List<Reading>();

    public virtual Role Role { get; set; } = null!;

    public virtual Student? Student { get; set; }

    public virtual Teacher? Teacher { get; set; }
}
