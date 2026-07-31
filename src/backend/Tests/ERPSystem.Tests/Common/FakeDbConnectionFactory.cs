using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text.RegularExpressions;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Tests.Common;

/// <summary>
/// In-memory IDbConnectionFactory لمحاكاة Dapper + DataSet بدون DB حقيقي.
/// كل query تستخرج اسم الـ table الأولى في FROM clause وترجع DataTable.
/// للـ tests فقط — لا يستخدم في production.
/// </summary>
public sealed class FakeDbConnectionFactory : IDbConnectionFactory
{
    public DataSet Data { get; } = new();

    public Task<IDbConnection> CreateOltpConnectionAsync(CancellationToken ct = default) =>
        Task.FromResult<IDbConnection>(new FakeDbConnection(Data));

    public Task<IDbConnection> CreateEventStoreConnectionAsync(CancellationToken ct = default) =>
        Task.FromResult<IDbConnection>(new FakeDbConnection(Data));

    /// <summary>
    /// نسخة الاختبار: في الذاكرة، بدون pool semantics (ما يهم لأن FakeDbConnection
    /// ما يشارك في pgbouncer transaction-mode). نرجّع FakeDbConnection عادي ليتمكّن
    /// الـ bootstrap tests من استدعاء نفس الـ surface area بتاع الـ production factory.
    /// </summary>
    public Task<IDbConnection> CreateEphemeralOltpConnectionAsync(CancellationToken ct = default) =>
        Task.FromResult<IDbConnection>(new FakeDbConnection(Data));

    /// <summary>
    /// نسخة الاختبار من direct migration connection: نفس FakeDbConnection (ما يهم
    /// لأن الـ tests كلها in-memory). نرجّع null-safe — لو الـ test ما يحتاج
    /// migration، الـ caller يفحص.
    /// </summary>
    public Task<IDbConnection?> CreateEphemeralMigrationConnectionAsync(CancellationToken ct = default) =>
        Task.FromResult<IDbConnection?>(new FakeDbConnection(Data));

    public void EnsureTable(string tableName)
    {
        if (!Data.Tables.Contains(tableName))
            Data.Tables.Add(tableName);
    }

    public void AddRow(string tableName, params object[] columns)
    {
        EnsureTable(tableName);
        var table = Data.Tables[tableName]!;
        var row = table.NewRow();
        for (int i = 0; i < columns.Length; i += 2)
        {
            var colName = columns[i].ToString()!;
            if (!table.Columns.Contains(colName))
            {
                var value = columns[i + 1];
                table.Columns.Add(colName, value?.GetType() ?? typeof(object));
            }
            row[colName] = columns[i + 1] ?? DBNull.Value;
        }
        table.Rows.Add(row);
    }

    public int Count(string tableName) =>
        Data.Tables.Contains(tableName) ? Data.Tables[tableName]!.Rows.Count : 0;
}

internal sealed class FakeDbConnection : DbConnection
{
    private readonly DataSet _ds;
    public FakeDbConnection(DataSet ds) => _ds = ds;
    public override string ConnectionString { get; set; } = string.Empty;
    public override string Database => "fake";
    public override string DataSource => "fake";
    public override string ServerVersion => "1.0";
    public override ConnectionState State { get; } = ConnectionState.Open;
    public override void ChangeDatabase(string databaseName) { }
    public override void Close() { }
    public override void Open() { }
    protected override DbCommand CreateDbCommand() => new FakeDbCommand(_ds);
    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
}

