using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReadingAdaptive.Application.AcademicFlow.Dtos;
using ReadingAdaptive.Application.AcademicFlow.Exceptions;
using ReadingAdaptive.Application.AcademicFlow.Interfaces;
using ReadingAdaptive.Application.Auth.Constants;

namespace ReadingAdaptive.Api.Controllers;

[ApiController]
[Authorize(Roles = "Student")]
[Route("api/academic-flow")]
public sealed class AcademicFlowController : ControllerBase
{
    private readonly IAcademicFlowService _academicFlowService;

    public AcademicFlowController(IAcademicFlowService academicFlowService)
    {
        _academicFlowService = academicFlowService;
    }

    [HttpGet("current")]
    [ProducesResponseType(typeof(AcademicFlowSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AcademicFlowSummaryDto>> GetCurrent(
        CancellationToken cancellationToken)
    {
        try
        {
            var studentId = GetAuthenticatedStudentId();
            var summary = await _academicFlowService.GetCurrentSummaryAsync(studentId, cancellationToken);
            return Ok(summary);
        }
        catch (AcademicFlowAccessDeniedException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
    }

    private int GetAuthenticatedStudentId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue(AuthClaimTypes.UserId);

        if (!int.TryParse(userIdClaim, out var studentId))
        {
            throw new AcademicFlowAccessDeniedException("Authenticated student claim is invalid.");
        }

        return studentId;
    }
}
