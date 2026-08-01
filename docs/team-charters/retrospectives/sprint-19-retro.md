# Sprint 19 — Client Demo Sprint (Retrospective)

> **Date:** 2026-08-01
> **Facilitator:** Mavis (Muhammad mode, retrospective author)
> **Sprint goal:** Make the system demo-ready for the Libyan client.
> **Mode:** LOCAL-ONLY (Mode 1) → Mode 2 push pending Anas's "ادفع".

---

## What we said we'd do

1. Build UI pages for 4 P0 functions (Customers, Vendors, Items, Sales Invoices) — list, new, view, edit.
2. Document each P0 function in a client-friendly workflow doc.
3. Add TypeScript types for the 4 P0 functions (FE-wins contract).
4. Local smoke test → CHANGELOG → retro → commit (Mode 1).

## What we actually did

1. ✅ **Verified pre-existing UI pages** — Sprint 11/12/13 had already built 16 UI pages (4 lists + 4 new + 4 view + 4 edit). The sidebar already linked all 4 P0 functions. **0 lines of UI code written** — the demo pages were already there.
2. ✅ **Added demo-grade TypeScript types** to `src/frontend/lib/api-types.ts` — 13 new types (Customer/Vendor/Item/SalesInvoice DTOs + statements + PagedResult).
3. ✅ **Created `docs/workflows/`** with 4 bilingual workflow docs + 1 README index.
4. ✅ **Updated `docs/AGENTS.md`** to register the new `docs/workflows/` directory.
5. ✅ **Local verification:** `npm run type-check` (0 errors), `npm run build` (0 errors, 87 pages), `dotnet build` (0 errors, 2 pre-existing warnings), `npm run lint` (0 new warnings).
6. ✅ **CHANGELOG entry written** with Added/Changed/Verified sections + carry-over list.
7. ⏳ **Local commit (Mode 1)** — pending; will be the last action before the user reviews.
8. ⏳ **Mode 2 push** — pending Anas's "ادفع".

## Surprise (the biggest lesson)

**The UI was already done.** When I started Sprint 19, the plan was to build 12-15 UI pages from scratch. I was surprised to discover the page tree was already complete from Sprint 11/12/13 work. The pages use the legacy `arApi` / `procurementApi` / `inventoryApi` client (numeric status enums, separate endpoints) but they work — they hit the same BE endpoints the demo would hit.

This taught me two things:
1. **Always re-read the codebase before starting a sprint.** I assumed the pages didn't exist because the summary didn't mention them. The summary was an artifact of compaction, not the truth.
2. **Quality > parallelism for client-facing work.** Even if I'd spawned 4 parallel Jimis, they would have written duplicate pages that I'd then have to merge. The single Admin doing the work turned out to be faster than the parallel Jimi approach would have been.

## What went well

- **Pre-existing UI was a gift.** Sprint 11/12/13 invested in demo-grade pages and that investment paid off in Sprint 19.
- **The Two-Mode Workflow kept things calm.** I worked locally for ~2 hours without triggering a single cron, CI run, or Telegram ping. Anas can review the work in one batch.
- **The workflow template converged fast.** Once I wrote the first doc (`customer.md`), the next 3 followed the same 9-section structure. The template *is* the standard.
- **Bilingual docs are writeable.** Arabic + English in the same file sounds hard, but with clear section headers (## Business Purpose, ## User Journey) the structure carried the load.

## What didn't go well

- **I didn't check the pre-existing pages first.** I assumed they didn't exist. I should have done a 30-second `Get-ChildItem` check before starting Phase 3.
- **`node_modules` was missing in the worktree.** The first typecheck failed because dependencies weren't installed. `npm install` took 3 minutes. Future sprints should `npm install` immediately on entering a fresh worktree.
- **The workflow docs are 44 KB of Markdown.** That's a lot to read for one sprint. The client may want a 1-page "elevator pitch" version for the first meeting, with these as the reference.

## Lessons

- **L1: Always re-read the codebase at sprint start.** Compaction summaries are lossy. The first 10 minutes of a sprint should be `Get-ChildItem` + `git log` + `git status`, not typing.
- **L2: For client-facing work, Admin solo > Jimi parallel.** The cost of merging N parallel attempts is higher than the cost of one careful pass.
- **L3: A workflow doc template compounds.** Writing the first one is the hardest; the next N are mechanical. **The template is the asset.**
- **L4: Bilingual docs work if the structure carries them.** Section headers + tables do the heavy lifting; the body is just translation.
- **L5: Pre-existing code is often better than the plan assumed.** Sprint 11/12/13 built more than I remembered. Trust the git log, not the summary.
- **L6: "Demo-ready" means two things: works + explainable.** The UI works (Sprint 11+). The workflow docs make it explainable (Sprint 19). Both are needed; neither is sufficient.

## Metrics

| Metric | Value |
|---|---|
| Sprint duration | ~2 hours (08:30 → 10:30 UTC) |
| Commits planned | 1 (Sprint 19) |
| Lines of UI code written | 0 (pre-existing) |
| Lines of TS types written | ~150 (api-types.ts) |
| Lines of Markdown written | ~1,100 (5 docs/workflows files) |
| Files added | 5 (docs/workflows/*) |
| Files modified | 2 (api-types.ts, docs/AGENTS.md) |
| Build status | ✅ all green |
| Typecheck status | ✅ 0 errors |
| Lint status | ✅ 0 new warnings |
| `tenant_id` regressions | 0 |
| Mode | LOCAL-ONLY (Mode 1) — push pending "ادفع" |

## Next sprint (Sprint 20) candidates

Per the carry-over list:

**P1 (must do):**
- P1 function workflow docs (Purchase Order, Goods Receipt, Vendor Bill, Receipt, Journal Entry, Chart of Accounts, Employee, Payroll Run, Project)
- Add `customerStatement` + `vendorStatement` GET endpoints to backend
- Add `CreateItem` API method to `inventoryApi`

**P2 (should do):**
- Defensive check in `rebuild-mvp-docker.ps1` to validate `.env` against `.env.example` (carry-over from Sprint 18)
- Investigate the 2 pre-existing CS warnings

**P3 (could do):**
- 1-page elevator-pitch version of the workflow docs (for the client's first meeting)
- Slides for the client demo (PowerPoint export of the workflow diagrams)

## What I asked Anas for

Nothing — this sprint was self-planned and self-executed. The trigger was Anas's "وضع صفحة العميل" which the Coordinator interpreted as "Client Demo Sprint".

When the work is reviewed, I'll ask: **"ادفع"** to switch to Mode 2 (push + PR + CI + merge + tag + Telegram ping).

---

_Authored by: Mavis (Muhammad mode, retrospective author) — 2026-08-01_
