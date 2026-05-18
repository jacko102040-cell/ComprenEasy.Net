using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReadingAdaptive.Application.Auth.Constants;
using ReadingAdaptive.Application.TeacherPanel.Dtos;
using ReadingAdaptive.Application.TeacherPanel.Exceptions;
using ReadingAdaptive.Application.TeacherPanel.Interfaces;

namespace ReadingAdaptive.Api.Controllers;

[ApiController]
[Authorize(Roles = "Teacher")]
[Route("api/teacher-panel")]
public class TeacherPanelController : ControllerBase
{
    private readonly ITeacherPanelService _teacherPanelService;

    public TeacherPanelController(ITeacherPanelService teacherPanelService)
    {
        _teacherPanelService = teacherPanelService;
    }

    [HttpGet("students")]
    [ProducesResponseType(typeof(IReadOnlyCollection<TeacherStudentListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<TeacherStudentListItemDto>>> GetStudents(
        CancellationToken cancellationToken)
    {
        try
        {
            var teacherId = GetAuthenticatedTeacherId();
            var students = await _teacherPanelService.GetStudentsAsync(teacherId, cancellationToken);
            return Ok(students);
        }
        catch (TeacherPanelAccessDeniedException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
    }

    [HttpGet("comment-tags")]
    [ProducesResponseType(typeof(IReadOnlyCollection<TeacherCommentTagDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<TeacherCommentTagDto>>> GetCommentTags(
        CancellationToken cancellationToken)
    {
        try
        {
            var teacherId = GetAuthenticatedTeacherId();
            var tags = await _teacherPanelService.GetCommentTagsAsync(teacherId, cancellationToken);
            return Ok(tags);
        }
        catch (TeacherPanelAccessDeniedException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
    }

    [HttpGet("students/{studentId:int}")]
    [ProducesResponseType(typeof(TeacherStudentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeacherStudentDetailDto>> GetStudentDetail(
        int studentId,
        CancellationToken cancellationToken)
    {
        try
        {
            var teacherId = GetAuthenticatedTeacherId();
            var student = await _teacherPanelService.GetStudentDetailAsync(teacherId, studentId, cancellationToken);
            return Ok(student);
        }
        catch (TeacherPanelAccessDeniedException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
        catch (TeacherPanelNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost("students/{studentId:int}/reset-password")]
    [ProducesResponseType(typeof(ResetStudentPasswordResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResetStudentPasswordResponseDto>> ResetStudentPassword(
        int studentId,
        CancellationToken cancellationToken)
    {
        try
        {
            var teacherId = GetAuthenticatedTeacherId();
            var result = await _teacherPanelService.ResetStudentPasswordAsync(
                teacherId,
                studentId,
                cancellationToken);

            return Ok(result);
        }
        catch (TeacherPanelAccessDeniedException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
        catch (TeacherPanelNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost("students/{studentId:int}/comments")]
    [ProducesResponseType(typeof(TeacherStudentCommentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeacherStudentCommentDto>> AddComment(
        int studentId,
        [FromBody] CreateTeacherCommentRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var teacherId = GetAuthenticatedTeacherId();
            var comment = await _teacherPanelService.AddCommentAsync(
                teacherId,
                studentId,
                request,
                cancellationToken);

            return StatusCode(StatusCodes.Status201Created, comment);
        }
        catch (TeacherPanelAccessDeniedException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
        catch (TeacherPanelNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (TeacherPanelValidationException exception)
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
            throw new TeacherPanelAccessDeniedException("La credencial del docente autenticado no es valida.");
        }

        return teacherId;
    }
}
