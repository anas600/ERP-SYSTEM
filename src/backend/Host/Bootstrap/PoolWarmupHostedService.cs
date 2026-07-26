// Pool warmup at startup — Phase 6.3 hotfix (PR #149 follow-up #2)
//
// المشكلة: أول DB call من HF Space → Supabase Supavisor بيشرب 30+ ثانية بسبب
//   - TLS handshake الجديد
//   - Supavisor warm-up للـ client IP الجديد
//   - pgbouncer transaction-mode assign الـ backend connection
//   - Npgsql read timeout = 30s default
//
// النتيجة: أول request user بعد startup (مثل /api/auth/register) يستنى 30+ ثانية
//   فيتلغى من Caddy بـ 504، والـbackend يرجع 500 (OperationCanceledException).
//
// الحل: hosted service يفتح N connections من الـ pool في الـ startup، يـ ping
//   Supabase بـ SELECT 1، ويرجّعهم للـ pool. بعد كذا الـ pool دافي، والـ requests
//   الجاية سريعة (<100ms).
//
// الترتيب: بعد DefaultHoldingBootstrapHostedService عشان ما يتداخل مع schema setup.
//   ما يـ throw لو فشل — نكمّل (warm-up best-effort، الـ user request ممكن يكون بطيء
//   في أسوأ حالة لكن ما يكسر الـ deploy).

using Dapper;
using ERPSystem.Shared.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Host.Bootstrap;

public sealed class PoolWarmupHostedService : IHostedService
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<PoolWarmupHostedService> _logger;

    public PoolWarmupHostedService(
        IDbConnectionFactory db,
        ILogger<PoolWarmupHostedService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        const int warmupCount = 2;  // MinPoolSize=2 → keep 2 connections alive
        const int perConnTimeoutSec = 10;  // hard cap per connection
        _logger.LogInformation(
            "[PoolWarmup] تسخين الـ connection pool في الخلفية — فتح {N} connections (timeout={T}s لكل واحدة)…",
            warmupCount, perConnTimeoutSec);

        // Fire-and-forget: ما نحبّس startup. لو الـ warmup أخذ وقت، الـ HTTP server
        // يفتح على أي حال، والـ user requests تستعمل connections متاحة.
        // الـ 10s timeout per connection يضمن إن ما نعلّقش على connection فاسد.
        _ = Task.Run(async () =>
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var tasks = new List<Task<(long ElapsedMilliseconds, bool ok, string? err)>>();

            for (int i = 0; i < warmupCount; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var localSw = System.Diagnostics.Stopwatch.StartNew();
                    try
                    {
                        using var perCts = new CancellationTokenSource(TimeSpan.FromSeconds(perConnTimeoutSec));
                        using var conn = await _db.CreateOltpConnectionAsync(perCts.Token);
                        await conn.ExecuteScalarAsync<int>(new CommandDefinition(
                            "SELECT 1", cancellationToken: perCts.Token));
                        localSw.Stop();
                        return (localSw.ElapsedMilliseconds, true, (string?)null);
                    }
                    catch (Exception ex)
                    {
                        localSw.Stop();
                        return (localSw.ElapsedMilliseconds, false, ex.Message);
                    }
                }));
            }

            try
            {
                var results = await Task.WhenAll(tasks);
                sw.Stop();
                var ok = results.Count(r => r.ok);
                var failed = warmupCount - ok;
                if (failed == 0)
                {
                    var maxMs = results.Max(r => r.ElapsedMilliseconds);
                    _logger.LogInformation(
                        "[PoolWarmup] ✅ تم تسخين {Count} connections بنجاح (max={Max}ms, total={Total}ms)",
                        ok, maxMs, sw.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogWarning(
                        "[PoolWarmup] ⚠️ {Ok}/{Count} connections نجحت، {Failed} فشلت — first user request قد يكون بطيء (total={Total}ms)",
                        ok, warmupCount, failed, sw.ElapsedMilliseconds);
                    foreach (var r in results.Where(r => !r.ok))
                    {
                        _logger.LogWarning("[PoolWarmup]   failed: {Err}", r.err);
                    }
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogWarning(ex,
                    "[PoolWarmup] ⚠️ تسخين الـ pool فشل بالكامل بعد {Ms}ms — first user request قد يكون بطيء",
                    sw.ElapsedMilliseconds);
            }
        });

        // Return فوراً — الـ warmup يجري في الخلفية
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
