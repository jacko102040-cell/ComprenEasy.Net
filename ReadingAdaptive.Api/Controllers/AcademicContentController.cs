using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReadingAdaptive.Application.AcademicContent.Dtos;
using ReadingAdaptive.Application.AcademicContent.Exceptions;
using ReadingAdaptive.Application.AcademicContent.Interfaces;
using ReadingAdaptive.Application.Auth.Constants;

namespace ReadingAdaptive.Api.Controllers;

[ApiController]
[Authorize(Roles = "Teacher")]
[Route("api/academic-content")]
public sealed class AcademicContentController : ControllerBase
{
    private readonly IAcademicContentService _academicContentService;

    public AcademicContentController(IAcademicContentService academicContentService)
    {
        _academicContentService = academicContentService;
    }

    [HttpGet("lookups")]
    public Task<ActionResult<ContentLookupsDto>> GetLookups(CancellationToken cancellationToken) =>
        ExecuteAsync(teacherId => _academicContentService.GetLookupsAsync(teacherId, cancellationToken));

    [HttpGet("readings")]
    public Task<ActionResult<IReadOnlyCollection<ContentReadingListItemDto>>> GetReadings(CancellationToken cancellationToken) =>
        ExecuteAsync(teacherId => _academicContentService.GetReadingsAsync(teacherId, cancellationToken));

    [HttpGet("readings/{readingId:int}")]
    public Task<ActionResult<ContentReadingDetailDto>> GetReading(int readingId, CancellationToken cancellationToken) =>
        ExecuteAsync(teacherId => _academicContentService.GetReadingAsync(teacherId, readingId, cancellationToken));

    [HttpPost("readings")]
    public Task<ActionResult<ContentReadingDetailDto>> CreateReading([FromBody] SaveContentReadingRequestDto request, CancellationToken cancellationToken) =>
        ExecuteAsync(
            teacherId => _academicContentService.CreateReadingAsync(teacherId, request, cancellationToken),
            StatusCodes.Status201Created);

    [HttpPut("readings/{readingId:int}")]
    public Task<ActionResult<ContentReadingDetailDto>> UpdateReading(int readingId, [FromBody] SaveContentReadingRequestDto request, CancellationToken cancellationToken) =>
        ExecuteAsync(teacherId => _academicContentService.UpdateReadingAsync(teacherId, readingId, request, cancellationToken));

    [HttpDelete("readings/{readingId:int}")]
    public Task<ActionResult<object>> ArchiveReading(int readingId, CancellationToken cancellationToken) =>
        ExecuteDeleteAsync(teacherId => _academicContentService.ArchiveReadingAsync(teacherId, readingId, cancellationToken));

    [HttpGet("assessments")]
    public Task<ActionResult<IReadOnlyCollection<ContentAssessmentListItemDto>>> GetAssessments(CancellationToken cancellationToken) =>
        ExecuteAsync(teacherId => _academicContentService.GetAssessmentsAsync(teacherId, cancellationToken));

    [HttpGet("assessments/{assessmentId:int}")]
    public Task<ActionResult<ContentAssessmentDetailDto>> GetAssessment(int assessmentId, CancellationToken cancellationToken) =>
        ExecuteAsync(teacherId => _academicContentService.GetAssessmentAsync(teacherId, assessmentId, cancellationToken));

    [HttpPost("assessments")]
    public Task<ActionResult<ContentAssessmentDetailDto>> CreateAssessment([FromBody] SaveContentAssessmentRequestDto request, CancellationToken cancellationToken) =>
        ExecuteAsync(
            teacherId => _academicContentService.CreateAssessmentAsync(teacherId, request, cancellationToken),
            StatusCodes.Status201Created);

    [HttpPut("assessments/{assessmentId:int}")]
    public Task<ActionResult<ContentAssessmentDetailDto>> UpdateAssessment(int assessmentId, [FromBody] SaveContentAssessmentRequestDto request, CancellationToken cancellationToken) =>
        ExecuteAsync(teacherId => _academicContentService.UpdateAssessmentAsync(teacherId, assessmentId, request, cancellationToken));

    [HttpDelete("assessments/{assessmentId:int}")]
    public Task<ActionResult<object>> ArchiveAssessment(int assessmentId, CancellationToken cancellationToken) =>
        ExecuteDeleteAsync(teacherId => _academicContentService.ArchiveAssessmentAsync(teacherId, assessmentId, cancellationToken));

    [HttpGet("questions")]
    public Task<ActionResult<IReadOnlyCollection<ContentQuestionListItemDto>>> GetQuestions(CancellationToken cancellationToken) =>
        ExecuteAsync(teacherId => _academicContentService.GetQuestionsAsync(teacherId, cancellationToken));

    [HttpGet("questions/{questionId:int}")]
    public Task<ActionResult<ContentQuestionDetailDto>> GetQuestion(int questionId, CancellationToken cancellationToken) =>
        ExecuteAsync(teacherId => _academicContentService.GetQuestionAsync(teacherId, questionId, cancellationToken));

    [HttpPost("questions")]
    public Task<ActionResult<ContentQuestionDetailDto>> CreateQuestion([FromBody] SaveContentQuestionRequestDto request, CancellationToken cancellationToken) =>
        ExecuteAsync(
            teacherId => _academicContentService.CreateQuestionAsync(teacherId, request, cancellationToken),
            StatusCodes.Status201Created);

    [HttpPut("questions/{questionId:int}")]
    public Task<ActionResult<ContentQuestionDetailDto>> UpdateQuestion(int questionId, [FromBody] SaveContentQuestionRequestDto request, CancellationToken cancellationToken) =>
        ExecuteAsync(teacherId => _academicContentService.UpdateQuestionAsync(teacherId, questionId, request, cancellationToken));

    [HttpDelete("questions/{questionId:int}")]
    public Task<ActionResult<object>> ArchiveQuestion(int questionId, CancellationToken cancellationToken) =>
        ExecuteDeleteAsync(teacherId => _academicContentService.ArchiveQuestionAsync(teacherId, questionId, cancellationToken));

    private async Task<ActionResult<T>> ExecuteAsync<T>(Func<int, Task<T>> operation, int successStatusCode = StatusCodes.Status200OK)
    {
        try
        {
            var teacherId = GetAuthenticatedTeacherId();
            var result = await operation(teacherId);
            return StatusCode(successStatusCode, result);
        }
        catch (AcademicContentAccessDeniedException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
        catch (AcademicContentNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (AcademicContentValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    private async Task<ActionResult<object>> ExecuteDeleteAsync(Func<int, Task> operation)
    {
        try
        {
            var teacherId = GetAuthenticatedTeacherId();
            await operation(teacherId);
            return Ok(new { message = "Archived." });
        }
        catch (AcademicContentAccessDeniedException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
        catch (AcademicContentNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (AcademicContentValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    private int GetAuthenticatedTeacherId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue(AuthClaimTypes.UserId);

        if (!int.TryParse(userIdClaim, out var teacherId))
        {
            throw new AcademicContentAccessDeniedException("La credencial del docente autenticado no es valida.");
        }

        return teacherId;
    }
}
