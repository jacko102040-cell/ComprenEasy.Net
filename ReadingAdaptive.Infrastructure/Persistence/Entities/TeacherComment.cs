using System;
using System.Collections.Generic;

namespace ReadingAdaptive.Infrastructure.Persistence.Entities;

public partial class TeacherComment
{
    public long TeacherCommentId { get; set; }

    public int TeacherId { get; set; }

    public int StudentId { get; set; }

    public long? AttemptId { get; set; }

    public byte CommentTagId { get; set; }

    public string CommentText { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual AssessmentAttempt? Attempt { get; set; }

    public virtual CommentTag CommentTag { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;

    public virtual Teacher Teacher { get; set; } = null!;
}
