using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ReadingAdaptive.Application.AcademicFlow.Interfaces;
using ReadingAdaptive.Application.Adaptive.Exceptions;
using ReadingAdaptive.Application.Adaptive.Interfaces;
using ReadingAdaptive.Application.Readings.Constants;
using ReadingAdaptive.Application.Readings.Dtos;
using ReadingAdaptive.Application.Readings.Exceptions;
using ReadingAdaptive.Application.Readings.Interfaces;
using ReadingAdaptive.Infrastructure.Persistence;
using ReadingAdaptive.Infrastructure.Persistence.Entities;

namespace ReadingAdaptive.Infrastructure.Services;

public sealed class ReadingService : IReadingService
{
    private const string CompletedStatus = "Completed";
    private const string InProgressStatus = "InProgress";
    private const string PendingStatus = "Pending";
    private const string ImmediateFeedbackType = "Immediate";
    private const string FinalFeedbackType = "Final";
    private const string InfoSeverity = "Info";

    private readonly ReadingAdaptiveDbContext _dbContext;
    private readonly IAcademicFlowService _academicFlowService;
    private readonly IAdaptiveRecommendationService _adaptiveRecommendationService;

    public ReadingService(
        ReadingAdaptiveDbContext dbContext,
        IAcademicFlowService academicFlowService,
        IAdaptiveRecommendationService adaptiveRecommendationService)
    {
        _dbContext = dbContext;
        _academicFlowService = academicFlowService;
        _adaptiveRecommendationService = adaptiveRecommendationService;
    }

    public async Task<IReadOnlyCollection<ActiveReadingDto>> GetActiveReadingsAsync(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        await EnsureReadingsAccessibleAsync(studentId, cancellationToken);

        return await _dbContext.Readings
            .AsNoTracking()
            .Where(reading => reading.IsActive)
            .OrderBy(reading => reading.Title)
            .Select(reading => new ActiveReadingDto(
                reading.ReadingId,
                reading.Title,
                reading.Summary,
                reading.ImageUrl,
                reading.DifficultyLevelId,
                reading.DifficultyLevel.Name,
                reading.EstimatedMinutes,
                reading.ReadingPhases.Count(phase => phase.IsEnabled)))
            .ToListAsync(cancellationToken);
    }

    public async Task<ReadingDetailDto> GetReadingDetailAsync(
        int readingId,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        await EnsureReadingsAccessibleAsync(studentId, cancellationToken);

        var reading = await _dbContext.Readings
            .AsNoTracking()
            .Where(item => item.ReadingId == readingId && item.IsActive)
            .Select(item => new ReadingDetailDto(
                item.ReadingId,
                item.Title,
                item.Summary,
                item.Content,
                item.ImageUrl,
                item.DifficultyLevelId,
                item.DifficultyLevel.Name,
                item.EstimatedMinutes))
            .SingleOrDefaultAsync(cancellationToken);

        if (reading is null)
        {
            throw new ReadingNotFoundException("Active reading was not found.");
        }

        return reading;
    }

    public async Task<IReadOnlyCollection<ReadingPhaseDto>> GetReadingPhasesAsync(
        int readingId,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        await EnsureReadingsAccessibleAsync(studentId, cancellationToken);
        await EnsureReadingExistsAsync(readingId, cancellationToken);

        return await _dbContext.ReadingPhases
            .AsNoTracking()
            .Where(phase => phase.ReadingId == readingId)
            .OrderBy(phase => phase.DisplayOrder)
            .Select(phase => new ReadingPhaseDto(
                phase.PhaseId,
                phase.Phase.Code,
                phase.Phase.DisplayName,
                phase.DisplayOrder,
                phase.IsEnabled,
                phase.IsRequired,
                phase.GuidanceText,
                phase.MinQuestionsToUnlockNext))
            .ToListAsync(cancellationToken);
    }

