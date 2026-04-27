using System.ComponentModel.DataAnnotations;

namespace ReadingAdaptive.Application.Auth.Dtos;

public sealed class LoginRequestDto
{
    [Required]
    [StringLength(30, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
}
