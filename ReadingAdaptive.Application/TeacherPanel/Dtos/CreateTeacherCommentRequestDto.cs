using System.ComponentModel.DataAnnotations;

namespace ReadingAdaptive.Application.TeacherPanel.Dtos;

public sealed class CreateTeacherCommentRequestDto
{
    public long? AttemptId { get; set; }

    [Range(1, byte.MaxValue)]
    public byte CommentTagId { get; set; }

    [Required]
    [StringLength(300)]
    public string CommentText { get; set; } = string.Empty;
}
