using System;
using System.Collections.Generic;
using ERPSystem.Modules.Finance.Entities;

namespace ERPSystem.Modules.Finance.Application;

// ============== Accounts (CoA) ==============

/// <summary>
/// Sprint 9 (Jimi 2 — T2): request body for <c>POST /api/finance/accounts</c>.
/// The service normalizes <c>Code</c> to upper-case and rejects duplicates
/// within the active company. Multi-Company model (Constitution Article 3):
/// the new account is scoped to <c>company_id</c> resolved from
/// <c>X-Company-Id</c> (or the caller's default), not <c>tenant_id</c>.
/// </summary>
public sealed class CreateAccountRequest
{
    /// <summary>Short code (unique within the company, e.g. "1100", "4100-COGS").</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Display name (Arabic primary; EN possible later).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional human-readable description.</summary>
    public string? Description { get; set; }

    /// <summary>Account type (1=Asset, 2=Liability, 3=Equity, 4=Revenue, 5=Expense).</summary>
    public AccountType Type { get; set; }

    /// <summary>Parent account id (for hierarchical CoA). <c>null</c> for a top-level account.</summary>
    public Guid? ParentAccountId { get; set; }

    /// <summary>Whether journal lines can post to this account. Defaults to <c>true</c>.</summary>
    public bool IsPostable { get; set; } = true;
}

/// <summary>
/// Sprint 9 (Jimi 2 — T2): account projection returned by the CoA endpoints
/// (<c>GET /api/finance/accounts</c>, <c>GET /api/finance/accounts/{id}</c>,
/// <c>GET /api/finance/accounts/by-code/{code}</c>, and as the body of
/// <c>POST /api/finance/accounts</c>).
/// </summary>
public sealed class AccountResponse
{
    /// <summary>Stable identifier (UUID v4).</summary>
    public Guid Id { get; set; }

    /// <summary>Short code (unique within the company).</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional human-readable description.</summary>
    public string? Description { get; set; }

    /// <summary>Account type (1=Asset, 2=Liability, 3=Equity, 4=Revenue, 5=Expense).</summary>
    public AccountType Type { get; set; }

    /// <summary>Normal balance side (1=Debit, 2=Credit) — derived from <see cref="Type"/> on insert.</summary>
    public NormalBalance NormalBalance { get; set; }

    /// <summary>Parent account id (hierarchical CoA). <c>null</c> for a top-level account.</summary>
    public Guid? ParentAccountId { get; set; }

    /// <summary>Whether journal lines can post to this account.</summary>
    public bool IsPostable { get; set; }

    /// <summary>Soft-delete flag. Inactive accounts are hidden from the default list view.</summary>
    public bool IsActive { get; set; }
}

// ============== Journal Entries ==============

/// <summary>
/// Sprint 9 (Jimi 2 — T2): request body for posting a journal entry. The
/// service enforces <c>Σ Debit == Σ Credit</c> and rejects non-postable
/// accounts. Returns a <see cref="JournalEntryResponse"/> on success.
/// </summary>
public sealed class PostJournalEntryRequest
{
    /// <summary>Entry date (UTC). Defaults to today when blank.</summary>
    public DateTime EntryDate { get; set; }

    /// <summary>Human-readable description (mandatory for audit trail).</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Optional external reference (e.g. "INV-2026-0042", "BILL-7821").</summary>
    public string? Reference { get; set; }

    /// <summary>At least 2 lines; Σ Debit must equal Σ Credit.</summary>
    public List<PostJournalLineRequest> Lines { get; set; } = new();
}

/// <summary>Sprint 9 (Jimi 2 — T2): a single line inside <see cref="PostJournalEntryRequest"/>.</summary>
public sealed class PostJournalLineRequest
{
    /// <summary>Account id (must be a postable account in the active company).</summary>
    public Guid AccountId { get; set; }

    /// <summary>Debit amount in the company's base currency. Use 0 when crediting.</summary>
    public decimal Debit { get; set; }

    /// <summary>Credit amount in the company's base currency. Use 0 when debiting.</summary>
    public decimal Credit { get; set; }

    /// <summary>Optional per-line description (visible on the journal report).</summary>
    public string? Description { get; set; }
}

/// <summary>
/// Sprint 9 (Jimi 2 — T2): full journal entry projection returned by the
/// journal endpoints. Includes line-level account details so the FE does not
/// have to do a follow-up join.
/// </summary>
public sealed class JournalEntryResponse
{
    /// <summary>Stable identifier (UUID v4).</summary>
    public Guid Id { get; set; }

    /// <summary>Sequential entry number (e.g. "JE-2026-000123").</summary>
    public string EntryNumber { get; set; } = string.Empty;

