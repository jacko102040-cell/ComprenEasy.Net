using ReadingAdaptive.Application.AcademicFlow.Dtos;

namespace ReadingAdaptive.Application.AcademicFlow.Interfaces;

public interface IAcademicFlowService
{
    Task<AcademicFlowSummaryDto> GetCurrentSummaryAsync(
        int studentId,
        CancellationToken cancellationToken = default);
}
