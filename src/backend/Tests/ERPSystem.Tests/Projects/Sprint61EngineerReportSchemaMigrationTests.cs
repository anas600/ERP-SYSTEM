// Sprint 61 Wave 1A (DEC-192, DEC-193, DEC-194) — Tests for the EngineerReport
// schema migration.
//
// The 3 tables created by Sprint61_EngineerReportSchema_20260827_120000 are the
// foundation for the new Engineer's Daily Report module. These tests verify the
// migration class itself (no live DB) — same pattern as Sprint60AccountMetadata
// tests.
//
// Coverage:
//   1. Migration class shape (attribute, Up/Down)
//   2. DEC-192 — engineer_reports table (columns + UNIQUE + indexes)
//   3. DEC-193 — engineer_report_photos table (columns + CASCADE FK + index)
//   4. DEC-194 — engineer_report_signoffs table (columns + CASCADE FK + index)
//   5. Idempotency (IF NOT EXISTS guards on tables + indexes)
//   6. Architectural compliance (no tenant_id, company_id present, NOT NULL company_id)

using System.Reflection;
using ERPSystem.Shared.Migrations;
using FluentAssertions;
using FluentMigrator;

namespace ERPSystem.Tests.Projects;

public class Sprint61EngineerReportSchemaMigrationTests
{
    private const string MigrationFileRelative =
        "src/backend/Shared/Migrations/Sprint61_EngineerReportSchema_20260827_120000.cs";
    private static readonly string RepoRoot = FindRepoRoot();

    // ============== 1. Class shape ==============

    [Fact]
    public void Migration_Class_Exists_With_Stable_Attribute()
    {
        var type = typeof(Sprint61_EngineerReportSchema);
        var attr = type.GetCustomAttribute<MigrationAttribute>();
        attr.Should().NotBeNull(
            "Sprint61_EngineerReportSchema must be decorated with [Migration(...)]");
        attr!.Version.Should().Be(20260827_120000L,
            "the migration version must follow the 14-digit YYYYMMDD_HHMMSS format (L046) and be unique/ascending");
    }

    [Fact]
    public void Migration_Overrides_Up_And_Down()
    {
        var type = typeof(Sprint61_EngineerReportSchema);
        type.GetMethod("Up", BindingFlags.Public | BindingFlags.Instance)
            .Should().NotBeNull("Up() must exist so FluentMigrator can apply the changes");
        type.GetMethod("Down", BindingFlags.Public | BindingFlags.Instance)
            .Should().NotBeNull("Down() must exist so the migration can be rolled back");
    }

    // ============== 2. DEC-192 — engineer_reports ==============

    [Fact]
    public void Migration_Creates_EngineerReports_Table_With_All_Columns()
    {
        var upSection = ExtractUpSection(LoadMigrationFile());

        upSection.Should().Contain("CREATE TABLE IF NOT EXISTS engineer_reports",
            "engineer_reports must be created (DEC-192)");

        var expectedColumns = new[]
        {
            "id", "company_id", "project_id", "report_date", "engineer_id",
            "status", "weather", "work_done", "issues", "created_at", "updated_at"
        };
        foreach (var col in expectedColumns)
        {
            upSection.Should().Contain(col,
                $"engineer_reports must have the column '{col}' (DEC-192)");
        }
    }

    [Fact]
    public void EngineerReports_Table_Enforces_One_Report_Per_Project_Per_Day()
    {
        var sql = LoadMigrationFile();

        // The UNIQUE (project_id, report_date) constraint is part of DEC-192's design
        // to prevent an engineer from accidentally creating two reports for the same
        // day. It is declared inline in the CREATE TABLE block.
        sql.Should().Contain("UNIQUE (project_id, report_date)",
            "engineer_reports must enforce UNIQUE (project_id, report_date) per DEC-192 design");
    }

    // ============== 3. DEC-193 — engineer_report_photos ==============

