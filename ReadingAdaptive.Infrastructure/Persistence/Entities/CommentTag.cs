using System;
using System.Collections.Generic;

namespace ReadingAdaptive.Infrastructure.Persistence.Entities;

public partial class CommentTag
{
    public byte CommentTagId { get; set; }

    public string Name { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<TeacherComment> TeacherComments { get; set; } = new List<TeacherComment>();
}
