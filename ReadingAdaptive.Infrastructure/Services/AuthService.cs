using Microsoft.EntityFrameworkCore;
using ReadingAdaptive.Application.Auth.Dtos;
using ReadingAdaptive.Application.Auth.Exceptions;
using ReadingAdaptive.Application.Auth.Interfaces;
using ReadingAdaptive.Infrastructure.Persistence;
using ReadingAdaptive.Infrastructure.Persistence.Entities;

namespace ReadingAdaptive.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private const string StudentRoleName = "Student";

    private readonly ReadingAdaptiveDbContext _dbContext;
    private readonly IPasswordHashService _passwordHashService;

    public AuthService(
        ReadingAdaptiveDbContext dbContext,
        IPasswordHashService passwordHashService)
    {
        _dbContext = dbContext;
        _passwordHashService = passwordHashService;
    }

    public async Task<AuthUserDto> RegisterStudentAsync(
        RegisterStudentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var username = request.Username.Trim().ToLowerInvariant();
        var fullName = request.FullName.Trim();
        var section = string.IsNullOrWhiteSpace(request.Section)
            ? null
            : request.Section.Trim();

        var usernameExists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Username == username, cancellationToken);

        if (usernameExists)
        {
            throw new UsernameAlreadyExistsException(username);
        }

        var studentRole = await _dbContext.Roles
            .AsNoTracking()
            .SingleOrDefaultAsync(
                role => role.Name == StudentRoleName && role.IsActive,
                cancellationToken);

        if (studentRole is null)
        {
            throw new InvalidOperationException("The Student role is not configured.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var user = new User
        {
            RoleId = studentRole.RoleId,
            FullName = fullName,
            Username = username,
            PasswordHash = _passwordHashService.HashPassword(request.Password),
            IsActive = true
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var student = new Student
        {
            StudentId = user.UserId,
            Grade = request.Grade ?? 1,
            Section = section
        };

        _dbContext.Students.Add(student);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new AuthUserDto(
            user.UserId,
            user.Username,
            user.FullName,
            studentRole.Name,
            student.Grade,
            student.Section);
    }

    public async Task<AuthUserDto> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var username = request.Username.Trim();

        var user = await _dbContext.Users
            .AsNoTracking()
            .Include(item => item.Role)
            .Include(item => item.Student)
            .SingleOrDefaultAsync(
                item => item.Username == username && item.IsActive,
                cancellationToken);

        if (user is null || !user.Role.IsActive)
        {
            throw new InvalidCredentialsException();
        }

        var isValidPassword = _passwordHashService.VerifyPassword(request.Password, user.PasswordHash);

        if (!isValidPassword)
        {
            throw new InvalidCredentialsException();
        }

        return new AuthUserDto(
            user.UserId,
            user.Username,
            user.FullName,
            user.Role.Name,
            user.Student?.Grade,
            user.Student?.Section);
    }

    public async Task<AuthUserDto?> GetCurrentUserAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.UserId == userId && user.IsActive)
            .Select(user => new AuthUserDto(
                user.UserId,
                user.Username,
                user.FullName,
                user.Role.Name,
                user.Student != null ? user.Student.Grade : null,
                user.Student != null ? user.Student.Section : null))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
