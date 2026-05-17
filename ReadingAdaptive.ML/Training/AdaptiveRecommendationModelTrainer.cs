using Microsoft.ML;
using ReadingAdaptive.ML.Models;

namespace ReadingAdaptive.ML.Training;

public sealed class AdaptiveRecommendationModelTrainer
{
    private static readonly string[] FeatureColumns =
    [
        nameof(AdaptiveRecommendationModelInput.TotalScore),
        nameof(AdaptiveRecommendationModelInput.LiteralScore),
        nameof(AdaptiveRecommendationModelInput.InferentialScore),
        nameof(AdaptiveRecommendationModelInput.CriticalScore),
        nameof(AdaptiveRecommendationModelInput.TotalCorrect),
        nameof(AdaptiveRecommendationModelInput.TotalErrors),
        nameof(AdaptiveRecommendationModelInput.AnsweredQuestions),
        nameof(AdaptiveRecommendationModelInput.TotalQuestions),
        nameof(AdaptiveRecommendationModelInput.CompletionPercentage),
        nameof(AdaptiveRecommendationModelInput.TotalTimeSeconds),
        nameof(AdaptiveRecommendationModelInput.AverageTimeSeconds),
        nameof(AdaptiveRecommendationModelInput.CurrentDifficultyRank),
        nameof(AdaptiveRecommendationModelInput.PreviousProgressDelta),
        nameof(AdaptiveRecommendationModelInput.CompletedReadingSessions)
    ];

    public void TrainAndSave(
        IEnumerable<AdaptiveRecommendationModelInput> trainingData,
        string outputModelPath)
    {
        ArgumentNullException.ThrowIfNull(trainingData);

        if (string.IsNullOrWhiteSpace(outputModelPath))
        {
            throw new ArgumentException("Output model path is required.", nameof(outputModelPath));
        }

        var mlContext = new MLContext(seed: 42);
        var dataView = mlContext.Data.LoadFromEnumerable(trainingData);

        var pipeline = mlContext.Transforms.Conversion.MapValueToKey(
                outputColumnName: "Label",
                inputColumnName: nameof(AdaptiveRecommendationModelInput.PredictedAction))
            .Append(mlContext.Transforms.Concatenate("Features", FeatureColumns))
            .Append(mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                labelColumnName: "Label",
                featureColumnName: "Features"))
            .Append(mlContext.Transforms.Conversion.MapKeyToValue(
                outputColumnName: nameof(AdaptiveRecommendationModelOutput.PredictedAction),
                inputColumnName: "PredictedLabel"));

        var model = pipeline.Fit(dataView);
        var directory = Path.GetDirectoryName(outputModelPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        mlContext.Model.Save(model, dataView.Schema, outputModelPath);
    }
}
