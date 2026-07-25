using Dapper;
using ERPSystem.Shared.Infrastructure;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Modules.Procurement.Infrastructure;

/// <summary>
/// عداد أرقام المستندات — يستخدم جدول بسيط في PostgreSQL لتوليد أرقام تسلسلية فريدة.
/// في الإنتاج، يُفضل استخدام sequences أصلية في Postgres، لكن نستخدم UPSERT + COALESCE لتوافق مرن.
/// Phase 6.1b: عمومي لكل الـ prefixes (لا يوجد scope لكل tenant).
/// </summary>
public sealed class DocumentSequenceRepository : IDocumentSequenceRepository
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<DocumentSequenceRepository> _logger;
    public DocumentSequenceRepository(IDbConnectionFactory db, ILogger<DocumentSequenceRepository> logger)
    {
        _db = db; _logger = logger;
    }

    public async Task<string> GetNextNumberAsync(string prefix, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);

        // نتأكد من وجود جدول العدادات (ينشأ تلقائياً في الـ migration، لكن آمن إذا لم يوجد)
        await conn.ExecuteAsync(new CommandDefinition(@"
            CREATE TABLE IF NOT EXISTS procurement_document_sequences (
                prefix VARCHAR(20) NOT NULL,
                last_number INT NOT NULL DEFAULT 0,
                PRIMARY KEY (prefix)
            )", cancellationToken: ct));

        // UPSERT: لو السجل غير موجود، ينشأ بـ last_number=1، وإلا يزداد
        await conn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO procurement_document_sequences (prefix, last_number)
            VALUES (@Prefix, 1)
            ON CONFLICT (prefix) DO UPDATE SET last_number = procurement_document_sequences.last_number + 1",
            new { Prefix = prefix }, cancellationToken: ct));

        var last = await conn.QueryFirstOrDefaultAsync<int>(new CommandDefinition(
            "SELECT last_number FROM procurement_document_sequences WHERE prefix = @Prefix",
            new { Prefix = prefix }, cancellationToken: ct));

        // تنسيق: PO-2026-0001 (السنة الحالية + رقم تسلسلي 4 أرقام)
        var year = DateTime.UtcNow.Year;
        var number = $"{prefix}-{year}-{last:D4}";
        _logger.LogDebug("Generated document number {Number} prefix {Prefix}", number, prefix);
        return number;
    }
}
