using System;
using System.Collections.Generic;

namespace ReadingAdaptive.Infrastructure.Persistence.Entities;

public partial class Badge
{
    public int BadgeId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? IconKey { get; set; }

    public string CriteriaCode { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<StudentBadge> StudentBadges { get; set; } = new List<StudentBadge>();
}
