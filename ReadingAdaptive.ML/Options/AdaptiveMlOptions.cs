namespace ReadingAdaptive.ML.Options;

public sealed class AdaptiveMlOptions
{
    public const string SectionName = "AdaptiveMl";

    public bool Enabled { get; set; }

    public string ModelPath { get; set; } = "App_Data/ml/adaptive-recommendation-model.zip";

    public string ModelVersion { get; set; } = "mlnet-sdca-v1";

    public float MinimumConfidence { get; set; } = 0.55f;
}
