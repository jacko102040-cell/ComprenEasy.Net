using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using ReadingAdaptive.Application.Adaptive.Constants;
using ReadingAdaptive.Application.Adaptive.Dtos;
using ReadingAdaptive.Application.Auth.Interfaces;
using ReadingAdaptive.Application.Evaluations.Constants;
using ReadingAdaptive.Application.Readings.Constants;
using ReadingAdaptive.Application.TeacherPanel.Dtos;
using ReadingAdaptive.Application.TeacherPanel.Exceptions;
using ReadingAdaptive.Application.TeacherPanel.Interfaces;
using ReadingAdaptive.Infrastructure.Persistence;
using ReadingAdaptive.Infrastructure.Persistence.Entities;

namespace ReadingAdaptive.Infrastructure.Services;

public sealed class TeacherPanelService : ITeacherPanelService
{
    private const string CompletedStatus = "Completed";

    private readonly ReadingAdaptiveDbContext _dbContext;
    private readonly IPasswordHashService _passwordHashService;

    public TeacherPanelService(
        ReadingAdaptiveDbContext dbContext,
        IPasswordHashService passwordHashService)
    {
        _dbContext = dbContext;
        _passwordHashService = passwordHashService;
    }

    public async Task<IReadOnlyCollection<TeacherStudentListItemDto>> GetStudentsAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        await EnsureTeacherAsync(teacherId, cancellationToken);

