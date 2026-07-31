// Sprint 8 T2 — Tests for FakeDbConnectionFactory AS alias support.
//
// Goal: prove that FakeDbDataReader now parses the SELECT clause and projects
// the underlying DataTable's columns to the alias names. This removes the
// "projected column names" workaround that forced tests to use the SAME
// column name in both AddRow and SELECT (instead of real SQL with AS aliases).
//
// Test approach: each test sets up an in-memory DataSet via AddRow using the
// BASE column names (e.g. `id`, `code`, `name`), then runs a SELECT with real
// AS aliases and asserts that the reader exposes the alias names + values.
//
// Per .mavis/AGENTS.md Rule 4: one test per scenario. Three scenarios:
//   1. Basic AS alias rename (3 columns, all aliased).
//   2. No AS alias — falls back to the original column names (backward compat).
//   3. AS alias on an expression (the column must exist, value is DBNull).
//
// Backward compatibility: existing tests using "SELECT id, name FROM items"
// (no AS) continue to work — the new ProjectColumns helper returns null in
// that case and the reader falls back to the direct DataTable.

using ERPSystem.Tests.Common;
using Xunit;

namespace ERPSystem.Tests.Common;

public class FakeDbConnectionFactoryTests
{
    /// <summary>
    /// Sprint 8 T2: real SQL with AS aliases now works in FakeDbDataReader.
    /// AddRow uses base column names; the reader projects to alias names.
    /// This is the core fix that removes the "projected column names" workaround.
    /// </summary>
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

        // Act + Assert
        using var conn = factory.CreateOltpConnectionAsync().GetAwaiter().GetResult();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id AS \"AccountId\", code AS \"AccountCode\", name AS \"AccountName\" FROM accounts";
        using var reader = cmd.ExecuteReader();

        Assert.Equal(3, reader.FieldCount);
        Assert.Equal("AccountId", reader.GetName(0));
        Assert.Equal("AccountCode", reader.GetName(1));
        Assert.Equal("AccountName", reader.GetName(2));
        Assert.True(reader.Read(), "the AddRow inserted one row that must be readable");
        Assert.Equal(accountId, reader.GetGuid(0));
        Assert.Equal("1000", reader.GetString(1));
        Assert.Equal("Cash", reader.GetString(2));
    }

    /// <summary>
    /// Backward compatibility: tests using "SELECT id, name FROM items" (no AS)
    /// must still work. The reader falls back to the direct DataTable columns
    /// when ProjectColumns finds no AS clauses.
    /// </summary>
    [Fact]
    public void NoAsAlias_FallsBackToDirectColumns()
    {
        // Arrange
        var factory = new FakeDbConnectionFactory();
        var id = Guid.NewGuid();
        factory.AddRow("items", "id", id, "name", "Widget");

        // Act + Assert
        using var conn = factory.CreateOltpConnectionAsync().GetAwaiter().GetResult();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, name FROM items"; // no AS
        using var reader = cmd.ExecuteReader();

        Assert.Equal(2, reader.FieldCount);
        Assert.Equal("id", reader.GetName(0));
        Assert.Equal("name", reader.GetName(1));
        Assert.True(reader.Read());
        Assert.Equal(id, reader.GetGuid(0));
        Assert.Equal("Widget", reader.GetString(1));
    }

    /// <summary>
    /// Expression alias: `(code || '-' || name) AS "DisplayName"` — the column
    /// must exist with the alias name, but the value is DBNull because we don't
    /// simulate the concatenation. This is the same behavior as production
    /// Dapper when given an aliased expression it cannot evaluate.
    /// </summary>
    [Fact]
    public void AsAlias_HandlesMultipleColumnsIncludingExpression()
    {
        // Arrange
        var factory = new FakeDbConnectionFactory();
        factory.AddRow("items", "id", 1, "code", "A1", "name", "Widget");

        // Act + Assert
        using var conn = factory.CreateOltpConnectionAsync().GetAwaiter().GetResult();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, code, name, (code || '-' || name) AS \"DisplayName\" FROM items";
        using var reader = cmd.ExecuteReader();

        Assert.Equal(4, reader.FieldCount);
        Assert.Equal("id", reader.GetName(0));
        Assert.Equal("code", reader.GetName(1));
        Assert.Equal("name", reader.GetName(2));
        Assert.Equal("DisplayName", reader.GetName(3));
        Assert.True(reader.Read());
        Assert.Equal(1, Convert.ToInt32(reader.GetValue(0)));
        Assert.Equal("A1", reader.GetString(1));
        Assert.Equal("Widget", reader.GetString(2));
        // The expression column exists with the alias name, but its value
        // is DBNull because FakeDb does not simulate the SQL expression.
        Assert.True(reader.IsDBNull(3), "expression alias value is DBNull (FakeDb does not simulate expressions)");
    }
}
