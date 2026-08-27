using Dapper;
using ERPSystem.Modules.Projects.Entities;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Projects.Infrastructure;

/// <summary>
/// Sprint 61 (DEC-194) — Dapper repository for <c>engineer_report_signoffs</c>.
///
/// <para><b>Purpose</b>: every signoff event (PM approves, Client rejects, Engineer
/// self-acknowledges) is a row in this table so the full audit trail is preserved.
/// The parent report's <c>status</c> is the source of truth for the *final* state
/// — multiple signoff events can stack (e.g. Rejected → resubmitted → Approved by a
/// different signer).</para>
///
/// <para><b>L19 / DEC-095</b>: every WHERE/INSERT includes <c>company_id</c>.</para>
/// </summary>
public sealed class EngineerReportSignoffRepository : IEngineerReportSignoffRepository
{
    private readonly IDbConnectionFactory _db;
    public EngineerReportSignoffRepository(IDbConnectionFactory db) => _db = db;

    private const string SelectColumns = @"
        id, company_id AS CompanyId, report_id AS ReportId, signer_id AS SignerId,
        signer_role AS SignerRole, signed_at AS SignedAt, signature_text AS SignatureText,
        comment, approved AS Approved";

    public async Task<EngineerReportSignoff?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<EngineerReportSignoff>(new CommandDefinition(
            $"SELECT {SelectColumns} FROM engineer_report_signoffs WHERE id = @Id LIMIT 1",
            new { Id = id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<EngineerReportSignoff>> ListByReportAsync(Guid reportId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var rows = await conn.QueryAsync<EngineerReportSignoff>(new CommandDefinition(
            $"SELECT {SelectColumns} FROM engineer_report_signoffs WHERE report_id = @ReportId ORDER BY signed_at ASC",
            new { ReportId = reportId }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task InsertAsync(EngineerReportSignoff signoff, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO engineer_report_signoffs
                (id, report_id, company_id, signer_id, signer_role,
                 signed_at, signature_text, comment, approved)
            VALUES
                (@Id, @ReportId, @CompanyId, @SignerId, @SignerRole,
                 @SignedAt, @SignatureText, @Comment, @Approved)",
            signoff, cancellationToken: ct));
    }
}
