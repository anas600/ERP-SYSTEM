using Dapper;
using ERPSystem.Shared.CompanyContext;
using ERPSystem.Shared.Infrastructure;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Modules.Procurement.Infrastructure;

/// <summary>
/// عداد أرقام المستندات — يستخدم جدول بسيط في PostgreSQL لتوليد أرقام تسلسلية فريدة.
/// في الإنتاج، يُفضل استخدام sequences أصلية في Postgres، لكن نستخدم UPSERT + COALESCE لتوافق مرن.
/// Sprint 24 (DEC-083): scoped per-company via company_id (Constitution Article 3).
/// في الـ v1 single-deployment، company_id = Holding؛ في multi-company future،
/// كل شركة لها عداد مستقل (PO-2026-0001 لكل شركة).
/// </summary>
public sealed class DocumentSequenceRepository : IDocumentSequenceRepository
{
    private readonly IDbConnectionFactory _db;
    private readonly ICompanyContext _companyContext;
    private readonly ILogger<DocumentSequenceRepository> _logger;
    public DocumentSequenceRepository(IDbConnectionFactory db, ICompanyContext companyContext, ILogger<DocumentSequenceRepository> logger)
    {
        _db = db; _companyContext = companyContext; _logger = logger;
    }

    public async Task<string> GetNextNumberAsync(string prefix, CancellationToken ct)
    {
        var companyId = _companyContext.CompanyId
            ?? throw new InvalidOperationException("Company not resolved — cannot generate document number without company_id.");
        using var conn = await _db.CreateOltpConnectionAsync(ct);

        // نتأكد من وجود جدول العدادات (ينشأ تلقائياً في الـ migration، لكن آمن إذا لم يوجد).
        // Sprint 24: PK = (company_id, prefix) — Constitution Article 3.
        await conn.ExecuteAsync(new CommandDefinition(@"
            CREATE TABLE IF NOT EXISTS procurement_document_sequences (
                company_id UUID NOT NULL,
                prefix VARCHAR(20) NOT NULL,
                last_number INT NOT NULL DEFAULT 0,
                PRIMARY KEY (company_id, prefix)
            )", cancellationToken: ct));

        // Sprint 30 (DEC-103): atomic increment + read in a SINGLE statement (RETURNING).
        // The previous version did UPSERT then SELECT separately, which has a race condition
        // when 2+ requests run concurrently — both see the same last_number. The fix uses
        // ON CONFLICT ... DO UPDATE ... RETURNING to get the new value atomically.
        var last = await conn.ExecuteScalarAsync<int>(new CommandDefinition(@"
            INSERT INTO procurement_document_sequences (company_id, prefix, last_number)
            VALUES (@CompanyId, @Prefix, 1)
            ON CONFLICT (company_id, prefix) DO UPDATE
                SET last_number = procurement_document_sequences.last_number + 1
            RETURNING last_number",
            new { CompanyId = companyId, Prefix = prefix }, cancellationToken: ct));

        // تنسيق: PO-2026-0001 (السنة الحالية + رقم تسلسلي 4 أرقام)
        var year = DateTime.UtcNow.Year;
        var number = $"{prefix}-{year}-{last:D4}";
        _logger.LogDebug("Generated document number {Number} prefix {Prefix} company {CompanyId}", number, prefix, companyId);
        return number;
    }
}
