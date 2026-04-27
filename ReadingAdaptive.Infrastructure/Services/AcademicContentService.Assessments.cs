using Microsoft.EntityFrameworkCore;
using ReadingAdaptive.Application.AcademicContent.Dtos;
using ReadingAdaptive.Application.AcademicContent.Exceptions;
using ReadingAdaptive.Application.Readings.Constants;

namespace ReadingAdaptive.Infrastructure.Services;

public sealed partial class AcademicContentService
{
    public async Task<IReadOnlyCollection<ContentAssessmentListItemDto>> GetAssessmentsAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        await EnsureHiddenAdminAsync(teacherId, cancellationToken);

        return await _dbContext.Assessments
            .AsNoTracking()
            .Where(item => SupportedAssessmentTypes.Contains(item.AssessmentType))
            .OrderBy(item => item.Title)
            .Select(item => new ContentAssessmentListItemDto(
                item.AssessmentId,
                item.AssessmentType,
                item.Title,
                item.Reading != null ? item.Reading.Title : null,
                item.DifficultyLevelId,
                item.DifficultyLevel != null ? item.DifficultyLevel.Name : null,
                item.IsActive,
                item.AssessmentQuestions.Count(question => question.IsActive)))
            .ToListAsync(cancellationToken);
    }

    public async Task<ContentAssessmentDetailDto> GetAssessmentAsync(
        int teacherId,
        int assessmentId,
        CancellationToken cancellationToken = default)
    {
        await EnsureHiddenAdminAsync(teacherId, cancellationToken);

        var assessment = await _dbContext.Assessments
            .AsNoTracking()
            .Include(item => item.AssessmentQuestions)
                .ThenInclude(item => item.Question)
                    .ThenInclude(item => item.Dimension)
            .SingleOrDefaultAsync(item => item.AssessmentId == assessmentId, cancellationToken);

        if (assessment is null || !SupportedAssessmentTypes.Contains(assessment.AssessmentType))
        {
            throw new AcademicContentNotFoundException("The selected assessment was not found.");
        }

        return MapAssessmentDetail(assessment);
    }

    public async Task<ContentAssessmentDetailDto> CreateAssessmentAsync(
        int teacherId,
        SaveContentAssessmentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await EnsureHiddenAdminAsync(teacherId, cancellationToken);
        await ValidateAssessmentRequestAsync(request, cancellationToken);

        var assessment = new Persistence.Entities.Assessment
        {
            AssessmentType = request.AssessmentType,
            ReadingId = NormalizeAssessmentReadingId(request),
            Title = request.Title.Trim(),
            Description = NormalizeOptional(request.Description),
            DifficultyLevelId = request.DifficultyLevelId,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Assessments.Add(assessment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await UpsertAssessmentQuestionsAsync(assessment.AssessmentId, request, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetAssessmentAsync(teacherId, assessment.AssessmentId, cancellationToken);
    }

    public async Task<ContentAssessmentDetailDto> UpdateAssessmentAsync(
        int teacherId,
        int assessmentId,
        SaveContentAssessmentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await EnsureHiddenAdminAsync(teacherId, cancellationToken);
        await ValidateAssessmentRequestAsync(request, cancellationToken);

        var assessment = await _dbContext.Assessments
            .Include(item => item.AssessmentQuestions)
            .SingleOrDefaultAsync(item => item.AssessmentId == assessmentId, cancellationToken);

        if (assessment is null || !SupportedAssessmentTypes.Contains(assessment.AssessmentType))
        {
            throw new AcademicContentNotFoundException("The selected assessment was not found.");
        }

        assessment.AssessmentType = request.AssessmentType;
        assessment.ReadingId = NormalizeAssessmentReadingId(request);
        assessment.Title = request.Title.Trim();
        assessment.Description = NormalizeOptional(request.Description);
        assessment.DifficultyLevelId = request.DifficultyLevelId;
        assessment.IsActive = request.IsActive;

        await UpsertAssessmentQuestionsAsync(assessmentId, request, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetAssessmentAsync(teacherId, assessmentId, cancellationToken);
    }

    public async Task ArchiveAssessmentAsync(
        int teacherId,
        int assessmentId,
        CancellationToken cancellationToken = default)
    {
        await EnsureHiddenAdminAsync(teacherId, cancellationToken);

        var assessment = await _dbContext.Assessments
            .SingleOrDefaultAsync(item => item.AssessmentId == assessmentId, cancellationToken);

        if (assessment is null)
        {
            throw new AcademicContentNotFoundException("The selected assessment was not found.");
        }

        assessment.IsActive = false;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
