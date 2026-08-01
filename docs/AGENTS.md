# 📚 AGENTS.md — docs/

> **Per-directory context for `/docs/`.** Read root AGENTS.md first.

**Last updated:** 2026-08-01 (Sprint 19 — added `docs/workflows/` for client demo, removed obsolete WORKFLOW.md reference, added notes/ reference)

---

## Purpose

This directory contains **governance, roadmap, architecture, and client-facing documentation** for ERP-SYSTEM.

The single sources of truth are:
- **Constitution (governance, active):** [`/CONSTITUTION.md`](../CONSTITUTION.md)
- **Roadmap:** [`docs/workflow/demo-roadmap.md`](./workflow/demo-roadmap.md)
- **Architecture:** [`docs/architecture/holding-company-architecture.md`](./architecture/holding-company-architecture.md)
- **Client workflows (P0 demo):** [`docs/workflows/`](./workflows/) — one document per function for the client
- **Architect notes:** [`docs/notes/`](./notes/) — analyses, retrospectives, design notes
- **Changelog (current):** [`/CHANGELOG.md`](../CHANGELOG.md) (at project root)
- **Changelog (historical, Phase 6 and earlier):** [`docs/CHANGELOG.md`](./CHANGELOG.md) (keep for audit trail)

## Ownership

| Role | Owner |
|------|-------|
| **Authoring** | Siti (Cloud Coordinator) + Muhammad (Architect) + Mavis Local (Admin) |
| **Approval** | Anas (Project Owner) |
| **Cleanup** | All agents must remove stale files |

## Local Contracts

- **One source of truth per topic.** No duplicate documentation.
- **No per-decision, per-cycle, per-hand-off documents.** They're in CHANGELOG.md or merged into CONSTITUTION.
- **No old files.** If a doc is no longer current, delete it.
- **No `tenant_*` references anywhere.** Use `company_*` only.
- **Client-facing docs are bilingual** (Arabic + English in the same file), use the workflow template in [`docs/workflows/README.md`](./workflows/README.md).

## Work Guidance

### Adding New Documentation
Before adding a new file, ask:
1. Is this about the **Constitution**? → Add to `/CONSTITUTION.md` instead.
2. Is this about the **Roadmap**? → Add to `docs/workflow/demo-roadmap.md`.
3. Is this about a **specific sprint**? → Add to `docs/workflow/sprint-N.md`.
4. Is this about **Architecture**? → Add to `docs/architecture/`.
5. Is this about a **client workflow** (function the client uses)? → Add to `docs/workflows/`.
6. Is this an **analyst/architect note**? → Add to `docs/notes/`.
7. Is this a **recent change**? → Add to `docs/CHANGELOG.md`.

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
| [`docs/workflows/`](./workflows/) | Client-facing workflow docs (one per P0 function) | Active (Sprint 19) |
| [`docs/notes/`](./notes/) | Analyst/architect notes (Muhammad, retrospectives, design analyses) | Active |
| [`docs/team-charters/`](./team-charters/) | Per-team roles, responsibilities, retrospectives | Active |

---

_Last updated: 2026-08-01 by Mavis (Admin mode, Sprint 19) — DOX framework applied, child index updated, WORKFLOW.md reference removed (deleted in Sprint 18)_
