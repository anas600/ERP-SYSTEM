using System.Diagnostics;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Host.Controllers;

/// <summary>
/// Health check endpoints — يفحص:
/// - /health        : liveness — عملية خفيفة بدون فحص قواعد بيانات
/// - /health/ready  : readiness — يفحص Postgres + Redis
/// </summary>
[ApiController]
[Route("health")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly IDbConnectionFactory _dbFactory;
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        IDbConnectionFactory dbFactory,
        IConnectionMultiplexer? redis,
        ILogger<HealthController> logger)
    {
        _dbFactory = dbFactory;
        _redis = redis;
        _logger = logger;
    }

    /// <summary>Liveness probe — خدمة الـ API نفسها</summary>
    [HttpGet("live")]
    public IActionResult Live() => Ok(new
    {
        status = "healthy",
        service = "ERP-SYSTEM",
        timestamp = DateTime.UtcNow,
    });

    /// <summary>Readiness probe — يفحص التبعيات (Postgres + Redis)</summary>
    [HttpGet("ready")]
    public async Task<IActionResult> Ready(CancellationToken ct)
    {
        var checks = new Dictionary<string, object>();
        var overallHealthy = true;
        var sw = new Stopwatch();

        // Postgres OLTP
        try
        {
            sw.Restart();
            using var conn = await _dbFactory.CreateOltpConnectionAsync(ct);
            var version = await conn.ExecuteScalarAsync<string>(new CommandDefinition("SELECT version()", cancellationToken: ct));
            checks["postgres_oltp"] = new { healthy = true, latencyMs = sw.ElapsedMilliseconds, version = version?.Split(' ').Take(2).Aggregate((a, b) => $"{a} {b}") };
        }
        catch (Exception ex)
        {
            overallHealthy = false;
            _logger.LogError(ex, "فشل فحص Postgres");
            checks["postgres_oltp"] = new { healthy = false, error = ex.Message };
        }

        // Redis (اختياري — قد لا يكون متوفراً في dev)
        if (_redis != null)
        {
            try
            {
                sw.Restart();
                var pong = await _redis.GetDatabase().PingAsync();
                checks["redis"] = new { healthy = pong > TimeSpan.Zero, latencyMs = sw.ElapsedMilliseconds, pingMs = pong.TotalMilliseconds };
            }
            catch (Exception ex)
            {
                // Redis ليس حرجة للـ readiness في المرحلة الحالية
                checks["redis"] = new { healthy = false, error = ex.Message, warning = "non-critical in dev" };
            }
        }
        else
        {
            checks["redis"] = new { healthy = false, error = "not configured" };
        }

        var response = new
        {
            status = overallHealthy ? "ready" : "degraded",
            timestamp = DateTime.UtcNow,
            checks,
        };

        return overallHealthy ? Ok(response) : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }
}
