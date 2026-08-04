using System.Data;
using System.Text;
using Dapper;
using ERPSystem.Shared.Infrastructure;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Shared.DataTypes;

/// <summary>
/// DEC-079 PoC: Reconciles the database schema with the JSON DataType definitions.
///
/// Algorithm:
/// 1. For each DataType:
///    a. If table doesn't exist → CREATE TABLE
///    b. For each field:
///       - If column doesn't exist → ADD COLUMN
///       - If FK defined → ADD FOREIGN KEY constraint (idempotent)
///    c. For each index:
///       - If index doesn't exist → CREATE INDEX
///
/// Edge cases:
/// - Column type changed: log warning, do NOT alter (manual migration)
/// - Column removed from JSON: log warning, do NOT drop (soft delete like ERPNext)
/// - FK target missing: error + skip constraint
/// - Bad JSON: registry already filtered these, registry.Errors has details
/// </summary>
public sealed class DataTypeMigrator
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<DataTypeMigrator> _logger;

    public DataTypeMigrator(IDbConnectionFactory db, ILogger<DataTypeMigrator> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<MigrationResult> ReconcileAsync(IEnumerable<DataType> dataTypes, CancellationToken ct)
    {
        var result = new MigrationResult();
        var dataTypeList = dataTypes.ToList();

        // Phase 6.3 hotfix (PR #149 round 3): الـ root cause الحقيقي كان
        // information_schema queries بدون table_schema filter (Supabase auth.users /
        // auth.refresh_tokens كانت بتلوّث TableExistsAsync). الـ direct connection
        // (port 5432) للـ migrations يبقى مهم كـ defense-in-depth (Supabase docs
        // توصي صراحة بيه).
        IDbConnection conn = await _db.CreateEphemeralMigrationConnectionAsync(ct)
            ?? await _db.CreateEphemeralOltpConnectionAsync(ct);

        // 2-pass architecture (PR #149 round 3): pass 1 ينشئ كل الـ tables + columns
        // + indexes بدون FKs. pass 2 يضيف FKs لما كل الـ target tables تكون موجودة.
        // السبب: alphabet order يخلّي User (users) يتعمل بعد Notification /
        // PasswordResetToken / RefreshToken / UserCompany / UserRole، فـ FKs بتاعتها
        // للـ users كانت تنفشل في الـ pass الواحد. الـ 2-pass يضمن كل الـ FKs تنضاف
        // من أول run.
        _logger.LogInformation("[DataTypeMigrator] Pass 1/2: tables + columns + indexes (no FKs yet)...");
        foreach (var dt in dataTypeList)
        {
            _logger.LogInformation("[DataTypeMigrator] Reconciling {Name} (table={Table}, version={Version}) [pass 1/2]",
                dt.Name, dt.Table, dt.Version);
            try
            {
                await ReconcileOneAsync(conn, dt, result, ct, includeForeignKeys: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DataTypeMigrator] Failed to reconcile {Name} [pass 1/2]", dt.Name);
                result.Errors.Add($"{dt.Name}: {ex.Message}");
            }
        }

        _logger.LogInformation("[DataTypeMigrator] Pass 2/2: foreign keys (all target tables now exist)...");
        foreach (var dt in dataTypeList)
        {
            _logger.LogInformation("[DataTypeMigrator] Reconciling {Name} (table={Table}, version={Version}) [pass 2/2 FKs]",
                dt.Name, dt.Table, dt.Version);
            try
            {
                await ReconcileOneAsync(conn, dt, result, ct, includeForeignKeys: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DataTypeMigrator] Failed to reconcile {Name} [pass 2/2 FKs]", dt.Name);
                result.Errors.Add($"{dt.Name}: {ex.Message}");
            }
        }

        _logger.LogInformation(
            "[DataTypeMigrator] Done. Tables created: {T}, columns added: {C}, indexes added: {I}, errors: {E}",
            result.TablesCreated, result.ColumnsAdded, result.IndexesCreated, result.Errors.Count);
        return result;
    }

    private async Task ReconcileOneAsync(IDbConnection conn, DataType dt, MigrationResult result, CancellationToken ct, bool includeForeignKeys)
    {
        // 1) Table existence
        var tableExists = await TableExistsAsync(conn, dt.Table, ct);
        if (!tableExists)
        {
            // Pre-create any sequences referenced by nextval() defaults.
            // Phase 6.0b hotfix: after Phase 6 nuclear clean slate, the
            // audit_log_id_seq doesn't exist. CREATE TABLE with a nextval()
            // default fails because the sequence doesn't exist.
            foreach (var f in dt.Fields)
            {
                if (!string.IsNullOrEmpty(f.Default) && f.Default.Contains("nextval("))
                {
                    var seqName = ExtractSequenceName(f.Default);
                    if (!string.IsNullOrEmpty(seqName))
                    {
                        await conn.ExecuteAsync(new CommandDefinition(
                            $"CREATE SEQUENCE IF NOT EXISTS {seqName};",
                            cancellationToken: ct));
                    }
                }
            }
            await CreateTableAsync(conn, dt, ct);
            result.TablesCreated.Add(dt.Table);
            _logger.LogInformation("[DataTypeMigrator] Created table {Table}", dt.Table);
            tableExists = true;
        }

        // 2) Columns
        var existingCols = await GetColumnsAsync(conn, dt.Table, ct);
        foreach (var field in dt.Fields)
        {
            if (existingCols.Contains(field.Name))
            {
                continue; // already exists — idempotent skip
            }
            await AddColumnAsync(conn, dt.Table, field, ct);
            result.ColumnsAdded.Add($"{dt.Table}.{field.Name}");
            _logger.LogInformation("[DataTypeMigrator] Added column {Table}.{Col} ({Type})",
                dt.Table, field.Name, field.Type);
        }

        // 3) Foreign keys (idempotent — pg_constraint check). تتعمل في pass 2 فقط
        // بعد ما كل الـ target tables تكون موجودة.
        if (includeForeignKeys)
        {
            foreach (var field in dt.Fields.Where(f => f.ForeignKey != null))
            {
                var fk = field.ForeignKey!;
                var fkName = fk.Name ?? $"fk_{dt.Table}_{field.Name}";
                if (await ConstraintExistsAsync(conn, fkName, ct))
                {
                    continue;
                }
                var targetTableExists = await TableExistsAsync(conn, fk.Table, ct);
                if (!targetTableExists)
                {
                    _logger.LogWarning("[DataTypeMigrator] FK target missing: {Table}.{Col} → {Target}. Skipping.",
                        dt.Table, field.Name, fk.Table);
                    continue;
                }
                await AddForeignKeyAsync(conn, dt.Table, field, fkName, ct);
                _logger.LogInformation("[DataTypeMigrator] Added FK {Name} on {Table}.{Col}",
                    fkName, dt.Table, field.Name);
            }
        }

        // 4) Indexes
        foreach (var idx in dt.Indexes)
        {
            if (await IndexExistsAsync(conn, idx.Name, ct))
            {
                continue;
            }
            await CreateIndexAsync(conn, dt.Table, idx, ct);
            result.IndexesCreated.Add(idx.Name);
            _logger.LogInformation("[DataTypeMigrator] Created index {Name} on {Table}({Cols})",
                idx.Name, dt.Table, string.Join(",", idx.Columns));
        }
    }

    // === Schema introspection helpers ===

    // Supabase ships its own `auth` schema with `users` + `refresh_tokens` tables
    // (GoTrue). Phase 6.3 hotfix (PR #149 round 3, real root cause): بدون فلتر
    // table_schema، TableExistsAsync("users") بترجع TRUE من auth.users، فالـ
    // precheck على FKs للـ users/user_companies/user_roles/notifications/password_
    // reset_tokens/refresh_tokens بيعتقد إن الـ target موجود → يـ proceed → الـ SQL
    // اللي بيستخدم `users` بدون schema qualifier بيفشل (relation users does not
    // exist لأن الـ public.users ما اتعملش CREATE). نفس المنطق ينطبق على
    // ConstraintExistsAsync (FKs بنفس الاسم ممكن تتشارك عبر schemas).
    private const string AppSchema = "public";

    private static async Task<bool> TableExistsAsync(IDbConnection conn, string table, CancellationToken ct)
    {
        var n = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = @S AND table_name = @T",
            new { S = AppSchema, T = table.ToLowerInvariant() }, cancellationToken: ct));
        return n > 0;
    }

    private static async Task<HashSet<string>> GetColumnsAsync(IDbConnection conn, string table, CancellationToken ct)
    {
        var cols = await conn.QueryAsync<string>(new CommandDefinition(
            @"SELECT column_name FROM information_schema.columns
              WHERE table_schema = @S AND table_name = @T",
            new { S = AppSchema, T = table.ToLowerInvariant() }, cancellationToken: ct));
        return new HashSet<string>(cols, StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<bool> ConstraintExistsAsync(IDbConnection conn, string name, CancellationToken ct)
    {
        var n = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            @"SELECT COUNT(*) FROM information_schema.table_constraints
              WHERE constraint_schema = @S AND constraint_name = @N",
            new { S = AppSchema, N = name.ToLowerInvariant() }, cancellationToken: ct));
        return n > 0;
    }

    private static async Task<bool> IndexExistsAsync(IDbConnection conn, string name, CancellationToken ct)
    {
        var n = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            @"SELECT COUNT(*) FROM pg_indexes WHERE schemaname = @S AND indexname = @N",
            new { S = AppSchema, N = name.ToLowerInvariant() }, cancellationToken: ct));
        return n > 0;
    }

    /// <summary>
    /// Extracts the sequence name from a Postgres DEFAULT expression like
    /// <c>nextval('audit_log_id_seq'::regclass)</c>. Returns the inner sequence
    /// name (with original quoting preserved), or null if not a nextval.
    /// </summary>
    private static string? ExtractSequenceName(string defaultExpr)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            defaultExpr, @"nextval\s*\(\s*'([^']+)'", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    // === Schema mutation helpers ===

    private static async Task CreateTableAsync(IDbConnection conn, DataType dt, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.Append($"CREATE TABLE {dt.Table} (");
        var first = true;
        var pkFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // DEC-083: composite PK support — use DataType.PrimaryKey if defined
        if (dt.PrimaryKey != null && dt.PrimaryKey.Count > 0)
        {
            foreach (var pk in dt.PrimaryKey) pkFields.Add(pk);
        }
        else
        {
            // Legacy: fields with primary_key=true
            foreach (var f in dt.Fields)
            {
                if (f.PrimaryKey) pkFields.Add(f.Name);
            }
        }

        foreach (var f in dt.Fields)
        {
            if (!first) sb.Append(", ");
            first = false;
            // DEC-112: Quoted=true forces double-quoted SQL identifier (e.g., "from", "to" — SQL reserved words).
            var ident = f.Quoted ? "\"" + f.Name.Replace("\"", "\"\"") + "\"" : QuoteIdent(f.Name);
            sb.Append(ident).Append(' ').Append(f.Type);
            if (pkFields.Contains(f.Name)) sb.Append(" NOT NULL");  // PK columns are implicitly NOT NULL
            else if (!f.Nullable) sb.Append(" NOT NULL");
            if (!string.IsNullOrEmpty(f.Default)) sb.Append(" DEFAULT ").Append(f.Default);
        }
        // Append the primary key constraint (composite or single)
        if (pkFields.Count > 0)
        {
            var pkCols = string.Join(", ", pkFields.Select(QuoteIdent));
            sb.Append(", PRIMARY KEY (").Append(pkCols).Append(")");
        }
        sb.Append(");");
        await conn.ExecuteAsync(new CommandDefinition(sb.ToString(), cancellationToken: ct));
    }

    private static async Task AddColumnAsync(IDbConnection conn, string table, FieldDefinition f, CancellationToken ct)
    {
        var sb = new StringBuilder();
        // DEC-112: Quoted=true forces double-quoted SQL identifier (for SQL reserved words).
        var ident = f.Quoted ? "\"" + f.Name.Replace("\"", "\"\"") + "\"" : QuoteIdent(f.Name);
        sb.Append($"ALTER TABLE {table} ADD COLUMN {ident} {f.Type}");
        if (!f.Nullable) sb.Append(" NOT NULL");
        if (!string.IsNullOrEmpty(f.Default)) sb.Append(" DEFAULT ").Append(f.Default);
        await conn.ExecuteAsync(new CommandDefinition(sb.ToString(), cancellationToken: ct));
    }

    private static async Task AddForeignKeyAsync(
        IDbConnection conn, string table, FieldDefinition field, string fkName, CancellationToken ct)
    {
        var fk = field.ForeignKey!;
        var onDelete = fk.OnDelete?.ToLowerInvariant() switch
        {
            "cascade" => "CASCADE",
            "set_null" => "SET NULL",
            "restrict" => "RESTRICT",
            _ => "NO ACTION"
        };
        var sql = $@"ALTER TABLE {table}
                     ADD CONSTRAINT {QuoteIdent(fkName)}
                     FOREIGN KEY ({QuoteIdent(field.Name)})
                     REFERENCES {fk.Table}({QuoteIdent(fk.Column)})
                     ON DELETE {onDelete}";
        await conn.ExecuteAsync(new CommandDefinition(sql, cancellationToken: ct));
    }

    private static async Task CreateIndexAsync(
        IDbConnection conn, string table, IndexDefinition idx, CancellationToken ct)
    {
        var cols = string.Join(", ", idx.Columns.Select(QuoteIdent));
        var unique = idx.Unique ? "UNIQUE" : "";
        var sql = $"CREATE {unique} INDEX {QuoteIdent(idx.Name)} ON {table}({cols})";
        await conn.ExecuteAsync(new CommandDefinition(sql, cancellationToken: ct));
    }

    /// <summary>
    /// Minimal identifier quoting (PostgreSQL). Avoids SQL injection from JSON values.
    /// Quotes anything that isn't [a-zA-Z0-9_] or starts with a digit.
    /// </summary>
    private static string QuoteIdent(string ident)
    {
        if (string.IsNullOrEmpty(ident)) return "\"\"";
        if (ident.All(c => char.IsLetterOrDigit(c) || c == '_') && !char.IsDigit(ident[0]))
        {
            return ident;  // safe — no quotes needed
        }
        return "\"" + ident.Replace("\"", "\"\"") + "\"";
    }
}

public sealed class MigrationResult
{
    public List<string> TablesCreated { get; } = new();
    public List<string> ColumnsAdded { get; } = new();
    public List<string> IndexesCreated { get; } = new();
    public List<string> Errors { get; } = new();
}
