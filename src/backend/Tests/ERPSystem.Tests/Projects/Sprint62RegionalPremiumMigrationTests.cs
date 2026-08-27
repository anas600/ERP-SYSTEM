// Sprint 62 Wave 1A (DEC-197) — Tests for the RegionalPremium schema migration.
//
// The new table (regional_premiums) and the two new columns on progress_billings
// (regional_premium_deducted + net_amount_after_premium) are the foundation for
// automatic NDB + CIT + SS deductions on Libyan construction billings. These
// tests verify the migration class itself (no live DB) — same pattern as
// Sprint61EngineerReportSchemaMigrationTests.
//
// Coverage:
//   1. Migration class shape (attribute, Up/Down)
//   2. DEC-197 — regional_premiums table (columns + UNIQUE + index)
//   3. DEC-197 — progress_billings columns added (idempotent)
//   4. Idempotency (IF NOT EXISTS guards on tables + indexes + ADD COLUMN)
//   5. Architectural compliance (no tenant_id, company_id present + NOT NULL)

using System.Reflection;
using ERPSystem.Shared.Migrations;
using FluentAssertions;
using FluentMigrator;

namespace ERPSystem.Tests.Projects;

public class Sprint62RegionalPremiumMigrationTests
{
    private const string MigrationFileRelative =
        "src/backend/Shared/Migrations/Sprint62_RegionalPremium_20260827_160000.cs";
    private static readonly string RepoRoot = FindRepoRoot();

    // ============== 1. Class shape ==============

    [Fact]
    public void Migration_Class_Exists_With_Stable_Attribute()
    {
        var type = typeof(Sprint62_RegionalPremium);
        var attr = type.GetCustomAttribute<MigrationAttribute>();
        attr.Should().NotBeNull(
            "Sprint62_RegionalPremium must be decorated with [Migration(...)]");
        attr!.Version.Should().Be(20260827_160000L,
            "the migration version must follow the 14-digit YYYYMMDD_HHMMSS format (L046) and be unique/ascending (after Sprint 61's 130000)");
    }

    [Fact]
    public void Migration_Overrides_Up_And_Down()
    {
        var type = typeof(Sprint62_RegionalPremium);
        type.GetMethod("Up", BindingFlags.Public | BindingFlags.Instance)
            .Should().NotBeNull("Up() must exist so FluentMigrator can apply the changes");
        type.GetMethod("Down", BindingFlags.Public | BindingFlags.Instance)
            .Should().NotBeNull("Down() must exist so the migration can be rolled back");
    }

    // ============== 2. DEC-197 — regional_premiums ==============

    [Fact]
    public void Migration_Creates_RegionalPremiums_Table_With_All_Columns()
    {
        var upSection = ExtractUpSection(LoadMigrationFile());

        upSection.Should().Contain("CREATE TABLE IF NOT EXISTS regional_premiums",
            "regional_premiums must be created (DEC-197)");

        var expectedColumns = new[]
        {
            "id", "company_id", "project_id", "region",
            "ndb_percent", "cit_percent", "ss_percent",
            "is_active", "created_at"
        };
        foreach (var col in expectedColumns)
        {
            upSection.Should().Contain(col,
                $"regional_premiums must have the column '{col}' (DEC-197)");
        }
    }

    [Fact]
    public void RegionalPremiums_Table_Enforces_One_Row_Per_Project_Per_Region()
    {
        var sql = LoadMigrationFile();

        // The UNIQUE (project_id, region) constraint is part of DEC-197's design
        // to prevent duplicate region rows on the same project. Declared inline in
        // the CREATE TABLE block.
        sql.Should().Contain("UNIQUE (project_id, region)",
            "regional_premiums must enforce UNIQUE (project_id, region) per DEC-197 design");
    }

    // ============== 3. DEC-197 — progress_billings columns ==============

    [Fact]
    public void Migration_Adds_Regional_Premium_Columns_To_ProgressBillings()
    {
        var upSection = ExtractUpSection(LoadMigrationFile());

        upSection.Should().Contain("ADD COLUMN IF NOT EXISTS regional_premium_deducted",
            "progress_billings must add regional_premium_deducted column (DEC-197)");
        upSection.Should().Contain("ADD COLUMN IF NOT EXISTS net_amount_after_premium",
            "progress_billings must add net_amount_after_premium column (DEC-197)");
        upSection.Should().Contain("NUMERIC(18,4)",
            "the two new columns must use NUMERIC(18,4) to match the existing amount columns on progress_billings");
    }

