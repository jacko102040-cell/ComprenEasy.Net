using Microsoft.EntityFrameworkCore;
using ReadingAdaptive.Application.AcademicContent.Dtos;
using ReadingAdaptive.Application.AcademicContent.Exceptions;
using ReadingAdaptive.Infrastructure.Persistence.Entities;

namespace ReadingAdaptive.Infrastructure.Services;

public sealed partial class AcademicContentService
{
    public async Task<IReadOnlyCollection<ContentQuestionListItemDto>> GetQuestionsAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        await EnsureHiddenAdminAsync(teacherId, cancellationToken);

        var items = await _dbContext.Questions
            .AsNoTracking()
            .OrderBy(item => item.Stem)
            .Select(item => new
            {
                item.QuestionId,
                item.Stem,
                DimensionName = item.Dimension.Name,
                item.QuestionType,
                DifficultyLevelName = item.DifficultyLevel != null ? item.DifficultyLevel.Name : null,
                item.IsActive
            })
            .ToListAsync(cancellationToken);

        var optionCounts = await _dbContext.QuestionOptions
            .AsNoTracking()
            .GroupBy(item => item.QuestionId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Key, item => item.Count, cancellationToken);

        return items
            .Select(item => new ContentQuestionListItemDto(
                item.QuestionId,
                item.Stem,
                item.DimensionName,
                item.QuestionType,
                item.DifficultyLevelName,
                item.IsActive,
                optionCounts.TryGetValue(item.QuestionId, out var count) ? count : 0))
            .ToList();
    }

    public async Task<ContentQuestionDetailDto> GetQuestionAsync(
        int teacherId,
        int questionId,
        CancellationToken cancellationToken = default)
    {
        await EnsureHiddenAdminAsync(teacherId, cancellationToken);

        var question = await _dbContext.Questions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.QuestionId == questionId, cancellationToken);

        if (question is null)
        {
            throw new AcademicContentNotFoundException("The selected question was not found.");
        }

        return await MapQuestionDetailAsync(question, cancellationToken);
    }

    public async Task<ContentQuestionDetailDto> CreateQuestionAsync(
        int teacherId,
        SaveContentQuestionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await EnsureHiddenAdminAsync(teacherId, cancellationToken);
        await ValidateQuestionRequestAsync(request, cancellationToken);

        var question = new Question
        {
            DimensionId = request.DimensionId,
            Stem = request.Stem.Trim(),
            QuestionType = request.QuestionType.Trim(),
            Explanation = NormalizeOptional(request.Explanation),
            DifficultyLevelId = request.DifficultyLevelId,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Questions.Add(question);
        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var option in request.Options)
        {
            _dbContext.QuestionOptions.Add(new QuestionOption
            {
                QuestionId = question.QuestionId,
                OptionText = option.OptionText.Trim(),
                IsCorrect = option.IsCorrect,
                DisplayOrder = option.DisplayOrder
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetQuestionAsync(teacherId, question.QuestionId, cancellationToken);
    }

    public async Task<ContentQuestionDetailDto> UpdateQuestionAsync(
        int teacherId,
        int questionId,
        SaveContentQuestionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await EnsureHiddenAdminAsync(teacherId, cancellationToken);
        await ValidateQuestionRequestAsync(request, cancellationToken);

        var question = await _dbContext.Questions
            .SingleOrDefaultAsync(item => item.QuestionId == questionId, cancellationToken);

        if (question is null)
        {
            throw new AcademicContentNotFoundException("The selected question was not found.");
        }

        question.DimensionId = request.DimensionId;
        question.Stem = request.Stem.Trim();
        question.QuestionType = request.QuestionType.Trim();
        question.Explanation = NormalizeOptional(request.Explanation);
        question.DifficultyLevelId = request.DifficultyLevelId;
        question.IsActive = request.IsActive;

        await UpsertQuestionOptionsAsync(questionId, request.Options, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetQuestionAsync(teacherId, questionId, cancellationToken);
    }

    public async Task ArchiveQuestionAsync(
        int teacherId,
        int questionId,
        CancellationToken cancellationToken = default)
    {
        await EnsureHiddenAdminAsync(teacherId, cancellationToken);

        var question = await _dbContext.Questions
            .SingleOrDefaultAsync(item => item.QuestionId == questionId, cancellationToken);

        if (question is null)
        {
            throw new AcademicContentNotFoundException("The selected question was not found.");
        }

        question.IsActive = false;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