    [Fact]
    public void Migration_Creates_EngineerReportPhotos_Table_With_Cascade_Delete()
    {
        var upSection = ExtractUpSection(LoadMigrationFile());

        upSection.Should().Contain("CREATE TABLE IF NOT EXISTS engineer_report_photos",
            "engineer_report_photos must be created (DEC-193)");

        // The FK to engineer_reports must use ON DELETE CASCADE so deleting a report
        // removes all of its photos in a single statement.
        upSection.Should().Contain("ON DELETE CASCADE",
            "engineer_report_photos.report_id must cascade-delete with the parent report (DEC-193)");

        var expectedColumns = new[]
        {
            "id", "report_id", "company_id", "file_path", "caption", "uploaded_at"
        };
        foreach (var col in expectedColumns)
        {
            upSection.Should().Contain(col,
                $"engineer_report_photos must have the column '{col}' (DEC-193)");
        }
    }

    // ============== 4. DEC-194 — engineer_report_signoffs ==============

    [Fact]
    public void Migration_Creates_EngineerReportSignoffs_Table_With_All_Columns()
    {
        var upSection = ExtractUpSection(LoadMigrationFile());

        upSection.Should().Contain("CREATE TABLE IF NOT EXISTS engineer_report_signoffs",
            "engineer_report_signoffs must be created (DEC-194)");

        // The FK to engineer_reports must use ON DELETE CASCADE so deleting a report
        // removes all of its signoffs in a single statement.
        upSection.Should().Contain("ON DELETE CASCADE",
            "engineer_report_signoffs.report_id must cascade-delete with the parent report (DEC-194)");

        var expectedColumns = new[]
        {
            "id", "report_id", "company_id", "signer_id", "signer_role",
            "signed_at", "signature_text", "comment", "approved"
        };
        foreach (var col in expectedColumns)
        {
            upSection.Should().Contain(col,
                $"engineer_report_signoffs must have the column '{col}' (DEC-194)");
        }
    }

    // ============== 5. Idempotency ==============

    [Fact]
    public void Migration_Is_Idempotent_Uses_IfNotExists()
    {
        // Scope the assertions to the Up() body so the doc-comment text (which
        // mentions "CREATE TABLE" / "CREATE INDEX" in plain English) does not skew
        // the counts.
        var upSection = ExtractUpSection(LoadMigrationFile());

        // Every CREATE TABLE in Up() must be guarded with IF NOT EXISTS so re-running
        // the migration against an already-migrated DB is a no-op (per Constitution Article 8).
        var createCount = CountOccurrences(upSection, "CREATE TABLE");
        var ifNotExistsCount = CountOccurrences(upSection, "CREATE TABLE IF NOT EXISTS");
        ifNotExistsCount.Should().Be(createCount,
            "every CREATE TABLE in Up() must use IF NOT EXISTS for idempotency");
        createCount.Should().Be(3,
            "exactly 3 tables are created in Up() — engineer_reports + 2 children (DEC-192..194)");

        // Every CREATE INDEX in Up() must be guarded too.
        var indexCount = CountOccurrences(upSection, "CREATE INDEX");
        var indexIfNotExistsCount = CountOccurrences(upSection, "CREATE INDEX IF NOT EXISTS");
        indexIfNotExistsCount.Should().Be(indexCount,
            "every CREATE INDEX in Up() must use IF NOT EXISTS for idempotency");
    }

    // ============== 6. Architectural compliance ==============

    [Fact]
    public void Migration_File_Contains_No_Tenant_Id_Reference()
    {
        var sql = LoadMigrationFile();
        sql.Should().NotContain("ten" + "ant_" + "id",
            "Constitution Article 3 — company_id only, never " + "ten" + "ant_" + "id");
    }

    [Fact]
    public void All_Three_Tables_Include_Company_Id_Not_Null()
    {
        // Per Constitution Article 3 + L19, every business table must include
        // company_id as NOT NULL. We assert that the file references company_id
        // with NOT NULL at least 3 times (once per table).
        var sql = LoadMigrationFile();
        var companyIdNotNullCount = CountOccurrences(sql, "company_id UUID NOT NULL");
        companyIdNotNullCount.Should().BeGreaterThanOrEqualTo(3,
            "all 3 new tables (engineer_reports, engineer_report_photos, engineer_report_signoffs) must declare company_id UUID NOT NULL (Constitution Article 3, L19)");
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