    /// <summary>Entry date.</summary>
    public DateTime EntryDate { get; set; }

    /// <summary>Human-readable description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Optional external reference.</summary>
    public string? Reference { get; set; }

    /// <summary>Status (1=Draft, 2=Posted, 3=Voided). Drafts can be edited; posted entries are immutable.</summary>
    public JournalEntryStatus Status { get; set; }

    /// <summary>UTC timestamp of the post operation. <c>null</c> while still a draft.</summary>
    public DateTime? PostedAt { get; set; }

    /// <summary>Per-line breakdown (always at least 2 lines for a balanced entry).</summary>
    public List<JournalLineResponse> Lines { get; set; } = new();

    /// <summary>Σ Debit across all lines. Must equal <see cref="TotalCredit"/> for a posted entry.</summary>
    public decimal TotalDebit { get; set; }

    /// <summary>Σ Credit across all lines.</summary>
    public decimal TotalCredit { get; set; }
}

/// <summary>Sprint 9 (Jimi 2 — T2): a single line inside <see cref="JournalEntryResponse"/>.</summary>
public sealed class JournalLineResponse
{
    /// <summary>1-based line number within the entry (preserves insert order).</summary>
    public int LineNumber { get; set; }

    /// <summary>Account id.</summary>
    public Guid AccountId { get; set; }

    /// <summary>Account code (denormalized for display).</summary>
    public string AccountCode { get; set; } = string.Empty;

    /// <summary>Account name (denormalized for display).</summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>Debit amount in base currency.</summary>
    public decimal Debit { get; set; }

    /// <summary>Credit amount in base currency.</summary>
    public decimal Credit { get; set; }

    /// <summary>Optional per-line description.</summary>
    public string? Description { get; set; }
}

// ============== General Ledger ==============

/// <summary>Sprint 9 (Jimi 2 — T2): a single row in the general-ledger report.</summary>
public sealed class LedgerLineResponse
{
    /// <summary>Entry date.</summary>
    public DateTime EntryDate { get; set; }

    /// <summary>Sequential journal entry number.</summary>
    public string EntryNumber { get; set; } = string.Empty;

    /// <summary>Journal entry id (for follow-up detail navigation).</summary>
    public string JournalEntryId { get; set; } = string.Empty;

    /// <summary>Optional external reference (e.g. invoice/bill number).</summary>
    public string? Reference { get; set; }

    /// <summary>Journal entry description (copied from the header for context).</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Account code (denormalized for display).</summary>
    public string AccountCode { get; set; } = string.Empty;

    /// <summary>Account name (denormalized for display).</summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>Debit amount (signed: positive for Debit-normal accounts).</summary>
    public decimal Debit { get; set; }

    /// <summary>Credit amount (signed: positive for Credit-normal accounts).</summary>
    public decimal Credit { get; set; }

    /// <summary>Running balance after this line (Σ debit − Σ credit, sign by <c>normal_balance</c>).</summary>
    public decimal RunningBalance { get; set; }
}

/// <summary>Sprint 9 (Jimi 2 — T2): account balance summary for a date range.</summary>
public sealed class AccountBalanceResponse
{
    /// <summary>Account id.</summary>
    public Guid AccountId { get; set; }

    /// <summary>Account code.</summary>
    public string AccountCode { get; set; } = string.Empty;

    /// <summary>Account name.</summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>Account type (1=Asset, 2=Liability, 3=Equity, 4=Revenue, 5=Expense).</summary>
    public AccountType Type { get; set; }

    /// <summary>Normal balance side (1=Debit, 2=Credit).</summary>
    public NormalBalance NormalBalance { get; set; }

    /// <summary>Σ Debit across the period.</summary>
    public decimal TotalDebit { get; set; }

    /// <summary>Σ Credit across the period.</summary>
    public decimal TotalCredit { get; set; }

    /// <summary>Net balance (Σ debit − Σ credit, sign by <see cref="NormalBalance"/>).</summary>
    public decimal Balance { get; set; }
}

// ============== Posting Rules ==============

/// <summary>Sprint 9 (Jimi 2 — T2): request body for creating a posting rule.</summary>
public sealed class CreatePostingRuleRequest
{
    /// <summary>Rule display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description of what the rule does.</summary>
    public string? Description { get; set; }

    /// <summary>The event that triggers this rule (e.g. SalesInvoicePosted).</summary>
    public TriggeringEvent EventType { get; set; }

    /// <summary>Template describing the GL accounts and amounts to post.</summary>
    public PostingRuleTemplate Template { get; set; } = new();
}
