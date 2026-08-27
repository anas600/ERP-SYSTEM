using System;

namespace ERPSystem.Modules.Finance.Entities;

public enum AccountType { Asset = 1, Liability = 2, Equity = 3, Revenue = 4, Expense = 5 }
public enum NormalBalance { Debit = 1, Credit = 2 }

public class Account
{
    public Guid Id { get; set; }
    // Sprint 28 (DEC-097): was Guid? — DB column is NOT NULL, so entity should match.
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AccountType Type { get; set; }
    public NormalBalance NormalBalance { get; set; }
    public Guid? ParentAccountId { get; set; }
    public bool IsPostable { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public bool IsIntercompany { get; set; } = false;
    /// <summary>
    /// Sprint 52a: CoA hierarchy level (1=L1 Class, 2=L2 Sub-class, 3=L3 Control, 4=L4 Detail).
    /// Computed from parent chain by AccountLevelBackfillHostedService.
    /// Null for legacy accounts not yet backfilled (set automatically on first run).
    /// </summary>
    public short? Level { get; set; }
    // ===== Sprint 60 Wave 2A (DEC-185) — Financial-Statement metadata =====
    // Wired from the 6 new columns added by Sprint60_AddAccountFsMetadata
    // (Wave 1, DEC-184). Defaults match the DB column defaults so existing
    // code paths (which construct Account without setting these) continue
    // to round-trip safely. Wave 2 (the actual migration job) will populate
    // these for legacy accounts.

    /// <summary>
    /// Financial-Statement type: 'BS' (Balance Sheet) or 'PL' (Profit &amp; Loss).
    /// NULL for legacy accounts not yet classified by the Wave 2 migration job.
    /// </summary>
    public string? FsType { get; set; }

    /// <summary>
    /// Financial-Statement section: 'Current Asset' | 'Non-Current Asset' |
    /// 'Current Liability' | 'Non-Current Liability' | 'Equity' | 'Revenue' |
    /// 'COGS' | 'OpEx' | 'Finance Income' | 'Finance Expense' | 'Tax' |
    /// 'Other' | 'Closing'. NULL for legacy accounts not yet classified.
    /// </summary>
    public string? Section { get; set; }

    /// <summary>
    /// TRUE when the account uses the canonical 4-level coding scheme
    /// (e.g. '1.1.01.002'). FALSE for legacy accounts that still use the
    /// old 4-digit code. Default TRUE matches the DB column DEFAULT TRUE
    /// for new rows; existing legacy rows are backfilled to FALSE by the
    /// Wave 1 migration.
    /// </summary>
    public bool IsCanonical { get; set; } = true;

    /// <summary>
    /// The canonical 4-level code (e.g. '1.1.01.002') for accounts that have
    /// been migrated. NULL for legacy accounts still on the old code.
    /// </summary>
    public string? NewCode { get; set; }

    /// <summary>
    /// Migration status: 'pending' (legacy, not yet migrated), 'migrated'
    /// (legacy → canonical complete), 'new' (created with canonical code),
    /// or 'deprecated' (no longer used). Default 'pending' matches the DB
    /// column DEFAULT 'pending'.
    /// </summary>
    public string MigrationStatus { get; set; } = "pending";

    /// <summary>
    /// Timestamp when the account was migrated from legacy to canonical code.
    /// NULL for legacy (unmigrated) accounts.
    /// </summary>
    public DateTime? MigratedAt { get; set; }
    // ===== End Sprint 60 Wave 2A =====

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
