using Microsoft.AspNetCore.Mvc;

namespace ReadingAdaptive.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new
        {
            ok = true,
            message = "ReadingAdaptive API is healthy.",
            timestamp = DateTime.UtcNow
        });
    }
}
