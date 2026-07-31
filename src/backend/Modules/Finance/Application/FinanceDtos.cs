using System;
using System.Collections.Generic;
using ERPSystem.Modules.Finance.Entities;

namespace ERPSystem.Modules.Finance.Application;

// ============== Accounts (CoA) ==============

public sealed class CreateAccountRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AccountType Type { get; set; }
    public Guid? ParentAccountId { get; set; }
    public bool IsPostable { get; set; } = true;
}

public sealed class AccountResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AccountType Type { get; set; }
    public NormalBalance NormalBalance { get; set; }
    public Guid? ParentAccountId { get; set; }
    public bool IsPostable { get; set; }
    public bool IsActive { get; set; }
}

// ============== Journal Entries ==============

public sealed class PostJournalEntryRequest
{
    public DateTime EntryDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public List<PostJournalLineRequest> Lines { get; set; } = new();
}

public sealed class PostJournalLineRequest
{
    public Guid AccountId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Description { get; set; }
}

public sealed class JournalEntryResponse
{
    public Guid Id { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public JournalEntryStatus Status { get; set; }
    public DateTime? PostedAt { get; set; }
    public List<JournalLineResponse> Lines { get; set; } = new();
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
}

public sealed class JournalLineResponse
{
    public int LineNumber { get; set; }
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Description { get; set; }
}

// ============== General Ledger ==============

public sealed class LedgerLineResponse
{
    public DateTime EntryDate { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public string JournalEntryId { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public string Description { get; set; } = string.Empty;
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal RunningBalance { get; set; }
}

public sealed class AccountBalanceResponse
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public NormalBalance NormalBalance { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal Balance { get; set; }
}

// ============== Posting Rules ==============

public sealed class CreatePostingRuleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TriggeringEvent EventType { get; set; }
    public PostingRuleTemplate Template { get; set; } = new();
}

// ============== Sprint 11 T2 (BE Jimi) — Demo-grade DTOs ==============
//
// These DTOs match the FE contract in `src/frontend/lib/api-types.ts` (T1).
// They are distinct from the existing `AccountResponse` / `JournalEntryResponse`
// shapes because the FE uses **string enums** for clarity on the new demo
// pages, while the legacy FE uses numeric enums (`type: 1..5`, `normalBalance: 1..2`).
//
// Article 3: no `tenant_id`. The wire shape stays `companyId` only.

// Sprint 11 T2: flat Chart of Accounts shape for the new /api/accounts
// endpoint. The legacy `AccountResponse` (numeric enums) is kept for the
// existing finance/accounts page; this one uses string enums to match
// the FE's `AccountType` / `NormalBalance` unions.
public sealed class AccountDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;            // 'Asset' | 'Liability' | 'Equity' | 'Revenue' | 'Expense'
    public string NormalBalance { get; set; } = string.Empty;   // 'Debit' | 'Credit'
    public Guid? ParentAccountId { get; set; }
    public bool IsPostable { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
}

// Sprint 11 T2: a single journal line item, surfaced as a "transaction" on
// the demo /transactions page. Joins to accounts on the BE for the
// display-only `accountCode` / `accountName` fields.
public sealed class TransactionDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid AccountId { get; set; }
    public string? AccountCode { get; set; }
    public string? AccountName { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? Reference { get; set; }
}

// Sprint 11 T2: consolidated KPIs across the entire Holding (all
// sub-companies). Returned by GET /api/holdings/dashboard.
//
// Field-by-field mapping to the FE's `HoldingDashboard` (api-types.ts):
//   totalRevenue       — sum of posted sales invoices across all sub-companies
//   totalExpenses      — sum of posted expense journal lines across all sub-companies
//   netProfit          — totalRevenue - totalExpenses
//   companyCount       — sub-companies (Holding itself excluded)
//   employeeCount      — total active employees across all sub-companies
//   treasuryBalance    — sum of cash + bank account balances across all sub-companies
//   recentTransactions — last 10 journal lines (across all sub-companies)
//   asOf               — snapshot timestamp (UTC, ISO 8601)
//   currency           — base currency (LYD by default)
public sealed class HoldingDashboardDto
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetProfit { get; set; }
    public int CompanyCount { get; set; }
    public int EmployeeCount { get; set; }
    public decimal TreasuryBalance { get; set; }
    public IReadOnlyList<TransactionDto> RecentTransactions { get; set; } = Array.Empty<TransactionDto>();
    public DateTime AsOf { get; set; } = DateTime.UtcNow;
    public string Currency { get; set; } = "LYD";
}
