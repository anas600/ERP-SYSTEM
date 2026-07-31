# 🚀 Sprint 8 T2: FakeDbConnectionFactory AS Alias Enhancement

**Date drafted:** 2026-07-31 04:10 UTC
**Architect:** Admin Team v1.8 (Mavis — محمد mode, Strategic Advisor)
**Implementer:** Mavis Local (Tech Lead) + 1 Local Jimi
**Owner:** Anas (Project Owner) — approved T2 = Option B at 04:08 UTC
**Duration target:** ~1.5 hours (small, well-bounded)
**Deliverable:** ONE PR (`feature/sprint-8-t2-fakedb-as-alias` → develop → main via 3-Layer Model)
**Predecessor:** Sprint 8 T1 (v1.8.3 governance) — committed at `76a5259`
**Goal:** Remove known technical debt in `FakeDbConnectionFactory` that forces tests to use projected column names as a workaround for SQL `AS` aliases.

---

## 🎯 Why this sprint

### Background

`src/backend/Tests/ERPSystem.Tests/Common/FakeDbConnectionFactory.cs` is an in-memory `IDbConnectionFactory` used by unit tests to simulate Dapper + DataSet without a real DB. It enables fast unit testing of the service layer.

### The Limitation

The `FakeDbDataReader` extracts the **table name** from SQL via regex on `FROM`/`JOIN`, then returns columns directly from the underlying `DataTable.Columns`. It does **not** parse the `SELECT` clause.

This means SQL like:
```sql
SELECT id AS "AccountId", code AS "AccountCode", name AS "AccountName" FROM accounts
```
...fails at Dapper deserialization because:
- The DataTable has columns `id, code, name`
- Dapper asks for `AccountId`, `AccountCode`, `AccountName`
- The reader returns `null` for those column names → deserialization fails

### The Workaround (current, fragile)

Tests in Sprint 7 T1 (lost in worktree reset, but the pattern persists in older tests) used **projected column names**:

```csharp
// Test code:
factory.AddRow("accounts", "AccountId", Guid.NewGuid(), "AccountCode", "1000", "AccountName", "Cash");
// But the SQL is:
"SELECT AccountId, AccountCode, AccountName FROM accounts"
// So projected column names (the same name in SELECT and DataTable) is required.
```

This is fragile:
- If a test author writes `SELECT id AS AccountId`, the test fails mysteriously
- The DataSet column type info is lost (column type becomes `object` not `Guid`)
- Adding new SELECT columns requires updating AddRow
- It's not "real SQL" — Dapper in production sees real `AS` aliases

### The Fix (T2 goal)

Make `FakeDbDataReader` parse the `SELECT` clause and project the underlying DataTable's columns into a new DataTable with the alias names. Tests can then write **real SQL**:

```sql
SELECT id AS "AccountId", code AS "AccountCode", name AS "AccountName" FROM accounts
```

...and `AddRow` uses the **base** column names:
```csharp
factory.AddRow("accounts", "id", Guid.NewGuid(), "code", "1000", "name", "Cash");
```

This:
- ✅ Aligns test SQL with production SQL
- ✅ Preserves type info (Guid stays Guid, not object)
- ✅ No more fragile "projected column names" convention
- ✅ Future tests (Sprint 9+) can use real `AS` aliases naturally

---

## 🏛️ Architectural Constraints (binding)

Per `docs/workflow/architecture.md` (10 soft rules) and `WORKFLOW.md` (Article 9 — 3-Layer Deploy Model):

1. **Article 3 — `company_id` Only**: tests must reference `company_id`, never `tenant_id`
2. **Article 8 Rule 6 — No EF Core**: code uses Dapper/FluentMigrator patterns
3. **Article 8 Rule 10 — Document in AGENTS.md**: update the **nearest** `AGENTS.md` (e.g., `src/backend/Tests/AGENTS.md` or `src/backend/Modules/Finance/AGENTS.md` if it exists)
4. **Rule 4 — One Test Per New Endpoint**: add 1+ unit test for the new AS alias feature
5. **0 source code regressions**: all 446 existing tests must still pass
6. **0 secrets in code**: no test passwords, tokens, etc.

---

## 📋 Tasks (T0–T3)

### T0 — Inventory

- Read `src/backend/Tests/ERPSystem.Tests/Common/FakeDbConnectionFactory.cs` ✅ (done by محمد)
- Read `src/backend/Tests/ERPSystem.Tests/Common/` for any other test helpers
- Identify all tests using `FakeDbConnectionFactory` (12+ files per grep)
- Note: **no need to migrate existing tests** (they continue to work; AS alias is additive)

