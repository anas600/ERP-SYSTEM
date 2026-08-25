// Sprint 60 Wave 3A (DEC-189 + DEC-190) — Chart-of-Accounts Validation Service.
//
// Wraps the data-integrity checks that the Sprint 60 Wave 3A migration
// (`Sprint60_BalanceMigrationValidation_20260825_004.cs`) performs as raw
// SQL + RAISE NOTICE into a typed, code-callable API. Lets the FE / ops
// dashboard run the same checks on demand without re-running the migration.
//
// Checks (in evaluation order):
//   1. Duplicate (company_id, code) UNIQUE violations on accounts
//   2. Orphan journal_lines (account_id references a missing account)
//   3. Trial balance mismatch (Σ debit ≠ Σ credit per company on posted lines)
//   4. Invalid account code format (not canonical, not a recognized legacy shape)
//   5. Legacy accounts (is_canonical = FALSE) — WARNING, not an error
//
// Article 3 — company_id only. No tenant_id anywhere.
// Returns a `CoAValidationResult` whose `IsValid` is false iff any ERROR
// severity issue is found. WARNINGs (e.g. legacy accounts) do not flip
// `IsValid` to false — they are surfaced for ops review.
//
// Pattern matches existing services in the Finance module:
// `IDbConnectionFactory` for Dapper access, `ICompanyContext` for context,
// no EF Core.
//
// Testability note: we pull all rows for the company into C# memory and
// aggregate there, rather than relying on SQL `COUNT(*)`, `GROUP BY`, or
// `SUM()`. This trades a small bit of efficiency for clean, deterministic
// in-memory testing via `FakeDbConnectionFactory` (which cannot simulate
// those SQL aggregations). Production-scale data is small (a Holding
// company has < 1000 accounts), so the in-memory pass is fine.

using System.Text.RegularExpressions;
using Dapper;
using ERPSystem.Shared.CompanyContext;
using ERPSystem.Shared.Infrastructure;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Modules.Finance.Application.Services;

/// <summary>
/// Severity of a single validation issue.
/// </summary>
public static class ValidationSeverity
{
    public const string Error   = "Error";
    public const string Warning = "Warning";
    public const string Info    = "Info";
}

/// <summary>
/// Stable error code for a validation issue. Use this for programmatic
/// branching (e.g. FE shows a yellow banner for `LEGACY_ACCOUNT` but a red
/// modal for `TRIAL_BALANCE_MISMATCH`).
/// </summary>
public static class ValidationCode
{
    public const string DuplicateCode         = "DUPLICATE_CODE";
    public const string OrphanJournalLine     = "ORPHAN_JOURNAL_LINE";
    public const string TrialBalanceMismatch  = "TRIAL_BALANCE_MISMATCH";
    public const string InvalidCodeFormat     = "INVALID_CODE_FORMAT";
    public const string LegacyAccount         = "LEGACY_ACCOUNT";
}

/// <summary>
/// A single issue found by <see cref="ICoAValidationService.ValidateAsync"/>.
/// </summary>
public sealed class ValidationIssue
{
    /// <summary>One of <see cref="ValidationSeverity"/>.</summary>
    public string Severity { get; init; } = ValidationSeverity.Info;

    /// <summary>One of <see cref="ValidationCode"/>. Stable for programmatic branching.</summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>Human-readable English message. Frontend can localize if needed.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Affected account id (if applicable). Null for whole-company issues (e.g. trial balance).</summary>
    public Guid? AccountId { get; init; }

    /// <summary>Affected account code (if applicable).</summary>
    public string? AccountCode { get; init; }
}

/// <summary>
/// Result of <see cref="ICoAValidationService.ValidateAsync"/>.
/// </summary>
public sealed class CoAValidationResult
{
    /// <summary>True iff no <see cref="ValidationSeverity.Error"/> issues were found.</summary>
    public bool IsValid => !Issues.Any(i => i.Severity == ValidationSeverity.Error);

