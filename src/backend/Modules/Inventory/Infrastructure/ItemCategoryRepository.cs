using System.Data;
using Dapper;
using ERPSystem.Modules.Inventory.Entities;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Inventory.Infrastructure;

public sealed class ItemCategoryRepository : IItemCategoryRepository
{
    private readonly IDbConnectionFactory _db;
    public ItemCategoryRepository(IDbConnectionFactory db) => _db = db;
    private const string Sel = @"id, code, name, description, parent_id AS ParentId,
        is_active AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt";

    public async Task<ItemCategory?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<ItemCategory>(new CommandDefinition(
            $"SELECT {Sel} FROM item_categories WHERE id = @Id LIMIT 1", new { Id = id }, cancellationToken: ct));
    }
    public async Task<ItemCategory?> GetByCodeAsync(string code, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<ItemCategory>(new CommandDefinition(
            $"SELECT {Sel} FROM item_categories WHERE LOWER(code) = LOWER(@Code) LIMIT 1",
            new { Code = code }, cancellationToken: ct));
    }
    public async Task<IReadOnlyList<ItemCategory>> ListAsync(bool includeInactive, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var sql = $"SELECT {Sel} FROM item_categories WHERE 1=1"
            + (includeInactive ? "" : " AND is_active = true") + " ORDER BY code";
        var rows = await conn.QueryAsync<ItemCategory>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.AsList();
    }
    public async Task<IReadOnlyList<ItemCategory>> ListChildrenAsync(Guid parentId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var rows = await conn.QueryAsync<ItemCategory>(new CommandDefinition(
            $"SELECT {Sel} FROM item_categories WHERE parent_id = @ParentId ORDER BY code",
            new { ParentId = parentId }, cancellationToken: ct));
        return rows.AsList();
    }
    public async Task InsertAsync(ItemCategory c, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        await InsertAsync(c, conn, null, ct);
    }

    public async Task InsertAsync(ItemCategory c, IDbConnection conn, IDbTransaction? tx, CancellationToken ct)
    {
        await conn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO item_categories (id, code, name, description, parent_id, is_active, created_at, updated_at)
            VALUES (@Id, @Code, @Name, @Description, @ParentId, @IsActive, @CreatedAt, @UpdatedAt)", c, transaction: tx, cancellationToken: ct));
    }
    public async Task UpdateAsync(ItemCategory c, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(@"
            UPDATE item_categories SET name = @Name, description = @Description, parent_id = @ParentId,
                                        is_active = @IsActive, updated_at = @UpdatedAt
            WHERE id = @Id", c, cancellationToken: ct));
    }
}
