using System.ComponentModel.DataAnnotations;

namespace ReadingAdaptive.Application.Auth.Dtos;

public sealed class RegisterStudentRequestDto
{
    [Required]
    [StringLength(150, MinimumLength = 3)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(30, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Range(1, 6)]
    public byte? Grade { get; set; }

    [StringLength(10)]
    public string? Section { get; set; }
}
