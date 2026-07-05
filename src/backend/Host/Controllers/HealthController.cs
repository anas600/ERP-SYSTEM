using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

/// <summary>
/// Health check endpoints for monitoring (Sprint-4 Day 1).
/// </summary>
[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet("live")]
    public IActionResult Live() => Ok(new
    {
        status = "alive",
        timestamp = DateTime.UtcNow,
        version = "1.0.0"
    });

    [HttpGet("startup")]
    public IActionResult Startup() => Ok(new
    {
        status = "started",
        timestamp = DateTime.UtcNow,
        uptimeSeconds = (DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime).TotalSeconds
    });
}