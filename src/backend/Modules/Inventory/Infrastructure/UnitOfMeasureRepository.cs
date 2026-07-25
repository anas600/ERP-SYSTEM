using System.Data;
using Dapper;
using ERPSystem.Modules.Inventory.Entities;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Inventory.Infrastructure;

public sealed class UnitOfMeasureRepository : IUnitOfMeasureRepository
{
    private readonly IDbConnectionFactory _db;
    public UnitOfMeasureRepository(IDbConnectionFactory db) => _db = db;

    public async Task<UnitOfMeasure?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<UnitOfMeasure>(new CommandDefinition(
            "SELECT id, code, name, symbol, is_active AS IsActive, created_at AS CreatedAt FROM units_of_measure WHERE id = @Id LIMIT 1",
            new { Id = id }, cancellationToken: ct));
    }
    public async Task<UnitOfMeasure?> GetByCodeAsync(string code, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<UnitOfMeasure>(new CommandDefinition(
            "SELECT id, code, name, symbol, is_active AS IsActive, created_at AS CreatedAt FROM units_of_measure WHERE LOWER(code) = LOWER(@Code) LIMIT 1",
            new { Code = code }, cancellationToken: ct));
    }
    public async Task<IReadOnlyList<UnitOfMeasure>> ListAsync(bool includeInactive, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var sql = "SELECT id, code, name, symbol, is_active AS IsActive, created_at AS CreatedAt FROM units_of_measure WHERE 1=1"
            + (includeInactive ? "" : " AND is_active = true") + " ORDER BY code";
        var rows = await conn.QueryAsync<UnitOfMeasure>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.AsList();
    }
    public async Task InsertAsync(UnitOfMeasure u, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        await InsertAsync(u, conn, null, ct);
    }

    public async Task InsertAsync(UnitOfMeasure u, IDbConnection conn, IDbTransaction? tx, CancellationToken ct)
    {
        await conn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO units_of_measure (id, code, name, symbol, is_active, created_at)
            VALUES (@Id, @Code, @Name, @Symbol, @IsActive, @CreatedAt)", u, transaction: tx, cancellationToken: ct));
    }
    public async Task UpdateAsync(UnitOfMeasure u, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE units_of_measure SET name = @Name, symbol = @Symbol, is_active = @IsActive WHERE id = @Id", u, cancellationToken: ct));
    }
}
