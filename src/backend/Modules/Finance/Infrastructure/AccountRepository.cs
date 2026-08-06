using System.Data;
using Dapper;
using ERPSystem.Modules.Finance.Entities;
using ERPSystem.Shared.Infrastructure;
using ERPSystem.Shared.SeedData;

namespace ERPSystem.Modules.Finance.Infrastructure;

public sealed class AccountRepository : IAccountRepository
{
    private readonly IDbConnectionFactory _db;
    public AccountRepository(IDbConnectionFactory db) => _db = db;

    private const string SelectColumns = @"
        id, company_id AS CompanyId, code, name, description, type,
        normal_balance AS NormalBalance, parent_account_id AS ParentAccountId,
        is_postable AS IsPostable, is_active AS IsActive, is_intercompany AS IsIntercompany,
        created_at AS CreatedAt, updated_at AS UpdatedAt";

    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        return await QueryFirstAsync(conn, "WHERE id = @Id", new { Id = id }, ct);
    }

    // Sprint 41 (DEC-127): company-scoped variant — uniqueness is per company.
    public async Task<Account?> GetByCodeAsync(string code, Guid companyId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        return await QueryFirstAsync(conn, "WHERE LOWER(code) = LOWER(@Code) AND company_id = @CompanyId",
            new { Code = code, CompanyId = companyId }, ct);
    }

    public async Task<Account?> GetByCodeAsync(string code, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        return await QueryFirstAsync(conn, "WHERE LOWER(code) = LOWER(@Code)",
            new { Code = code }, ct);
    }

    public async Task<IReadOnlyList<Account>> ListAsync(bool includeInactive, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var sql = $"SELECT {SelectColumns} FROM accounts WHERE 1=1"
            + (includeInactive ? "" : " AND is_active = true") + " ORDER BY code";
        var rows = await conn.QueryAsync<Account>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<Account>> ListChildrenAsync(Guid parentId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var rows = await conn.QueryAsync<Account>(new CommandDefinition(
            $"SELECT {SelectColumns} FROM accounts WHERE parent_account_id = @ParentId ORDER BY code",
            new { ParentId = parentId }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<Account>> ListByCompanyAsync(Guid? companyId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var sql = $"SELECT {SelectColumns} FROM accounts WHERE 1=1";
        if (companyId.HasValue) sql += " AND company_id = @CompanyId";
        sql += " ORDER BY code";
        var rows = await conn.QueryAsync<Account>(new CommandDefinition(sql, new { CompanyId = companyId }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task InsertAsync(Account account, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        await InsertAsync(account, conn, null, ct);
    }

    public async Task InsertAsync(Account account, IDbConnection conn, IDbTransaction? tx, CancellationToken ct)
    {
        await conn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO accounts (id, company_id, code, name, description, type, normal_balance,
                                  parent_account_id, is_postable, is_active, is_intercompany, created_at, updated_at)
            VALUES (@Id, @CompanyId, @Code, @Name, @Description, @Type, @NormalBalance,
                    @ParentAccountId, @IsPostable, @IsActive, @IsIntercompany, @CreatedAt, @UpdatedAt)",
            account, transaction: tx, cancellationToken: ct));
    }

    public async Task UpdateAsync(Account account, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(@"
            UPDATE accounts SET name = @Name, description = @Description, type = @Type,
                                normal_balance = @NormalBalance, parent_account_id = @ParentAccountId,
                                is_postable = @IsPostable, is_active = @IsActive,
                                is_intercompany = @IsIntercompany, updated_at = @UpdatedAt
            WHERE id = @Id", account, cancellationToken: ct));
    }

    public async Task<int> CountPostingsAsync(Guid accountId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM journal_lines WHERE account_id = @AccountId",
            new { AccountId = accountId }, cancellationToken: ct));
    }

    public async Task EnsureDefaultCoAAsync(Guid companyId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        await EnsureDefaultCoAAsync(companyId, conn, null, ct);
    }

    public async Task EnsureDefaultCoAAsync(Guid companyId, IDbConnection conn, IDbTransaction? tx, CancellationToken ct)
    {
        // P1-9: CoA-seeded inside the register-flow transaction. Read uses the same conn so it
        // sees the just-inserted companies row, and the subsequent inserts roll back together
        // with the company insert if anything else fails.
        var existingCoA = await conn.QueryFirstOrDefaultAsync<Account>(new CommandDefinition(
            $"SELECT {SelectColumns} FROM accounts WHERE LOWER(code) = LOWER(@Code) LIMIT 1",
            new { Code = "0000" }, transaction: tx, cancellationToken: ct));
        if (existingCoA != null) return;

        // DEC-093 + perf fix: Compute all IDs in-memory (topological), then ONE batched INSERT
        // with 47 rows via Postgres unnest(). Was previously 47 sequential INSERT round-trips
        // (~3.5s on Supabase eu-central-1 per register); now ~200ms.
        var allEntries = DefaultCoASeed.HoldingAccounts.ToList();
        var idByCode = new Dictionary<string, Guid>();
        var accountObjects = new List<Account>(allEntries.Count);

        // Pass 1: roots (no parent)
        foreach (var (code, name, type, parentCode, postable, intercompany) in allEntries.Where(e => e.ParentCode == null))
        {
            var acc = NewAccount(companyId, code, name, type, null, postable, intercompany);
            idByCode[code] = acc.Id;
            accountObjects.Add(acc);
        }
        // Pass 2: children (parent already mapped)
        foreach (var (code, name, type, parentCode, postable, intercompany) in allEntries.Where(e => e.ParentCode != null))
        {
            if (!idByCode.TryGetValue(parentCode!, out var parentId))
                throw new InvalidOperationException($"CoA seed bug: parent code {parentCode} not resolved before child {code}");
            var acc = NewAccount(companyId, code, name, type, parentId, postable, intercompany);
            idByCode[code] = acc.Id;
            accountObjects.Add(acc);
        }

        if (accountObjects.Count == 0) return;

        // Single batched INSERT using unnest() — 1 round-trip for 47 rows
        // type + normal_balance are integer columns in Postgres (per accounts.json).
        // We pass int[] instead of text[] + cast to skip the parse step.
        const string batchInsertSql = @"
            INSERT INTO accounts (id, company_id, code, name, type, normal_balance,
                                  parent_account_id, is_postable, is_active, is_intercompany, created_at, updated_at)
            SELECT u.id, @CompanyId, u.code, u.name, u.type, u.balance,
                   u.parent_id, u.postable, true, u.intercompany, now(), now()
            FROM unnest(@Ids::uuid[], @Codes::text[], @Names::text[], @Types::int[], @Balances::int[],
                        @ParentIds::uuid[], @Postables::bool[], @Inters::bool[])
            AS u(id, code, name, type, balance, parent_id, postable, intercompany);";

        await conn.ExecuteAsync(new CommandDefinition(batchInsertSql, new
        {
            CompanyId = companyId,
            Ids = accountObjects.Select(a => a.Id).ToArray(),
            Codes = accountObjects.Select(a => a.Code).ToArray(),
            Names = accountObjects.Select(a => a.Name).ToArray(),
            Types = accountObjects.Select(a => (int)a.Type).ToArray(),
            Balances = accountObjects.Select(a => (int)a.NormalBalance).ToArray(),
            ParentIds = accountObjects.Select(a => a.ParentAccountId ?? (Guid?)null).ToArray(),
            Postables = accountObjects.Select(a => a.IsPostable).ToArray(),
            Inters = accountObjects.Select(a => a.IsIntercompany).ToArray(),
        }, transaction: tx, cancellationToken: ct));
    }

    public async Task CloneCoAFromCompanyAsync(Guid targetCompanyId, Guid sourceCompanyId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var sourceAccounts = (await conn.QueryAsync<Account>(new CommandDefinition(@$"
            SELECT {SelectColumns} FROM accounts WHERE company_id = @SourceId ORDER BY code",
            new { SourceId = sourceCompanyId }, cancellationToken: ct))).AsList();
        if (sourceAccounts.Count == 0) return;
        var idMapping = new Dictionary<Guid, Guid>();
        foreach (var src in sourceAccounts) idMapping[src.Id] = Guid.NewGuid();
        // Pass 1: roots
        foreach (var src in sourceAccounts.Where(a => a.ParentAccountId == null))
            await InsertAsync(new Account
            {
                Id = idMapping[src.Id], CompanyId = targetCompanyId,
                Code = src.Code, Name = src.Name, Description = src.Description, Type = src.Type,
                NormalBalance = src.NormalBalance, IsPostable = src.IsPostable, IsActive = src.IsActive,
                IsIntercompany = src.IsIntercompany, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            }, ct);
        // Pass 2: children
        foreach (var src in sourceAccounts.Where(a => a.ParentAccountId != null))
        {
            Guid? newParentId = src.ParentAccountId.HasValue && idMapping.TryGetValue(src.ParentAccountId.Value, out var mapped) ? mapped : null;
            await InsertAsync(new Account
            {
                Id = idMapping[src.Id], CompanyId = targetCompanyId,
                Code = src.Code, Name = src.Name, Description = src.Description, Type = src.Type,
                NormalBalance = src.NormalBalance, ParentAccountId = newParentId,
                IsPostable = src.IsPostable, IsActive = src.IsActive, IsIntercompany = src.IsIntercompany,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            }, ct);
        }
    }

    private static Account NewAccount(Guid companyId, string code, string name, AccountType type, Guid? parentId, bool postable, bool intercompany)
    {
        var now = DateTime.UtcNow;
        return new Account
        {
            Id = Guid.NewGuid(), CompanyId = companyId, Code = code, Name = name,
            Type = type,
            NormalBalance = type == AccountType.Asset || type == AccountType.Expense ? NormalBalance.Debit : NormalBalance.Credit,
            ParentAccountId = parentId, IsPostable = postable, IsActive = true, IsIntercompany = intercompany,
            CreatedAt = now, UpdatedAt = now
        };
    }

    private static async Task<Account?> QueryFirstAsync(IDbConnection conn, string where, object p, CancellationToken ct)
    {
        return await conn.QueryFirstOrDefaultAsync<Account>(new CommandDefinition(
            $"SELECT {SelectColumns} FROM accounts " + where + " LIMIT 1", p, cancellationToken: ct));
    }
}