    public async Task<ReadingSessionProgressDto> StartReadingSessionAsync(
        int readingId,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        await EnsureReadingsAccessibleAsync(studentId, cancellationToken);

        var reading = await GetActiveReadingEntityAsync(readingId, cancellationToken);
        var assessment = await GetOrCreateReadingAssessmentAsync(reading, cancellationToken);

        var existingAttempt = await _dbContext.AssessmentAttempts
            .Include(attempt => attempt.Assessment)
            .Include(attempt => attempt.AttemptPhaseProgresses)
                .ThenInclude(progress => progress.Phase)
            .AsSplitQuery()
            .SingleOrDefaultAsync(
                attempt =>
                    attempt.AssessmentId == assessment.AssessmentId &&
                    attempt.StudentId == studentId &&
                    attempt.Status == InProgressStatus,
                cancellationToken);

        if (existingAttempt is not null)
        {
            return await BuildSessionProgressDtoAsync(existingAttempt, cancellationToken);
        }

        var readingPhases = await GetEnabledReadingPhasesAsync(readingId, cancellationToken);

        if (readingPhases.Count == 0)
        {
            throw new ReadingValidationException("The selected reading has no enabled phases configured.");
        }

        var lastAttemptNumber = await _dbContext.AssessmentAttempts
            .AsNoTracking()
            .Where(attempt => attempt.AssessmentId == assessment.AssessmentId && attempt.StudentId == studentId)
            .Select(attempt => (int?)attempt.AttemptNumber)
            .MaxAsync(cancellationToken) ?? 0;

        var attempt = new AssessmentAttempt
        {
            AssessmentId = assessment.AssessmentId,
            StudentId = studentId,
            AttemptNumber = checked((byte)(lastAttemptNumber + 1)),
            Status = InProgressStatus,
            StartedAt = DateTime.UtcNow,
            TotalScore = 0m,
            LiteralScore = 0m,
            InferentialScore = 0m,
            CriticalScore = 0m,
            TotalCorrect = 0,
            TotalErrors = 0
        };

        _dbContext.AssessmentAttempts.Add(attempt);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var phaseProgresses = readingPhases
            .Select((phase, index) => new AttemptPhaseProgress
            {
                AttemptId = attempt.AttemptId,
                PhaseId = phase.PhaseId,
                SequenceOrder = checked((byte)(index + 1)),
                Status = index == 0 ? InProgressStatus : PendingStatus,
                StartedAt = index == 0 ? DateTime.UtcNow : null,
                UnlockReason = index == 0 ? "Initial reading phase unlocked." : null
            })
            .ToList();

        _dbContext.AttemptPhaseProgresses.AddRange(phaseProgresses);
        await _dbContext.SaveChangesAsync(cancellationToken);

        attempt.AttemptPhaseProgresses = phaseProgresses;
        attempt.Assessment = assessment;

        return await BuildSessionProgressDtoAsync(attempt, cancellationToken);
    }

