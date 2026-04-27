namespace ReadingAdaptive.Infrastructure.Configuration;

public sealed class AcademicFlowOptions
{
    public const string SectionName = "AcademicFlow";

    public int MinimumReadingSessionsRequired { get; set; } = 3;
}
