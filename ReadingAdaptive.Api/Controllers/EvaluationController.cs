using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReadingAdaptive.Application.Auth.Constants;
using ReadingAdaptive.Application.Evaluations.Constants;
using ReadingAdaptive.Application.Evaluations.Dtos;
using ReadingAdaptive.Application.Evaluations.Exceptions;
using ReadingAdaptive.Application.Evaluations.Interfaces;

namespace ReadingAdaptive.Api.Controllers;

[ApiController]
[Authorize(Roles = "Student")]
[Route("api/evaluations")]
public class EvaluationController : ControllerBase
{
    private readonly IEvaluationService _evaluationService;

    public EvaluationController(IEvaluationService evaluationService)
    {
        _evaluationService = evaluationService;
    }

    [HttpGet("pretests/active")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ActiveAssessmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ActiveAssessmentDto>>> GetActivePretests(
        CancellationToken cancellationToken)
    {
        try
        {
            var studentId = GetAuthenticatedStudentId();
            var evaluations = await _evaluationService.GetActiveAssessmentsAsync(
                AssessmentTypes.Pretest,
                studentId,
                cancellationToken);

            return Ok(evaluations);
        }
        catch (EvaluationAccessDeniedException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
        catch (EvaluationValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("posttests/active")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ActiveAssessmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ActiveAssessmentDto>>> GetActivePosttests(
        CancellationToken cancellationToken)
    {
        try
        {
            var studentId = GetAuthenticatedStudentId();
            var evaluations = await _evaluationService.GetActiveAssessmentsAsync(
                AssessmentTypes.Posttest,
                studentId,
                cancellationToken);

            return Ok(evaluations);
        }
        catch (EvaluationAccessDeniedException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
        catch (EvaluationValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("pre-post-comparison")]
    [ProducesResponseType(typeof(PrePostComparisonSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PrePostComparisonSummaryDto>> GetLatestPrePostComparison(
        CancellationToken cancellationToken)
    {
        try
        {
            var studentId = GetAuthenticatedStudentId();
            var comparison = await _evaluationService.GetLatestPrePostComparisonAsync(studentId, cancellationToken);
            return Ok(comparison);
        }
        catch (EvaluationAccessDeniedException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
    }

    [HttpGet("{assessmentId:int}")]
    [ProducesResponseType(typeof(AssessmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssessmentDetailDto>> GetAssessmentDetail(
        int assessmentId,
        CancellationToken cancellationToken)
    {
        try
        {
            var studentId = GetAuthenticatedStudentId();
            var assessment = await _evaluationService.GetAssessmentDetailAsync(
                assessmentId,
                studentId,
                cancellationToken);
            return Ok(assessment);
        }
        catch (EvaluationAccessDeniedException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
        catch (EvaluationNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (EvaluationValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("{assessmentId:int}/attempts")]
    [ProducesResponseType(typeof(StartAssessmentAttemptResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StartAssessmentAttemptResponseDto>> StartAttempt(
        int assessmentId,
        CancellationToken cancellationToken)
    {
        try
        {
            var studentId = GetAuthenticatedStudentId();
            var attempt = await _evaluationService.StartAttemptAsync(assessmentId, studentId, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, attempt);
        }
        catch (EvaluationAccessDeniedException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
        catch (EvaluationNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (EvaluationValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("attempts/{attemptId:long}/answers")]
    [ProducesResponseType(typeof(SaveAttemptAnswersResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaveAttemptAnswersResponseDto>> SaveAnswers(
        long attemptId,
        [FromBody] SaveAttemptAnswersRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var studentId = GetAuthenticatedStudentId();
            var response = await _evaluationService.SaveAnswersAsync(attemptId, studentId, request, cancellationToken);
            return Ok(response);
        }
        catch (EvaluationAccessDeniedException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
        catch (EvaluationNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (EvaluationValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("attempts/{attemptId:long}/finish")]
    [ProducesResponseType(typeof(AssessmentAttemptResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssessmentAttemptResultDto>> FinishAttempt(
        long attemptId,
        CancellationToken cancellationToken)
    {
        try
        {
            var studentId = GetAuthenticatedStudentId();
            var result = await _evaluationService.FinishAttemptAsync(attemptId, studentId, cancellationToken);
            return Ok(result);
        }
        catch (EvaluationAccessDeniedException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
        catch (EvaluationNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (EvaluationValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("attempts/{attemptId:long}/result")]
    [ProducesResponseType(typeof(AssessmentAttemptResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssessmentAttemptResultDto>> GetAttemptResult(
        long attemptId,
        CancellationToken cancellationToken)
    {
        try
        {
            var studentId = GetAuthenticatedStudentId();
            var result = await _evaluationService.GetAttemptResultAsync(attemptId, studentId, cancellationToken);
            return Ok(result);
        }
        catch (EvaluationAccessDeniedException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
        catch (EvaluationNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (EvaluationValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    private int GetAuthenticatedStudentId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue(AuthClaimTypes.UserId);

        if (!int.TryParse(userIdClaim, out var studentId))
        {
            throw new EvaluationAccessDeniedException("Authenticated student claim is invalid.");
        }

        return studentId;
    }
}
