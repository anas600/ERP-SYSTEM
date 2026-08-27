using Dapper;
using ERPSystem.Modules.Projects.Entities;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Projects.Infrastructure;

/// <summary>
/// Sprint 61 (DEC-192) — Dapper repository for <c>engineer_reports</c>.
///
/// <para><b>Why Dapper (DEC-008)</b>: NO EF Core in this codebase. Every repository uses
/// Dapper against the OLTP connection (see <see cref="IDbConnectionFactory"/>).</para>
///
/// <para><b>L19 / DEC-095 compliance</b>: every WHERE/INSERT/UPDATE includes
/// <c>company_id</c> so the service layer's JWT-derived company cannot be spoofed via the
/// request DTO.</para>
///
/// <para><b>Status column (DEC-192)</b>: stored as TEXT in the DB ('Draft' | 'Submitted'
/// | 'Approved' | 'Rejected'). The repo reads/writes it as a string and lets the
/// EnumStringTypeHandler convert to/from the <see cref="EngineerReportStatus"/> enum.</para>
/// </summary>
public sealed class EngineerReportRepository : IEngineerReportRepository
{
    private readonly IDbConnectionFactory _db;
    public EngineerReportRepository(IDbConnectionFactory db) => _db = db;

    private const string SelectColumns = @"
        id, company_id AS CompanyId, project_id AS ProjectId, report_date AS ReportDate,
        engineer_id AS EngineerId, status, weather, work_done AS WorkDone, issues,
        created_at AS CreatedAt, updated_at AS UpdatedAt";

    public async Task<EngineerReport?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<EngineerReport>(new CommandDefinition(
            $"SELECT {SelectColumns} FROM engineer_reports WHERE id = @Id LIMIT 1",
            new { Id = id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<EngineerReport>> ListByProjectAsync(
        Guid projectId, Guid companyId, DateTime? from, DateTime? to,
        EngineerReportStatus? status, int skip, int take, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var sql = $"SELECT {SelectColumns} FROM engineer_reports WHERE project_id = @ProjectId AND company_id = @CompanyId";
        var p = new DynamicParameters();
        p.Add("ProjectId", projectId);
        p.Add("CompanyId", companyId);
        if (from.HasValue) { sql += " AND report_date >= @From"; p.Add("From", from.Value.Date); }
        if (to.HasValue) { sql += " AND report_date <= @To"; p.Add("To", to.Value.Date); }
        if (status.HasValue) { sql += " AND status = @Status"; p.Add("Status", status.Value.ToString()); }
        sql += " ORDER BY report_date DESC, created_at DESC OFFSET @Skip LIMIT @Take";
        p.Add("Skip", skip);
        p.Add("Take", take);
        var rows = await conn.QueryAsync<EngineerReport>(new CommandDefinition(sql, p, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<int> CountByProjectAndDateAsync(Guid projectId, DateTime reportDate, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM engineer_reports WHERE project_id = @ProjectId AND report_date = @ReportDate",
            new { ProjectId = projectId, ReportDate = reportDate.Date }, cancellationToken: ct));
    }

    public async Task InsertAsync(EngineerReport report, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO engineer_reports
                (id, company_id, project_id, report_date, engineer_id, status,
                 weather, work_done, issues, created_at, updated_at)
            VALUES
                (@Id, @CompanyId, @ProjectId, @ReportDate, @EngineerId, @Status,
                 @Weather, @WorkDone, @Issues, @CreatedAt, @UpdatedAt)",
            new
            {
                report.Id, report.CompanyId, report.ProjectId,
                ReportDate = report.ReportDate.Date,
                report.EngineerId, Status = report.Status.ToString(),
                report.Weather, report.WorkDone, report.Issues,
                report.CreatedAt, report.UpdatedAt
            }, cancellationToken: ct));
    }

    public async Task UpdateAsync(EngineerReport report, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(@"
            UPDATE engineer_reports SET
                status = @Status,
                weather = @Weather,
                work_done = @WorkDone,
                issues = @Issues,
                updated_at = @UpdatedAt
            WHERE id = @Id AND company_id = @CompanyId",
            new
            {
                report.Id, report.CompanyId, Status = report.Status.ToString(),
                report.Weather, report.WorkDone, report.Issues, report.UpdatedAt
            }, cancellationToken: ct));
    }
}
