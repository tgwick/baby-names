using Microsoft.AspNetCore.Mvc;
using NameMatch.Application.DTOs;

namespace NameMatch.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<object>> Get()
    {
        return Ok(ApiResponse<object>.Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow
        }));
    }
}
