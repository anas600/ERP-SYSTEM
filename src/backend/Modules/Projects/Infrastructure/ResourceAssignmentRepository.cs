using Dapper;
using ERPSystem.Modules.Projects.Entities;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Projects.Infrastructure;

public sealed class ResourceAssignmentRepository : IResourceAssignmentRepository
{
    private readonly IDbConnectionFactory _db;
    public ResourceAssignmentRepository(IDbConnectionFactory db) => _db = db;
    // DEC-112: "from" and "to" are SQL reserved words — must be double-quoted in every reference.
    // The column was created as quoted identifier in resource_assignments.json (quoted: true).
    private const string Sel = @"id, company_id AS CompanyId, project_id AS ProjectId, task_id AS TaskId,
        resource_id AS ResourceId, user_id AS UserId, ""from"" AS ""From"", ""to"" AS ""To"",
        hourly_rate AS HourlyRate, created_at AS CreatedAt";

    public async Task<ResourceAssignment?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<ResourceAssignment>(new CommandDefinition(
            $"SELECT {Sel} FROM resource_assignments WHERE id = @Id LIMIT 1", new { Id = id }, cancellationToken: ct));
    }
    public async Task<IReadOnlyList<ResourceAssignment>> ListByProjectAsync(Guid projectId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        // DEC-112: ORDER BY also needs quoted "from".
        var rows = await conn.QueryAsync<ResourceAssignment>(new CommandDefinition(
            $"SELECT {Sel} FROM resource_assignments WHERE project_id = @ProjectId ORDER BY \"from\"",
            new { ProjectId = projectId }, cancellationToken: ct));
        return rows.AsList();
    }
    public async Task InsertAsync(ResourceAssignment assignment, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        // Sprint 28 (DEC-095): include company_id in INSERT.
        // DEC-112: "from" and "to" must be quoted in the column list.
        await conn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO resource_assignments (id, company_id, project_id, task_id, resource_id, user_id, ""from"", ""to"", hourly_rate, created_at)
            VALUES (@Id, @CompanyId, @ProjectId, @TaskId, @ResourceId, @UserId, @From, @To, @HourlyRate, @CreatedAt)",
            assignment, cancellationToken: ct));
    }
    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition("DELETE FROM resource_assignments WHERE id = @Id", new { Id = id }, cancellationToken: ct));
    }
}
