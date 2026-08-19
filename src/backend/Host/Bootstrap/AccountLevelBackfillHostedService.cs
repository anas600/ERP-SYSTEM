// Sprint 52a (Phase 4) — 4-level CoA hierarchy.
//
// يضيف عمود `level` لجدول accounts (موجود في accounts.json من Sprint 52a)
// ويحسب المستوى من الـ parent chain (depth-from-root).
//
// الخوارزمية: لكل حساب بـ level IS NULL، نمشي للأعلى عبر parent_account_id
// حتى نصل لحساب بدون أب (root). عدد الخطوات = level (1..4).
// idempotent: حسابات بـ level NOT NULL ما تتعدّل.
//
// السبب: الشجرة الموحّدة (Sprint 50+51) فيها حسابات بدون parent_id (L1 = 5
// حسابات جذر). بقية الحسابات (L2/L3/L4) كلها مرتبطة بـ parent. حساب المستوى
// بهذه الطريقة يعطينا هيكل 4-مستويات IFRS-compliant (Class → Sub-class →
// Control → Detail) بدون تعديل على بيانات قديمة.

using Dapper;
using ERPSystem.Shared.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Host.Bootstrap;

public sealed class AccountLevelBackfillHostedService : IHostedService
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<AccountLevelBackfillHostedService> _logger;

    public AccountLevelBackfillHostedService(
        IDbConnectionFactory db,
        ILogger<AccountLevelBackfillHostedService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var conn = await _db.CreateEphemeralOltpConnectionAsync(cancellationToken);

        // 1) كم حساب ما عنده level بعد؟
        var pending = (int)await conn.ExecuteScalarAsync<long>(new CommandDefinition(
            "SELECT COUNT(*) FROM accounts WHERE level IS NULL",
            cancellationToken: cancellationToken));

        if (pending == 0)
        {
            _logger.LogInformation("[Sprint52a] Account level backfill: all accounts already have level set (skipped)");
            return;
        }

        _logger.LogInformation("[Sprint52a] Account level backfill: computing levels for {Pending} accounts...", pending);

        // 2) Idempotency gate: اعملها مرة واحدة في الـ lifetime للـ process.
        //    نستخدم UPDATE ... RETURNING عشان نعرف كم row اتعدّل بدون SELECT ثاني.

        // Pass 1: roots (parent_account_id IS NULL) → level = 1
        var rootsUpdated = await conn.ExecuteAsync(new CommandDefinition(@"
            UPDATE accounts SET level = 1, updated_at = now()
            WHERE level IS NULL AND parent_account_id IS NULL",
            cancellationToken: cancellationToken));
        _logger.LogInformation("[Sprint52a]   L1 (roots) updated: {N}", rootsUpdated);

        // Pass 2..4: children by BFS-like iterative deepening.
        // نعدّ لـ 4 passes عشان أعمق CoA عندنا 4 مستويات.
        // كل pass: الحساب اللي parent عنده level = N-1 يصير level = N.
        for (int targetLevel = 2; targetLevel <= 4; targetLevel++)
        {
            var sql = $@"
                UPDATE accounts AS child
                SET level = @Lvl, updated_at = now()
                FROM accounts AS parent
                WHERE child.parent_account_id = parent.id
                  AND child.level IS NULL
                  AND parent.level = @ParentLvl";
            var updated = await conn.ExecuteAsync(new CommandDefinition(sql,
                new { Lvl = (short)targetLevel, ParentLvl = (short)(targetLevel - 1) },
                cancellationToken: cancellationToken));
            _logger.LogInformation("[Sprint52a]   L{Lvl} updated: {N}", targetLevel, updated);
        }

        // 3) أي حساب بعد بـ level IS NULL = orphan (parent غير موجود أو NULL pointer chain).
        //    نخليه level = 99 (sentinel) عشان نكتشفه في التقارير بدل ما نتجاهله.
        var orphans = await conn.ExecuteAsync(new CommandDefinition(@"
            UPDATE accounts SET level = 99, updated_at = now()
            WHERE level IS NULL",
            cancellationToken: cancellationToken));
        if (orphans > 0)
        {
            _logger.LogWarning("[Sprint52a]   {N} orphan accounts (parent chain broken) — set to level=99", orphans);
        }

        // 4) تقرير ملخّص
        var summary = (await conn.QueryAsync<(short level, long count)>(new CommandDefinition(
            "SELECT level, COUNT(*) AS count FROM accounts GROUP BY level ORDER BY level",
            cancellationToken: cancellationToken))).ToList();
        foreach (var (level, count) in summary)
        {
            var label = level switch
            {
                1 => "L1 (Class — الأصول/الالتزامات/حقوق الملكية/الإيرادات/المصروفات)",
                2 => "L2 (Sub-class — مجموعات رئيسية)",
                3 => "L3 (Control — حسابات وسيطة)",
                4 => "L4 (Detail — حسابات تفصيلية قابلة للترحيل)",
                99 => "ORPHAN (parent chain broken — needs manual fix)",
                _ => $"L{level}"
            };
            _logger.LogInformation("[Sprint52a]   {Label}: {Count} accounts", label, count);
        }

        _logger.LogInformation("[Sprint52a] Account level backfill DONE.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
