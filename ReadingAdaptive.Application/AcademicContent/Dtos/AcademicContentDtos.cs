namespace ReadingAdaptive.Application.AcademicContent.Dtos;

public sealed record ContentLookupItemDto(
    int Id,
    string Name);

public sealed record ContentPhaseLookupDto(
    byte PhaseId,
    string Code,
    string DisplayName,
    byte DefaultOrder);

public sealed record ContentQuestionLookupDto(
    int QuestionId,
    string Stem,
    string DimensionName);

public sealed record ContentLookupsDto(
    IReadOnlyCollection<ContentLookupItemDto> DifficultyLevels,
    IReadOnlyCollection<ContentLookupItemDto> Dimensions,
    IReadOnlyCollection<ContentPhaseLookupDto> Phases,
    IReadOnlyCollection<ContentLookupItemDto> Readings,
    IReadOnlyCollection<ContentQuestionLookupDto> Questions);

public sealed record ContentReadingListItemDto(
    int ReadingId,
    string Title,
    string DifficultyLevelName,
    bool IsActive,
    int EnabledPhaseCount,
    int AssessmentCount);

public sealed record ContentReadingPhaseEditorDto(
    byte PhaseId,
    string Code,
    string DisplayName,
    byte DisplayOrder,
    bool IsEnabled,
    bool IsRequired,
    string? GuidanceText,
    byte? MinQuestionsToUnlockNext);

public sealed record ContentReadingDetailDto(
    int ReadingId,
    string Title,
    string? Summary,
    string Content,
    string? ImageUrl,
    byte DifficultyLevelId,
    int? EstimatedMinutes,
    bool IsActive,
    IReadOnlyCollection<ContentReadingPhaseEditorDto> Phases);

public sealed record SaveContentReadingPhaseDto(
    byte PhaseId,
    byte DisplayOrder,
    bool IsEnabled,
    bool IsRequired,
    string? GuidanceText,
    byte? MinQuestionsToUnlockNext);

public sealed record SaveContentReadingRequestDto(
    string Title,
    string? Summary,
    string Content,
    string? ImageUrl,
    byte DifficultyLevelId,
    int? EstimatedMinutes,
    bool IsActive,
    IReadOnlyCollection<SaveContentReadingPhaseDto> Phases);

public sealed record ContentAssessmentListItemDto(
    int AssessmentId,
    string AssessmentType,
    string Title,
    string? ReadingTitle,
    byte? DifficultyLevelId,
    string? DifficultyLevelName,
    bool IsActive,
    int QuestionCount);

public sealed record ContentAssessmentQuestionEditorDto(
    int? AssessmentQuestionId,
    int QuestionId,
    string QuestionStem,
    string DimensionName,
    byte? PhaseId,
    byte DisplayOrder,
    decimal Points,
    bool IsActive);

public sealed record ContentAssessmentDetailDto(
    int AssessmentId,
    string AssessmentType,
    int? ReadingId,
    string Title,
    string? Description,
    byte? DifficultyLevelId,
    bool IsActive,
    IReadOnlyCollection<ContentAssessmentQuestionEditorDto> Questions);

public sealed record SaveContentAssessmentQuestionDto(
    int? AssessmentQuestionId,
    int QuestionId,
    byte? PhaseId,
    byte DisplayOrder,
    decimal Points,
    bool IsActive);

public sealed record SaveContentAssessmentRequestDto(
    string AssessmentType,
    int? ReadingId,
    string Title,
    string? Description,
    byte? DifficultyLevelId,
    bool IsActive,
    IReadOnlyCollection<SaveContentAssessmentQuestionDto> Questions);

public sealed record ContentQuestionOptionEditorDto(
    int? OptionId,
    string OptionText,
    bool IsCorrect,
    byte DisplayOrder);

public sealed record ContentQuestionListItemDto(
    int QuestionId,
    string Stem,
    string DimensionName,
    string QuestionType,
    string? DifficultyLevelName,
    bool IsActive,
    int OptionCount);

public sealed record ContentQuestionDetailDto(
    int QuestionId,
    byte DimensionId,
    string Stem,
    string QuestionType,
    string? Explanation,
    byte? DifficultyLevelId,
    bool IsActive,
    IReadOnlyCollection<ContentQuestionOptionEditorDto> Options);

public sealed record SaveContentQuestionOptionDto(
    int? OptionId,
    string OptionText,
    bool IsCorrect,
    byte DisplayOrder);

public sealed record SaveContentQuestionRequestDto(
    byte DimensionId,
    string Stem,
    string QuestionType,
    string? Explanation,
    byte? DifficultyLevelId,
    bool IsActive,
    IReadOnlyCollection<SaveContentQuestionOptionDto> Options);
