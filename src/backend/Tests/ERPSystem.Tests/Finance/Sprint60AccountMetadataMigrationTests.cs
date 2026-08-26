// Sprint 60 Wave 1 (DEC-184) — Tests for the AddAccountFsMetadata migration.
//
// Per Anas's CoA-Final-Proposal-2026-08-24, the Wave 1 DB foundation adds 6
// Financial-Statement metadata columns to the `accounts` table. These tests
// verify the migration class itself (without a live DB) — the heavy lifting
// (apply + roll back) is exercised by the `MigrationRunnerHostedService`
// when the app starts; the unit tests assert that the migration is correctly
// authored: idempotent, has both Up/Down, references the right column names,
// and applies the right defaults to existing rows.
//
// Pattern follows HoldingSmokeTest (no DB, pure reflection + file-content
// checks). This is the cheapest test that catches "I renamed the column but
// forgot to update the SQL" mistakes.

using System.Reflection;
using ERPSystem.Shared.Migrations;
using FluentAssertions;
using FluentMigrator;

namespace ERPSystem.Tests.Finance;

public class Sprint60AccountMetadataMigrationTests
{
    private const string MigrationFileRelative = "src/backend/Shared/Migrations/Sprint60_AddAccountFsMetadata_20260825_001.cs";
    private static readonly string RepoRoot = FindRepoRoot();

    [Fact]
    public void Migration_Class_Exists_With_Stable_Attribute()
    {
        var type = typeof(Sprint60_AddAccountFsMetadata);
        var attr = type.GetCustomAttribute<MigrationAttribute>();
        attr.Should().NotBeNull("Sprint60_AddAccountFsMetadata must be decorated with [Migration(...)]");
        attr!.Version.Should().Be(20260825_120000L,
            "the migration version must be unique and ascending (after Sprint28_Audit 20260802_220000)");
    }

    [Fact]
    public void Migration_Overrides_Up_And_Down()
    {
        var type = typeof(Sprint60_AddAccountFsMetadata);
        type.GetMethod("Up", BindingFlags.Public | BindingFlags.Instance)
            .Should().NotBeNull("Up() must exist so FluentMigrator can apply the changes");
        type.GetMethod("Down", BindingFlags.Public | BindingFlags.Instance)
            .Should().NotBeNull("Down() must exist so the migration can be rolled back");
    }

    [Fact]
    public void Migration_File_References_All_6_New_Columns()
    {
        var sql = LoadMigrationFile();

        var expected = new[]
        {
            "fs_type",
            "section",
            "is_canonical",
            "new_code",
            "migration_status",
            "migrated_at"
        };

        foreach (var col in expected)
        {
            sql.Should().Contain(col,
                $"the migration must add the column '{col}' to accounts (per DEC-184)");
        }
    }

    [Fact]
    public void Migration_Is_Idempotent_Uses_IfNotExists()
    {
        var sql = LoadMigrationFile();

        // Every ALTER must be guarded with IF NOT EXISTS so re-running the migration
        // against an already-migrated DB is a no-op (per Constitution Article 8 idempotency).
        var alterCount = CountOccurrences(sql, "ALTER TABLE accounts ADD COLUMN");
        var ifNotExistsCount = CountOccurrences(sql, "ADD COLUMN IF NOT EXISTS");
        ifNotExistsCount.Should().Be(alterCount,
            "every ALTER TABLE accounts ADD COLUMN must use IF NOT EXISTS for idempotency");
        alterCount.Should().BeGreaterThanOrEqualTo(6, "all 6 DEC-184 columns must be added");
    }

    [Fact]
    public void Migration_Backfills_Existing_Rows_As_Legacy()
    {
        var sql = LoadMigrationFile();

        // The migration must mark pre-existing rows as legacy (is_canonical = FALSE,
        // migration_status = 'pending') so the future Wave 2 migration job knows what
        // to migrate. If a row was created with the canonical code from the start, it
        // would already have is_canonical = TRUE (column default) and migration_status
        // = 'new' — the UPDATE is a no-op for those.
        sql.Should().Contain("is_canonical = FALSE",
            "existing rows must be marked as not-yet-canonical");
        sql.Should().Contain("migration_status = COALESCE(migration_status, 'pending')",
            "existing rows must keep their migration_status (or default to 'pending')");
    }

    [Fact]
    public void Migration_Down_Drops_All_6_Columns()
    {
        var sql = LoadMigrationFile();

        var downSection = ExtractDownSection(sql);
        var expected = new[] { "fs_type", "section", "is_canonical", "new_code", "migration_status", "migrated_at" };
        foreach (var col in expected)
        {
            downSection.Should().Contain($"DROP COLUMN IF EXISTS {col}",
                $"Down() must drop the column '{col}' so the migration is reversible");
        }
    }

    [Fact]
    public void Migration_File_Contains_No_Tenant_Id_Reference()
    {
        var sql = LoadMigrationFile();
        sql.Should().NotContain("tenant_id",
            "Constitution Article 3 — company_id only, never tenant_id");
    }

    // ============ Helpers ============

    private static string LoadMigrationFile()
    {
        var path = Path.Combine(RepoRoot, MigrationFileRelative.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(path).Should().BeTrue($"migration file must exist at {path}");
        return File.ReadAllText(path);
    }

    private static int CountOccurrences(string haystack, string needle)
        => System.Text.RegularExpressions.Regex.Matches(haystack, System.Text.RegularExpressions.Regex.Escape(needle)).Count;

    private static string ExtractDownSection(string sql)
    {
        // Naive but sufficient: take everything after the first "Down()" until the end of class.
        var idx = sql.IndexOf("public override void Down()", StringComparison.Ordinal);
        idx.Should().BeGreaterThan(0, "Down() method must exist in the migration file");
        return sql.Substring(idx);
    }

    private static string FindRepoRoot()
    {
        // Walk up from the test binary until we find a folder that contains
        // both "src/backend" and "CHANGELOG.md" — that is the repo root.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "CHANGELOG.md")) &&
                Directory.Exists(Path.Combine(dir.FullName, "src", "backend")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Could not find repo root (no CHANGELOG.md + src/backend ancestor).");
    }
}
