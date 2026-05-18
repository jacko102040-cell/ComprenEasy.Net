using Microsoft.EntityFrameworkCore;
using ReadingAdaptive.Application.AcademicContent.Dtos;
using ReadingAdaptive.Application.AcademicContent.Exceptions;
using ReadingAdaptive.Application.AcademicContent.Interfaces;
using ReadingAdaptive.Infrastructure.Persistence.Entities;

namespace ReadingAdaptive.Infrastructure.Services;

public sealed partial class AcademicContentService : IAcademicContentService
{
    public async Task<ContentLookupsDto> GetLookupsAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        await EnsureHiddenAdminAsync(teacherId, cancellationToken);

        var difficultyLevels = await _dbContext.DifficultyLevels
            .AsNoTracking()
            .OrderBy(item => item.RankOrder)
            .Select(item => new ContentLookupItemDto(item.DifficultyLevelId, item.Name))
            .ToListAsync(cancellationToken);

        var dimensions = await _dbContext.Dimensions
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .Select(item => new ContentLookupItemDto(item.DimensionId, item.Name))
            .ToListAsync(cancellationToken);

        var phases = await _dbContext.Phases
            .AsNoTracking()
            .OrderBy(item => item.DefaultOrder)
            .Select(item => new ContentPhaseLookupDto(item.PhaseId, item.Code, item.DisplayName, item.DefaultOrder))
            .ToListAsync(cancellationToken);

        var readings = await _dbContext.Readings
            .AsNoTracking()
            .OrderBy(item => item.Title)
            .Select(item => new ContentLookupItemDto(item.ReadingId, item.Title))
            .ToListAsync(cancellationToken);

        var questions = await _dbContext.Questions
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.Stem)
            .Select(item => new ContentQuestionLookupDto(item.QuestionId, item.Stem, item.Dimension.Name))
            .ToListAsync(cancellationToken);

        return new ContentLookupsDto(difficultyLevels, dimensions, phases, readings, questions);
    }

    public async Task<IReadOnlyCollection<ContentReadingListItemDto>> GetReadingsAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        await EnsureHiddenAdminAsync(teacherId, cancellationToken);

        return await _dbContext.Readings
            .AsNoTracking()
            .OrderBy(item => item.Title)
            .Select(item => new ContentReadingListItemDto(
                item.ReadingId,
                item.Title,
                item.DifficultyLevel.Name,
                item.IsActive,
                item.ReadingPhases.Count(phase => phase.IsEnabled),
                item.Assessments.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<ContentReadingDetailDto> GetReadingAsync(
        int teacherId,
        int readingId,
        CancellationToken cancellationToken = default)
    {
        await EnsureHiddenAdminAsync(teacherId, cancellationToken);

        var reading = await _dbContext.Readings
            .AsNoTracking()
            .Include(item => item.ReadingPhases)
                .ThenInclude(item => item.Phase)
            .SingleOrDefaultAsync(item => item.ReadingId == readingId, cancellationToken);

        if (reading is null)
        {
            throw new AcademicContentNotFoundException("No se encontro la lectura seleccionada.");
        }

        return MapReadingDetail(reading);
    }

    public async Task<ContentReadingDetailDto> CreateReadingAsync(
        int teacherId,
        SaveContentReadingRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await EnsureHiddenAdminAsync(teacherId, cancellationToken);
        await ValidateReadingRequestAsync(request, cancellationToken);

        var reading = new Reading
        {
            Title = request.Title.Trim(),
            Summary = NormalizeOptional(request.Summary),
            Content = request.Content.Trim(),
            ImageUrl = NormalizeOptional(request.ImageUrl),
            DifficultyLevelId = request.DifficultyLevelId,
            EstimatedMinutes = request.EstimatedMinutes,
            IsActive = request.IsActive,
            CreatedByUserId = teacherId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Readings.Add(reading);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await UpsertReadingPhasesAsync(reading.ReadingId, request.Phases, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetReadingAsync(teacherId, reading.ReadingId, cancellationToken);
    }

    public async Task<ContentReadingDetailDto> UpdateReadingAsync(
        int teacherId,
        int readingId,
        SaveContentReadingRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await EnsureHiddenAdminAsync(teacherId, cancellationToken);
        await ValidateReadingRequestAsync(request, cancellationToken);

        var reading = await _dbContext.Readings
            .Include(item => item.ReadingPhases)
            .SingleOrDefaultAsync(item => item.ReadingId == readingId, cancellationToken);

        if (reading is null)
        {
            throw new AcademicContentNotFoundException("No se encontro la lectura seleccionada.");
        }

        reading.Title = request.Title.Trim();
        reading.Summary = NormalizeOptional(request.Summary);
        reading.Content = request.Content.Trim();
        reading.ImageUrl = NormalizeOptional(request.ImageUrl);
        reading.DifficultyLevelId = request.DifficultyLevelId;
        reading.EstimatedMinutes = request.EstimatedMinutes;
        reading.IsActive = request.IsActive;

        await UpsertReadingPhasesAsync(readingId, request.Phases, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetReadingAsync(teacherId, readingId, cancellationToken);
    }

    public async Task ArchiveReadingAsync(
        int teacherId,
        int readingId,
        CancellationToken cancellationToken = default)
    {
        await EnsureHiddenAdminAsync(teacherId, cancellationToken);

        var reading = await _dbContext.Readings
            .SingleOrDefaultAsync(item => item.ReadingId == readingId, cancellationToken);

        if (reading is null)
        {
            throw new AcademicContentNotFoundException("No se encontro la lectura seleccionada.");
        }

        reading.IsActive = false;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