    public async Task<ReadingSessionProgressDto> SavePhaseProgressAsync(
        long attemptId,
        byte phaseId,
        int studentId,
        SaveReadingPhaseProgressRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var (attempt, phaseProgress, readingPhase) = await GetOwnedReadingPhaseAsync(
            attemptId,
            phaseId,
            studentId,
            cancellationToken);

        EnsureSessionIsInProgress(attempt);

        if (string.Equals(phaseProgress.Status, CompletedStatus, StringComparison.OrdinalIgnoreCase))
        {
            throw new ReadingValidationException("Completed phases cannot receive additional progress.");
        }

        if (phaseProgress.StartedAt is null)
        {
            phaseProgress.StartedAt = DateTime.UtcNow;
        }

        phaseProgress.Status = InProgressStatus;
        phaseProgress.TimeSpentSeconds = (phaseProgress.TimeSpentSeconds ?? 0) + request.TimeSpentSeconds;

        if (!string.IsNullOrWhiteSpace(request.ProgressNote))
        {
            phaseProgress.UnlockReason = request.ProgressNote.Trim();
        }

        await SavePhaseAnswersAsync(
            attempt,
            phaseId,
            request.Answers,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await BuildSessionProgressDtoAsync(attempt, cancellationToken);
    }

    public async Task<ReadingSessionProgressDto> CompletePhaseAsync(
        long attemptId,
        byte phaseId,
        int studentId,
        SaveReadingPhaseProgressRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var (attempt, phaseProgress, readingPhase) = await GetOwnedReadingPhaseAsync(
            attemptId,
            phaseId,
            studentId,
            cancellationToken);

        EnsureSessionIsInProgress(attempt);

        if (phaseProgress.StartedAt is null)
        {
            phaseProgress.StartedAt = DateTime.UtcNow;
        }

        phaseProgress.Status = CompletedStatus;
        phaseProgress.CompletedAt = DateTime.UtcNow;
        phaseProgress.TimeSpentSeconds = (phaseProgress.TimeSpentSeconds ?? 0) + request.TimeSpentSeconds;

        if (!string.IsNullOrWhiteSpace(request.ProgressNote))
        {
            phaseProgress.UnlockReason = request.ProgressNote.Trim();
        }

        await SavePhaseAnswersAsync(
            attempt,
            phaseId,
            request.Answers,
            cancellationToken);

        await EnsurePhaseMeetsMinimumAnswersAsync(
            attempt,
            readingPhase,
            cancellationToken);

        await LogFeedbackAsync(
            attempt.AttemptId,
            phaseId,
            $"{readingPhase.Phase.DisplayName} phase completed.",
            ImmediateFeedbackType,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await BuildSessionProgressDtoAsync(attempt, cancellationToken);
    }

    public async Task<ReadingSessionProgressDto> GetReadingSessionProgressAsync(
        long attemptId,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        var attempt = await GetOwnedReadingAttemptAsync(attemptId, studentId, cancellationToken);
        return await BuildSessionProgressDtoAsync(attempt, cancellationToken);
    }

    public async Task<ReadingSessionProgressDto> FinishReadingSessionAsync(
        long attemptId,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        var attempt = await GetOwnedReadingAttemptAsync(attemptId, studentId, cancellationToken);
        EnsureSessionIsInProgress(attempt);

        var requiredPhases = await _dbContext.ReadingPhases
            .AsNoTracking()
            .Where(phase => phase.ReadingId == attempt.Assessment.ReadingId && phase.IsEnabled && phase.IsRequired)
            .Select(phase => phase.PhaseId)
            .ToListAsync(cancellationToken);

        var completedRequiredPhaseIds = attempt.AttemptPhaseProgresses
            .Where(progress => string.Equals(progress.Status, CompletedStatus, StringComparison.OrdinalIgnoreCase))
            .Select(progress => progress.PhaseId)
            .ToHashSet();

        if (requiredPhases.Any(phaseId => !completedRequiredPhaseIds.Contains(phaseId)))
        {
            throw new ReadingValidationException("All required phases must be completed before finishing the reading session.");
        }

        var enabledPhaseCount = attempt.AttemptPhaseProgresses.Count;
        var completedPhaseCount = attempt.AttemptPhaseProgresses.Count(progress =>
            string.Equals(progress.Status, CompletedStatus, StringComparison.OrdinalIgnoreCase));
        var totalTimeSeconds = attempt.AttemptPhaseProgresses.Sum(progress => progress.TimeSpentSeconds ?? 0);
        var snapshot = await BuildReadingAttemptSnapshotAsync(attempt, cancellationToken);

        if (totalTimeSeconds == 0)
        {
            totalTimeSeconds = await _dbContext.AttemptAnswers
                .AsNoTracking()
                .Where(answer => answer.AttemptId == attempt.AttemptId)
                .SumAsync(answer => answer.AnswerTimeSeconds ?? 0, cancellationToken);
        }

        if (totalTimeSeconds == 0)
        {
            totalTimeSeconds = Math.Max(0, Convert.ToInt32((DateTime.UtcNow - attempt.StartedAt).TotalSeconds));
        }

        attempt.Status = CompletedStatus;
        attempt.FinishedAt = DateTime.UtcNow;
        attempt.TotalTimeSeconds = totalTimeSeconds;
        attempt.CompletionPercentage = snapshot.TotalQuestions == 0
            ? (enabledPhaseCount == 0
                ? 0m
                : Math.Round((decimal)completedPhaseCount / enabledPhaseCount * 100m, 2, MidpointRounding.AwayFromZero))
            : snapshot.CompletionPercentage;
        attempt.TotalScore = snapshot.TotalScore;
        attempt.LiteralScore = snapshot.LiteralScore;
        attempt.InferentialScore = snapshot.InferentialScore;
        attempt.CriticalScore = snapshot.CriticalScore;
        attempt.TotalCorrect = snapshot.TotalCorrect;
        attempt.TotalErrors = snapshot.TotalErrors;

        await LogFeedbackAsync(
            attempt.AttemptId,
            null,
            "Reading session completed.",
            FinalFeedbackType,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await TryGenerateAdaptiveRecommendationAsync(attempt.AttemptId, studentId, cancellationToken);

        return await BuildSessionProgressDtoAsync(attempt, cancellationToken);
    }

    private async Task EnsureStudentAsync(int studentId, CancellationToken cancellationToken)
    {
        var student = await _dbContext.Students
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.StudentId == studentId, cancellationToken);

        if (student is null)
        {
            throw new ReadingAccessDeniedException("Authenticated user is not registered as a student.");
        }

        if (!student.IsEnabledForTest)
        {
            throw new ReadingAccessDeniedException("Student is not enabled for reading sessions.");
        }
    }

    private async Task EnsureReadingsAccessibleAsync(int studentId, CancellationToken cancellationToken)
    {
        await EnsureStudentAsync(studentId, cancellationToken);

        var summary = await _academicFlowService.GetCurrentSummaryAsync(studentId, cancellationToken);

        if (summary.CanAccessReadings)
        {
            return;
        }

        if (summary.HasCompletedPosttest)
        {
            throw new ReadingValidationException(
                "Reading intervention has already been closed. Review the final comparison instead.");
        }

        throw new ReadingValidationException(
            "Reading intervention becomes available after completing the pretest.");
    }

    private async Task EnsureReadingExistsAsync(int readingId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Readings
            .AsNoTracking()
            .AnyAsync(reading => reading.ReadingId == readingId && reading.IsActive, cancellationToken);

        if (!exists)
        {
            throw new ReadingNotFoundException("Active reading was not found.");
        }
    }

    private async Task<Reading> GetActiveReadingEntityAsync(int readingId, CancellationToken cancellationToken)
    {
        var reading = await _dbContext.Readings
            .Include(item => item.DifficultyLevel)
            .SingleOrDefaultAsync(item => item.ReadingId == readingId && item.IsActive, cancellationToken);

        if (reading is null)
        {
            throw new ReadingNotFoundException("Active reading was not found.");
        }

        return reading;
    }

    private async Task<List<ReadingPhase>> GetEnabledReadingPhasesAsync(
        int readingId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ReadingPhases
            .Include(phase => phase.Phase)
            .Where(phase => phase.ReadingId == readingId && phase.IsEnabled)
            .OrderBy(phase => phase.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    private async Task<Assessment> GetOrCreateReadingAssessmentAsync(
        Reading reading,
        CancellationToken cancellationToken)
    {
        var existingAssessment = await _dbContext.Assessments
            .SingleOrDefaultAsync(
                assessment =>
                    assessment.ReadingId == reading.ReadingId &&
                    assessment.AssessmentType == ReadingAssessmentTypes.ReadingPractice,
                cancellationToken);

        if (existingAssessment is not null)
        {
            return existingAssessment;
        }

        var assessment = new Assessment
        {
            AssessmentType = ReadingAssessmentTypes.ReadingPractice,
            ReadingId = reading.ReadingId,
            Title = reading.Title,
            Description = $"Reading session for {reading.Title}",
            DifficultyLevelId = reading.DifficultyLevelId,
            IsActive = true
        };

        _dbContext.Assessments.Add(assessment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return assessment;
    }

    private async Task<AssessmentAttempt> GetOwnedReadingAttemptAsync(
        long attemptId,
        int studentId,
        CancellationToken cancellationToken)
    {
        await EnsureStudentAsync(studentId, cancellationToken);

        var attempt = await _dbContext.AssessmentAttempts
            .Include(item => item.Assessment)
            .ThenInclude(assessment => assessment.Reading)
            .Include(item => item.AttemptPhaseProgresses)
            .ThenInclude(progress => progress.Phase)
            .AsSplitQuery()
            .SingleOrDefaultAsync(item => item.AttemptId == attemptId, cancellationToken);

        if (attempt is null)
        {
            throw new ReadingNotFoundException("Reading session was not found.");
        }

        if (attempt.StudentId != studentId)
        {
            throw new ReadingAccessDeniedException("You cannot operate on another student's reading session.");
        }

        if (!string.Equals(
                attempt.Assessment.AssessmentType,
                ReadingAssessmentTypes.ReadingPractice,
                StringComparison.Ordinal))
        {
            throw new ReadingValidationException("The selected attempt does not belong to a reading session.");
        }

        return attempt;
    }

    private async Task<(AssessmentAttempt Attempt, AttemptPhaseProgress PhaseProgress, ReadingPhase ReadingPhase)>
        GetOwnedReadingPhaseAsync(
            long attemptId,
            byte phaseId,
            int studentId,
            CancellationToken cancellationToken)
    {
        var attempt = await GetOwnedReadingAttemptAsync(attemptId, studentId, cancellationToken);

        var phaseProgress = attempt.AttemptPhaseProgresses
            .SingleOrDefault(progress => progress.PhaseId == phaseId);

        if (phaseProgress is null)
        {
            throw new ReadingNotFoundException("Reading phase progress was not found for this session.");
        }

        var readingId = attempt.Assessment.ReadingId
            ?? throw new ReadingValidationException("Reading session is not linked to a valid reading.");

        var readingPhase = await _dbContext.ReadingPhases
            .Include(item => item.Phase)
            .SingleOrDefaultAsync(
                item => item.ReadingId == readingId && item.PhaseId == phaseId && item.IsEnabled,
                cancellationToken);

        if (readingPhase is null)
        {
            throw new ReadingValidationException("The selected phase is not enabled for this reading.");
        }

        return (attempt, phaseProgress, readingPhase);
    }

    private void EnsureSessionIsInProgress(AssessmentAttempt attempt)
    {
        if (!string.Equals(attempt.Status, InProgressStatus, StringComparison.OrdinalIgnoreCase))
        {
            throw new ReadingValidationException("Only in-progress reading sessions can be updated.");
        }
    }

    private async Task SavePhaseAnswersAsync(
        AssessmentAttempt attempt,
        byte phaseId,
        IReadOnlyCollection<SaveReadingPhaseAnswerItemDto> answers,
        CancellationToken cancellationToken)
    {
        if (answers.Count == 0)
        {
            return;
        }

        if (answers.Select(answer => answer.QuestionId).Distinct().Count() != answers.Count)
        {
            throw new ReadingValidationException("Duplicated question ids are not allowed in the same request.");
        }

        var questionIds = answers
            .Select(answer => answer.QuestionId)
            .Distinct()
            .ToList();

        var phaseQuestions = await _dbContext.AssessmentQuestions
            .AsNoTracking()
            .Where(question =>
                question.AssessmentId == attempt.AssessmentId &&
                question.IsActive &&
                question.PhaseId == phaseId &&
                questionIds.Contains(question.QuestionId))
            .Select(question => new
            {
                question.QuestionId,
                question.Points
            })
            .ToListAsync(cancellationToken);

        if (phaseQuestions.Count != questionIds.Count)
        {
            throw new ReadingValidationException("One or more questions do not belong to the selected phase.");
        }

        var optionIds = answers
            .Select(answer => answer.SelectedOptionId)
            .Distinct()
            .ToList();

        var options = await _dbContext.QuestionOptions
            .AsNoTracking()
            .Where(option => optionIds.Contains(option.OptionId))
            .Select(option => new
            {
                option.OptionId,
                option.QuestionId,
                option.IsCorrect
            })
            .ToListAsync(cancellationToken);

        if (options.Count != optionIds.Count)
        {
            throw new ReadingValidationException("One or more selected options are invalid.");
        }

        var questionPoints = phaseQuestions.ToDictionary(question => question.QuestionId, question => question.Points);
        var optionsById = options.ToDictionary(option => option.OptionId);

        var existingAnswers = await _dbContext.AttemptAnswers
            .Where(answer => answer.AttemptId == attempt.AttemptId && questionIds.Contains(answer.QuestionId))
            .ToDictionaryAsync(answer => answer.QuestionId, cancellationToken);

        var answeredAt = DateTime.UtcNow;

        foreach (var incomingAnswer in answers)
        {
            if (!optionsById.TryGetValue(incomingAnswer.SelectedOptionId, out var selectedOption) ||
                selectedOption.QuestionId != incomingAnswer.QuestionId)
            {
                throw new ReadingValidationException("A selected option does not match its question.");
            }

            var isCorrect = selectedOption.IsCorrect;
            var scoreObtained = isCorrect ? questionPoints[incomingAnswer.QuestionId] : 0m;

            if (existingAnswers.TryGetValue(incomingAnswer.QuestionId, out var existingAnswer))
            {
                existingAnswer.SelectedOptionId = incomingAnswer.SelectedOptionId;
                existingAnswer.IsCorrect = isCorrect;
                existingAnswer.ScoreObtained = scoreObtained;
                existingAnswer.AnswerTimeSeconds = incomingAnswer.AnswerTimeSeconds;
                existingAnswer.AnsweredAt = answeredAt;
                continue;
            }

            _dbContext.AttemptAnswers.Add(new AttemptAnswer
            {
                AttemptId = attempt.AttemptId,
                QuestionId = incomingAnswer.QuestionId,
                SelectedOptionId = incomingAnswer.SelectedOptionId,
                IsCorrect = isCorrect,
                ScoreObtained = scoreObtained,
                AnswerTimeSeconds = incomingAnswer.AnswerTimeSeconds,
                AnsweredAt = answeredAt
            });
        }
    }

    private async Task<ReadingSessionProgressDto> BuildSessionProgressDtoAsync(
        AssessmentAttempt attempt,
        CancellationToken cancellationToken)
    {
        var readingId = attempt.Assessment.ReadingId
            ?? throw new ReadingValidationException("Reading session is not linked to a valid reading.");

        var readingPhases = await _dbContext.ReadingPhases
            .AsNoTracking()
            .Include(item => item.Phase)
            .Where(item => item.ReadingId == readingId && item.IsEnabled)
            .OrderBy(item => item.DisplayOrder)
            .ToListAsync(cancellationToken);
        var questionsByPhaseId = await BuildReadingPhaseQuestionsAsync(
            attempt.AssessmentId,
            attempt.AttemptId,
            cancellationToken);
        var snapshot = await BuildReadingAttemptSnapshotAsync(attempt, cancellationToken);

        var phaseProgressesByPhaseId = attempt.AttemptPhaseProgresses
            .ToDictionary(progress => progress.PhaseId);

        var phaseDtos = readingPhases
            .Select(config =>
            {
                phaseProgressesByPhaseId.TryGetValue(config.PhaseId, out var progress);
                questionsByPhaseId.TryGetValue(config.PhaseId, out var questions);
                questions ??= [];

                return new PhaseProgressDto(
                    config.PhaseId,
                    config.Phase.Code,
                    config.Phase.DisplayName,
                    progress?.SequenceOrder ?? config.DisplayOrder,
                    config.IsRequired,
                    config.MinQuestionsToUnlockNext,
                    progress?.Status ?? PendingStatus,
                    progress?.StartedAt,
                    progress?.CompletedAt,
                    progress?.TimeSpentSeconds ?? 0,
                    config.GuidanceText,
                    questions.Count(question => question.SelectedOptionId != null),
                    questions.Count,
                    questions);
            })
            .OrderBy(item => item.SequenceOrder)
            .ToList();

        var totalPhases = phaseDtos.Count;
        var completedPhases = phaseDtos.Count(phase =>
            string.Equals(phase.Status, CompletedStatus, StringComparison.OrdinalIgnoreCase));
        var completionPercentage = totalPhases == 0
            ? 0m
            : Math.Round((decimal)completedPhases / totalPhases * 100m, 2, MidpointRounding.AwayFromZero);
        var totalTimeSeconds = attempt.TotalTimeSeconds ?? phaseDtos.Sum(phase => phase.TimeSpentSeconds);
        var currentPhase = phaseDtos.FirstOrDefault(phase =>
            !string.Equals(phase.Status, CompletedStatus, StringComparison.OrdinalIgnoreCase));

        return new ReadingSessionProgressDto(
            attempt.AttemptId,
            readingId,
            attempt.AssessmentId,
            attempt.Assessment.Reading?.Title ?? attempt.Assessment.Title,
            attempt.Status,
            attempt.AttemptNumber,
            attempt.StartedAt,
            attempt.FinishedAt,
            string.Equals(attempt.Status, CompletedStatus, StringComparison.OrdinalIgnoreCase) &&
            attempt.CompletionPercentage.HasValue
                ? attempt.CompletionPercentage.Value
                : completionPercentage,
            attempt.TotalScore ?? snapshot.TotalScore,
            attempt.LiteralScore ?? snapshot.LiteralScore,
            attempt.InferentialScore ?? snapshot.InferentialScore,
            attempt.CriticalScore ?? snapshot.CriticalScore,
            attempt.TotalCorrect ?? snapshot.TotalCorrect,
            attempt.TotalErrors ?? snapshot.TotalErrors,
            totalTimeSeconds,
            currentPhase?.PhaseId,
            currentPhase?.Code,
            phaseDtos);
    }

    private async Task<Dictionary<byte, IReadOnlyCollection<ReadingPhaseQuestionDto>>> BuildReadingPhaseQuestionsAsync(
        int assessmentId,
        long attemptId,
        CancellationToken cancellationToken)
    {
        var questionRows = await _dbContext.AssessmentQuestions
            .AsNoTracking()
            .Where(question => question.AssessmentId == assessmentId && question.IsActive && question.PhaseId != null)
            .OrderBy(question => question.PhaseId)
            .ThenBy(question => question.DisplayOrder)
            .Select(question => new
            {
                PhaseId = question.PhaseId!.Value,
                question.QuestionId,
                question.DisplayOrder,
                question.Points,
                question.Question.DimensionId,
                DimensionName = question.Question.Dimension.Name,
                question.Question.Stem
            })
            .ToListAsync(cancellationToken);

        var questionIds = questionRows.Select(question => question.QuestionId).Distinct().ToList();

        if (questionIds.Count == 0)
        {
            return [];
        }

        var options = await _dbContext.QuestionOptions
            .AsNoTracking()
            .Where(option => questionIds.Contains(option.QuestionId))
            .OrderBy(option => option.QuestionId)
            .ThenBy(option => option.DisplayOrder)
            .Select(option => new
            {
                option.QuestionId,
                Dto = new ReadingPhaseQuestionOptionDto(
                    option.OptionId,
                    option.OptionText,
                    option.DisplayOrder)
            })
            .ToListAsync(cancellationToken);

        var answers = await _dbContext.AttemptAnswers
            .AsNoTracking()
            .Where(answer => answer.AttemptId == attemptId && questionIds.Contains(answer.QuestionId))
            .ToDictionaryAsync(answer => answer.QuestionId, cancellationToken);

        var optionsByQuestionId = options
            .GroupBy(option => option.QuestionId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<ReadingPhaseQuestionOptionDto>)group.Select(item => item.Dto).ToList());

        return questionRows
            .GroupBy(question => question.PhaseId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<ReadingPhaseQuestionDto>)group
                    .Select(question =>
                    {
                        answers.TryGetValue(question.QuestionId, out var answer);

                        return new ReadingPhaseQuestionDto(
                            question.QuestionId,
                            question.DisplayOrder,
                            question.DimensionId,
                            question.DimensionName,
                            question.Stem,
                            question.Points,
                            answer?.SelectedOptionId,
                            answer?.IsCorrect,
                            answer?.ScoreObtained ?? 0m,
                            answer?.AnswerTimeSeconds ?? 0,
                            optionsByQuestionId.TryGetValue(question.QuestionId, out var questionOptions)
                                ? questionOptions
                                : []);
                    })
                    .ToList());
    }

    private async Task<ReadingAttemptSnapshot> BuildReadingAttemptSnapshotAsync(
        AssessmentAttempt attempt,
        CancellationToken cancellationToken)
    {
        var questionData = await _dbContext.AssessmentQuestions
            .AsNoTracking()
            .Where(question =>
                question.AssessmentId == attempt.AssessmentId &&
                question.IsActive &&
                question.PhaseId != null)
            .Select(question => new
            {
            question.QuestionId,
            question.Points,
            DimensionName = question.Question.Dimension.Name
        })
        .ToListAsync(cancellationToken);

        if (questionData.Count == 0)
        {
            return new ReadingAttemptSnapshot(0m, 0m, 0m, 0m, 0, 0, 0m, 0);
        }

        var answers = await _dbContext.AttemptAnswers
            .AsNoTracking()
            .Where(answer => answer.AttemptId == attempt.AttemptId)
            .ToListAsync(cancellationToken);

        var answersByQuestionId = answers.ToDictionary(answer => answer.QuestionId);
        var totalAvailablePoints = questionData.Sum(question => question.Points);
        var totalScoreObtained = 0m;
        var totalCorrect = 0;
        var totalIncorrect = 0;
        var answeredQuestions = 0;
        var literalAvailable = 0m;
        var inferentialAvailable = 0m;
        var criticalAvailable = 0m;
        var literalObtained = 0m;
        var inferentialObtained = 0m;
        var criticalObtained = 0m;

        foreach (var question in questionData)
        {
            answersByQuestionId.TryGetValue(question.QuestionId, out var answer);

            var scoreObtained = answer?.ScoreObtained ?? 0m;

            if (answer?.SelectedOptionId is not null)
            {
                answeredQuestions++;
            }

            if (answer?.IsCorrect == true)
            {
                totalCorrect++;
            }
            else if (answer?.SelectedOptionId is not null)
            {
                totalIncorrect++;
            }

            totalScoreObtained += scoreObtained;

            switch (MapDimensionBucket(question.DimensionName))
            {
                case DimensionBucket.Literal:
                    literalAvailable += question.Points;
                    literalObtained += scoreObtained;
                    break;
                case DimensionBucket.Inferential:
                    inferentialAvailable += question.Points;
                    inferentialObtained += scoreObtained;
                    break;
                case DimensionBucket.Critical:
                    criticalAvailable += question.Points;
                    criticalObtained += scoreObtained;
                    break;
            }
        }

        return new ReadingAttemptSnapshot(
            CalculateScorePercentage(totalScoreObtained, totalAvailablePoints),
            CalculateScorePercentage(literalObtained, literalAvailable),
            CalculateScorePercentage(inferentialObtained, inferentialAvailable),
            CalculateScorePercentage(criticalObtained, criticalAvailable),
            totalCorrect,
            totalIncorrect,
            RoundPercentage(answeredQuestions, questionData.Count),
            questionData.Count);
    }

    private static decimal CalculateScorePercentage(decimal obtained, decimal available)
    {
        if (available <= 0)
        {
            return 0m;
        }

        return Math.Round((obtained / available) * 100m, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal RoundPercentage(int numerator, int denominator)
    {
        if (denominator <= 0)
        {
            return 0m;
        }

        return Math.Round((decimal)numerator / denominator * 100m, 2, MidpointRounding.AwayFromZero);
    }

    private async Task EnsurePhaseMeetsMinimumAnswersAsync(
        AssessmentAttempt attempt,
        ReadingPhase readingPhase,
        CancellationToken cancellationToken)
    {
        if (!readingPhase.IsRequired ||
            !readingPhase.MinQuestionsToUnlockNext.HasValue ||
            readingPhase.MinQuestionsToUnlockNext.Value <= 0)
        {
            return;
        }

        var phaseQuestionIds = await _dbContext.AssessmentQuestions
            .AsNoTracking()
            .Where(question =>
                question.AssessmentId == attempt.AssessmentId &&
                question.IsActive &&
                question.PhaseId == readingPhase.PhaseId)
            .Select(question => question.QuestionId)
            .ToListAsync(cancellationToken);

        if (phaseQuestionIds.Count == 0)
        {
            return;
        }

        var answeredCount = await _dbContext.AttemptAnswers
            .AsNoTracking()
            .Where(answer =>
                answer.AttemptId == attempt.AttemptId &&
                answer.SelectedOptionId != null &&
                phaseQuestionIds.Contains(answer.QuestionId))
            .Select(answer => answer.QuestionId)
            .Distinct()
            .CountAsync(cancellationToken);

        if (answeredCount >= readingPhase.MinQuestionsToUnlockNext.Value)
        {
            return;
        }

        throw new ReadingValidationException(
            $"You need to answer at least {readingPhase.MinQuestionsToUnlockNext.Value} question(s) before completing the {readingPhase.Phase.DisplayName} phase.");
    }

    private static DimensionBucket MapDimensionBucket(string dimensionName)
    {
        var normalized = Normalize(dimensionName);

        if (normalized.Contains("infer"))
        {
            return DimensionBucket.Inferential;
        }

        if (normalized.Contains("crit"))
        {
            return DimensionBucket.Critical;
        }

        return DimensionBucket.Literal;
    }

    private static string Normalize(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private async Task TryGenerateAdaptiveRecommendationAsync(
        long attemptId,
        int studentId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _adaptiveRecommendationService.GenerateRecommendationAsync(
                attemptId,
                studentId,
                cancellationToken);
        }
        catch (AdaptiveAccessDeniedException)
        {
        }
        catch (AdaptiveNotFoundException)
        {
        }
        catch (AdaptiveValidationException)
        {
        }
    }

    private async Task LogFeedbackAsync(
        long attemptId,
        byte? phaseId,
        string message,
        string feedbackType,
        CancellationToken cancellationToken)
    {
        _dbContext.FeedbackLogs.Add(new FeedbackLog
        {
            AttemptId = attemptId,
            PhaseId = phaseId,
            FeedbackType = feedbackType,
            Message = message,
            Severity = InfoSeverity
        });

        await Task.CompletedTask;
    }

    private sealed record ReadingAttemptSnapshot(
        decimal TotalScore,
        decimal LiteralScore,
        decimal InferentialScore,
        decimal CriticalScore,
        int TotalCorrect,
        int TotalErrors,
        decimal CompletionPercentage,
        int TotalQuestions);

    private enum DimensionBucket
    {
        Literal,
        Inferential,
        Critical
    }
}
