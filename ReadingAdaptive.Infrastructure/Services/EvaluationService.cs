using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ReadingAdaptive.Application.AcademicFlow.Interfaces;
using ReadingAdaptive.Application.Adaptive.Exceptions;
using ReadingAdaptive.Application.Adaptive.Interfaces;
using ReadingAdaptive.Application.Evaluations.Constants;
using ReadingAdaptive.Application.Evaluations.Dtos;
using ReadingAdaptive.Application.Evaluations.Exceptions;
using ReadingAdaptive.Application.Evaluations.Interfaces;
using ReadingAdaptive.Infrastructure.Persistence;
using ReadingAdaptive.Infrastructure.Persistence.Entities;

namespace ReadingAdaptive.Infrastructure.Services;

public sealed class EvaluationService : IEvaluationService
{
    private const string CompletedStatus = "Completed";
    private const string InProgressStatus = "InProgress";
    private const string ReadingsStage = "Readings";
    private readonly ReadingAdaptiveDbContext _dbContext;
    private readonly IAcademicFlowService _academicFlowService;
    private readonly IAdaptiveRecommendationService _adaptiveRecommendationService;

    public EvaluationService(
        ReadingAdaptiveDbContext dbContext,
        IAcademicFlowService academicFlowService,
        IAdaptiveRecommendationService adaptiveRecommendationService)
    {
        _dbContext = dbContext;
        _academicFlowService = academicFlowService;
        _adaptiveRecommendationService = adaptiveRecommendationService;
    }

    public async Task<IReadOnlyCollection<ActiveAssessmentDto>> GetActiveAssessmentsAsync(
        string assessmentType,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        ValidateAssessmentType(assessmentType);
        await EnsureStudentAsync(studentId, cancellationToken);
        await EnsureAssessmentTypeAccessibleAsync(assessmentType, studentId, cancellationToken);

        return await _dbContext.Assessments
            .AsNoTracking()
            .Where(assessment => assessment.IsActive && assessment.AssessmentType == assessmentType)
            .OrderBy(assessment => assessment.Title)
            .Select(assessment => new ActiveAssessmentDto(
                assessment.AssessmentId,
                assessment.AssessmentType,
                assessment.Title,
                assessment.Description,
                assessment.DifficultyLevelId,
                assessment.AssessmentQuestions.Count(question => question.IsActive)))
            .ToListAsync(cancellationToken);
    }

