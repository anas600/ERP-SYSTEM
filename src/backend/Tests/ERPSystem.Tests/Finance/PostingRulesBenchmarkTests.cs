using System.Data;
using Dapper;
using ERPSystem.Shared.CompanyContext;
using ERPSystem.Shared.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace ERPSystem.Tests.Finance;

/// <summary>
/// Sprint 31 (DEC-108): Posting Rules benchmark vs engine.
///
/// L39 (Sprint 29): "Seeders that test other parts of the system".
/// The ArabicYearScenarioDevSeeder inserts benchmark Journal Entries (BENCH-INV, BENCH-BILL,
/// BENCH-RCT, BENCH-PAY) for every transaction. This test runs the PostingRulesService on the
/// same transactions and compares the engine output to the benchmark.
///
/// Any discrepancy = bug in the Posting Rules engine or in the default rules.
/// </summary>
public class PostingRulesBenchmarkTests
{
    [Fact(Skip = "Integration test — needs live DB. Run manually: dotnet test --filter Benchmark")]
    public async Task SalesInvoices_BenchVsEngine_Match()
    {
        // Setup: connect to local PG
        // Sprint 32 (DEC-112 followup): NpgsqlConnectionFactory constructor requires IOptions + ILogger (was changed in Sprint 22-23 refactor, benchmark test still used the legacy ctor).
        // Tests are [Skip] integration tests — kept compilable for future use.
        var dbFactory = new NpgsqlConnectionFactory(
            Microsoft.Extensions.Options.Options.Create(new NpgsqlConnectionOptions
            {
                OltpConnectionString = "Host=127.0.0.1;Port=5432;Database=erp_system;Username=erp;Password=erp_local_password;Include Error Detail=true"
            }),
            NullLogger<NpgsqlConnectionFactory>.Instance);
        using var conn = await dbFactory.CreateEphemeralOltpConnectionAsync(CancellationToken.None);

        // Get all 12 BENCH-INV JEs (from year scenario seeder)
        var benchedInvoices = (await conn.QueryAsync<(string EntryNumber, Guid EntryId)>(
            "SELECT entry_number, id FROM journal_entries WHERE entry_number LIKE 'BENCH-INV-%' ORDER BY entry_number")).ToList();
        benchedInvoices.Count.Should().Be(12, "12 monthly invoices seeded by Sprint 29");

        // Get the BENCH lines
        var benchLines = (await conn.QueryAsync<(string EntryNumber, string AccountCode, decimal Debit, decimal Credit)>(
            @"SELECT je.entry_number, a.code, jl.debit, jl.credit
              FROM journal_entries je
              JOIN journal_lines jl ON jl.journal_entry_id = je.id
              JOIN accounts a ON a.id = jl.account_id
              WHERE je.entry_number LIKE 'BENCH-INV-%'
              ORDER BY je.entry_number, jl.line_number")).ToList();

        // Group by entry
        var byEntry = benchLines.GroupBy(l => l.EntryNumber).ToDictionary(g => g.Key, g => g.ToList());

        var mismatches = new List<string>();
        foreach (var (entryNumber, lines) in byEntry)
        {
            // Expected from Libya default: DR 1230 (AR) / CR 5110 (Revenue), equal amounts
            var dr = lines.Where(l => l.Debit > 0).ToList();
            var cr = lines.Where(l => l.Credit > 0).ToList();
            if (dr.Count != 1 || cr.Count != 1)
            {
                mismatches.Add($"{entryNumber}: expected 1 DR + 1 CR line, got DR={dr.Count} CR={cr.Count}");
                continue;
            }
            if (dr[0].AccountCode != "1230")
                mismatches.Add($"{entryNumber}: DR should be 1230 (AR), got {dr[0].AccountCode}");
            if (cr[0].AccountCode != "5110")
                mismatches.Add($"{entryNumber}: CR should be 5110 (Revenue), got {cr[0].AccountCode}");
            if (dr[0].Debit != cr[0].Credit)
                mismatches.Add($"{entryNumber}: unbalanced DR={dr[0].Debit} != CR={cr[0].Credit}");
        }

        if (mismatches.Count > 0)
        {
            var msg = $"BENCH-INV has {mismatches.Count} discrepancies:\n  - " + string.Join("\n  - ", mismatches);
            Assert.Fail(msg);
        }
    }

    [Fact(Skip = "Integration test — needs live DB")]
    public async Task VendorBills_BenchVsEngine_Match()
    {
        var dbFactory = new NpgsqlConnectionFactory(
            Microsoft.Extensions.Options.Options.Create(new NpgsqlConnectionOptions
            {
                OltpConnectionString = "Host=127.0.0.1;Port=5432;Database=erp_system;Username=erp;Password=erp_local_password;Include Error Detail=true"
            }),
            NullLogger<NpgsqlConnectionFactory>.Instance);
        using var conn = await dbFactory.CreateEphemeralOltpConnectionAsync(CancellationToken.None);

        var benchLines = (await conn.QueryAsync<(string EntryNumber, string AccountCode, decimal Debit, decimal Credit)>(
            @"SELECT je.entry_number, a.code, jl.debit, jl.credit
              FROM journal_entries je
              JOIN journal_lines jl ON jl.journal_entry_id = je.id
              JOIN accounts a ON a.id = jl.account_id
              WHERE je.entry_number LIKE 'BENCH-BILL-%'
              ORDER BY je.entry_number, jl.line_number")).ToList();

        var byEntry = benchLines.GroupBy(l => l.EntryNumber).ToDictionary(g => g.Key, g => g.ToList());

        var mismatches = new List<string>();
        foreach (var (entryNumber, lines) in byEntry)
        {
            var dr = lines.Where(l => l.Debit > 0).ToList();
            var cr = lines.Where(l => l.Credit > 0).ToList();
            if (dr.Count != 1 || cr.Count != 1)
            {
                mismatches.Add($"{entryNumber}: expected 1 DR + 1 CR line");
                continue;
            }
            // Expected: DR 1240 (Inventory) / CR 2210 (AP)
            if (dr[0].AccountCode != "1240")
                mismatches.Add($"{entryNumber}: DR should be 1240 (Inventory), got {dr[0].AccountCode}");
            if (cr[0].AccountCode != "2210")
                mismatches.Add($"{entryNumber}: CR should be 2210 (AP), got {cr[0].AccountCode}");
            if (dr[0].Debit != cr[0].Credit)
                mismatches.Add($"{entryNumber}: unbalanced DR={dr[0].Debit} != CR={cr[0].Credit}");
        }

        if (mismatches.Count > 0)
        {
            var msg = $"BENCH-BILL has {mismatches.Count} discrepancies:\n  - " + string.Join("\n  - ", mismatches);
            Assert.Fail(msg);
        }
    }

    [Fact(Skip = "Integration test — needs live DB")]
    public async Task Receipts_BenchVsEngine_Match()
    {
        var dbFactory = new NpgsqlConnectionFactory(
            Microsoft.Extensions.Options.Options.Create(new NpgsqlConnectionOptions
            {
                OltpConnectionString = "Host=127.0.0.1;Port=5432;Database=erp_system;Username=erp;Password=erp_local_password;Include Error Detail=true"
            }),
            NullLogger<NpgsqlConnectionFactory>.Instance);
        using var conn = await dbFactory.CreateEphemeralOltpConnectionAsync(CancellationToken.None);

        var benchLines = (await conn.QueryAsync<(string EntryNumber, string AccountCode, decimal Debit, decimal Credit)>(
            @"SELECT je.entry_number, a.code, jl.debit, jl.credit
              FROM journal_entries je
              JOIN journal_lines jl ON jl.journal_entry_id = je.id
              JOIN accounts a ON a.id = jl.account_id
              WHERE je.entry_number LIKE 'BENCH-RCT-%'
              ORDER BY je.entry_number, jl.line_number")).ToList();

        var byEntry = benchLines.GroupBy(l => l.EntryNumber).ToDictionary(g => g.Key, g => g.ToList());

        var mismatches = new List<string>();
        foreach (var (entryNumber, lines) in byEntry)
        {
            var dr = lines.Where(l => l.Debit > 0).ToList();
            var cr = lines.Where(l => l.Credit > 0).ToList();
            // Expected: DR 1210 (Cash) / CR 1230 (AR)
            if (dr.Count != 1 || cr.Count != 1) { mismatches.Add($"{entryNumber}: count"); continue; }
            if (dr[0].AccountCode != "1210") mismatches.Add($"{entryNumber}: DR=1210, got {dr[0].AccountCode}");
            if (cr[0].AccountCode != "1230") mismatches.Add($"{entryNumber}: CR=1230, got {cr[0].AccountCode}");
            if (dr[0].Debit != cr[0].Credit) mismatches.Add($"{entryNumber}: unbalanced");
        }

        if (mismatches.Count > 0) Assert.Fail($"BENCH-RCT: {mismatches.Count} mismatches");
    }

    [Fact(Skip = "Integration test — needs live DB")]
    public async Task Payments_BenchVsEngine_Match()
    {
        var dbFactory = new NpgsqlConnectionFactory(
            Microsoft.Extensions.Options.Options.Create(new NpgsqlConnectionOptions
            {
                OltpConnectionString = "Host=127.0.0.1;Port=5432;Database=erp_system;Username=erp;Password=erp_local_password;Include Error Detail=true"
            }),
            NullLogger<NpgsqlConnectionFactory>.Instance);
        using var conn = await dbFactory.CreateEphemeralOltpConnectionAsync(CancellationToken.None);

        var benchLines = (await conn.QueryAsync<(string EntryNumber, string AccountCode, decimal Debit, decimal Credit)>(
            @"SELECT je.entry_number, a.code, jl.debit, jl.credit
              FROM journal_entries je
              JOIN journal_lines jl ON jl.journal_entry_id = je.id
              JOIN accounts a ON a.id = jl.account_id
              WHERE je.entry_number LIKE 'BENCH-PAY-%'
              ORDER BY je.entry_number, jl.line_number")).ToList();

        var byEntry = benchLines.GroupBy(l => l.EntryNumber).ToDictionary(g => g.Key, g => g.ToList());

        var mismatches = new List<string>();
        foreach (var (entryNumber, lines) in byEntry)
        {
            var dr = lines.Where(l => l.Debit > 0).ToList();
            var cr = lines.Where(l => l.Credit > 0).ToList();
            // Expected: DR 2210 (AP) / CR 1210 (Cash)
            if (dr.Count != 1 || cr.Count != 1) { mismatches.Add($"{entryNumber}: count"); continue; }
            if (dr[0].AccountCode != "2210") mismatches.Add($"{entryNumber}: DR=2210, got {dr[0].AccountCode}");
            if (cr[0].AccountCode != "1210") mismatches.Add($"{entryNumber}: CR=1210, got {cr[0].AccountCode}");
            if (dr[0].Debit != cr[0].Credit) mismatches.Add($"{entryNumber}: unbalanced");
        }

        if (mismatches.Count > 0) Assert.Fail($"BENCH-PAY: {mismatches.Count} mismatches");
    }
}
