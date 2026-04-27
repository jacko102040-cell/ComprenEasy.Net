using Microsoft.EntityFrameworkCore;
using ReadingAdaptive.Application.AcademicContent.Dtos;
using ReadingAdaptive.Application.AcademicContent.Exceptions;
using ReadingAdaptive.Application.Readings.Constants;
using ReadingAdaptive.Infrastructure.Persistence.Entities;

namespace ReadingAdaptive.Infrastructure.Services;

public sealed partial class AcademicContentService
{
    private async Task<Teacher> EnsureHiddenAdminAsync(int teacherId, CancellationToken cancellationToken)
    {
        var teacher = await _dbContext.Teachers
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.TeacherId == teacherId, cancellationToken);

        if (teacher is null)
        {
            throw new AcademicContentAccessDeniedException("Authenticated user is not registered as a teacher.");
        }

        if (!teacher.IsHiddenAdmin)
        {
            throw new AcademicContentAccessDeniedException("Authenticated teacher is not allowed to manage academic content.");
        }

        return teacher;
    }

    private async Task ValidateReadingRequestAsync(
        SaveContentReadingRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new AcademicContentValidationException("Reading title is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new AcademicContentValidationException("Reading content is required.");
        }

        var difficultyExists = await _dbContext.DifficultyLevels
            .AsNoTracking()
            .AnyAsync(item => item.DifficultyLevelId == request.DifficultyLevelId, cancellationToken);

        if (!difficultyExists)
        {
            throw new AcademicContentValidationException("Selected difficulty level is invalid.");
        }

        if (request.Phases.Select(item => item.PhaseId).Distinct().Count() != request.Phases.Count)
        {
            throw new AcademicContentValidationException("Duplicated reading phases are not allowed.");
        }

        if (request.Phases.Select(item => item.DisplayOrder).Distinct().Count() != request.Phases.Count)
        {
            throw new AcademicContentValidationException("Duplicated phase display order values are not allowed.");
        }

        var phaseIds = request.Phases.Select(item => item.PhaseId).Distinct().ToList();

        if (phaseIds.Count > 0)
        {
            var existingPhaseCount = await _dbContext.Phases
                .AsNoTracking()
                .CountAsync(item => phaseIds.Contains(item.PhaseId), cancellationToken);

            if (existingPhaseCount != phaseIds.Count)
            {
                throw new AcademicContentValidationException("One or more selected reading phases are invalid.");
            }
        }
    }

    private async Task ValidateAssessmentRequestAsync(
        SaveContentAssessmentRequestDto request,
        CancellationToken cancellationToken)
    {
        var isReadingPractice = request.AssessmentType == ReadingAssessmentTypes.ReadingPractice;

        if (!SupportedAssessmentTypes.Contains(request.AssessmentType))
        {
            throw new AcademicContentValidationException("Unsupported assessment type.");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new AcademicContentValidationException("Assessment title is required.");
        }

        if (request.DifficultyLevelId.HasValue)
        {
            var difficultyExists = await _dbContext.DifficultyLevels
                .AsNoTracking()
                .AnyAsync(item => item.DifficultyLevelId == request.DifficultyLevelId.Value, cancellationToken);

            if (!difficultyExists)
            {
                throw new AcademicContentValidationException("Selected difficulty level is invalid.");
            }
        }

        if (isReadingPractice)
        {
            if (!request.ReadingId.HasValue)
            {
                throw new AcademicContentValidationException("ReadingPractice assessments must be linked to a reading.");
            }

            var readingExists = await _dbContext.Readings
                .AsNoTracking()
                .AnyAsync(item => item.ReadingId == request.ReadingId.Value, cancellationToken);

            if (!readingExists)
            {
                throw new AcademicContentValidationException("Selected reading is invalid.");
            }
        }
        else if (request.Questions.Any(item => item.PhaseId.HasValue))
        {
            throw new AcademicContentValidationException("Only ReadingPractice assessments can assign phases to questions.");
        }

        if (request.Questions.Select(item => item.QuestionId).Distinct().Count() != request.Questions.Count)
        {
            throw new AcademicContentValidationException("Duplicated questions are not allowed in the same assessment payload.");
        }

        if (request.Questions.Select(item => item.DisplayOrder).Distinct().Count() != request.Questions.Count)
        {
            throw new AcademicContentValidationException("Duplicated question display order values are not allowed.");
        }

        if (request.Questions.Any(item => item.Points <= 0))
        {
            throw new AcademicContentValidationException("Question points must be greater than 0.");
        }

        if (isReadingPractice && request.Questions.Any(item => !item.PhaseId.HasValue))
        {
            throw new AcademicContentValidationException("ReadingPractice questions must include a valid phase.");
        }

        var questionIds = request.Questions.Select(item => item.QuestionId).Distinct().ToList();

        if (questionIds.Count > 0)
        {
            var existingQuestionCount = await _dbContext.Questions
                .AsNoTracking()
                .CountAsync(item => questionIds.Contains(item.QuestionId), cancellationToken);

            if (existingQuestionCount != questionIds.Count)
            {
                throw new AcademicContentValidationException("One or more selected assessment questions are invalid.");
            }
        }

        var phaseIds = request.Questions
            .Where(item => item.PhaseId.HasValue)
            .Select(item => item.PhaseId!.Value)
            .Distinct()
            .ToList();

        if (phaseIds.Count > 0 && !isReadingPractice)
        {
            var existingPhaseCount = await _dbContext.Phases
                .AsNoTracking()
                .CountAsync(item => phaseIds.Contains(item.PhaseId), cancellationToken);

            if (existingPhaseCount != phaseIds.Count)
            {
                throw new AcademicContentValidationException("One or more selected assessment phases are invalid.");
            }
        }

        if (isReadingPractice)
        {
            await ValidateReadingPracticeQuestionPhasesAsync(request.ReadingId!.Value, phaseIds, cancellationToken);
        }
    }

    private async Task ValidateQuestionRequestAsync(
        SaveContentQuestionRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Stem))
        {
            throw new AcademicContentValidationException("Question stem is required.");
        }

        if (string.IsNullOrWhiteSpace(request.QuestionType))
        {
            throw new AcademicContentValidationException("Question type is required.");
        }

        if (request.Options.Count < 2)
        {
            throw new AcademicContentValidationException("At least two options are required.");
        }

        var correctOptionsCount = request.Options.Count(item => item.IsCorrect);

        if (correctOptionsCount != 1)
        {
            throw new AcademicContentValidationException("Exactly one option must be marked as correct.");
        }

        if (request.Options.Any(item => string.IsNullOrWhiteSpace(item.OptionText)))
        {
            throw new AcademicContentValidationException("Option text is required.");
        }

        if (request.Options.Select(item => item.DisplayOrder).Distinct().Count() != request.Options.Count)
        {
            throw new AcademicContentValidationException("Duplicated option display order values are not allowed.");
        }

        var dimensionExists = await _dbContext.Dimensions
            .AsNoTracking()
            .AnyAsync(item => item.DimensionId == request.DimensionId, cancellationToken);

        if (!dimensionExists)
        {
            throw new AcademicContentValidationException("Selected dimension is invalid.");
        }
    }

    private async Task UpsertReadingPhasesAsync(
        int readingId,
        IReadOnlyCollection<SaveContentReadingPhaseDto> phases,
        CancellationToken cancellationToken)
    {
        var existingPhases = await _dbContext.ReadingPhases
            .Where(item => item.ReadingId == readingId)
            .ToListAsync(cancellationToken);

        var existingByPhaseId = existingPhases.ToDictionary(item => item.PhaseId);

        foreach (var phase in phases)
        {
            if (existingByPhaseId.TryGetValue(phase.PhaseId, out var existing))
            {
                existing.DisplayOrder = phase.DisplayOrder;
                existing.IsEnabled = phase.IsEnabled;
                existing.IsRequired = phase.IsRequired;
                existing.GuidanceText = NormalizeOptional(phase.GuidanceText);
                existing.MinQuestionsToUnlockNext = phase.MinQuestionsToUnlockNext;
                continue;
            }

            _dbContext.ReadingPhases.Add(new ReadingPhase
            {
                ReadingId = readingId,
                PhaseId = phase.PhaseId,
                DisplayOrder = phase.DisplayOrder,
                IsEnabled = phase.IsEnabled,
                IsRequired = phase.IsRequired,
                GuidanceText = NormalizeOptional(phase.GuidanceText),
                MinQuestionsToUnlockNext = phase.MinQuestionsToUnlockNext
            });
        }

        var providedPhaseIds = phases.Select(item => item.PhaseId).ToHashSet();

        foreach (var missingPhase in existingPhases.Where(item => !providedPhaseIds.Contains(item.PhaseId)))
        {
            missingPhase.IsEnabled = false;
        }
    }

    private async Task UpsertAssessmentQuestionsAsync(
        int assessmentId,
        SaveContentAssessmentRequestDto request,
        CancellationToken cancellationToken)
    {
        var existingQuestions = await _dbContext.AssessmentQuestions
            .Where(item => item.AssessmentId == assessmentId)
            .ToListAsync(cancellationToken);

        var existingById = existingQuestions.ToDictionary(item => item.AssessmentQuestionId);

        foreach (var question in request.Questions)
        {
            if (question.AssessmentQuestionId.HasValue)
            {
                if (!existingById.TryGetValue(question.AssessmentQuestionId.Value, out var existing))
                {
                    throw new AcademicContentValidationException("One or more assessment question ids are invalid for this assessment.");
                }

                existing.QuestionId = question.QuestionId;
                existing.PhaseId = request.AssessmentType == ReadingAssessmentTypes.ReadingPractice
                    ? question.PhaseId
                    : null;
                existing.DisplayOrder = question.DisplayOrder;
                existing.Points = question.Points;
                existing.IsActive = question.IsActive;
                continue;
            }

            _dbContext.AssessmentQuestions.Add(new AssessmentQuestion
            {
                AssessmentId = assessmentId,
                QuestionId = question.QuestionId,
                PhaseId = request.AssessmentType == ReadingAssessmentTypes.ReadingPractice ? question.PhaseId : null,
                DisplayOrder = question.DisplayOrder,
                Points = question.Points,
                IsActive = question.IsActive
            });
        }

        var retainedIds = request.Questions
            .Where(item => item.AssessmentQuestionId.HasValue)
            .Select(item => item.AssessmentQuestionId!.Value)
            .ToHashSet();

        foreach (var missing in existingQuestions.Where(item => !retainedIds.Contains(item.AssessmentQuestionId)))
        {
            missing.IsActive = false;
        }
    }

    private async Task UpsertQuestionOptionsAsync(
        int questionId,
        IReadOnlyCollection<SaveContentQuestionOptionDto> options,
        CancellationToken cancellationToken)
    {
        var existingOptions = await _dbContext.QuestionOptions
            .Where(item => item.QuestionId == questionId)
            .ToListAsync(cancellationToken);

        var existingById = existingOptions.ToDictionary(item => item.OptionId);

        foreach (var option in options)
        {
            if (option.OptionId.HasValue && existingById.TryGetValue(option.OptionId.Value, out var existing))
            {
                existing.OptionText = option.OptionText.Trim();
                existing.IsCorrect = option.IsCorrect;
                existing.DisplayOrder = option.DisplayOrder;
                continue;
            }

            _dbContext.QuestionOptions.Add(new QuestionOption
            {
                QuestionId = questionId,
                OptionText = option.OptionText.Trim(),
                IsCorrect = option.IsCorrect,
                DisplayOrder = option.DisplayOrder
            });
        }

        var retainedIds = options
            .Where(item => item.OptionId.HasValue)
            .Select(item => item.OptionId!.Value)
            .ToHashSet();

        var removableOptions = existingOptions.Where(item => !retainedIds.Contains(item.OptionId)).ToList();

        if (removableOptions.Count == 0)
        {
            return;
        }

        var removableIds = removableOptions.Select(item => item.OptionId).ToList();
        var usedOptionIds = await _dbContext.AttemptAnswers
            .AsNoTracking()
            .Where(item => item.SelectedOptionId != null && removableIds.Contains(item.SelectedOptionId.Value))
            .Select(item => item.SelectedOptionId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (usedOptionIds.Count > 0)
        {
            throw new AcademicContentValidationException("Options already used in student attempts cannot be removed.");
        }

        _dbContext.QuestionOptions.RemoveRange(removableOptions);
    }

    private async Task ValidateReadingPracticeQuestionPhasesAsync(
        int readingId,
        IReadOnlyCollection<byte> phaseIds,
        CancellationToken cancellationToken)
    {
        if (phaseIds.Count == 0)
        {
            return;
        }

        var existingReadingPhaseIds = await _dbContext.ReadingPhases
            .AsNoTracking()
            .Where(item => item.ReadingId == readingId && phaseIds.Contains(item.PhaseId))
            .Select(item => item.PhaseId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (existingReadingPhaseIds.Count != phaseIds.Count)
        {
            throw new AcademicContentValidationException(
                "One or more selected phases do not belong to the linked reading.");
        }
    }

    private async Task<ContentQuestionDetailDto> MapQuestionDetailAsync(
        Question question,
        CancellationToken cancellationToken)
    {
        var options = await _dbContext.QuestionOptions
            .AsNoTracking()
            .Where(item => item.QuestionId == question.QuestionId)
            .OrderBy(item => item.DisplayOrder)
            .Select(item => new ContentQuestionOptionEditorDto(
                item.OptionId,
                item.OptionText,
                item.IsCorrect,
                item.DisplayOrder))
            .ToListAsync(cancellationToken);

        return new ContentQuestionDetailDto(
            question.QuestionId,
            question.DimensionId,
            question.Stem,
            question.QuestionType,
            question.Explanation,
            question.DifficultyLevelId,
            question.IsActive,
            options);
    }

    private static ContentReadingDetailDto MapReadingDetail(Reading reading)
    {
        return new ContentReadingDetailDto(
            reading.ReadingId,
            reading.Title,
            reading.Summary,
            reading.Content,
            reading.ImageUrl,
            reading.DifficultyLevelId,
            reading.EstimatedMinutes,
            reading.IsActive,
            reading.ReadingPhases
                .OrderBy(item => item.DisplayOrder)
                .Select(item => new ContentReadingPhaseEditorDto(
                    item.PhaseId,
                    item.Phase.Code,
                    item.Phase.DisplayName,
                    item.DisplayOrder,
                    item.IsEnabled,
                    item.IsRequired,
                    item.GuidanceText,
                    item.MinQuestionsToUnlockNext))
                .ToList());
    }

    private static ContentAssessmentDetailDto MapAssessmentDetail(Persistence.Entities.Assessment assessment)
    {
        return new ContentAssessmentDetailDto(
            assessment.AssessmentId,
            assessment.AssessmentType,
            assessment.ReadingId,
            assessment.Title,
            assessment.Description,
            assessment.DifficultyLevelId,
            assessment.IsActive,
            assessment.AssessmentQuestions
                .OrderBy(item => item.DisplayOrder)
                .Select(item => new ContentAssessmentQuestionEditorDto(
                    item.AssessmentQuestionId,
                    item.QuestionId,
                    item.Question.Stem,
                    item.Question.Dimension.Name,
                    item.PhaseId,
                    item.DisplayOrder,
                    item.Points,
                    item.IsActive))
                .ToList());
    }

    private static int? NormalizeAssessmentReadingId(SaveContentAssessmentRequestDto request)
    {
        return request.AssessmentType == ReadingAssessmentTypes.ReadingPractice
            ? request.ReadingId
            : null;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
