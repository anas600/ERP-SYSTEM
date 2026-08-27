using Dapper;
using ERPSystem.Modules.Projects.Entities;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Projects.Infrastructure;

/// <summary>
/// Sprint 64 / DEC-221 — Dapper repository for <c>subcontractors</c>.
///
/// <para><b>Why Dapper (DEC-008)</b>: NO EF Core in this codebase. Every
/// repository uses Dapper against the OLTP connection (see
/// <see cref="IDbConnectionFactory"/>).</para>
///
/// <para><b>L19 / DEC-095</b>: every WHERE / INSERT / UPDATE includes
/// <c>company_id</c> so the service layer's JWT-derived company cannot be
/// spoofed via the request DTO. Update uses <c>company_id</c> in the WHERE
/// clause as a defense-in-depth check.</para>
/// </summary>
public sealed class SubcontractorRepository : ISubcontractorRepository
{
    private readonly IDbConnectionFactory _db;
    public SubcontractorRepository(IDbConnectionFactory db) => _db = db;

    private const string SelectColumns = @"
        id, company_id AS CompanyId, code, name, name_ar AS NameAr,
        contact_person AS ContactPerson, phone, email,
        trade_specialty AS TradeSpecialty, tax_id AS TaxId,
        is_active AS IsActive,
        created_at AS CreatedAt, updated_at AS UpdatedAt";

    public async Task<Subcontractor?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<Subcontractor>(new CommandDefinition(
            $"SELECT {SelectColumns} FROM subcontractors WHERE id = @Id LIMIT 1",
            new { Id = id }, cancellationToken: ct));
    }

    public async Task<Subcontractor?> GetByCodeAsync(Guid companyId, string code, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<Subcontractor>(new CommandDefinition(
            $"SELECT {SelectColumns} FROM subcontractors WHERE company_id = @CompanyId AND code = @Code LIMIT 1",
            new { CompanyId = companyId, Code = code }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<Subcontractor>> ListAsync(
        Guid companyId, bool? isActive, string? tradeSpecialty, int skip, int take, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        // Dynamic WHERE assembly — keep the SQL inline for transparency.
        var sql = $@"
            SELECT {SelectColumns}
            FROM subcontractors
            WHERE company_id = @CompanyId
              AND (@IsActive IS NULL OR is_active = @IsActive)
              AND (@TradeSpecialty IS NULL OR trade_specialty = @TradeSpecialty)
            ORDER BY code ASC
            OFFSET @Skip LIMIT @Take";
        var rows = await conn.QueryAsync<Subcontractor>(new CommandDefinition(
            sql,
            new { CompanyId = companyId, IsActive = isActive, TradeSpecialty = tradeSpecialty, Skip = skip, Take = take },
            cancellationToken: ct));
        return rows.AsList();
    }

    public async Task InsertAsync(Subcontractor sub, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            INSERT INTO subcontractors
                (id, company_id, code, name, name_ar, contact_person, phone, email,
                 trade_specialty, tax_id, is_active, created_at, updated_at)
            VALUES
                (@Id, @CompanyId, @Code, @Name, @NameAr, @ContactPerson, @Phone, @Email,
                 @TradeSpecialty, @TaxId, @IsActive, @CreatedAt, @UpdatedAt)";
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            sub.Id, sub.CompanyId, sub.Code, sub.Name, sub.NameAr,
            sub.ContactPerson, sub.Phone, sub.Email,
            sub.TradeSpecialty, sub.TaxId, sub.IsActive,
            sub.CreatedAt, sub.UpdatedAt
        }, cancellationToken: ct));
    }

    public async Task UpdateAsync(Subcontractor sub, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            UPDATE subcontractors SET
                code = @Code, name = @Name, name_ar = @NameAr,
                contact_person = @ContactPerson, phone = @Phone, email = @Email,
                trade_specialty = @TradeSpecialty, tax_id = @TaxId,
                is_active = @IsActive, updated_at = @UpdatedAt
            WHERE id = @Id AND company_id = @CompanyId";
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            sub.Id, sub.CompanyId, sub.Code, sub.Name, sub.NameAr,
            sub.ContactPerson, sub.Phone, sub.Email,
            sub.TradeSpecialty, sub.TaxId, sub.IsActive,
            sub.UpdatedAt
        }, cancellationToken: ct));
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            UPDATE subcontractors SET is_active = FALSE, updated_at = NOW()
            WHERE id = @Id AND is_active = TRUE";
        var rows = await conn.ExecuteAsync(new CommandDefinition(
            sql, new { Id = id }, cancellationToken: ct));
        return rows > 0;
    }
}