### T1 — Local Jimi (1 JimI max, ~1.5h, per R7)

**Scope: SINGLE FILE MODIFICATION + 1 NEW TEST FILE**

**File 1: `src/backend/Tests/ERPSystem.Tests/Common/FakeDbConnectionFactory.cs` (modify)**

Add a `ProjectColumns(string sql, DataTable source)` helper to `FakeDbDataReader` (or extract to a shared helper class). Behavior:

```csharp
/// <summary>
/// Parse SELECT clause + project DataTable columns to alias names.
/// Returns a NEW DataTable with renamed columns (does NOT mutate the source).
/// If SELECT has no AS aliases, returns the source table unchanged.
/// </summary>
internal static DataTable? ProjectColumns(string sql, DataSet ds, string tableName)
{
    // 1. Find "SELECT <col-list> FROM <tableName>" — case-insensitive
    var match = Regex.Match(
        sql,
        @"\bSELECT\s+(?<cols>.+?)\s+FROM\s+([a-zA-Z_][a-zA-Z0-9_]*)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline);
    if (!match.Success) return null;
    if (!ds.Tables.Contains(tableName)) return null;
    var source = ds.Tables[tableName]!;
    var columnList = match.Groups["cols"].Value;

    // 2. Parse comma-separated columns: "id AS \"AccountId\", code, name AS \"AccountName\""
    var projected = source.Clone();  // copy schema (empty rows)
    var aliasToSource = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    foreach (var raw in SplitColumns(columnList))
    {
        var col = raw.Trim();
        string sourceName, aliasName;
        var asMatch = Regex.Match(col, @"^(?<src>.+?)\s+AS\s+(?<alias>.+)$", RegexOptions.IgnoreCase);
        if (asMatch.Success)
        {
            sourceName = Unquote(asMatch.Groups["src"].Value.Trim());
            aliasName = Unquote(asMatch.Groups["alias"].Value.Trim());
        }
        else
        {
            sourceName = Unquote(col);
            aliasName = sourceName;
        }
        // If alias is new, add column (preserve type from source if exists)
        if (!projected.Columns.Contains(aliasName))
        {
            if (source.Columns.Contains(sourceName))
            {
                projected.Columns.Add(aliasName, source.Columns[sourceName]!.DataType);
            }
            else
            {
                projected.Columns.Add(aliasName, typeof(object));
            }
        }
        aliasToSource[aliasName] = sourceName;
    }

    // 3. Copy rows with projection
    foreach (DataRow srcRow in source.Rows)
    {
        var newRow = projected.NewRow();
        foreach (DataColumn col in projected.Columns)
        {
            var srcCol = aliasToSource[col.ColumnName];
            newRow[col.ColumnName] = srcRow[srcCol];
        }
        projected.Rows.Add(newRow);
    }

    return projected;
}

private static IEnumerable<string> SplitColumns(string columnList)
{
    // Simple split on top-level commas (ignore commas inside parens/quotes).
    // Implementation: regex-based or hand-rolled state machine.
    // For MVP: use Regex.Split with negative-lookbehind for quotes/parens.
    // Examples that must work:
    //   "id, code, name" → ["id", "code", "name"]
    //   "id AS \"AccountId\", code AS \"AccountCode\"" → ["id AS \"AccountId\"", "code AS \"AccountCode\""]
    //   "COUNT(*) AS total, MAX(id) AS last_id" → ["COUNT(*) AS total", "MAX(id) AS last_id"]
    //   "id, name || ' (' || code || ')' AS display" → ["id", "name || ' (' || code || ')' AS display"]
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

private static string Unquote(string s) =>
    s.Length >= 2 && s[0] == '"' && s[^1] == '"' ? s.Substring(1, s.Length - 2) : s;
```

Then in `FakeDbDataReader` constructor:
```csharp
public FakeDbDataReader(DataSet ds, string sql)
{
    var tableName = ExtractTableName(sql);
    if (!ds.Tables.Contains(tableName))
    {
        _table = null;
        return;
    }
    // Try projection first; fall back to direct table if SELECT has no AS
    _table = ProjectColumns(sql, ds, tableName) ?? ds.Tables[tableName]!;
}
```

**Note:** `ExecuteScalar` (for COUNT) does NOT need this change — it only matches `COUNT(*) FROM <table>` and returns the row count, not affected by aliases.

**File 2: `src/backend/Tests/ERPSystem.Tests/Common/FakeDbConnectionFactoryTests.cs` (NEW)**

Add a new test file demonstrating AS alias works:

