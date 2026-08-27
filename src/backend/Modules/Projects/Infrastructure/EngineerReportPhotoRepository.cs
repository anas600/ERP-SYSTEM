using Dapper;
using ERPSystem.Modules.Projects.Entities;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Projects.Infrastructure;

/// <summary>
/// Sprint 61 (DEC-193) — Dapper repository for <c>engineer_report_photos</c>.
///
/// <para><b>Storage strategy (DEC-193)</b>: photos live on disk under
/// <c>wwwroot/uploads/engineer-reports/{reportId}/</c> (gitignored). The table holds the
/// path (relative to wwwroot) and an optional caption. The application layer is
/// responsible for writing the file to disk and then INSERTing the row in the same
/// transaction window (best-effort: the disk write is the source of truth, the DB row
/// is the index).</para>
///
/// <para><b>L19 / DEC-095</b>: every WHERE/INSERT includes <c>company_id</c> (denormalized
/// for FK performance — see entity notes).</para>
/// </summary>
public sealed class EngineerReportPhotoRepository : IEngineerReportPhotoRepository
{
    private readonly IDbConnectionFactory _db;
    public EngineerReportPhotoRepository(IDbConnectionFactory db) => _db = db;

    private const string SelectColumns = @"
        id, company_id AS CompanyId, report_id AS ReportId,
        file_path AS FilePath, caption, uploaded_at AS UploadedAt";

    public async Task<EngineerReportPhoto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<EngineerReportPhoto>(new CommandDefinition(
            $"SELECT {SelectColumns} FROM engineer_report_photos WHERE id = @Id LIMIT 1",
            new { Id = id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<EngineerReportPhoto>> ListByReportAsync(Guid reportId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var rows = await conn.QueryAsync<EngineerReportPhoto>(new CommandDefinition(
            $"SELECT {SelectColumns} FROM engineer_report_photos WHERE report_id = @ReportId ORDER BY uploaded_at ASC",
            new { ReportId = reportId }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<int> CountByReportAsync(Guid reportId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM engineer_report_photos WHERE report_id = @ReportId",
            new { ReportId = reportId }, cancellationToken: ct));
    }

    public async Task InsertAsync(EngineerReportPhoto photo, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO engineer_report_photos
                (id, report_id, company_id, file_path, caption, uploaded_at)
            VALUES
                (@Id, @ReportId, @CompanyId, @FilePath, @Caption, @UploadedAt)",
            new
            {
                photo.Id, photo.ReportId, photo.CompanyId,
                photo.FilePath, photo.Caption, photo.UploadedAt
            }, cancellationToken: ct));
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM engineer_report_photos WHERE id = @Id",
            new { Id = id }, cancellationToken: ct));
    }
}