internal sealed class FakeDbCommand : DbCommand
{
    private readonly DataSet _ds;
    private readonly FakeDbParameterCollection _parameters = new();
    public FakeDbCommand(DataSet ds) => _ds = ds;
    public override string CommandText { get; set; } = string.Empty;
    public override int CommandTimeout { get; set; } = 30;
    public override CommandType CommandType { get; set; } = CommandType.Text;
    public override bool DesignTimeVisible { get; set; }
    public override UpdateRowSource UpdatedRowSource { get; set; }
    protected override DbConnection? DbConnection { get; set; }
    protected override DbParameterCollection DbParameterCollection => _parameters;
    protected override DbTransaction? DbTransaction { get; set; }
    public override void Cancel() { }
    public override void Prepare() { }
    protected override DbParameter CreateDbParameter() => new FakeDbParameter();
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) =>
        new FakeDbDataReader(_ds, CommandText);
    public override int ExecuteNonQuery() => 0;
    // Sprint 1 (T3 / Block A): COUNT(*) / COUNT(1) support for unit tests.
    // Previously returned null → Dapper defaulted to 0 for any COUNT.
    // Now extracts the FROM table name and returns its row count.
    // The WHERE clause is ignored (same limitation as FakeDbDataReader).
    // This is a strict improvement: tests that did not rely on COUNT
    // getting 0 are unaffected; tests that do COUNT against a known
    // row count can now assert exact values (see DashboardSummaryTests).
    //
    // Sprint 2 (T6 / Block A): also handle the Postgres `::int` cast that
    // production repos use after COUNT(*). Without this, SQL like
    // `SELECT COUNT(*)::int FROM companies` does not match the regex
    // (the `::int` is between `)` and `FROM`). The updated regex allows an
    // optional cast like `::int` or `::bigint` between the close paren and
    // the FROM keyword.
    public override object? ExecuteScalar()
    {
        // Match COUNT(*), COUNT(1), or COUNT(somecol) optionally followed by a
        // Postgres cast `::<typename>`, then FROM <table>. The capture group is
        // the table name; the argument inside the parens is intentionally
        // permissive (matches anything that doesn't contain a closing paren).
        var m = System.Text.RegularExpressions.Regex.Match(
            CommandText, @"\bCOUNT\s*\(\s*[^)]*\)\s*(?:::\w+\s*)?FROM\s+([a-zA-Z_][a-zA-Z0-9_]*)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (m.Success)
        {
            var table = m.Groups[1].Value;
            return _ds.Tables.Contains(table) ? _ds.Tables[table]!.Rows.Count : 0;
        }
        return null;
    }
}

internal sealed class FakeDbParameter : DbParameter
{
    public override string ParameterName { get; set; } = string.Empty;
    public override object? Value { get; set; }
    public override DbType DbType { get; set; }
    public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
    public override bool IsNullable { get; set; }
    public override int Size { get; set; }
    public override string SourceColumn { get; set; } = string.Empty;
    public override bool SourceColumnNullMapping { get; set; }
    public override DataRowVersion SourceVersion { get; set; } = DataRowVersion.Current;
    public override void ResetDbType() { }
}

internal sealed class FakeDbParameterCollection : DbParameterCollection
{
    private readonly List<DbParameter> _params = new();
    public override int Add(object value) { _params.Add((DbParameter)value); return _params.Count - 1; }
    public override void AddRange(Array values) { foreach (var v in values) _params.Add((DbParameter)v); }
    public override void Clear() => _params.Clear();
    public override bool Contains(object value) => _params.Contains((DbParameter)value);
    public override int IndexOf(object value) => _params.IndexOf((DbParameter)value);
    public override void Insert(int index, object value) => _params.Insert(index, (DbParameter)value);
    public override void Remove(object value) => _params.Remove((DbParameter)value);
    public override void RemoveAt(int index) => _params.RemoveAt(index);
    public override void RemoveAt(string parameterName) { var i = IndexOf(parameterName); if (i >= 0) _params.RemoveAt(i); }
    protected override DbParameter GetParameter(int index) => _params[index];
    protected override DbParameter GetParameter(string parameterName) => _params[IndexOf(parameterName)];
    protected override void SetParameter(int index, DbParameter value) => _params[index] = value;
    protected override void SetParameter(string parameterName, DbParameter value) => _params[IndexOf(parameterName)] = value;
    public override int Count => _params.Count;
    public override object SyncRoot => _params;
    public override int IndexOf(string parameterName) => _params.FindIndex(p => p.ParameterName == parameterName);
    public override bool Contains(string parameterName) => IndexOf(parameterName) >= 0;
    public override void CopyTo(Array array, int index) { for (int i = index; i < array.Length && i - index < _params.Count; i++) array.SetValue(_params[i - index], i); }
    public override System.Collections.IEnumerator GetEnumerator() => _params.GetEnumerator();
}

