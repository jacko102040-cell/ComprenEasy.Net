using System;
using System.Collections.Generic;

namespace ReadingAdaptive.Infrastructure.Persistence.Entities;

public partial class Student
{
    public int StudentId { get; set; }

    public byte Grade { get; set; }

    public string? Section { get; set; }

    public bool IsEnabledForTest { get; set; }

    public string? Notes { get; set; }

    public virtual ICollection<AdaptiveRecommendation> AdaptiveRecommendations { get; set; } = new List<AdaptiveRecommendation>();

    public virtual ICollection<AssessmentAttempt> AssessmentAttempts { get; set; } = new List<AssessmentAttempt>();

    public virtual ICollection<PasswordResetLog> PasswordResetLogs { get; set; } = new List<PasswordResetLog>();

    public virtual ICollection<StudentBadge> StudentBadges { get; set; } = new List<StudentBadge>();

    public virtual User StudentNavigation { get; set; } = null!;

    public virtual ICollection<TeacherComment> TeacherComments { get; set; } = new List<TeacherComment>();
}
