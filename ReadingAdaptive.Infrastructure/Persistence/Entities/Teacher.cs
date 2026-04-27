using System;
using System.Collections.Generic;

namespace ReadingAdaptive.Infrastructure.Persistence.Entities;

public partial class Teacher
{
    public int TeacherId { get; set; }

    public bool CanResetPasswords { get; set; }

    public bool IsHiddenAdmin { get; set; }

    public virtual ICollection<TeacherComment> TeacherComments { get; set; } = new List<TeacherComment>();

    public virtual User TeacherNavigation { get; set; } = null!;
}
