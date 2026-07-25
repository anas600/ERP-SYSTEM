using System.Data;
using Dapper;
using ERPSystem.Modules.Companies.Entities;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Companies.Infrastructure;

public sealed class CompanyRepository : ICompanyRepository
{
    private readonly IDbConnectionFactory _db;
    public CompanyRepository(IDbConnectionFactory db) => _db = db;

    private const string SelectColumns = @"id, code, name, legal_name AS LegalName,
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

    public async Task<IReadOnlyList<Company>> ListAsync(bool includeInactive, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var sql = $"SELECT {SelectColumns} FROM companies WHERE 1=1"
            + (includeInactive ? "" : " AND is_active = true") + " ORDER BY code";
        var rows = await conn.QueryAsync<Company>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.AsList();
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
            INSERT INTO companies (id, code, name, legal_name, parent_company_id,
                                   is_group, base_currency, is_active, created_at, updated_at)
            VALUES (@Id, @Code, @Name, @LegalName, @ParentCompanyId,
                    @IsGroup, @BaseCurrency, @IsActive, @CreatedAt, @UpdatedAt)",
            company, transaction: tx, cancellationToken: ct));
    }

    public async Task UpdateAsync(Company company, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(@"
            UPDATE companies SET name = @Name, legal_name = @LegalName,
                                 base_currency = @BaseCurrency, is_active = @IsActive,
                                 updated_at = @UpdatedAt
            WHERE id = @Id", company, cancellationToken: ct));
    }
}
