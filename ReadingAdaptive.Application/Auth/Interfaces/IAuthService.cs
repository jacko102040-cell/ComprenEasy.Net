using ReadingAdaptive.Application.Auth.Dtos;

namespace ReadingAdaptive.Application.Auth.Interfaces;

public interface IAuthService
{
    Task<AuthUserDto> RegisterStudentAsync(
        RegisterStudentRequestDto request,
        CancellationToken cancellationToken = default);

    Task<AuthUserDto> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default);

    Task<AuthUserDto?> GetCurrentUserAsync(
        int userId,
        CancellationToken cancellationToken = default);
}
