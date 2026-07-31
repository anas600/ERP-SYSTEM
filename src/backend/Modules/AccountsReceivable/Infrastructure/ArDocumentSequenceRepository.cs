using Dapper;
using ERPSystem.Shared.Infrastructure;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Modules.AccountsReceivable.Infrastructure;

/// <summary>
/// عداد أرقام مستندات AR — يستخدم جدول مستقل لتوليد أرقام تسلسلية فريدة لكل company.
/// يدعم بادئات: SI (SalesInvoice), RC (Receipt).
/// </summary>
public sealed class ArDocumentSequenceRepository : IArDocumentSequenceRepository
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<ArDocumentSequenceRepository> _logger;
    public ArDocumentSequenceRepository(IDbConnectionFactory db, ILogger<ArDocumentSequenceRepository> logger)
    {
        _db = db; _logger = logger;
    }

    public async Task<string> GetNextNumberAsync(Guid companyId, string prefix, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);

        // نتأكد من وجود جدول العدادات (ينشأ تلقائياً في الـ migration، لكن آمن إذا لم يوجد)
        await conn.ExecuteAsync(new CommandDefinition(@"
            CREATE TABLE IF NOT EXISTS ar_document_sequences (
                company_id UUID NOT NULL,
                prefix VARCHAR(20) NOT NULL,
                last_number INT NOT NULL DEFAULT 0,
                PRIMARY KEY (company_id, prefix)
            )", cancellationToken: ct));

        // UPSERT
        await conn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO ar_document_sequences (company_id, prefix, last_number)
            VALUES (@CompanyId, @Prefix, 1)
            ON CONFLICT (company_id, prefix) DO UPDATE SET last_number = ar_document_sequences.last_number + 1",
            new { CompanyId = companyId, Prefix = prefix }, cancellationToken: ct));

        var last = await conn.QueryFirstOrDefaultAsync<int>(new CommandDefinition(
            "SELECT last_number FROM ar_document_sequences WHERE company_id = @CompanyId AND prefix = @Prefix",
            new { CompanyId = companyId, Prefix = prefix }, cancellationToken: ct));

        var year = DateTime.UtcNow.Year;
        var number = $"{prefix}-{year}-{last:D6}";
        _logger.LogDebug("Generated AR document number {Number} for company {CompanyId} prefix {Prefix}", number, companyId, prefix);
        return number;
    }
}
