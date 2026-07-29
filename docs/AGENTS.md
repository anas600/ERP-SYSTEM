# 📚 AGENTS.md — docs/

> **Per-directory context for `/docs/`.** Read root AGENTS.md first.

**Last updated:** 2026-07-29 (DOX framework applied)

---

## Purpose

This directory contains **governance, roadmap, and architecture documentation** for ERP-SYSTEM.

The single source of truth is:
- **Constitution:** [`/CONSTITUTION.md`](../CONSTITUTION.md)
- **Roadmap:** [`docs/workflow/demo-roadmap.md`](./workflow/demo-roadmap.md)
- **Architecture:** [`docs/architecture/holding-company-architecture.md`](./architecture/holding-company-architecture.md)
- **Changelog:** [`docs/CHANGELOG.md`](./CHANGELOG.md)

## Ownership

| Role | Owner |
|------|-------|
| **Authoring** | Siti (Cloud Coordinator) + Muhammad (Architect) |
| **Approval** | Anas (Project Owner) |
| **Cleanup** | All agents must remove stale files |

## Local Contracts

- **One source of truth per topic.** No duplicate documentation.
- **No per-decision, per-cycle, per-hand-off documents.** They're in CHANGELOG.md or merged into CONSTITUTION.
- **No old files.** If a doc is no longer current, delete it.
- **No `tenant_*` references anywhere.** Use `company_*` only.

## Work Guidance

### Adding New Documentation
Before adding a new file, ask:
1. Is this about the **Constitution**? → Add to `/CONSTITUTION.md` instead.
2. Is this about the **Roadmap**? → Add to `docs/workflow/demo-roadmap.md`.
3. Is this about a **specific sprint**? → Add to `docs/workflow/sprint-N.md`.
4. Is this about **Architecture**? → Add to `docs/architecture/`.
5. Is this a **recent change**? → Add to `docs/CHANGELOG.md`.

If none of the above, **don't add it.** Update existing.

### Style
- Concise, operational.
- Document stable contracts, not diary entries.
- Delete stale notes instead of explaining history.
- Use English for technical terms; Arabic for user-facing content.

## Verification

- [ ] No `tenant_id` references: `grep -r "tenant_id" docs/`.
- [ ] No files older than 90 days without explicit reason.
- [ ] All cross-references resolve (use grep, not click).
- [ ] CHANGELOG.md is current with this sprint.

## Child DOX Index

| Path | Scope | Status |
|------|-------|--------|
| [`docs/architecture/`](./architecture/) | Architecture documentation (Siti-authored) | Active |
| [`docs/workflow/`](./workflow/) | Roadmap + sprint plans | Active |

---

_Last updated: 2026-07-29 by Mavis (Muhammad mode) — DOX framework applied_