    // ============== 4. Idempotency ==============

    [Fact]
    public void Migration_Is_Idempotent_Uses_IfNotExists()
    {
        // Scope the assertions to the Up() body so the doc-comment text (which
        // mentions "CREATE TABLE" / "CREATE INDEX" / "ADD COLUMN" in plain English)
        // does not skew the counts.
        var upSection = ExtractUpSection(LoadMigrationFile());

        // Every CREATE TABLE in Up() must be guarded with IF NOT EXISTS so re-running
        // the migration against an already-migrated DB is a no-op (per Constitution Article 8).
        var createCount = CountOccurrences(upSection, "CREATE TABLE");
        var ifNotExistsCount = CountOccurrences(upSection, "CREATE TABLE IF NOT EXISTS");
        ifNotExistsCount.Should().Be(createCount,
            "every CREATE TABLE in Up() must use IF NOT EXISTS for idempotency");
        createCount.Should().Be(1,
            "exactly 1 table is created in Up() — regional_premiums (DEC-197)");

        // Every CREATE INDEX in Up() must be guarded too.
        var indexCount = CountOccurrences(upSection, "CREATE INDEX");
        var indexIfNotExistsCount = CountOccurrences(upSection, "CREATE INDEX IF NOT EXISTS");
        indexIfNotExistsCount.Should().Be(indexCount,
            "every CREATE INDEX in Up() must use IF NOT EXISTS for idempotency");

        // The two ADD COLUMN statements must use IF NOT EXISTS so the migration is
        // safe to re-run on a DB that already has the columns.
        var addColumnCount = CountOccurrences(upSection, "ADD COLUMN");
        var addColumnIfNotExistsCount = CountOccurrences(upSection, "ADD COLUMN IF NOT EXISTS");
        addColumnIfNotExistsCount.Should().Be(addColumnCount,
            "every ADD COLUMN in Up() must use IF NOT EXISTS for idempotency");
        addColumnCount.Should().Be(2,
            "exactly 2 columns are added in Up() — regional_premium_deducted + net_amount_after_premium (DEC-197)");
    }

    // ============== 5. Architectural compliance ==============

    [Fact]
    public void Migration_File_Contains_No_Tenant_Id_Reference()
    {
        var sql = LoadMigrationFile();
        sql.Should().NotContain("ten" + "ant_" + "id",
            "Constitution Article 3 — company_id only, never " + "ten" + "ant_" + "id");
    }

    [Fact]
    public void RegionalPremiums_Table_Has_Company_Id_Not_Null()
    {
        // Per Constitution Article 3 + L19, every new business table must include
        // company_id as NOT NULL. The new regional_premiums table must have
        // company_id UUID NOT NULL.
        var upSection = ExtractUpSection(LoadMigrationFile());
        upSection.Should().Contain("company_id UUID NOT NULL REFERENCES companies(id)",
            "regional_premiums must declare company_id UUID NOT NULL REFERENCES companies(id) (Constitution Article 3, L19)");
    }

    // ============== Helpers ==============

    private static string LoadMigrationFile()
    {
        var path = Path.Combine(RepoRoot, MigrationFileRelative.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(path).Should().BeTrue($"migration file must exist at {path}");
        return File.ReadAllText(path);
    }

    private static int CountOccurrences(string haystack, string needle)
        => System.Text.RegularExpressions.Regex.Matches(
            haystack,
            System.Text.RegularExpressions.Regex.Escape(needle)).Count;

    private static string ExtractUpSection(string sql)
    {
        var idx = sql.IndexOf("public override void Up()", StringComparison.Ordinal);
        idx.Should().BeGreaterThan(0, "Up() method must exist");
        var downIdx = sql.IndexOf("public override void Down()", StringComparison.Ordinal);
        downIdx.Should().BeGreaterThan(idx, "Down() must come after Up()");
        return sql.Substring(idx, downIdx - idx);
    }

    private static string FindRepoRoot()
    {
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
