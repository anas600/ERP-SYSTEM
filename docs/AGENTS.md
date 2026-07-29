# 📚 AGENTS.md — docs/

> **Per-directory context for `/docs/`.**

**Last updated:** 2026-07-29 (Cleanup)

---

## 🎯 Purpose

This directory contains **governance, roadmap, and architecture documentation** for ERP-SYSTEM.

**Rules:**

1. **One source of truth per topic:**
   - Constitutional questions → `CONSTITUTION.md` (root, not here)
   - Roadmap questions → `workflow/demo-roadmap.md`
   - Architecture questions → `architecture/`
   - Recent changes → `CHANGELOG.md`

2. **No per-decision, per-cycle, per-hand-off documents.** They're in CHANGELOG.md or merged into CONSTITUTION.

3. **No old files.** If a doc is no longer current, delete it.

---

## 📁 Subdirectories

| Path | Purpose |
|------|---------|
| `workflow/` | Roadmap + sprint plans (demo-roadmap.md, sprint-N.md) |
| `architecture/` | Architecture documentation (Siti-authored) |

---

## 🚫 What's NOT in this directory anymore (deleted in 2026-07-29 cleanup)

- ❌ `dec-051` through `dec-111` (per-decision folders) — merged into CONSTITUTION
- ❌ `DEC-070`, `DEC-071`, `DEC-072` files — merged into CONSTITUTION Articles 10-12
- ❌ `E2E-TEST-*` files — historical, not needed
- ❌ `HANDOFF-*.md` (5 files) — replaced by sprint docs in `workflow/`
- ❌ `PHASE6-*` (5 files) — completed, archived in CHANGELOG
- ❌ `RELEASE-REPORT-*.html` — historical
- ❌ `seed-*.sql` (8 files) — moved to `src/backend/Host/Bootstrap/` or `docs/seed-sprint4-demo-data.sql`
- ❌ `SYSTEM-FUNCTIONAL-SPECIFICATION.*` — replaced by architecture doc
- ❌ `governance/` (29 files) — simplified, key info in CONSTITUTION
- ❌ `research/`, `runbooks/`, `workflows/` — historical

**Keep it lean. One place per topic. Update, don't multiply.**

---

## ✏️ Adding New Documentation

Before adding a new file, ask:

1. Is this about the **Constitution**? → Add to `CONSTITUTION.md` instead.
2. Is this about the **Roadmap**? → Add to `docs/workflow/demo-roadmap.md`.
3. Is this about a **specific sprint**? → Add to `docs/workflow/sprint-N.md`.
4. Is this about **Architecture**? → Add to `docs/architecture/`.
5. Is this a **recent change**? → Add to `docs/CHANGELOG.md`.

If none of the above, **don't add it.** Update existing.

---

_Last updated: 2026-07-29 by Mavis (Muhammad mode), approved by Anas_
