# 📦 Hand-Off v2 — Cycle 2: 6.2 Tests Refactor ONLY (No Staging/Production)

> **From:** سيتي (Cloud Coordinator) — Session 406067545768199, Cloud  
> **To:** Mavis Local (Tech Lead) — your session, Windows  
> **Cycle:** 2 / 20 — **ACTIVE ✅**  
> **v1 Created:** 2026-07-27 21:14 UTC  
> **v2 Updated:** 2026-07-27 22:35 UTC (per DEC-070)

---

## 🆕 What's New in v2 (per DEC-070)

**Major changes:**
1. ❌ **Block B (3-Layer DB) REMOVED** — No staging/production work
2. ✅ **Mavis Local = Tech Lead** — Full admin authority on develop
3. ✅ **Playwright E2E optional** — Not required for merge
4. ✅ **--force-with-lease allowed** — branch protection updated
5. ✅ **Self-merge permitted** — admin can bypass reviews
6. ✅ **Mavis Local leads Jimis** — Jimi تنفيذي + Jimi تحليلي work for you
7. ✅ **Mavis (Cloud) = Architectural Guardian** — Strategy/Governance role

**Authority basis:** DEC-070 (2026-07-27 22:33 UTC, per Anas)

---

## 🎯 Cycle 2 v2 Scope: 6.2 Tests Refactor ONLY

### Block A (Mavis Local) — Full Power

**Tasks:**
- **T1**: Search for `tenant_id`, `TenantId`, `Tenant`, `ITenantContext`, `TenantContext` across 31 C# test files
- **T2**: Rename `tenant_id` → `company_id` (case-sensitive)
- **T3**: Update test signatures (constructor params, method args, variable names)
- **T4**: Update test fixtures (`FakeDbConnectionFactory`, `ErpWebApplicationFactory`, `TestJwtGenerator`)
- **T5**: Update assertion expectations
- **T6**: Run `dotnet test` → all green
- **T7**: Update 10 Playwright e2e specs (optional, no CI gate)
- **T8**: Add 3 new test cases:
  - `HoldingBootstrap_Seeds_DefaultHolding_And_CoA` (integration)
  - `UserCompany_Limits_Access_To_Assigned_Companies` (unit)
  - `CompanySwitcher_Switches_Active_Company_In_Context` (unit)

**Estimated time:** 2-3 hours

**You're free to:**
- Self-merge when ready (admin bypass)
- Use `--force-with-lease` (branch protection allows)
- Skip Playwright e2e (not required)
- Use Jimis as you see fit
- Open PRs from any branch
- Use the local Supabase dev project (already exists)

---

## 🔧 GitHub Settings (now in effect)

| Setting | Value | Note |
|---------|-------|------|
| **Required status checks** | Backend, Frontend, CodeQL, TruffleHog, Analyze×2 | ❌ No Playwright |
| **Required reviews** | 1 (admin bypass: ON) | You can self-merge |
| **Allow force-pushes** | ✅ ENABLED | Use --force-with-lease |
| **Required linear history** | ✅ ON | No merge commits (squash or rebase) |
| **Dismiss stale reviews** | ✅ ON | Old approvals auto-dismissed on push |

---

## 🛑 DO NOT Touch (per DEC-070)

- ❌ Supabase STAGING project (any kind)
- ❌ Production environment
- ❌ STAGING_* secrets in GitHub
- ❌ reset-staging-db.yml workflow
- ❌ STAGING_DATABASE_URL

These are FROZEN until Anas issues explicit approval.

---

## 🤝 Role Assignment (per DEC-070)

| Role | Agent | What They Do |
|------|-------|--------------|
| **Project Owner** | Anas | Decisions, direction |
| **Tech Lead (operational)** | **Mavis Local (YOU)** | Code, tests, PRs, merges, leads Jimis |
| **Architectural Guardian** | Mavis (Cloud) | Reviews, governance, DECs, crons |
| **Executor** | Jimi تنفيذي | Code implementation (you direct) |
| **Analyst** | Jimi تحليلي | Investigation, design (you direct) |
| **Coordinator (sub-mode)** | سيتي (Cloud) | Hand-offs, merges, governance files |
| **Strategic Advisor (sub-mode)** | محمد (Cloud) | Analysis, plans |
| **DevOps (sub-mode)** | Dev (Cloud) | Infrastructure |

**Communication:**
- You ↔ Jimis: via shared worktree + branch
- You ↔ Mavis (Cloud): via docs/governance/hand-offs/ + crons
- You ↔ Anas: via Telegram (if urgent) or hand-offs

---

## 📋 Verification (lighter than before)

```bash
# 1. Build
npx tsc --noEmit
dotnet build Host/ERP-SYSTEM.csproj

# 2. xUnit tests (REQUIRED)
dotnet test --filter "FullyQualifiedName~ERP"

# 3. New test cases
dotnet test --filter "HoldingBootstrap_Seeds_DefaultHolding_And_CoA"
dotnet test --filter "UserCompany_Limits_Access_To_Assigned_Companies"
dotnet test --filter "CompanySwitcher_Switches_Active_Company_In_Context"

# Playwright is OPTIONAL (not CI gated)
# npx playwright test  # Skip if Tier 1 latency
```

---

## 🚨 Risk Notes

- **R1**: If `ITenantContext` still exists in code, keep as deprecated alias (avoid scope creep)
- **R2**: If local Supabase dev project has data issues, use snapshots or recreate schema
- **R3**: If Jimis block on a question, escalate to me via docs/governance/hand-offs/

---

## 📡 Async Protocol (Reminder)

- Cron `monitor-cycle-2-pr-merge` is ACTIVE (every 3 min)
- Silent on no-change
- Notify on state-change (CONFLICTING → mergeable, etc.)
- Self-delete on merge
- I'll merge your PR when CI green + you say "ready for merge"

---

## 🚀 When Ready to Start

1. Read this hand-off (you're doing it now)
2. Verify branch state: `git fetch && git status`
3. Optionally create a feature branch: `git checkout -b feature/cycle-2-tests-refactor`
4. Do the work
5. Open PR to develop: `gh pr create --base develop`
6. When CI green, say "ready for merge" (via hand-off or Telegram)
7. I (سيتي) will merge

**You have full authority. Go. 🎯**

---

**Signed:** سيتي (Cloud Coordinator, session 406067545768199)  
**Authority:** DEC-070 (Anas)  
**Date:** 2026-07-27 22:35 UTC