    public async Task<AssessmentDetailDto> GetAssessmentDetailAsync(
        int assessmentId,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        await EnsureStudentAsync(studentId, cancellationToken);

        var assessment = await _dbContext.Assessments
            .AsNoTracking()
            .Where(item =>
                item.AssessmentId == assessmentId &&
                item.IsActive &&
                AssessmentTypes.SupportedTypes.Contains(item.AssessmentType))
            .Select(item => new
            {
                item.AssessmentId,
                item.AssessmentType,
                item.Title,
                item.Description,
                item.DifficultyLevelId
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (assessment is null)
        {
            throw new EvaluationNotFoundException("No se encontro una evaluacion activa.");
        }

        await EnsureAssessmentTypeAccessibleAsync(
            assessment.AssessmentType,
            studentId,
            cancellationToken);

        var questions = await _dbContext.AssessmentQuestions
            .AsNoTracking()
            .Where(item => item.AssessmentId == assessmentId && item.IsActive && item.Question.IsActive)
            .OrderBy(item => item.DisplayOrder)
            .Select(item => new
            {
                item.QuestionId,
                item.DisplayOrder,
                item.Points,
                item.Question.DimensionId,
                DimensionName = item.Question.Dimension.Name,
                item.Question.Stem
            })
            .ToListAsync(cancellationToken);

        var questionIds = questions.Select(question => question.QuestionId).ToList();

        var options = await _dbContext.QuestionOptions
            .AsNoTracking()
            .Where(option => questionIds.Contains(option.QuestionId))
            .OrderBy(option => option.QuestionId)
            .ThenBy(option => option.DisplayOrder)
            .Select(option => new
            {
                option.QuestionId,
                Dto = new AssessmentOptionDto(
                    option.OptionId,
                    option.OptionText,
                    option.DisplayOrder)
            })
            .ToListAsync(cancellationToken);

        var optionsByQuestionId = options
            .GroupBy(option => option.QuestionId)
            .ToDictionary(group => group.Key, group => (IReadOnlyCollection<AssessmentOptionDto>)group.Select(item => item.Dto).ToList());

        var questionDtos = questions
            .Select(question => new AssessmentQuestionDto(
                question.QuestionId,
                question.DisplayOrder,
                question.DimensionId,
                question.DimensionName,
                question.Stem,
                question.Points,
                optionsByQuestionId.TryGetValue(question.QuestionId, out var questionOptions)
                    ? questionOptions
                    : []))
            .ToList();

        return new AssessmentDetailDto(
            assessment.AssessmentId,
            assessment.AssessmentType,
            assessment.Title,
            assessment.Description,
            assessment.DifficultyLevelId,
            questionDtos);
    }

    public async Task<StartAssessmentAttemptResponseDto> StartAttemptAsync(
        int assessmentId,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        await EnsureStudentAsync(studentId, cancellationToken);

        var assessment = await GetActiveAssessmentEntityAsync(assessmentId, cancellationToken);
        await EnsureAssessmentTypeAccessibleAsync(
            assessment.AssessmentType,
            studentId,
            cancellationToken);

        var currentAttempt = await _dbContext.AssessmentAttempts
            .AsNoTracking()
            .Where(attempt =>
                attempt.AssessmentId == assessmentId &&
                attempt.StudentId == studentId &&
                attempt.Status == InProgressStatus)
            .OrderByDescending(attempt => attempt.StartedAt)
            .Select(attempt => new StartAssessmentAttemptResponseDto(
                attempt.AttemptId,
                attempt.AssessmentId,
                attempt.AttemptNumber,
                attempt.Status,
                attempt.StartedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (currentAttempt is not null)
        {
            return currentAttempt;
        }

        var hasCompletedAttempt = await _dbContext.AssessmentAttempts
            .AsNoTracking()
            .AnyAsync(
                attempt =>
                    attempt.AssessmentId == assessmentId &&
                    attempt.StudentId == studentId &&
                    attempt.Status == CompletedStatus,
                cancellationToken);

        if (hasCompletedAttempt)
        {
            throw new EvaluationValidationException(
                "Ya existe un intento completado para esta evaluacion.");
        }

        var lastAttemptNumber = await _dbContext.AssessmentAttempts
            .AsNoTracking()
            .Where(attempt => attempt.AssessmentId == assessmentId && attempt.StudentId == studentId)
            .Select(attempt => (int?)attempt.AttemptNumber)
            .MaxAsync(cancellationToken) ?? 0;

        var nextAttemptNumber = checked((byte)(lastAttemptNumber + 1));

        var attempt = new AssessmentAttempt
        {
            AssessmentId = assessment.AssessmentId,
            StudentId = studentId,
            AttemptNumber = nextAttemptNumber,
            Status = InProgressStatus,
            StartedAt = DateTime.UtcNow
        };

        _dbContext.AssessmentAttempts.Add(attempt);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new StartAssessmentAttemptResponseDto(
            attempt.AttemptId,
            attempt.AssessmentId,
            attempt.AttemptNumber,
            attempt.Status,
            attempt.StartedAt);
    }

    public async Task<SaveAttemptAnswersResponseDto> SaveAnswersAsync(
        long attemptId,
        int studentId,
        SaveAttemptAnswersRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.Answers.Count == 0)
        {
            throw new EvaluationValidationException("Se requiere al menos una respuesta.");
        }

        if (request.Answers.Select(answer => answer.QuestionId).Distinct().Count() != request.Answers.Count)
        {
            throw new EvaluationValidationException("No se permiten ids de pregunta duplicados en la misma solicitud.");
        }

        var attempt = await GetOwnedAttemptEntityAsync(attemptId, studentId, cancellationToken);

        if (!string.Equals(attempt.Status, InProgressStatus, StringComparison.OrdinalIgnoreCase))
        {
            throw new EvaluationValidationException("Solo los intentos en curso pueden recibir respuestas.");
        }

        var questionIds = request.Answers
            .Select(answer => answer.QuestionId)
            .Distinct()
            .ToList();

        var assessmentQuestions = await _dbContext.AssessmentQuestions
            .AsNoTracking()
            .Where(question =>
                question.AssessmentId == attempt.AssessmentId &&
                question.IsActive &&
                questionIds.Contains(question.QuestionId))
            .Select(question => new
            {
                question.QuestionId,
                question.Points
            })
            .ToListAsync(cancellationToken);

        if (assessmentQuestions.Count != questionIds.Count)
        {
            throw new EvaluationValidationException("Una o mas preguntas no pertenecen a la evaluacion seleccionada.");
        }

        var optionIds = request.Answers
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
            throw new EvaluationValidationException("Una o mas opciones seleccionadas no son validas.");
        }

        var questionPoints = assessmentQuestions.ToDictionary(question => question.QuestionId, question => question.Points);
        var optionsById = options.ToDictionary(option => option.OptionId);

        var existingAnswers = await _dbContext.AttemptAnswers
            .Where(answer => answer.AttemptId == attemptId && questionIds.Contains(answer.QuestionId))
            .ToDictionaryAsync(answer => answer.QuestionId, cancellationToken);

        var answeredAt = DateTime.UtcNow;

        foreach (var incomingAnswer in request.Answers)
        {
            if (!optionsById.TryGetValue(incomingAnswer.SelectedOptionId, out var selectedOption) ||
                selectedOption.QuestionId != incomingAnswer.QuestionId)
            {
                throw new EvaluationValidationException("Una opcion seleccionada no coincide con su pregunta.");
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
                AttemptId = attemptId,
                QuestionId = incomingAnswer.QuestionId,
                SelectedOptionId = incomingAnswer.SelectedOptionId,
                IsCorrect = isCorrect,
                ScoreObtained = scoreObtained,
                AnswerTimeSeconds = incomingAnswer.AnswerTimeSeconds,
                AnsweredAt = answeredAt
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var totalQuestions = await _dbContext.AssessmentQuestions
            .AsNoTracking()
            .CountAsync(
                question => question.AssessmentId == attempt.AssessmentId && question.IsActive,
                cancellationToken);

        var answeredQuestions = await _dbContext.AttemptAnswers
            .AsNoTracking()
            .CountAsync(
                answer => answer.AttemptId == attemptId && answer.SelectedOptionId != null,
                cancellationToken);

        var completionPercentage = totalQuestions == 0
            ? 0m
            : RoundPercentage(answeredQuestions, totalQuestions);

        return new SaveAttemptAnswersResponseDto(
            attemptId,
            request.Answers.Count,
            answeredQuestions,
            totalQuestions,
            completionPercentage);
    }

    public async Task<AssessmentAttemptResultDto> FinishAttemptAsync(
        long attemptId,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        var attempt = await GetOwnedAttemptEntityAsync(attemptId, studentId, cancellationToken);

        if (string.Equals(attempt.Status, CompletedStatus, StringComparison.OrdinalIgnoreCase))
        {
            return await GetAttemptResultAsync(attemptId, studentId, cancellationToken);
        }

        if (!string.Equals(attempt.Status, InProgressStatus, StringComparison.OrdinalIgnoreCase))
        {
            throw new EvaluationValidationException("Solo los intentos en curso pueden finalizarse.");
        }

        await EnsureAssessmentTypeAccessibleAsync(
            attempt.Assessment.AssessmentType,
            studentId,
            cancellationToken);

        await EnsureAttemptHasAllRequiredAnswersAsync(attempt, cancellationToken);

        var snapshot = await BuildAttemptSnapshotAsync(attempt, cancellationToken);
        var finishedAt = DateTime.UtcNow;
        var totalTimeSeconds = snapshot.Answers.Sum(answer => answer.AnswerTimeSeconds);

        if (totalTimeSeconds == 0)
        {
            totalTimeSeconds = Math.Max(0, Convert.ToInt32((finishedAt - attempt.StartedAt).TotalSeconds));
        }

        attempt.Status = CompletedStatus;
        attempt.FinishedAt = finishedAt;
        attempt.TotalScore = snapshot.TotalScore;
        attempt.LiteralScore = snapshot.LiteralScore;
        attempt.InferentialScore = snapshot.InferentialScore;
        attempt.CriticalScore = snapshot.CriticalScore;
        attempt.TotalCorrect = snapshot.TotalCorrect;
        attempt.TotalErrors = snapshot.TotalErrors;
        attempt.TotalTimeSeconds = totalTimeSeconds;
        attempt.CompletionPercentage = snapshot.CompletionPercentage;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await TryGenerateAdaptiveRecommendationAsync(attempt.AttemptId, studentId, cancellationToken);

        return snapshot with
        {
            Status = attempt.Status,
            FinishedAt = attempt.FinishedAt,
            TotalTimeSeconds = totalTimeSeconds
        };
    }

    public async Task<AssessmentAttemptResultDto> GetAttemptResultAsync(
        long attemptId,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        var attempt = await GetOwnedAttemptEntityAsync(attemptId, studentId, cancellationToken);
        var snapshot = await BuildAttemptSnapshotAsync(attempt, cancellationToken);

        var totalTimeSeconds = attempt.TotalTimeSeconds ??
            snapshot.Answers.Sum(answer => answer.AnswerTimeSeconds);

        if (totalTimeSeconds == 0 && attempt.FinishedAt.HasValue)
        {
            totalTimeSeconds = Math.Max(0, Convert.ToInt32((attempt.FinishedAt.Value - attempt.StartedAt).TotalSeconds));
        }

        return snapshot with
        {
            Status = attempt.Status,
            FinishedAt = attempt.FinishedAt,
            TotalTimeSeconds = totalTimeSeconds
        };
    }

    public async Task<PrePostComparisonSummaryDto> GetLatestPrePostComparisonAsync(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        await EnsureStudentAsync(studentId, cancellationToken);

        var completedAttempts = await _dbContext.AssessmentAttempts
            .AsNoTracking()
            .Include(attempt => attempt.Assessment)
            .Where(attempt =>
                attempt.StudentId == studentId &&
                attempt.Status == CompletedStatus &&
                AssessmentTypes.SupportedTypes.Contains(attempt.Assessment.AssessmentType))
            .OrderByDescending(attempt => attempt.FinishedAt ?? attempt.StartedAt)
            .ThenByDescending(attempt => attempt.AttemptId)
            .ToListAsync(cancellationToken);

        var latestPretest = completedAttempts
            .FirstOrDefault(attempt => attempt.Assessment.AssessmentType == AssessmentTypes.Pretest);
        var latestPosttest = completedAttempts
            .FirstOrDefault(attempt => attempt.Assessment.AssessmentType == AssessmentTypes.Posttest);

        if (latestPretest is null && latestPosttest is null)
        {
            return CreateUnavailableComparison(
                "Aun no hay un pretest y un posttest completados para comparar.");
        }

        if (latestPretest is null)
        {
            var latestPosttestMetrics = await BuildComparisonMetricsAsync(latestPosttest!, cancellationToken);

            return CreateUnavailableComparison(
                "La comparacion final aun no esta disponible porque falta completar un pretest.",
                null,
                latestPosttest,
                null,
                latestPosttestMetrics);
        }

        if (latestPosttest is null)
        {
            var latestPretestMetrics = await BuildComparisonMetricsAsync(latestPretest, cancellationToken);

            return CreateUnavailableComparison(
                "La comparacion final aun no esta disponible porque falta completar un posttest.",
                latestPretest,
                null,
                latestPretestMetrics,
                null);
        }

        var pretestMetrics = await BuildComparisonMetricsAsync(latestPretest, cancellationToken);
        var posttestMetrics = await BuildComparisonMetricsAsync(latestPosttest, cancellationToken);

        return new PrePostComparisonSummaryDto(
            true,
            "Comparacion disponible.",
            latestPretest.FinishedAt ?? latestPretest.StartedAt,
            latestPosttest.FinishedAt ?? latestPosttest.StartedAt,
            pretestMetrics.TotalScore,
            posttestMetrics.TotalScore,
            CalculateImprovement(pretestMetrics.TotalScore, posttestMetrics.TotalScore),
            pretestMetrics.LiteralScore,
            posttestMetrics.LiteralScore,
            CalculateImprovement(pretestMetrics.LiteralScore, posttestMetrics.LiteralScore),
            pretestMetrics.InferentialScore,
            posttestMetrics.InferentialScore,
            CalculateImprovement(pretestMetrics.InferentialScore, posttestMetrics.InferentialScore),
            pretestMetrics.CriticalScore,
            posttestMetrics.CriticalScore,
            CalculateImprovement(pretestMetrics.CriticalScore, posttestMetrics.CriticalScore));
    }

    private async Task EnsureStudentAsync(int studentId, CancellationToken cancellationToken)
    {
        var student = await _dbContext.Students
            .AsNoTracking()
            .SingleOrDefaultAsync(student => student.StudentId == studentId, cancellationToken);

        if (student is null)
        {
            throw new EvaluationAccessDeniedException("El usuario autenticado no esta registrado como estudiante.");
        }

        if (!student.IsEnabledForTest)
        {
            throw new EvaluationAccessDeniedException("El estudiante no esta habilitado para evaluaciones.");
        }
    }

    private async Task<Assessment> GetActiveAssessmentEntityAsync(int assessmentId, CancellationToken cancellationToken)
    {
        var assessment = await _dbContext.Assessments
            .SingleOrDefaultAsync(
                item =>
                    item.AssessmentId == assessmentId &&
                    item.IsActive &&
                    AssessmentTypes.SupportedTypes.Contains(item.AssessmentType),
                cancellationToken);

        if (assessment is null)
        {
            throw new EvaluationNotFoundException("No se encontro una evaluacion activa.");
        }

        return assessment;
    }

    private async Task<AssessmentAttempt> GetOwnedAttemptEntityAsync(
        long attemptId,
        int studentId,
        CancellationToken cancellationToken)
    {
        await EnsureStudentAsync(studentId, cancellationToken);

        var attempt = await _dbContext.AssessmentAttempts
            .Include(item => item.Assessment)
            .SingleOrDefaultAsync(item => item.AttemptId == attemptId, cancellationToken);

        if (attempt is null)
        {
            throw new EvaluationNotFoundException("No se encontro el intento de evaluacion.");
        }

        if (attempt.StudentId != studentId)
        {
            throw new EvaluationAccessDeniedException("No puedes operar sobre el intento de otro estudiante.");
        }

        if (!AssessmentTypes.SupportedTypes.Contains(attempt.Assessment.AssessmentType))
        {
            throw new EvaluationValidationException("El intento seleccionado no pertenece a un tipo de evaluacion compatible.");
        }

        return attempt;
    }

    private async Task EnsureAttemptHasAllRequiredAnswersAsync(
        AssessmentAttempt attempt,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(attempt.Assessment.AssessmentType, AssessmentTypes.Pretest, StringComparison.Ordinal) &&
            !string.Equals(attempt.Assessment.AssessmentType, AssessmentTypes.Posttest, StringComparison.Ordinal))
        {
            return;
        }

        var totalQuestions = await _dbContext.AssessmentQuestions
            .AsNoTracking()
            .CountAsync(
                question =>
                    question.AssessmentId == attempt.AssessmentId &&
                    question.IsActive &&
                    question.Question.IsActive,
                cancellationToken);

        var answeredQuestions = await _dbContext.AttemptAnswers
            .AsNoTracking()
            .CountAsync(
                answer => answer.AttemptId == attempt.AttemptId && answer.SelectedOptionId != null,
                cancellationToken);

        var missingAnswers = totalQuestions - answeredQuestions;

        if (missingAnswers > 0)
        {
            throw new EvaluationValidationException(
                $"No se puede finalizar {attempt.Assessment.AssessmentType}. Aun quedan {missingAnswers} pregunta(s) sin responder.");
        }
    }

    private async Task<AssessmentAttemptResultDto> BuildAttemptSnapshotAsync(
        AssessmentAttempt attempt,
        CancellationToken cancellationToken)
    {
        var questionData = await _dbContext.AssessmentQuestions
            .AsNoTracking()
            .Where(question => question.AssessmentId == attempt.AssessmentId && question.IsActive)
            .OrderBy(question => question.DisplayOrder)
            .Select(question => new
            {
                question.QuestionId,
                question.Points,
                question.Question.Stem,
                DimensionName = question.Question.Dimension.Name
            })
            .ToListAsync(cancellationToken);

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
        var answerResults = new List<AttemptAnswerResultDto>(questionData.Count);

        foreach (var question in questionData)
        {
            answersByQuestionId.TryGetValue(question.QuestionId, out var answer);

            var scoreObtained = answer?.ScoreObtained ?? 0m;
            var answerTimeSeconds = answer?.AnswerTimeSeconds ?? 0;

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

            answerResults.Add(new AttemptAnswerResultDto(
                question.QuestionId,
                question.Stem,
                question.DimensionName,
                answer?.SelectedOptionId,
                answer?.IsCorrect,
                scoreObtained,
                answerTimeSeconds));
        }

        var totalQuestions = questionData.Count;
        var completionPercentage = totalQuestions == 0
            ? 0m
            : RoundPercentage(answeredQuestions, totalQuestions);

        // TotalErrors counts only answered questions that were incorrect.
        // Unanswered questions remain separate and are reflected by CompletionPercentage.
        return new AssessmentAttemptResultDto(
            attempt.AttemptId,
            attempt.AssessmentId,
            attempt.Assessment.AssessmentType,
            attempt.Assessment.Title,
            attempt.Status,
            attempt.StartedAt,
            attempt.FinishedAt,
            CalculateScorePercentage(totalScoreObtained, totalAvailablePoints),
            CalculateScorePercentage(literalObtained, literalAvailable),
            CalculateScorePercentage(inferentialObtained, inferentialAvailable),
            CalculateScorePercentage(criticalObtained, criticalAvailable),
            totalCorrect,
            totalIncorrect,
            attempt.TotalTimeSeconds ?? answerResults.Sum(answer => answer.AnswerTimeSeconds),
            completionPercentage,
            answerResults);
    }

    private async Task EnsureAssessmentTypeAccessibleAsync(
        string assessmentType,
        int studentId,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(assessmentType, AssessmentTypes.Pretest, StringComparison.Ordinal) &&
            !string.Equals(assessmentType, AssessmentTypes.Posttest, StringComparison.Ordinal))
        {
            return;
        }

        var summary = await _academicFlowService.GetCurrentSummaryAsync(studentId, cancellationToken);

        if (string.Equals(assessmentType, AssessmentTypes.Pretest, StringComparison.Ordinal))
        {
            throw new EvaluationValidationException(
                "El Pretest no esta disponible en el flujo academico actual.");
        }

        if (summary.CanAccessPosttest)
        {
            return;
        }

        if (summary.HasCompletedPosttest)
        {
            throw new EvaluationValidationException(
                "El Posttest ya fue completado.");
        }

        throw new EvaluationValidationException(
            "El Posttest solo esta disponible despues de completar la intervencion minima de lectura.");
    }

    private async Task<ComparisonMetrics> BuildComparisonMetricsAsync(
        AssessmentAttempt attempt,
        CancellationToken cancellationToken)
    {
        if (attempt.TotalScore.HasValue &&
            attempt.LiteralScore.HasValue &&
            attempt.InferentialScore.HasValue &&
            attempt.CriticalScore.HasValue)
        {
            return new ComparisonMetrics(
                attempt.TotalScore.Value,
                attempt.LiteralScore.Value,
                attempt.InferentialScore.Value,
                attempt.CriticalScore.Value);
        }

        var snapshot = await BuildAttemptSnapshotAsync(attempt, cancellationToken);

        return new ComparisonMetrics(
            snapshot.TotalScore,
            snapshot.LiteralScore,
            snapshot.InferentialScore,
            snapshot.CriticalScore);
    }

    private static PrePostComparisonSummaryDto CreateUnavailableComparison(
        string message,
        AssessmentAttempt? pretestAttempt = null,
        AssessmentAttempt? posttestAttempt = null,
        ComparisonMetrics? pretestMetrics = null,
        ComparisonMetrics? posttestMetrics = null)
    {
        return new PrePostComparisonSummaryDto(
            false,
            message,
            pretestAttempt?.FinishedAt ?? pretestAttempt?.StartedAt,
            posttestAttempt?.FinishedAt ?? posttestAttempt?.StartedAt,
            pretestMetrics?.TotalScore,
            posttestMetrics?.TotalScore,
            null,
            pretestMetrics?.LiteralScore,
            posttestMetrics?.LiteralScore,
            null,
            pretestMetrics?.InferentialScore,
            posttestMetrics?.InferentialScore,
            null,
            pretestMetrics?.CriticalScore,
            posttestMetrics?.CriticalScore,
            null);
    }

    private static decimal CalculateImprovement(decimal baseline, decimal current)
    {
        return Math.Round(current - baseline, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal CalculateScorePercentage(decimal obtained, decimal available)
    {
        if (available <= 0)
        {
            return 0m;
        }

        // Scores are exposed as percentages in the 0-100 range.
        return Math.Round((obtained / available) * 100m, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal RoundPercentage(int numerator, int denominator)
    {
        return Math.Round((decimal)numerator / denominator * 100m, 2, MidpointRounding.AwayFromZero);
    }

    private static void ValidateAssessmentType(string assessmentType)
    {
        if (!AssessmentTypes.SupportedTypes.Contains(assessmentType))
        {
            throw new EvaluationValidationException("Solo se admiten evaluaciones Pretest y Posttest.");
        }
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

    private enum DimensionBucket
    {
        Literal,
        Inferential,
        Critical
    }

    private sealed record ComparisonMetrics(
        decimal TotalScore,
        decimal LiteralScore,
        decimal InferentialScore,
        decimal CriticalScore);
}
