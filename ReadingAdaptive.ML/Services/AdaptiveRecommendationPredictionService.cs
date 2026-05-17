using System.Globalization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.ML;
using ReadingAdaptive.Application.Adaptive.Constants;
using ReadingAdaptive.Application.Adaptive.Dtos;
using ReadingAdaptive.Application.Adaptive.Interfaces;
using ReadingAdaptive.ML.Models;
using ReadingAdaptive.ML.Options;

namespace ReadingAdaptive.ML.Services;

public sealed class AdaptiveRecommendationPredictionService : IAdaptiveRecommendationPredictionService
{
    private static readonly HashSet<string> ValidActions = new(StringComparer.Ordinal)
    {
        AdaptivePredictedActions.Reinforce,
        AdaptivePredictedActions.AdvanceWithSupport,
        AdaptivePredictedActions.Advance
    };

    private readonly AdaptiveMlOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly MLContext _mlContext = new(seed: 42);
    private readonly object _modelLock = new();

    private ITransformer? _model;
    private bool _loadAttempted;
    private string? _loadFailureReason;

    public AdaptiveRecommendationPredictionService(
        IOptions<AdaptiveMlOptions> options,
        IHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
    }

    public AdaptiveMlPredictionResult Predict(AdaptiveMlPredictionRequest request)
    {
        if (!_options.Enabled)
        {
            return Unavailable("disabled");
        }

        var model = GetModel();
        if (model is null)
        {
            return Unavailable(_loadFailureReason ?? "model-unavailable");
        }

        try
        {
            var input = MapInput(request);
            var predictionEngine = _mlContext.Model
                .CreatePredictionEngine<AdaptiveRecommendationModelInput, AdaptiveRecommendationModelOutput>(model);
            var prediction = predictionEngine.Predict(input);
            var scores = prediction.Score ?? Array.Empty<float>();
            var confidence = CalculateConfidence(scores);
            var action = prediction.PredictedAction;

            if (string.IsNullOrWhiteSpace(action))
            {
                return Unavailable("empty-prediction", BuildRawOutput("ml", action, confidence, scores));
            }

            if (!ValidActions.Contains(action))
            {
                return new AdaptiveMlPredictionResult(
                    true,
                    false,
                    action,
                    confidence,
                    scores,
                    ResolveModelVersion(),
                    "invalid-action",
                    BuildRawOutput("ml", action, confidence, scores));
            }

            if (confidence < _options.MinimumConfidence)
            {
                return new AdaptiveMlPredictionResult(
                    true,
                    false,
                    action,
                    confidence,
                    scores,
                    ResolveModelVersion(),
                    "low-confidence",
                    BuildRawOutput("ml", action, confidence, scores));
            }

            return new AdaptiveMlPredictionResult(
                true,
                true,
                action,
                confidence,
                scores,
                ResolveModelVersion(),
                null,
                BuildRawOutput("ml", action, confidence, scores));
        }
        catch (Exception exception)
        {
            return Unavailable($"prediction-failed:{exception.GetType().Name}");
        }
    }

    private ITransformer? GetModel()
    {
        if (_loadAttempted)
        {
            return _model;
        }

        lock (_modelLock)
        {
            if (_loadAttempted)
            {
                return _model;
            }

            _loadAttempted = true;

            try
            {
                var modelPath = ResolveModelPath();
                if (!File.Exists(modelPath))
                {
                    _loadFailureReason = "model-not-found";
                    return null;
                }

                _model = _mlContext.Model.Load(modelPath, out _);
                return _model;
            }
            catch (Exception exception)
            {
                _loadFailureReason = $"model-load-failed:{exception.GetType().Name}";
                _model = null;
                return null;
            }
        }
    }

    private string ResolveModelPath()
    {
        if (Path.IsPathRooted(_options.ModelPath))
        {
            return _options.ModelPath;
        }

        return Path.GetFullPath(Path.Combine(_environment.ContentRootPath, _options.ModelPath));
    }

    private AdaptiveMlPredictionResult Unavailable(string fallbackReason, string? rawOutput = null)
    {
        return AdaptiveMlPredictionResult.Unavailable(
            ResolveModelVersion(),
            fallbackReason,
            rawOutput ?? $"source=rules;fallbackReason={fallbackReason}");
    }

    private string ResolveModelVersion()
    {
        return string.IsNullOrWhiteSpace(_options.ModelVersion)
            ? "mlnet-sdca-v1"
            : _options.ModelVersion;
    }

    private static AdaptiveRecommendationModelInput MapInput(AdaptiveMlPredictionRequest request)
    {
        return new AdaptiveRecommendationModelInput
        {
            TotalScore = request.TotalScore,
            LiteralScore = request.LiteralScore,
            InferentialScore = request.InferentialScore,
            CriticalScore = request.CriticalScore,
            TotalCorrect = request.TotalCorrect,
            TotalErrors = request.TotalErrors,
            AnsweredQuestions = request.AnsweredQuestions,
            TotalQuestions = request.TotalQuestions,
            CompletionPercentage = request.CompletionPercentage,
            TotalTimeSeconds = request.TotalTimeSeconds,
            AverageTimeSeconds = request.AverageTimeSeconds,
            CurrentDifficultyRank = request.CurrentDifficultyRank,
            PreviousProgressDelta = request.PreviousProgressDelta,
            CompletedReadingSessions = request.CompletedReadingSessions
        };
    }

    private static float CalculateConfidence(IReadOnlyCollection<float> scores)
    {
        if (scores.Count == 0)
        {
            return 0f;
        }

        var max = scores
            .Where(score => !float.IsNaN(score) && !float.IsInfinity(score))
            .DefaultIfEmpty(0f)
            .Max();

        return Math.Clamp(max, 0f, 1f);
    }

    private static string BuildRawOutput(
        string source,
        string? action,
        float confidence,
        IReadOnlyCollection<float> scores)
    {
        var scoreText = string.Join(
            "|",
            scores.Select(score => score.ToString("0.####", CultureInfo.InvariantCulture)));

        return string.Create(
            CultureInfo.InvariantCulture,
            $"source={source};action={action};confidence={confidence:0.####};scores={scoreText}");
    }
}
