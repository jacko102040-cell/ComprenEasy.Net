using ReadingAdaptive.Application.Evaluations.Constants;
using ReadingAdaptive.Application.Readings.Constants;
using ReadingAdaptive.Infrastructure.Persistence;

namespace ReadingAdaptive.Infrastructure.Services;

public sealed partial class AcademicContentService
{
    private static readonly string[] SupportedAssessmentTypes =
        [AssessmentTypes.Pretest, AssessmentTypes.Posttest, ReadingAssessmentTypes.ReadingPractice];

    private readonly ReadingAdaptiveDbContext _dbContext;

    public AcademicContentService(ReadingAdaptiveDbContext dbContext)
    {
        _dbContext = dbContext;
    }
}
