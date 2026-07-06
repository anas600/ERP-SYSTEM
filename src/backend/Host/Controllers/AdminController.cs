using System.Collections.Concurrent;
using ERPSystem.Shared.SeedData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

/// <summary>
/// Admin endpoints — manual triggers for sensitive operations.
/// Sprint-4 Day 2 (DEC-042). Admin-only.
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AdminController> _logger;

    // In-memory job tracker (per-process; resets on restart).
    // For production, swap with DB-backed job table or Hangfire.
    private static readonly ConcurrentDictionary<Guid, SeedJobStatus> _jobs = new();

    public AdminController(IServiceProvider serviceProvider, ILogger<AdminController> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// تشغيل يدوي لـ AlFajr scenario seeder (~5K records — safe).
    /// يعيد jobId فوراً، التنفيذ في background.
    /// </summary>
    [HttpPost("seed/alfajr")]
    public IActionResult TriggerAlFajrSeed()
    {
        var jobId = Guid.NewGuid();
        _jobs[jobId] = new SeedJobStatus(jobId, "alfajr", "queued", DateTime.UtcNow);

        _ = Task.Run(async () =>
        {
            await RunSeedJobAsync(jobId, "alfajr", async (sp, ct) =>
            {
                var seeder = sp.GetRequiredService<ScenarioSeederHostedService>();
                await seeder.StartAsync(ct);
            });
        });

        return Accepted(new
        {
            jobId,
            scenario = "alfajr",
            status = "started",
            statusEndpoint = $"/api/admin/seed/status/{jobId}",
            note = "Background execution. Poll status endpoint for progress."
        });
    }

    /// <summary>
    /// تشغيل يدوي لـ AlBurj scenario seeder (~30K records — DEC-009 prevention).
    /// NOT YET IMPLEMENTED — الـ AlBurjSeeder class was removed in DEC-009 cleanup.
    /// Use POST /api/admin/seed/alfajr for the supported scenario.
    /// </summary>
    [HttpPost("seed/alburj")]
    public IActionResult TriggerAlBurjSeed()
    {
        _logger.LogWarning("AlBurj seed requested but class not implemented (DEC-009 prevention). Use /seed/alfajr.");
        return StatusCode(StatusCodes.Status501NotImplemented, new
        {
            error = "alburj_seeder_not_implemented",
            message = "AlBurjSeeder was removed after DEC-009 incident (30K records flooded the DB). " +
                      "Use POST /api/admin/seed/alfajr for the supported scenario. " +
                      "Implementing AlBurjSeeder safely is tracked in Sprint-5+ roadmap.",
            alternative = "/api/admin/seed/alfajr"
        });
    }

    /// <summary>
    /// استعلام حالة job معين.
    /// </summary>
    [HttpGet("seed/status/{jobId}")]
    public IActionResult GetSeedStatus(Guid jobId)
    {
        if (!_jobs.TryGetValue(jobId, out var status))
        {
            return NotFound(new { jobId, error = "job_not_found_or_already_cleaned" });
        }
        return Ok(status);
    }

    /// <summary>
    /// قائمة آخر 20 job (للمسؤول).
    /// </summary>
    [HttpGet("seed/jobs")]
    public IActionResult ListSeedJobs()
    {
        var recent = _jobs.Values
            .OrderByDescending(j => j.StartedAt)
            .Take(20)
            .ToList();
        return Ok(new { count = recent.Count, jobs = recent });
    }

    // ============ Private helpers ============

    private async Task RunSeedJobAsync(Guid jobId, string scenario, Func<IServiceProvider, CancellationToken, Task> work)
    {
        _jobs[jobId] = _jobs[jobId] with { Status = "running", StartedAt = DateTime.UtcNow };
        _logger.LogInformation("Seed job {JobId} ({Scenario}) started", jobId, scenario);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            using var scope = _serviceProvider.CreateScope();
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            await work(scope.ServiceProvider, cts.Token);

            sw.Stop();
            _jobs[jobId] = _jobs[jobId] with { Status = "completed", FinishedAt = DateTime.UtcNow, DurationSeconds = sw.Elapsed.TotalSeconds };
            _logger.LogInformation("Seed job {JobId} ({Scenario}) completed in {Sec}s", jobId, scenario, sw.Elapsed.TotalSeconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _jobs[jobId] = _jobs[jobId] with
            {
                Status = "failed",
                FinishedAt = DateTime.UtcNow,
                DurationSeconds = sw.Elapsed.TotalSeconds,
                Error = ex.Message
            };
            _logger.LogError(ex, "Seed job {JobId} ({Scenario}) FAILED after {Sec}s", jobId, scenario, sw.Elapsed.TotalSeconds);
        }
    }

    // ============ Status record ============

    private record SeedJobStatus(
        Guid JobId,
        string Scenario,
        string Status,
        DateTime StartedAt,
        DateTime? FinishedAt = null,
        double? DurationSeconds = null,
        string? Error = null
    );
}