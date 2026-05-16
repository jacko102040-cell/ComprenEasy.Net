using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReadingAdaptive.Application.Auth.Constants;
using ReadingAdaptive.Application.Readings.Dtos;
using ReadingAdaptive.Application.Readings.Exceptions;
using ReadingAdaptive.Application.Readings.Interfaces;

namespace ReadingAdaptive.Api.Controllers;

[ApiController]
[Authorize(Roles = "Student")]
[Route("api/readings")]
public class ReadingsController : ControllerBase
{
    private readonly IReadingService _readingService;

    public ReadingsController(IReadingService readingService)
    {
        _readingService = readingService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<ActiveReadingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ActiveReadingDto>>> GetActiveReadings(
        CancellationToken cancellationToken)
    {
        try
        {
            var studentId = GetAuthenticatedStudentId();
            var readings = await _readingService.GetActiveReadingsAsync(studentId, cancellationToken);
            return Ok(readings);
        }
        catch (ReadingAccessDeniedException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
        catch (ReadingValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("progress")]
    [ProducesResponseType(typeof(ReadingProgressSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReadingProgressSummaryDto>> GetReadingProgress(
        CancellationToken cancellationToken)
    {
        try
        {
            var studentId = GetAuthenticatedStudentId();
            var progress = await _readingService.GetReadingProgressAsync(studentId, cancellationToken);
            return Ok(progress);
        }
        catch (ReadingAccessDeniedException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
        catch (ReadingValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("{readingId:int}")]
    [ProducesResponseType(typeof(ReadingDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReadingDetailDto>> GetReadingDetail(
        int readingId,
        CancellationToken cancellationToken)
    {
        try
        {
            var studentId = GetAuthenticatedStudentId();
            var reading = await _readingService.GetReadingDetailAsync(readingId, studentId, cancellationToken);
            return Ok(reading);
        }
        catch (ReadingAccessDeniedException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
        catch (ReadingNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (ReadingValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("{readingId:int}/phases")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ReadingPhaseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<ReadingPhaseDto>>> GetReadingPhases(
        int readingId,
        CancellationToken cancellationToken)
    {
        try
        {
            var studentId = GetAuthenticatedStudentId();
            var phases = await _readingService.GetReadingPhasesAsync(readingId, studentId, cancellationToken);
            return Ok(phases);
        }
        catch (ReadingAccessDeniedException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
        catch (ReadingNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (ReadingValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("{readingId:int}/sessions")]
    [ProducesResponseType(typeof(ReadingSessionProgressDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReadingSessionProgressDto>> StartReadingSession(
        int readingId,
        CancellationToken cancellationToken)
    {
        try
        {
            var studentId = GetAuthenticatedStudentId();
            var session = await _readingService.StartReadingSessionAsync(readingId, studentId, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, session);
        }
        catch (ReadingAccessDeniedException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
        catch (ReadingNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (ReadingValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPut("sessions/{attemptId:long}/phases/{phaseId:int}/progress")]
    [ProducesResponseType(typeof(ReadingSessionProgressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReadingSessionProgressDto>> SavePhaseProgress(
        long attemptId,
        int phaseId,
        [FromBody] SaveReadingPhaseProgressRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!TryConvertPhaseId(phaseId, out var normalizedPhaseId, out var errorResult))
            {
                return errorResult;
            }

            var studentId = GetAuthenticatedStudentId();
            var session = await _readingService.SavePhaseProgressAsync(
                attemptId,
                normalizedPhaseId,
                studentId,
                request,
                cancellationToken);

            return Ok(session);
        }
        catch (ReadingAccessDeniedException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
        catch (ReadingNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (ReadingValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("sessions/{attemptId:long}/phases/{phaseId:int}/complete")]
    [ProducesResponseType(typeof(ReadingSessionProgressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReadingSessionProgressDto>> CompletePhase(
        long attemptId,
        int phaseId,
        [FromBody] SaveReadingPhaseProgressRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!TryConvertPhaseId(phaseId, out var normalizedPhaseId, out var errorResult))
            {
                return errorResult;
            }

            var studentId = GetAuthenticatedStudentId();
            var session = await _readingService.CompletePhaseAsync(
                attemptId,
                normalizedPhaseId,
                studentId,
                request,
                cancellationToken);

            return Ok(session);
        }
        catch (ReadingAccessDeniedException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
        catch (ReadingNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (ReadingValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("sessions/{attemptId:long}")]
    [ProducesResponseType(typeof(ReadingSessionProgressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReadingSessionProgressDto>> GetReadingSessionProgress(
        long attemptId,
        CancellationToken cancellationToken)
    {
        try
        {
            var studentId = GetAuthenticatedStudentId();
            var session = await _readingService.GetReadingSessionProgressAsync(
                attemptId,
                studentId,
                cancellationToken);

            return Ok(session);
        }
        catch (ReadingAccessDeniedException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
        catch (ReadingNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (ReadingValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("sessions/{attemptId:long}/finish")]
    [ProducesResponseType(typeof(ReadingSessionProgressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReadingSessionProgressDto>> FinishReadingSession(
        long attemptId,
        CancellationToken cancellationToken)
    {
        try
        {
            var studentId = GetAuthenticatedStudentId();
            var session = await _readingService.FinishReadingSessionAsync(
                attemptId,
                studentId,
                cancellationToken);

            return Ok(session);
        }
        catch (ReadingAccessDeniedException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
        catch (ReadingNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (ReadingValidationException exception)
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
            throw new ReadingAccessDeniedException("Authenticated student claim is invalid.");
        }

        return studentId;
    }

    private bool TryConvertPhaseId(
        int phaseId,
        out byte normalizedPhaseId,
        out ActionResult<ReadingSessionProgressDto> errorResult)
    {
        if (phaseId < byte.MinValue || phaseId > byte.MaxValue)
        {
            normalizedPhaseId = default;
            errorResult = BadRequest(new { message = "Phase id must be between 0 and 255." });
            return false;
        }

        normalizedPhaseId = (byte)phaseId;
        errorResult = null!;
        return true;
    }
}
