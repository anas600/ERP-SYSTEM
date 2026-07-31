using System.Data;
using Dapper;
using ERPSystem.Modules.Companies.Entities;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Companies.Infrastructure;

public sealed class CompanyRepository : ICompanyRepository
{
    private readonly IDbConnectionFactory _db;
    public CompanyRepository(IDbConnectionFactory db) => _db = db;

    private const string SelectColumns = @"id, code, name, slug, legal_name AS LegalName,
        parent_company_id AS ParentCompanyId, is_group AS IsGroup,
        base_currency AS BaseCurrency, is_active AS IsActive,
        created_at AS CreatedAt, updated_at AS UpdatedAt";

    public async Task<Company?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<Company>(new CommandDefinition(
            $"SELECT {SelectColumns} FROM companies WHERE id = @Id LIMIT 1",
            new { Id = id }, cancellationToken: ct));
    }

    public async Task<Company?> GetByCodeAsync(string code, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<Company>(new CommandDefinition(
            $"SELECT {SelectColumns} FROM companies WHERE LOWER(code) = LOWER(@Code) LIMIT 1",
            new { Code = code }, cancellationToken: ct));
    }

    // Phase 6.0b (P6-0b): tenant-less lookup used by the bootstrap startup hook
    // to detect whether the default Holding (code='000', is_group=true, no parent)
    // already exists. The companies table is a single global scope (companies are
    // shared across all users), so no tenant/company filter is needed.
    public async Task<Guid?> GetHoldingCompanyIdAsync(CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<Guid?>(new CommandDefinition(
            @"SELECT id FROM companies
              WHERE is_group = true
                AND parent_company_id IS NULL
                AND code = '000'
              LIMIT 1",
            cancellationToken: ct));
    }

    // Sprint 1 (T2 / Block A): lookup a Holding by its URL-friendly slug.
    // The Holding is identified by is_group=true AND parent_company_id IS NULL.
    // Slug lookup is case-insensitive (lowercased) so /api/holdings/mfa-holding and
    // /api/holdings/MFA-Holding both resolve to the same row.
    public async Task<Company?> GetHoldingBySlugAsync(string slug, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<Company>(new CommandDefinition(
            $@"SELECT {SelectColumns} FROM companies
               WHERE is_group = true
                 AND parent_company_id IS NULL
                 AND LOWER(slug) = LOWER(@Slug)
               LIMIT 1",
            new { Slug = slug }, cancellationToken: ct));
    }

    // Sprint 2 (T3 / Block A): general slug lookup (any company, not just Holdings).
    // The companies table has a unique index on slug (see companies.json in
    // data-types), so at most one row will match.
    public async Task<Company?> GetBySlugAsync(string slug, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<Company>(new CommandDefinition(
            $@"SELECT {SelectColumns} FROM companies
               WHERE LOWER(slug) = LOWER(@Slug)
               LIMIT 1",
            new { Slug = slug }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<Company>> ListAsync(bool includeInactive, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var sql = $"SELECT {SelectColumns} FROM companies WHERE 1=1"
            + (includeInactive ? "" : " AND is_active = true") + " ORDER BY code";
        var rows = await conn.QueryAsync<Company>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.AsList();
    }

    // Sprint 2 (T1 / Block A): paged list for GET /api/companies. Returns at most
    // `take` rows starting at offset `skip` (computed from page/pageSize in the
    // service layer). Includes inactive rows when includeInactive=true.
    public async Task<IReadOnlyList<Company>> ListPagedAsync(int skip, int take, bool includeInactive, CancellationToken ct)
    {
        if (take < 1) take = 20;
        if (skip < 0) skip = 0;
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var sql = $"SELECT {SelectColumns} FROM companies WHERE 1=1"
            + (includeInactive ? "" : " AND is_active = true")
            + " ORDER BY code"
            + " OFFSET @Skip LIMIT @Take";
        var rows = await conn.QueryAsync<Company>(new CommandDefinition(sql,
            new { Skip = skip, Take = take }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<int> CountAsync(bool includeInactive, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var sql = "SELECT COUNT(*)::int FROM companies WHERE 1=1"
            + (includeInactive ? "" : " AND is_active = true");
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, cancellationToken: ct));
    }

    // Sprint 2 (T1 / Block A): user-scoped paged list. The multi-company scope is
    // enforced via the user_companies join — only companies the given user has been
    // assigned to are returned. The user_companies join is inner so the result is
    // the intersection of (companies) ∩ (companies the user can see).
    public async Task<IReadOnlyList<Company>> ListByUserAsync(Guid userId, int skip, int take, bool includeInactive, CancellationToken ct)
    {
        if (take < 1) take = 20;
        if (skip < 0) skip = 0;
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var sql = $@"SELECT {SelectColumns.Replace("AS ParentCompanyId", "AS ParentCompanyId")}
                    FROM companies c
                    INNER JOIN user_companies uc ON uc.company_id = c.id
                    WHERE uc.user_id = @UserId"
            + (includeInactive ? "" : " AND c.is_active = true")
            + " ORDER BY c.code"
            + " OFFSET @Skip LIMIT @Take";
        var rows = await conn.QueryAsync<Company>(new CommandDefinition(sql,
            new { UserId = userId, Skip = skip, Take = take }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<int> CountByUserAsync(Guid userId, bool includeInactive, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var sql = @"SELECT COUNT(*)::int FROM companies c
                    INNER JOIN user_companies uc ON uc.company_id = c.id
                    WHERE uc.user_id = @UserId"
            + (includeInactive ? "" : " AND c.is_active = true");
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql,
            new { UserId = userId }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<Company>> ListSubsidiariesAsync(Guid parentCompanyId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var rows = await conn.QueryAsync<Company>(new CommandDefinition(
            $"SELECT {SelectColumns} FROM companies WHERE parent_company_id = @ParentId ORDER BY code",
            new { ParentId = parentCompanyId }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task InsertAsync(Company company, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        await InsertAsync(company, conn, null, ct);
    }

    public async Task InsertAsync(Company company, IDbConnection conn, IDbTransaction? tx, CancellationToken ct)
    {
        await conn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO companies (id, code, name, slug, legal_name, parent_company_id,
                                   is_group, base_currency, is_active, created_at, updated_at)
            VALUES (@Id, @Code, @Name, @Slug, @LegalName, @ParentCompanyId,
                    @IsGroup, @BaseCurrency, @IsActive, @CreatedAt, @UpdatedAt)",
            company, transaction: tx, cancellationToken: ct));
    }

    public async Task UpdateAsync(Company company, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(@"
            UPDATE companies SET name = @Name, slug = @Slug, legal_name = @LegalName,
                                 base_currency = @BaseCurrency, is_active = @IsActive,
                                 updated_at = @UpdatedAt
            WHERE id = @Id", company, cancellationToken: ct));
    }
}
