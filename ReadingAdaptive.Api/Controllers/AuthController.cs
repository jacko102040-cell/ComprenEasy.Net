using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReadingAdaptive.Application.Auth.Constants;
using ReadingAdaptive.Application.Auth.Dtos;
using ReadingAdaptive.Application.Auth.Exceptions;
using ReadingAdaptive.Application.Auth.Interfaces;

namespace ReadingAdaptive.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("register-student")]
    [ProducesResponseType(typeof(AuthUserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthUserDto>> RegisterStudent(
        [FromBody] RegisterStudentRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await _authService.RegisterStudentAsync(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, user);
        }
        catch (UsernameAlreadyExistsException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthUserDto>> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await _authService.LoginAsync(request, cancellationToken);
            await SignInAsync(user);
            return Ok(user);
        }
        catch (InvalidCredentialsException exception)
        {
            return Unauthorized(new { message = exception.Message });
        }
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(AuthUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuthUserDto>> Me(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue(AuthClaimTypes.UserId);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "La credencial del usuario autenticado no es valida." });
        }

        var user = await _authService.GetCurrentUserAsync(userId, cancellationToken);

        if (user is null)
        {
            return NotFound(new { message = "No se encontro al usuario autenticado." });
        }

        return Ok(user);
    }

    private async Task SignInAsync(AuthUserDto user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.GivenName, user.FullName),
            new(ClaimTypes.Role, user.Role),
            new(AuthClaimTypes.UserId, user.UserId.ToString()),
            new(AuthClaimTypes.Username, user.Username),
            new(AuthClaimTypes.FullName, user.FullName),
            new(AuthClaimTypes.Role, user.Role)
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        principal,
        new AuthenticationProperties
        {
            IsPersistent = false,
            AllowRefresh = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        });
    }
}
