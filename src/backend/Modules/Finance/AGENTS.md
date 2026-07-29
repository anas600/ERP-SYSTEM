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

_Last updated: 2026-07-29 by Mavis (Muhammad mode) — DOX framework applied_
