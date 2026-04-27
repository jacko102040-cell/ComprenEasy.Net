namespace ReadingAdaptive.Application.Auth.Dtos;

public sealed record AuthUserDto(
    int UserId,
    string Username,
    string FullName,
    string Role,
    byte? Grade,
    string? Section);