        var students = await _dbContext.Students
            .AsNoTracking()
            .Include(student => student.StudentNavigation)
            .OrderBy(student => student.StudentNavigation.FullName)
            .Select(student => new
            {
                student.StudentId,
                student.StudentNavigation.FullName,
                student.StudentNavigation.Username,
                student.Grade,
                student.Section,
                student.IsEnabledForTest,
                student.StudentNavigation.IsActive,
                LastAttemptAt = student.AssessmentAttempts
                    .OrderByDescending(attempt => attempt.FinishedAt ?? attempt.StartedAt)
                    .Select(attempt => (DateTime?)(attempt.FinishedAt ?? attempt.StartedAt))
                    .FirstOrDefault(),
                LatestScore = student.AssessmentAttempts
                    .Where(attempt => attempt.Status == CompletedStatus && attempt.TotalScore != null)
                    .OrderByDescending(attempt => attempt.FinishedAt ?? attempt.StartedAt)
                    .Select(attempt => attempt.TotalScore)
                    .FirstOrDefault(),
                LatestRecommendationAction = student.AdaptiveRecommendations
                    .OrderByDescending(recommendation => recommendation.CreatedAt)
                    .Select(recommendation => recommendation.PredictedAction)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return students
            .Select(student => new TeacherStudentListItemDto(
                student.StudentId,
                student.FullName,
                student.Username,
                student.Grade,
                student.Section,
                student.IsEnabledForTest,
                student.IsActive,
                student.LastAttemptAt,
                student.LatestScore,
                student.LatestRecommendationAction))
            .ToList();
    }

    public async Task<TeacherStudentDetailDto> GetStudentDetailAsync(
        int teacherId,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        await EnsureTeacherAsync(teacherId, cancellationToken);

        var student = await _dbContext.Students
            .AsNoTracking()
            .Include(item => item.StudentNavigation)
            .SingleOrDefaultAsync(item => item.StudentId == studentId, cancellationToken);

        if (student is null)
        {
            throw new TeacherPanelNotFoundException("No se encontro el estudiante seleccionado.");
        }

        var attempts = await _dbContext.AssessmentAttempts
            .AsNoTracking()
            .Where(attempt => attempt.StudentId == studentId)
            .Include(attempt => attempt.Assessment)
            .OrderByDescending(attempt => attempt.StartedAt)
            .ToListAsync(cancellationToken);

        var evaluationAttempts = attempts
            .Where(attempt => attempt.Assessment.AssessmentType != ReadingAssessmentTypes.ReadingPractice)
            .Select(attempt => new TeacherStudentAssessmentAttemptDto(
                attempt.AttemptId,
                attempt.AssessmentId,
                attempt.Assessment.Title,
                attempt.Assessment.AssessmentType,
                attempt.Status,
                attempt.StartedAt,
                attempt.FinishedAt,
                attempt.TotalScore,
                attempt.CompletionPercentage,
                attempt.TotalTimeSeconds))
            .ToList();

        var readingSessions = attempts
            .Where(attempt => attempt.Assessment.AssessmentType == ReadingAssessmentTypes.ReadingPractice)
            .Select(attempt => new TeacherStudentReadingSessionDto(
                attempt.AttemptId,
                attempt.AssessmentId,
                attempt.Assessment.ReadingId,
                attempt.Assessment.ReadingId != null ? attempt.Assessment.Title : attempt.Assessment.Title,
                attempt.Status,
                attempt.StartedAt,
                attempt.FinishedAt,
                attempt.CompletionPercentage,
                attempt.TotalTimeSeconds))
            .ToList();

        var recommendations = await _dbContext.AdaptiveRecommendations
            .AsNoTracking()
            .Where(recommendation => recommendation.StudentId == studentId)
            .Include(recommendation => recommendation.CurrentDifficultyLevel)
            .Include(recommendation => recommendation.RecommendedDifficultyLevel)
            .Include(recommendation => recommendation.RecommendedAssessment)
            .Include(recommendation => recommendation.SourceAttempt)
                .ThenInclude(attempt => attempt.Assessment)
            .OrderByDescending(recommendation => recommendation.CreatedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        var commentTags = await GetActiveCommentTagsInternalAsync(cancellationToken);

        var comments = await _dbContext.TeacherComments
            .AsNoTracking()
            .Where(comment => comment.StudentId == studentId)
            .Include(comment => comment.CommentTag)
            .Include(comment => comment.Teacher)
                .ThenInclude(teacher => teacher.TeacherNavigation)
            .OrderByDescending(comment => comment.CreatedAt)
            .Take(20)
            .Select(comment => new TeacherStudentCommentDto(
                comment.TeacherCommentId,
                comment.Teacher.TeacherNavigation.FullName,
                comment.CommentTagId,
                comment.CommentTag.Name,
                comment.AttemptId,
                comment.CommentText,
                comment.CreatedAt))
            .ToListAsync(cancellationToken);

        var completedEvaluationCount = evaluationAttempts.Count(attempt => attempt.Status == CompletedStatus);
        var completedReadingCount = readingSessions.Count(session => session.Status == CompletedStatus);
        var latestCompletedScore = evaluationAttempts
            .Where(attempt => attempt.Status == CompletedStatus && attempt.TotalScore != null)
            .OrderByDescending(attempt => attempt.FinishedAt ?? attempt.StartedAt)
            .Select(attempt => attempt.TotalScore)
            .FirstOrDefault();
        var lastActivityAt = attempts
            .OrderByDescending(attempt => attempt.FinishedAt ?? attempt.StartedAt)
            .Select(attempt => (DateTime?)(attempt.FinishedAt ?? attempt.StartedAt))
            .FirstOrDefault();

        var progressSummary = new TeacherStudentProgressSummaryDto(
            completedEvaluationCount,
            completedReadingCount,
            latestCompletedScore,
            recommendations.FirstOrDefault()?.PredictedAction,
            lastActivityAt);

        return new TeacherStudentDetailDto(
            student.StudentId,
            student.StudentNavigation.FullName,
            student.StudentNavigation.Username,
            student.Grade,
            student.Section,
            student.IsEnabledForTest,
            student.StudentNavigation.IsActive,
            progressSummary,
            evaluationAttempts,
            readingSessions,
            recommendations.Select(MapAdaptiveRecommendation).ToList(),
            comments,
            commentTags);
    }

    public async Task<IReadOnlyCollection<TeacherCommentTagDto>> GetCommentTagsAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        await EnsureTeacherAsync(teacherId, cancellationToken);
        return await GetActiveCommentTagsInternalAsync(cancellationToken);
    }

    public async Task<ResetStudentPasswordResponseDto> ResetStudentPasswordAsync(
        int teacherId,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        var teacher = await EnsureTeacherAsync(teacherId, cancellationToken);

        if (!teacher.CanResetPasswords)
        {
            throw new TeacherPanelAccessDeniedException("El docente autenticado no puede restablecer las contrasenas de los estudiantes.");
        }

        var studentUser = await _dbContext.Users
            .Include(user => user.Student)
            .SingleOrDefaultAsync(
                user => user.UserId == studentId && user.Student != null && user.IsActive,
                cancellationToken);

        if (studentUser is null)
        {
            throw new TeacherPanelNotFoundException("No se encontro el estudiante seleccionado.");
        }

        var temporaryPassword = BuildTemporaryPassword(studentId);
        studentUser.PasswordHash = _passwordHashService.HashPassword(temporaryPassword);
        studentUser.UpdatedAt = DateTime.UtcNow;

        _dbContext.PasswordResetLogs.Add(new PasswordResetLog
        {
            StudentId = studentId,
            ResetByUserId = teacherId,
            Reason = "Teacher panel password reset.",
            ResetAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ResetStudentPasswordResponseDto(
            studentId,
            studentUser.FullName,
            temporaryPassword,
            DateTime.UtcNow);
    }

    public async Task<TeacherStudentCommentDto> AddCommentAsync(
        int teacherId,
        int studentId,
        CreateTeacherCommentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await EnsureTeacherAsync(teacherId, cancellationToken);
        await EnsureStudentExistsAsync(studentId, cancellationToken);

        var commentTag = await _dbContext.CommentTags
            .AsNoTracking()
            .SingleOrDefaultAsync(tag => tag.CommentTagId == request.CommentTagId && tag.IsActive, cancellationToken);

        if (commentTag is null)
        {
            throw new TeacherPanelValidationException("La etiqueta de comentario seleccionada no esta disponible.");
        }

        if (request.AttemptId is long attemptId)
        {
            var ownsAttempt = await _dbContext.AssessmentAttempts
                .AsNoTracking()
                .AnyAsync(attempt => attempt.AttemptId == attemptId && attempt.StudentId == studentId, cancellationToken);

            if (!ownsAttempt)
            {
                throw new TeacherPanelValidationException("El intento seleccionado no pertenece al estudiante.");
            }
        }

        var comment = new TeacherComment
        {
            TeacherId = teacherId,
            StudentId = studentId,
            AttemptId = request.AttemptId,
            CommentTagId = request.CommentTagId,
            CommentText = request.CommentText.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.TeacherComments.Add(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var savedComment = await _dbContext.TeacherComments
            .AsNoTracking()
            .Where(item => item.TeacherCommentId == comment.TeacherCommentId)
            .Include(item => item.CommentTag)
            .Include(item => item.Teacher)
                .ThenInclude(teacher => teacher.TeacherNavigation)
            .Select(item => new TeacherStudentCommentDto(
                item.TeacherCommentId,
                item.Teacher.TeacherNavigation.FullName,
                item.CommentTagId,
                item.CommentTag.Name,
                item.AttemptId,
                item.CommentText,
                item.CreatedAt))
            .SingleAsync(cancellationToken);

        return savedComment;
    }

    private async Task<Teacher> EnsureTeacherAsync(int teacherId, CancellationToken cancellationToken)
    {
        var teacher = await _dbContext.Teachers
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.TeacherId == teacherId, cancellationToken);

        return teacher
            ?? throw new TeacherPanelAccessDeniedException("El usuario autenticado no esta registrado como docente.");
    }

    private async Task EnsureStudentExistsAsync(int studentId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Students
            .AsNoTracking()
            .AnyAsync(student => student.StudentId == studentId, cancellationToken);

        if (!exists)
        {
            throw new TeacherPanelNotFoundException("No se encontro el estudiante seleccionado.");
        }
    }

    private async Task<IReadOnlyCollection<TeacherCommentTagDto>> GetActiveCommentTagsInternalAsync(
        CancellationToken cancellationToken)
    {
        return await _dbContext.CommentTags
            .AsNoTracking()
            .Where(tag => tag.IsActive)
            .OrderBy(tag => tag.Name)
            .Select(tag => new TeacherCommentTagDto(tag.CommentTagId, tag.Name))
            .ToListAsync(cancellationToken);
    }

    private static string BuildTemporaryPassword(int studentId)
    {
        var suffix = Convert.ToHexString(RandomNumberGenerator.GetBytes(3));
        return $"Temp{studentId}{suffix}!";
    }

    private static AdaptiveRecommendationDto MapAdaptiveRecommendation(AdaptiveRecommendation recommendation)
    {
        var recommendedActivityType = recommendation.RecommendedAssessment?.AssessmentType ??
            (string.Equals(
                recommendation.SourceAttempt.Assessment.AssessmentType,
                ReadingAssessmentTypes.ReadingPractice,
                StringComparison.Ordinal)
                ? AdaptiveActivityTypes.Reading
                : null);

        return new AdaptiveRecommendationDto(
            recommendation.RecommendationId,
            recommendation.SourceAttemptId,
            recommendation.SourceAttempt.AssessmentId,
            recommendation.SourceAttempt.Assessment.AssessmentType,
            recommendation.SourceAttempt.Assessment.Title,
            recommendation.CurrentDifficultyLevelId,
            recommendation.CurrentDifficultyLevel.Name,
            recommendation.RecommendedDifficultyLevelId,
            recommendation.RecommendedDifficultyLevel.Name,
            recommendation.PredictedAction,
            recommendedActivityType,
            null,
            recommendation.RecommendedAssessment?.ReadingId,
            recommendation.RecommendedAssessmentId,
            recommendation.RecommendedAssessment?.Title,
            recommendation.EngineType,
            recommendation.ConfidenceScore,
            recommendation.CreatedAt);
    }
}