```csharp
using ERPSystem.Tests.Common;
using Xunit;

namespace ERPSystem.Tests.Common;

/// <summary>
/// Tests for FakeDbConnectionFactory AS alias support.
/// Per Sprint 8 T2: enables tests to write real SQL with AS aliases
/// instead of using projected column names workaround.
/// </summary>
public class FakeDbConnectionFactoryTests
{
    [Fact]
    public void AsAlias_RenamesColumnsInReader()
    {
        // Arrange
        var factory = new FakeDbConnectionFactory();
        var accountId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        factory.EnsureTable("accounts");
        factory.AddRow("accounts",
            "id", accountId,
            "company_id", companyId,
            "code", "1000",
            "name", "Cash");

        // Act
        var result = new List<Dictionary<string, object?>>();
        using (var conn = factory.CreateOltpConnectionAsync().GetAwaiter().GetResult())
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT id AS \"AccountId\", code AS \"AccountCode\", name AS \"AccountName\" FROM accounts";
            using var reader = cmd.ExecuteReader();
            Assert.Equal(3, reader.FieldCount);
            Assert.Equal("AccountId", reader.GetName(0));
            Assert.Equal("AccountCode", reader.GetName(1));
            Assert.Equal("AccountName", reader.GetName(2));
            Assert.True(reader.Read());
            Assert.Equal(accountId, reader.GetGuid(0));
            Assert.Equal("1000", reader.GetString(1));
            Assert.Equal("Cash", reader.GetString(2));
        }
    }

    [Fact]
    public void NoAsAlias_FallsBackToDirectColumns()
    {
        // Existing test pattern (projected column names) must still work.
        var factory = new FakeDbConnectionFactory();
        var id = Guid.NewGuid();
        factory.AddRow("items", "id", id, "name", "Widget");

        using var conn = factory.CreateOltpConnectionAsync().GetAwaiter().GetResult();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, name FROM items";  // no AS
        using var reader = cmd.ExecuteReader();
        Assert.Equal(2, reader.FieldCount);
        Assert.Equal("id", reader.GetName(0));
        Assert.Equal("name", reader.GetName(1));
    }

    [Fact]
    public void AsAlias_HandlesMultipleColumnsIncludingExpression()
    {
        // Concatenation expression with AS
        var factory = new FakeDbConnectionFactory();
        factory.AddRow("items", "id", 1, "code", "A1", "name", "Widget");
        using var conn = factory.CreateOltpConnectionAsync().GetAwaiter().GetResult();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, code, name, (code || '-' || name) AS \"DisplayName\" FROM items";
        using var reader = cmd.ExecuteReader();
        Assert.Equal(4, reader.FieldCount);
        Assert.Equal("DisplayName", reader.GetName(3));
        // For the expression, the projected column will have null value
        // (we don't simulate the expression), but the column must exist
        Assert.True(reader.Read());
    }
}
```

**File 3: `src/backend/Modules/Finance/AGENTS.md` (UPDATE — if exists; else create)**

Add a new section documenting the test pattern:

```markdown
## Test Pattern: SQL AS Alias Support (added 2026-07-31, Sprint 8 T2)

When writing tests that use `FakeDbConnectionFactory`, you can now use real SQL with `AS` aliases:

\`\`\`csharp
// SQL (production-style):
"SELECT id AS \"AccountId\", code AS \"AccountCode\" FROM accounts"

// AddRow uses BASE column names (not aliases):
factory.AddRow("accounts",
    "id", Guid.NewGuid(),
    "code", "1000");
\`\`\`

The FakeDbDataReader projects the underlying DataTable's columns to the alias names.
This aligns test SQL with production SQL (no more "projected column names" workaround).

Edge cases supported:
- Mixed aliased + non-aliased columns
- Quoted identifiers (`AS "AccountId"`)
- Expression aliases (the column exists but value is `object` — not simulated)
- Multiple aliases per SELECT
- Aliases on aggregates (`COUNT(*) AS total` — not used in our tests, but parsed correctly)
```

**File 4: `CHANGELOG.md` (UPDATE — Mavis Local adds at PR open time)**

Add an entry under Sprint 8:
```
## Sprint 8 T2 — AS Alias Enhancement (2026-07-31)

### Added
- `FakeDbConnectionFactory` — `FakeDbDataReader.ProjectColumns` helper (parses SELECT clause + projects DataTable)
- `FakeDbConnectionFactoryTests` — 3 unit tests (AS alias works, no-aliased fallback, expression alias)
- `src/backend/Modules/Finance/AGENTS.md` — Test pattern documented

### Notes
- T2 = Option B (per Muhammad's recommendation, approved by Anas 04:08 UTC)
- Removes known technical debt (T1 tests needed projected column names workaround)
- Existing tests unaffected (additive change)
- Sprint 9+ tests can use real AS aliases naturally
```