internal sealed class FakeDbDataReader : DbDataReader
{
    private readonly DataTable? _table;
    private int _rowIndex = -1;

    public FakeDbDataReader(DataSet ds, string sql)
    {
        var tableName = ExtractTableName(sql);
        if (!ds.Tables.Contains(tableName))
        {
            _table = null;
            return;
        }
        // Sprint 8 T2: try projecting SELECT aliases onto the DataTable's columns.
        // Falls back to the direct table if SELECT parsing fails or no AS aliases are present.
        _table = ProjectColumns(sql, ds, tableName) ?? ds.Tables[tableName]!;
    }

    private static string ExtractTableName(string sql)
    {
        // نمط بسيط: نلتقط أول كلمة بعد FROM أو JOIN
        var m = Regex.Match(sql, @"\b(?:FROM|JOIN)\s+([a-zA-Z_][a-zA-Z0-9_]*)", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : "unknown";
    }

    /// <summary>
    /// Sprint 8 T2: parse the SELECT clause + project the DataTable's columns
    /// onto the alias names. Returns a NEW DataTable (does not mutate the source).
    ///
    /// Behavior:
    /// - SELECT <col-list> FROM <tableName> is parsed (case-insensitive, singleline).
    /// - For each column in the list, an optional `AS <alias>` is recognized.
    /// - Quoted aliases (`AS "AccountId"`) are unquoted.
    /// - Columns without an alias keep their source name.
    /// - Aggregate/expression columns (`COUNT(*) AS total`, `(code || '-' || name) AS "DisplayName"`)
    ///   get the alias column added with `typeof(object)` (the value is left as DBNull
    ///   because we don't simulate the expression).
    /// - If SELECT parsing fails (no SELECT, no FROM, etc.), returns null so the
    ///   caller falls back to the direct table.
    /// </summary>
    internal static DataTable? ProjectColumns(string sql, DataSet ds, string tableName)
    {
        // 1. Find "SELECT <col-list> FROM <tableName>" — case-insensitive, singleline.
        var match = Regex.Match(
            sql,
            @"\bSELECT\s+(?<cols>.+?)\s+FROM\s+([a-zA-Z_][a-zA-Z0-9_]*)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!match.Success) return null;
        if (!ds.Tables.Contains(tableName)) return null;

        var source = ds.Tables[tableName]!;
        var columnList = match.Groups["cols"].Value;

        // 2. Parse comma-separated columns with depth/quote tracking.
        // Start with an EMPTY projected table — we only expose the columns
        // named in the SELECT, not the entire source schema. This matches
        // what Dapper expects (a reader with N columns for `SELECT a, b, c`).
        var projected = new DataTable(tableName);
        // Track each projected column's source column ORDINAL (or -1 for expression aliases).
        // We use ordinal instead of name for the row-copy lookup so that
        // case-mismatches between SQL (e.g. `a.id AS Id`) and the underlying
        // DataTable column name (e.g. `Id`) work transparently.
        var aliasToSourceOrdinal = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var raw in SplitColumns(columnList))
        {
            var col = raw.Trim();
            string sourceName;
            string aliasName;

            // Check for AS clause. The regex is non-greedy on the source part
            // so `id AS "AccountId"` matches with src=id, alias="AccountId".
            var asMatch = Regex.Match(col, @"^(?<src>.+?)\s+AS\s+(?<alias>.+)$", RegexOptions.IgnoreCase);
            if (asMatch.Success)
            {
                // Strip any table alias prefix (e.g. `a.id` → `id`, `accounts.code` → `code`).
                sourceName = StripTableAlias(Unquote(asMatch.Groups["src"].Value.Trim()));
                aliasName = Unquote(asMatch.Groups["alias"].Value.Trim());
            }
            else
            {
                sourceName = StripTableAlias(Unquote(col));
                aliasName = sourceName;
            }

            // Add the projected column. Find the source column by trying:
            //   1. The ALIAS name (case-insensitive) — handles the old "projected
            //      column names" pattern where the test added columns with the
            //      SELECT-alias names (e.g. "Id", "UserId") and the SQL uses
            //      `a.id AS Id`. Even though the SQL source name is `a.id`
            //      (stripped → `id`) and doesn't match "Id" in some case-sensitive
            //      sense, the ALIAS name matches the source directly.
            //   2. The SQL source name (stripped, case-insensitive) — handles the
            //      new "real SQL" pattern where AddRow uses base names and the
            //      SQL aliases them: `AddRow("id", ...)` + `SELECT id AS "AccountId"`.
            //   3. -1 (not found) — expression/aggregate alias, value will be DBNull.
            if (!projected.Columns.Contains(aliasName))
            {
                int srcOrdinal = FindSourceOrdinal(source, aliasName);
                if (srcOrdinal < 0)
                {
                    srcOrdinal = FindSourceOrdinal(source, sourceName);
                }

                if (srcOrdinal >= 0)
                {
                    projected.Columns.Add(aliasName, source.Columns[srcOrdinal].DataType);
                }
                else
                {
                    projected.Columns.Add(aliasName, typeof(object));
                }
                aliasToSourceOrdinal[aliasName] = srcOrdinal;
            }
        }

        // 3. Copy rows with projection. The projected table only has the
        // SELECT columns (aliased or not), so the row copy iterates over the
        // projected columns and maps each back to its source by ordinal.
        // For expression/aggregate aliases whose source column does not exist
        // in the underlying DataTable, the value is DBNull.
        foreach (DataRow srcRow in source.Rows)
        {
            var newRow = projected.NewRow();
            foreach (DataColumn col in projected.Columns)
            {
                var srcOrdinal = aliasToSourceOrdinal[col.ColumnName];
                if (srcOrdinal >= 0)
                {
                    newRow[col.ColumnName] = srcRow[srcOrdinal];
                }
                else
                {
                    newRow[col.ColumnName] = DBNull.Value;
                }
            }
            projected.Rows.Add(newRow);
        }

        return projected;
    }

    /// <summary>
    /// Split a SELECT column list on top-level commas, ignoring commas inside
    /// parentheses or quotes. State machine: depth counter for parens,
    /// boolean for double-quote tracking.
    /// </summary>
    private static IEnumerable<string> SplitColumns(string columnList)
    {
        int depth = 0;
        bool inQuote = false;
        var current = new System.Text.StringBuilder();
        foreach (var ch in columnList)
        {
            if (ch == '"' && (current.Length == 0 || current[current.Length - 1] != '\\'))
                inQuote = !inQuote;
            if (!inQuote)
            {
                if (ch == '(') depth++;
                else if (ch == ')') depth--;
                else if (ch == ',' && depth == 0)
                {
                    yield return current.ToString();
                    current.Clear();
                    continue;
                }
            }
            current.Append(ch);
        }
        if (current.Length > 0) yield return current.ToString();
    }

    /// <summary>
    /// Strip surrounding double-quotes from a SQL identifier (e.g. `"AccountId"` → `AccountId`).
    /// Leaves unquoted identifiers unchanged.
    /// </summary>
    private static string Unquote(string s) =>
        s.Length >= 2 && s[0] == '"' && s[^1] == '"' ? s.Substring(1, s.Length - 2) : s;

    /// <summary>
    /// Strip an optional table alias prefix from a column reference.
    /// `a.id` → `id`, `accounts.code` → `code`, `id` → `id`.
    /// </summary>
    private static string StripTableAlias(string s)
    {
        var dot = s.IndexOf('.');
        return dot >= 0 ? s.Substring(dot + 1) : s;
    }

    /// <summary>
    /// Find the ordinal of a source column by name (case-insensitive).
    /// Returns -1 if not found. Used to map SELECT column references back
    /// to the underlying DataTable schema.
    /// </summary>
    private static int FindSourceOrdinal(DataTable source, string columnName)
    {
        for (int i = 0; i < source.Columns.Count; i++)
        {
            if (string.Equals(source.Columns[i].ColumnName, columnName, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }
        return -1;
    }

    public override object this[int i] => _table!.Rows[_rowIndex][i];
    public override object this[string name] => _table!.Rows[_rowIndex][name];
    public override int Depth => 0;
    public override bool IsClosed => false;
    public override int RecordsAffected => 0;
    public override int FieldCount => _table?.Columns.Count ?? 0;
    public override bool HasRows => _table?.Rows.Count > 0;
    public override bool GetBoolean(int i) => Convert.ToBoolean(_table!.Rows[_rowIndex][i], CultureInfo.InvariantCulture);
    public override byte GetByte(int i) => Convert.ToByte(_table!.Rows[_rowIndex][i], CultureInfo.InvariantCulture);
    public override long GetBytes(int i, long dataOffset, byte[]? buffer, int bufferOffset, int length) => 0;
    public override char GetChar(int i) => Convert.ToChar(_table!.Rows[_rowIndex][i], CultureInfo.InvariantCulture);
    public override long GetChars(int i, long dataOffset, char[]? buffer, int bufferOffset, int length) => 0;
    public override string GetDataTypeName(int i) => _table!.Columns[i].DataType.Name;
    public override DateTime GetDateTime(int i) => Convert.ToDateTime(_table!.Rows[_rowIndex][i], CultureInfo.InvariantCulture);
    public override decimal GetDecimal(int i) => Convert.ToDecimal(_table!.Rows[_rowIndex][i], CultureInfo.InvariantCulture);
    public override double GetDouble(int i) => Convert.ToDouble(_table!.Rows[_rowIndex][i], CultureInfo.InvariantCulture);
    public override Type GetFieldType(int i) => _table!.Columns[i].DataType;
    public override float GetFloat(int i) => Convert.ToSingle(_table!.Rows[_rowIndex][i], CultureInfo.InvariantCulture);
    public override Guid GetGuid(int i) => Guid.Parse(_table!.Rows[_rowIndex][i].ToString()!);
    public override short GetInt16(int i) => Convert.ToInt16(_table!.Rows[_rowIndex][i], CultureInfo.InvariantCulture);
    public override int GetInt32(int i) => Convert.ToInt32(_table!.Rows[_rowIndex][i], CultureInfo.InvariantCulture);
    public override long GetInt64(int i) => Convert.ToInt64(_table!.Rows[_rowIndex][i], CultureInfo.InvariantCulture);
    public override string GetName(int i) => _table!.Columns[i].ColumnName;
    public override int GetOrdinal(string name) => _table!.Columns.IndexOf(name);
    public override string GetString(int i) => _table!.Rows[_rowIndex][i].ToString()!;
    public override object GetValue(int i) => _table!.Rows[_rowIndex][i];
    public override int GetValues(object[] values)
    {
        var row = _table!.Rows[_rowIndex];
        for (int i = 0; i < values.Length && i < row.ItemArray.Length; i++) values[i] = row[i];
        return Math.Min(values.Length, row.ItemArray.Length);
    }
    public override bool IsDBNull(int i) => _table!.Rows[_rowIndex][i] is DBNull;
    public override bool Read() { _rowIndex++; return _rowIndex < (_table?.Rows.Count ?? 0); }
    public override bool NextResult() => false;
    public override void Close() { }
    public override System.Collections.IEnumerator GetEnumerator() => throw new NotSupportedException();
}
