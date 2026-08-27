using Dapper;
using ERPSystem.Modules.Projects.Entities;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Projects.Infrastructure;

/// <summary>
/// Sprint 62 / DEC-197 — Dapper repository for <c>regional_premiums</c>.
///
/// <para><b>Why Dapper (DEC-008)</b>: NO EF Core in this codebase. Every repository
/// uses Dapper against the OLTP connection (see <see cref="IDbConnectionFactory"/>).</para>
///
/// <para><b>L19 / DEC-095 compliance</b>: every WHERE/INSERT/UPDATE includes
/// <c>company_id</c> in the lookup so the service layer's JWT-derived company cannot
/// be spoofed via the request DTO.</para>
///
/// <para>The repository is intentionally minimal: it does not validate IsActive in
/// the SQL — that's the service layer's responsibility, since the same query
/// ("all premiums for a project") is useful for the list endpoint and for the
/// "find the active one" calculation.</para>
/// </summary>
public sealed class RegionalPremiumRepository : IRegionalPremiumRepository
{
    private readonly IDbConnectionFactory _db;
    public RegionalPremiumRepository(IDbConnectionFactory db) => _db = db;

    private const string SelectColumns = @"
        id, company_id AS CompanyId, project_id AS ProjectId, region,
        ndb_percent AS NdbPercent, cit_percent AS CitPercent, ss_percent AS SsPercent,
        is_active AS IsActive, created_at AS CreatedAt";

    public async Task<RegionalPremium?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<RegionalPremium>(new CommandDefinition(
            $"SELECT {SelectColumns} FROM regional_premiums WHERE id = @Id LIMIT 1",
            new { Id = id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<RegionalPremium>> ListByProjectAsync(Guid projectId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = $@"
            SELECT {SelectColumns}
            FROM regional_premiums
            WHERE project_id = @ProjectId
            ORDER BY is_active DESC, region ASC";
        var rows = await conn.QueryAsync<RegionalPremium>(new CommandDefinition(
            sql, new { ProjectId = projectId }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task InsertAsync(RegionalPremium p, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            INSERT INTO regional_premiums
                (id, company_id, project_id, region, ndb_percent, cit_percent,
                 ss_percent, is_active, created_at)
            VALUES
                (@Id, @CompanyId, @ProjectId, @Region, @NdbPercent, @CitPercent,
                 @SsPercent, @IsActive, @CreatedAt)";
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            p.Id, p.CompanyId, p.ProjectId, p.Region,
            p.NdbPercent, p.CitPercent, p.SsPercent, p.IsActive, p.CreatedAt
        }, cancellationToken: ct));
    }

    public async Task UpdateAsync(RegionalPremium p, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            UPDATE regional_premiums SET
                region = @Region,
                ndb_percent = @NdbPercent,
                cit_percent = @CitPercent,
                ss_percent = @SsPercent,
                is_active = @IsActive
            WHERE id = @Id AND company_id = @CompanyId";
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            p.Id, p.CompanyId, p.Region,
            p.NdbPercent, p.CitPercent, p.SsPercent, p.IsActive
        }, cancellationToken: ct));
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM regional_premiums WHERE id = @Id",
            new { Id = id }, cancellationToken: ct));
    }
}