    /// <summary>All issues (errors + warnings + info), in evaluation order.</summary>
    public List<ValidationIssue> Issues { get; init; } = new();

    public int ErrorCount   => Issues.Count(i => i.Severity == ValidationSeverity.Error);
    public int WarningCount => Issues.Count(i => i.Severity == ValidationSeverity.Warning);
    public int InfoCount    => Issues.Count(i => i.Severity == ValidationSeverity.Info);
}

public interface ICoAValidationService
{
    /// <summary>
    /// Run all CoA validation checks for the given company.
    /// </summary>
    /// <param name="companyId">The company to validate. All queries are scoped to this id.</param>
    /// <param name="ctx">Company context (used for logging / future per-user scopes).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<CoAValidationResult> ValidateAsync(Guid companyId, ICompanyContext ctx, CancellationToken ct);
}

public sealed class CoAValidationService : ICoAValidationService
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<CoAValidationService> _logger;

    // Canonical 4-level: e.g. "1.1.01" (L3) or "1.1.01.001" (L4) — at least 2 dot-separated parts.
    private static readonly Regex CanonicalCodePattern =
        new(@"^\d+(\.\d+){2,3}$", RegexOptions.Compiled);

    // Recognized legacy shapes: 1101, 1101-001, 9201, 9201-001, 71, 72, 7101, 7102, 7201, etc.
    // Pattern: 2-4 digit root, optional "-NNN" 3-digit suffix.
    private static readonly Regex LegacyCodePattern =
        new(@"^\d{2,4}(-\d{3})?$", RegexOptions.Compiled);

    public CoAValidationService(IDbConnectionFactory db, ILogger<CoAValidationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<CoAValidationResult> ValidateAsync(Guid companyId, ICompanyContext ctx, CancellationToken ct)
    {
        _logger.LogInformation("CoA validation starting for company {CompanyId}", companyId);

        var issues = new List<ValidationIssue>();

        using var conn = await _db.CreateOltpConnectionAsync(ct);

        // ------------------------------------------------------------------
        // Pre-fetch: pull all accounts + all (posted) journal_lines into memory
        // once, then run all checks in C#. This is the same pattern the
        // trial-balance / general-ledger services use for the Holding
        // dashboard — read the whole company in one pass, aggregate in
        // C#. See service-level docstring for the testability rationale.
        // ------------------------------------------------------------------
        var allAccounts = (await conn.QueryAsync<AccountRow>(new CommandDefinition(@"
            SELECT id AS Id, code AS Code
            FROM accounts
            WHERE company_id = @CompanyId",
            new { CompanyId = companyId }, cancellationToken: ct))).ToList();

        var allLines = (await conn.QueryAsync<JournalLineRow>(new CommandDefinition(@"
            SELECT jl.id AS Id, jl.account_id AS AccountId, jl.debit AS Debit, jl.credit AS Credit,
                   je.status AS EntryStatus
            FROM journal_lines jl
            INNER JOIN journal_entries je
                ON je.id = jl.journal_entry_id
                AND je.company_id = jl.company_id
            WHERE jl.company_id = @CompanyId
              AND je.status = 2",
            new { CompanyId = companyId }, cancellationToken: ct))).ToList();

        // ------------------------------------------------------------------
        // Check 1: Duplicate (company_id, code) UNIQUE violations
        // ------------------------------------------------------------------
        var duplicates = allAccounts
            .GroupBy(a => a.Code)
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var dup in duplicates)
        {
            // The error references the code (not a specific account) because
            // duplicates are a table-level constraint violation. We pick the
            // first account id as a representative for the optional AccountId field.
            var firstId = dup.First().Id;
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Code = ValidationCode.DuplicateCode,
                Message = $"Account code '{dup.Key}' appears {dup.Count()} times for this company (UNIQUE violation).",
                AccountId = firstId,
                AccountCode = dup.Key
            });
        }

        // ------------------------------------------------------------------
        // Check 2: Orphan journal_lines (account_id references a missing account)
        // The FK on journal_lines.account_id should prevent this, but we
        // surface any defensive findings.
        // ------------------------------------------------------------------
        var knownAccountIds = allAccounts.Select(a => a.Id).ToHashSet();
        var orphans = allLines
            .Where(l => !knownAccountIds.Contains(l.AccountId))
            .GroupBy(l => l.AccountId)
            .ToList();

        foreach (var orphan in orphans)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Code = ValidationCode.OrphanJournalLine,
                Message = $"Found {orphan.Count()} journal_line(s) referencing missing account {orphan.Key}.",
                AccountId = orphan.Key
            });
        }

        // ------------------------------------------------------------------
        // Check 3: Trial balance mismatch (Σ debit ≠ Σ credit per company)
        // allLines is already filtered to Posted only (status=2), so the sum
        // is exactly the per-company posted balance.
        // ------------------------------------------------------------------
        var totalDebit = allLines.Sum(l => l.Debit);
        var totalCredit = allLines.Sum(l => l.Credit);
        if (totalDebit != totalCredit)
        {
            var variance = totalDebit - totalCredit;
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Code = ValidationCode.TrialBalanceMismatch,
                Message = $"Trial balance mismatch: Dr={totalDebit:N2}, Cr={totalCredit:N2}, variance={variance:N2}."
            });
        }

        // ------------------------------------------------------------------
        // Check 4: Invalid account code format
        // An account is invalid if its code matches neither the canonical
        // 4-level dot pattern nor a recognized legacy 4-digit pattern.
        // ------------------------------------------------------------------
        foreach (var account in allAccounts)
        {
            if (!IsValidCodeFormat(account.Code))
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Error,
                    Code = ValidationCode.InvalidCodeFormat,
                    Message = $"Account '{account.Code}' has an invalid code format (not canonical, not legacy).",
                    AccountId = account.Id,
                    AccountCode = account.Code
                });
            }
        }

        // ------------------------------------------------------------------
        // Check 5: Legacy accounts (is_canonical = FALSE) — WARNING only.
        // We need fs_type + is_canonical + migration_status for this check.
        // Pull only the columns we need; the second read is cheap and keeps
        // the previous in-memory passes lean.
        // ------------------------------------------------------------------
        var legacyCandidates = (await conn.QueryAsync<(Guid Id, bool IsCanonical, string MigrationStatus)>(new CommandDefinition(@"
            SELECT id AS Id, is_canonical AS IsCanonical, migration_status AS MigrationStatus
            FROM accounts
            WHERE company_id = @CompanyId",
            new { CompanyId = companyId }, cancellationToken: ct))).ToList();

        var legacyCount = legacyCandidates.Count(a => !a.IsCanonical && a.MigrationStatus == "pending");

        if (legacyCount > 0)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Code = ValidationCode.LegacyAccount,
                Message = $"{legacyCount} legacy account(s) still using the old 4-digit code (is_canonical=FALSE, migration_status='pending'). These are not errors — Wave 2B intentionally left them on the legacy code pending a future migration wave."
            });
        }

        var result = new CoAValidationResult { Issues = issues };
        _logger.LogInformation(
            "CoA validation completed for company {CompanyId}: {Errors} errors, {Warnings} warnings",
            companyId, result.ErrorCount, result.WarningCount);

        return result;
    }

    /// <summary>
    /// True iff the code matches either the canonical 4-level dot pattern
    /// (e.g. <c>1.1.01</c>, <c>1.1.01.001</c>) or a recognized legacy shape
    /// (e.g. <c>1101</c>, <c>1101-001</c>, <c>9201</c>, <c>71</c>, <c>7101</c>).
    /// </summary>
    private static bool IsValidCodeFormat(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        return CanonicalCodePattern.IsMatch(code) || LegacyCodePattern.IsMatch(code);
    }

    // ---- Row DTOs for the in-memory pass ----
    private sealed class AccountRow
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
    }

    private sealed class JournalLineRow
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public int EntryStatus { get; set; }
    }
}
