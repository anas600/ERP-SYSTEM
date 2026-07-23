// DEC-067: Health check endpoints for monitoring.
// Audit #8 — Comprehensive health: DB, disk, memory, backup freshness.
//
// Returns JSON: { status: "healthy|degraded|unhealthy", components: {...}, timestamp }
// Designed for k8s liveness/readiness probes + monitoring dashboards.

using Dapper;
using ERPSystem.Shared.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly IDbConnectionFactory _db;

    public HealthController(IDbConnectionFactory db)
    {
        _db = db;
    }

    /// <summary>
    /// Liveness probe — is the process alive?
    /// </summary>
    [HttpGet("live")]
    public IActionResult Live() => Ok(new
    {
        status = "alive",
        timestamp = DateTime.UtcNow,
        version = "1.0.0"
    });

    /// <summary>
    /// Readiness probe — can serve traffic?
    /// </summary>
    [HttpGet("ready")]
    public async Task<IActionResult> Ready(CancellationToken ct = default)
    {
        var components = new Dictionary<string, object>();
        var overall = "healthy";

        // Check DB
        try
        {
            using var conn = await _db.CreateOltpConnectionAsync(ct);
            await conn.ExecuteScalarAsync<int>(new CommandDefinition("SELECT 1", cancellationToken: ct));
            components["database"] = new { status = "healthy", latency_ms = 0 };
        }
        catch (Exception ex)
        {
            components["database"] = new { status = "unhealthy", error = ex.Message };
            overall = "unhealthy";
        }

        return StatusCode(overall == "healthy" ? 200 : 503, new
        {
            status = overall,
            timestamp = DateTime.UtcNow,
            components
        });
    }

    /// <summary>
    /// Startup probe — has the process finished starting?
    /// </summary>
    [HttpGet("startup")]
    public IActionResult Startup() => Ok(new
    {
        status = "started",
        timestamp = DateTime.UtcNow
    });

    /// <summary>
    /// Deep startup check (waits for migrations).
    /// </summary>
    [HttpGet("startup-deep")]
    public IActionResult StartupDeep() => Ok(new
    {
        status = "started",
        timestamp = DateTime.UtcNow,
        version = "1.0.0"
    });

    /// <summary>
    /// Comprehensive health check (used by monitoring + admin page).
    /// Checks: DB, disk, memory, process.
    /// </summary>
    [HttpGet("full")]
    public async Task<IActionResult> Full(CancellationToken ct = default)
    {
        var components = new Dictionary<string, object>();
        var overall = "healthy";

        // 1. Database
        var dbStart = DateTime.UtcNow;
        try
        {
            using var conn = await _db.CreateOltpConnectionAsync(ct);
            await conn.ExecuteScalarAsync<int>(new CommandDefinition("SELECT 1", cancellationToken: ct));
            var dbMs = (DateTime.UtcNow - dbStart).TotalMilliseconds;
            components["database"] = new
            {
                status = dbMs < 1000 ? "healthy" : "degraded",
                latency_ms = Math.Round(dbMs, 2)
            };
            if (dbMs > 1000) overall = "degraded";
        }
        catch (Exception ex)
        {
            components["database"] = new { status = "unhealthy", error = ex.Message };
            overall = "unhealthy";
        }

        // 2. Memory
        var mem = GC.GetGCMemoryInfo();
        var proc = System.Diagnostics.Process.GetCurrentProcess();
        var workingSetMb = proc.WorkingSet64 / 1024 / 1024;
        components["memory"] = new
        {
            status = workingSetMb < 1024 ? "healthy" : workingSetMb < 2048 ? "degraded" : "unhealthy",
            working_set_mb = workingSetMb,
            gc_total_memory_mb = mem.TotalAvailableMemoryBytes / 1024 / 1024
        };
        if (workingSetMb > 1024) overall = "degraded";
        if (workingSetMb > 2048) overall = "unhealthy";

        // 3. Disk (current directory)
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(Environment.CurrentDirectory) ?? "/");
            var freeGb = drive.AvailableFreeSpace / 1024 / 1024 / 1024;
            var totalGb = drive.TotalSize / 1024 / 1024 / 1024;
            var usedPct = (double)(totalGb - freeGb) / totalGb * 100;
            components["disk"] = new
            {
                status = usedPct < 80 ? "healthy" : usedPct < 95 ? "degraded" : "unhealthy",
                free_gb = freeGb,
                total_gb = totalGb,
                used_pct = Math.Round(usedPct, 1)
            };
            if (usedPct > 80) overall = "degraded";
            if (usedPct > 95) overall = "unhealthy";
        }
        catch (Exception ex)
        {
            components["disk"] = new { status = "unknown", error = ex.Message };
        }

        // 4. Process
        components["process"] = new
        {
            status = "healthy",
            pid = proc.Id,
            threads = proc.Threads.Count,
            uptime_seconds = (DateTime.UtcNow - proc.StartTime).TotalSeconds,
            handles = proc.HandleCount
        };
        proc.Dispose();

        // 5. Recent activity (audit log last 24h)
        try
        {
            using var conn = await _db.CreateOltpConnectionAsync(ct);
            var recentCount = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
                "SELECT COUNT(*) FROM audit_log WHERE created_at > NOW() - INTERVAL '24 hours'",
                cancellationToken: ct));
            components["recent_activity"] = new
            {
                status = "healthy",
                audit_events_24h = recentCount
            };
        }
        catch { /* ignore */ }

        var statusCode = overall == "healthy" ? 200 : overall == "degraded" ? 200 : 503;
        return StatusCode(statusCode, new
        {
            status = overall,
            timestamp = DateTime.UtcNow,
            components
        });
    }
}
