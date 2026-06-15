using Dapper;
using ERPSystem.Modules.Finance.Entities;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Finance.Infrastructure;

public sealed class AccountRepository : IAccountRepository
{
    private readonly IDbConnectionFactory _db;

    public AccountRepository(IDbConnectionFactory db) => _db = db;

    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        return await QueryFirstAsync(conn, "WHERE id = @Id", new { Id = id }, ct);
    }

    public async Task<Account?> GetByCodeAsync(Guid tenantId, string code, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        return await QueryFirstAsync(conn, "WHERE tenant_id = @TenantId AND LOWER(code) = LOWER(@Code)",
            new { TenantId = tenantId, Code = code }, ct);
    }

    public async Task<IReadOnlyList<Account>> ListAsync(Guid tenantId, bool includeInactive, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var sql = @"
            SELECT id, tenant_id AS TenantId, code, name, description, type, normal_balance AS NormalBalance,
                   parent_account_id AS ParentAccountId, is_postable AS IsPostable,
                   is_active AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM accounts
            WHERE tenant_id = @TenantId"
            + (includeInactive ? "" : " AND is_active = true")
            + " ORDER BY code";
        var rows = await conn.QueryAsync<Account>(new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<Account>> ListChildrenAsync(Guid parentId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            SELECT id, tenant_id AS TenantId, code, name, description, type, normal_balance AS NormalBalance,
                   parent_account_id AS ParentAccountId, is_postable AS IsPostable,
                   is_active AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM accounts WHERE parent_account_id = @ParentId ORDER BY code";
        var rows = await conn.QueryAsync<Account>(new CommandDefinition(sql, new { ParentId = parentId }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task InsertAsync(Account account, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            INSERT INTO accounts (id, tenant_id, code, name, description, type, normal_balance,
                                  parent_account_id, is_postable, is_active, created_at, updated_at)
            VALUES (@Id, @TenantId, @Code, @Name, @Description, @Type, @NormalBalance,
                    @ParentAccountId, @IsPostable, @IsActive, @CreatedAt, @UpdatedAt)";
        await conn.ExecuteAsync(new CommandDefinition(sql, account, cancellationToken: ct));
    }

    public async Task UpdateAsync(Account account, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            UPDATE accounts SET name = @Name, description = @Description, type = @Type,
                                normal_balance = @NormalBalance, parent_account_id = @ParentAccountId,
                                is_postable = @IsPostable, is_active = @IsActive, updated_at = @UpdatedAt
            WHERE id = @Id";
        await conn.ExecuteAsync(new CommandDefinition(sql, account, cancellationToken: ct));
    }

    public async Task<int> CountPostingsAsync(Guid accountId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = "SELECT COUNT(1) FROM journal_lines WHERE account_id = @AccountId";
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { AccountId = accountId }, cancellationToken: ct));
    }

    public async Task EnsureDefaultCoAAsync(Guid tenantId, CancellationToken ct)
    {
        // شجرة حسابات افتراضية لأي مستأجر جديد
        // النمط: كود رقمي 4 خانات (1100, 1101, 4100, ...)
        var defaults = new (string Code, string Name, AccountType Type, string? ParentCode, bool Postable)[]
        {
            // ====== 1xxx: Assets ======
            ("1000", "الأصول", AccountType.Asset, null, false),
            ("1100", "النقدية وما في حكمها", AccountType.Asset, "1000", false),
            ("1110", "الصندوق", AccountType.Asset, "1100", true),
            ("1120", "البنك", AccountType.Asset, "1100", true),
            ("1200", "المدينون", AccountType.Asset, "1000", true),
            ("1300", "المخزون", AccountType.Asset, "1000", true),
            // ====== 2xxx: Liabilities ======
            ("2000", "الخصوم", AccountType.Liability, null, false),
            ("2100", "الدائنون", AccountType.Liability, "2000", true),
            ("2200", "القروض", AccountType.Liability, "2000", true),
            // ====== 3xxx: Equity ======
            ("3000", "حقوق الملكية", AccountType.Equity, null, false),
            ("3100", "رأس المال", AccountType.Equity, "3000", true),
            ("3200", "الأرباح المحتجزة", AccountType.Equity, "3000", true),
            // ====== 4xxx: Revenue ======
            ("4000", "الإيرادات", AccountType.Revenue, null, false),
            ("4100", "إيرادات المبيعات", AccountType.Revenue, "4000", true),
            ("4200", "إيرادات الخدمات", AccountType.Revenue, "4000", true),
            // ====== 5xxx: Expenses ======
            ("5000", "المصروفات", AccountType.Expense, null, false),
            ("5100", "تكلفة البضاعة المباعة", AccountType.Expense, "5000", true),
            ("5200", "مصروفات الرواتب", AccountType.Expense, "5000", true),
            ("5300", "مصروفات الإيجار", AccountType.Expense, "5000", true),
            ("5400", "مصروفات أخرى", AccountType.Expense, "5000", true),
        };

        // Inserter على مرحلتين: أولاً الأب، ثم الأبناء (لأن FK على parent_account_id)
        var idByCode = new Dictionary<string, Guid>();
        foreach (var (code, name, type, _, _) in defaults.Where(d => d.ParentCode == null))
        {
            if (await GetByCodeAsync(tenantId, code, ct) != null) continue;
            var acc = NewAccount(tenantId, code, name, type);
            await InsertAsync(acc, ct);
            idByCode[code] = acc.Id;
        }

        foreach (var (code, name, type, parentCode, postable) in defaults.Where(d => d.ParentCode != null))
        {
            if (await GetByCodeAsync(tenantId, code, ct) != null) continue;
            if (!idByCode.TryGetValue(parentCode!, out var parentId))
            {
                // الحساب الأب موجود بالفعل (idempotent re-run)
                var parent = await GetByCodeAsync(tenantId, parentCode!, ct);
                if (parent == null) continue; // skip — should not happen
                parentId = parent.Id;
            }
            var acc = NewAccount(tenantId, code, name, type, parentId, postable);
            await InsertAsync(acc, ct);
            idByCode[code] = acc.Id;
        }
    }

    private static Account NewAccount(Guid tenantId, string code, string name, AccountType type, Guid? parentId = null, bool postable = false)
    {
        var now = DateTime.UtcNow;
        return new Account
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code,
            Name = name,
            Type = type,
            NormalBalance = type switch
            {
                AccountType.Asset or AccountType.Expense => NormalBalance.Debit,
                _ => NormalBalance.Credit
            },
            ParentAccountId = parentId,
            IsPostable = postable,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static async Task<Account?> QueryFirstAsync(System.Data.IDbConnection conn, string whereClause, object parameters, CancellationToken ct)
    {
        var sql = @"
            SELECT id, tenant_id AS TenantId, code, name, description, type, normal_balance AS NormalBalance,
                   parent_account_id AS ParentAccountId, is_postable AS IsPostable,
                   is_active AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM accounts " + whereClause + " LIMIT 1";
        return await conn.QueryFirstOrDefaultAsync<Account>(new CommandDefinition(sql, parameters, cancellationToken: ct));
    }
}
