using System;
using System.Collections.Generic;

namespace ReadingAdaptive.Infrastructure.Persistence.Entities;

public partial class Dimension
{
    public byte DimensionId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
}
