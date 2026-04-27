using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReadingAdaptive.Application.Adaptive.Dtos;
using ReadingAdaptive.Application.Adaptive.Exceptions;
using ReadingAdaptive.Application.Adaptive.Interfaces;
using ReadingAdaptive.Application.Auth.Constants;

namespace ReadingAdaptive.Api.Controllers;

[ApiController]
[Authorize(Roles = "Student")]
[Route("api/adaptive-recommendations")]
public class AdaptiveRecommendationsController : ControllerBase
{
    private readonly IAdaptiveRecommendationService _adaptiveRecommendationService;

    public AdaptiveRecommendationsController(IAdaptiveRecommendationService adaptiveRecommendationService)
    {
        _adaptiveRecommendationService = adaptiveRecommendationService;
    }

    [HttpGet("latest")]
    [ProducesResponseType(typeof(AdaptiveRecommendationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdaptiveRecommendationDto>> GetLatestRecommendation(
        CancellationToken cancellationToken)
    {
        try
        {
            var studentId = GetAuthenticatedStudentId();
            var recommendation = await _adaptiveRecommendationService.GetLatestRecommendationAsync(
                studentId,
                cancellationToken);

            return Ok(recommendation);
        }
        catch (AdaptiveAccessDeniedException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
        catch (AdaptiveNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost("attempts/{attemptId:long}/generate")]
    [ProducesResponseType(typeof(AdaptiveRecommendationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdaptiveRecommendationDto>> GenerateRecommendation(
        long attemptId,
        CancellationToken cancellationToken)
    {
        try
        {
            var studentId = GetAuthenticatedStudentId();
            var recommendation = await _adaptiveRecommendationService.GenerateRecommendationAsync(
                attemptId,
                studentId,
                cancellationToken);

            return StatusCode(StatusCodes.Status201Created, recommendation);
        }
        catch (AdaptiveAccessDeniedException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
        catch (AdaptiveNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (AdaptiveValidationException exception)
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
            throw new AdaptiveAccessDeniedException("Authenticated student claim is invalid.");
        }

        return studentId;
    }
}
