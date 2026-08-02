using Dapper;
using ERPSystem.Shared.CompanyContext;
using ERPSystem.Shared.Infrastructure;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Modules.Payments.Infrastructure;

/// <summary>
/// عداد أرقام مستندات الـ Payments — يعيد استخدام نفس جدول procurement_document_sequences
/// (الموجود في Procuremet) لتجنّب جدول منفصل. الـ prefix المدعوم هنا: "PAY".
///
/// ملاحظة معمارية: الجدول مش 1:1 مع Procurement في الـ concept — الأفضل لاحقاً نقله إلى
/// Finance (يدفع كل الـ tenants). الآن نعيد استخدامه لتجنّب migration إضافية.
///
/// Sprint 24 (DEC-083): scoped per-company (Constitution Article 3).
/// </summary>
public sealed class PaymentSequenceRepository : IPaymentSequenceRepository
{
    private readonly IDbConnectionFactory _db;
    private readonly ICompanyContext _companyContext;
    private readonly ILogger<PaymentSequenceRepository> _logger;
    public PaymentSequenceRepository(IDbConnectionFactory db, ICompanyContext companyContext, ILogger<PaymentSequenceRepository> logger)
    {
        _db = db; _companyContext = companyContext; _logger = logger;
    }

    public async Task<string> GetNextPaymentNumberAsync(CancellationToken ct)
    {
        var companyId = _companyContext.CompanyId
            ?? throw new InvalidOperationException("Company not resolved — cannot generate payment number without company_id.");
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        // Sprint 24: PK = (company_id, prefix) — Constitution Article 3.
        await conn.ExecuteAsync(new CommandDefinition(@"
            CREATE TABLE IF NOT EXISTS procurement_document_sequences (
                company_id UUID NOT NULL,
                prefix VARCHAR(20) NOT NULL,
                last_number INT NOT NULL DEFAULT 0,
                PRIMARY KEY (company_id, prefix)
            )", cancellationToken: ct));

        await conn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO procurement_document_sequences (company_id, prefix, last_number)
            VALUES (@CompanyId, @Prefix, 1)
            ON CONFLICT (company_id, prefix) DO UPDATE
            SET last_number = procurement_document_sequences.last_number + 1",
            new { CompanyId = companyId, Prefix = "PAY" }, cancellationToken: ct));

        var last = await conn.QueryFirstOrDefaultAsync<int>(new CommandDefinition(
            "SELECT last_number FROM procurement_document_sequences WHERE company_id = @CompanyId AND prefix = @Prefix",
            new { CompanyId = companyId, Prefix = "PAY" }, cancellationToken: ct));

        var year = DateTime.UtcNow.Year;
        var number = $"PAY-{year}-{last:D4}";
        _logger.LogDebug("Generated payment number {Number} company {CompanyId}", number, companyId);
        return number;
    }
}

public interface IPaymentSequenceRepository
{
    Task<string> GetNextPaymentNumberAsync(CancellationToken ct);
}
