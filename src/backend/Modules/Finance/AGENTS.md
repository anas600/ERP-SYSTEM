# 💰 AGENTS.md — src/backend/Modules/Finance/

> **Finance module** (Accounts, Transactions, CoA). Read all parent AGENTS.md files first.

**Last updated:** 2026-07-29 (DOX framework applied)

---

## Purpose

Chart of Accounts (CoA), journal entries, transactions, bank accounts. The core financial engine.

## Ownership

| Role | Owner |
|------|-------|
| **Authoring** | Jimi تنفيذي |
| **Schema review** | Anas |

## Local Contracts

### Schema
- `accounts` — `id`, `company_id`, `code`, `name`, `type` (Asset/Liability/Equity/Revenue/Expense), `parent_account_id`, `is_postable`, `is_intercompany`.
- `transactions` — `id`, `company_id`, `account_id`, `debit`, `credit`, `description`, `created_at`.
- `bank_accounts` — `id`, `company_id`, `bank_name`, `account_number`, `balance`.
- **All rows MUST have `company_id`** (per Constitution Article 3).

### Double-Entry Rule
- Every transaction has `debit = credit` (sum of debits = sum of credits).
- Enforced in `FinanceService.PostJournalEntryAsync()`.

## Work Guidance

### Adding a New Account Type
1. Update `AccountType` enum in `Domain/Entities/Account.cs`.
2. Add migration if needed.
3. Update CoA seed in `Shared/SeedData/DefaultCoASeed.cs`.
4. Add validation in `Application/Services/FinanceService.cs`.

## Verification

- [ ] `dotnet test --filter "Finance"` — all green.
- [ ] No `tenant_id`.
- [ ] All accounts have `company_id`.
- [ ] Double-entry enforced.

---

## 🧪 Test Pattern: SQL AS Alias Support (added 2026-07-31, Sprint 8 T2)

When writing tests that use `FakeDbConnectionFactory`, you can now use **real SQL with `AS` aliases** instead of the old "projected column names" workaround.

### Before T2 (workaround)

```csharp
// Test code: column names in AddRow must match SELECT column names
factory.AddRow("accounts",
    "AccountId", Guid.NewGuid(),   // <-- alias as base column name
    "AccountCode", "1000",        // <-- alias as base column name
    "AccountName", "Cash");
"SELECT AccountId, AccountCode, AccountName FROM accounts"  // <-- no AS
```

This is fragile: the DataTable column types are inferred from the AddRow values, and the SQL doesn't match production SQL (which uses `id AS "AccountId"`).

### After T2 (real SQL)

```csharp
// SQL (production-style):
"SELECT id AS \"AccountId\", code AS \"AccountCode\", name AS \"AccountName\" FROM accounts"

// AddRow uses BASE column names (the underlying DataTable schema):
factory.AddRow("accounts",
    "id", Guid.NewGuid(),
    "code", "1000",
    "name", "Cash");
```

The `FakeDbDataReader` parses the SELECT clause and projects the underlying DataTable's columns to the alias names. The reader's `GetName(i)` returns the alias, but the values are pulled from the source column with the matching base name. This aligns test SQL with production SQL.

### Edge cases supported

- **Mixed aliased + non-aliased columns** — `SELECT id, code AS "AccountCode", name FROM accounts` works.
- **Quoted identifiers** — `AS "AccountId"` is unquoted to `AccountId`.
- **Expression aliases** — `(code || '-' || name) AS "DisplayName"` creates the column with the alias name, but the value is `DBNull` (FakeDb does not simulate the SQL expression).
- **Multiple aliases per SELECT** — any number of aliased columns is fine.
- **Aggregate aliases** — `COUNT(*) AS total` parses correctly, but the value is `object` (use `ExecuteScalar` for real COUNT semantics; this change only affects the reader).
- **No `AS`** — falls back to the direct DataTable columns. Existing tests using the projected-name convention continue to work.

### Implementation

- `ProjectColumns(string sql, DataSet ds, string tableName)` — internal static helper in `FakeDbConnectionFactory.cs`
- `SplitColumns(string columnList)` — depth/quote-aware state machine for splitting the SELECT column list
- `Unquote(string s)` — strips surrounding double-quotes
- Modified `FakeDbDataReader` constructor to try projection first, fall back to direct table

### Tests

`src/backend/Tests/ERPSystem.Tests/Common/FakeDbConnectionFactoryTests.cs` — 3 tests:
- `AsAlias_RenamesColumnsInReader` — happy path
- `NoAsAlias_FallsBackToDirectColumns` — backward compatibility
- `AsAlias_HandlesMultipleColumnsIncludingExpression` — expression alias edge case

---

_Last updated: 2026-07-29 by Mavis (Muhammad mode) — DOX framework applied_
_2026-07-31: Sprint 8 T2 — added Test Pattern: SQL AS Alias Support (Local Team takeover)_