---

### T2 — Verify (Mavis Local)

```bash
# In worktree
dotnet build
dotnet test --filter "FullyQualifiedName~FakeDbConnectionFactoryTests"  # new tests
dotnet test  # full suite — must stay 446 passed / 0 failed
grep -r "tenant_id" src/   # 0 matches
grep -r "password\s*=" src/ # 0 secrets
```

**DoD:**
- [ ] 0 build errors
- [ ] 3 new tests pass (FakeDbConnectionFactoryTests)
- [ ] 0 regressions in existing 446 tests
- [ ] 0 `tenant_id` references introduced
- [ ] 0 secrets introduced
- [ ] AGENTS.md updated with new test pattern
- [ ] CHANGELOG.md updated

### T3 — Open PR (Mavis Local, per Template 1 v2 + 3-Layer Model)

- Branch: `feature/sprint-8-t2-fakedb-as-alias` (off develop)
- PR title: `test(be): Sprint 8 T2 — FakeDbConnectionFactory AS alias enhancement`
- PR body: standard format (Context + Files + T2 verify + DoD)
- **Target:** develop (Layer 1 — ci-fast.yml will run, fast feedback)
- **After merge to develop:** develop-pr-monitor cron (when sync resumed) will deploy to main (Layer 3 — ci-deploy-prod.yml, manual approval)
- **After opening PR:** ping Admin Team via `mavis cron once` with `session.mode=sessionId` and `session.session_id=mvs_a1a821a951504cce80ee1fddb98053be`
- **DO NOT** self-merge (per Template 1 v2)
- **DO NOT** update state.json (per v1.8.3 — develop-pr-monitor + coordinator-watchdog handle)

---

## 📊 Success Metrics

| Metric | Target | How to Measure |
|--------|--------|----------------|
| **New tests added** | 3 (FakeDbConnectionFactoryTests) | `dotnet test --filter "FakeDbConnectionFactoryTests"` |
| **Test failures** | 0 | `dotnet test` (full suite) |
| **Regressions** | 0 | Existing 446 tests still pass |
| **Build errors** | 0 | `dotnet build` |
| **Architecture clean** | 0 tenant_id, 0 secrets | grep checks |
| **Cycle duration** | ≤ 2.5h | PR open within 2.5h of T0 |

---

## 🚨 Risks

| Risk | Mitigation |
|------|------------|
| **Regex parsing fragile** — complex SQL may break ProjectColumns | Use state-machine `SplitColumns` (depth tracking); unit test with multiple SQL variations |
| **Type coercion** — projected column type from source, but `string \|\| string` returns object | Add column with `typeof(object)` for expressions; tests that need expression result should not rely on FakeDb |
| **Backward compat** — existing tests using "projected column names" | The new code falls back to direct columns if no AS in SELECT. Existing tests unaffected. |
| **PR too large** — Mavis Local might over-engineer | Stick to T2 scope: 1 file modified, 1 new test file, 1 AGENTS.md update. No other changes. |

---

## 🏃 Coordination Protocol (per v1.8.2 + 3-Layer Model)

### Mavis Local's role
- T0: inventory (read FakeDbConnectionFactory.cs + identify test files using it) — done by Admin
- T1: spawn 1 Local Jimi (≤ 1.5h per R7)
- T2: verify (build + test + grep)
- T3: open PR → develop (Layer 1)

### Admin Team's role
- Hand-off provided (this document)
- After PR opens: develop-pr-monitor cron detects → merges to develop (Layer 1 fast)
- When develop → main: ci-deploy-prod.yml (Layer 3, manual approval) → prod

### Communication
- **Primary:** state.json (ball transitions)
- **Secondary:** PR comments
- **Tool for hand-off back:** `mavis cron once` with `session.mode=sessionId` + `session.session_id=mvs_a1a821a951504cce80ee1fddb98053be`

---

## 📌 Out of Scope (defer to Sprint 9+)

- Migrate existing tests from projected column names to AS alias (NOT REQUIRED — they still work)
- Refactor `ExecuteScalar` to support aliased aggregates (already supports `::int` cast)
- Add SQL syntax support for `JOIN` projections (only FROM supported for now)
- Performance optimization (the regex is fine for unit test scale)

---

*Hand-off drafted: 2026-07-31 04:10 UTC by Admin Team v1.8 (Mavis — محمد mode, Strategic Advisor).*
*T2 = Option B per Anas approval 04:08 UTC. Reference: Sprint 7 T1 lost tests used projected column names workaround; T2 removes root cause. Per 3-Layer Deploy Model (WORKFLOW.md Article 9, v1.8.3 governance).*
