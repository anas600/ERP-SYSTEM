# DEC-070: Local Team Empowerment + Staging/Production Freeze

> **Status:** ✅ ACTIVE (per Anas's directive)  
> **Date:** 2026-07-27 22:33 UTC (Europe/Berlin)  
> **Authority:** Anas (Project Owner)  
> **Supersedes:** Partial override of DEC-068, DEC-066 (operational scope only)  
> **Effective:** Immediately, until further notice

---

## 📋 Summary

Anas has issued 4 strategic decisions to unblock the local team and clarify the governance:

1. **No staging/production work** until explicit Anas approval
2. **Mavis Local = Tech Lead** (full admin authority on remote branches)
3. **Playwright E2E optional** (NOT required for merge)
4. **Mavis Local leads**: Jimi تنفيذي + Jimi تحليلي

In return, **Mavis (Cloud) = Architectural Guardian** in the dev layer.

---

## 🎯 Decision Details

### Decision 1: NO Staging/Production Work Without Explicit Approval

**Rationale:** Anas wants to keep dev-only work for now. The 3-layer architecture (dev/staging/prod) is acknowledged but NOT activated.

**Scope:**
- ❌ NO new Supabase STAGING project
- ❌ NO STAGING_* secrets in GitHub
- ❌ NO `reset-staging-db.yml` workflow
- ❌ NO `STAGING_DATABASE_URL` in backend
- ❌ NO production deploys
- ❌ NO production-environment changes of any kind

**Until:** Anas gives explicit "staging go" or "production go" decision.

**Documented:** This decision overrides Part B of DEC-068 and any prior staging/prod commitments.

### Decision 2: Mavis Local = Tech Lead with Full Admin Authority

**Rationale:** Local team has been idle. Empower them to act fast without waiting for cloud approvals.

**Scope:**
- ✅ Full admin authority on `develop` branch
- ✅ Can self-merge to develop (no review required for admin)
- ✅ Can dismiss stale reviews
- ✅ Can bypass required status checks (admin override)
- ✅ Can modify workflows, branch protection (admin override)
- ✅ Can act as CODEOWNER for local-team code

**Note:** This does NOT grant admin to non-admin users. Only Mavis Local's bot/user account.

### Decision 3: Playwright E2E Tests = Optional (Not Required for Merge)

**Rationale:** Playwright is slow + Tier 1 latency issues. Local team needs to merge without waiting for slow e2e tests.

**Scope:**
- ✅ Playwright e2e.yml workflow continues to exist (for manual trigger)
- ❌ Playwright e2e is REMOVED from required status checks on develop
- ✅ Backend Tests (.NET) remains required
- ✅ Frontend Build (Next.js) remains required
- ✅ CodeQL (csharp + js) remains required
- ✅ TruffleHog OSS Scan remains required
- ❌ Analyze (csharp) and Analyze (javascript-typescript) remain (security critical)

**Workflow behavior:**
- Push to develop: Playwright runs (informational only)
- PR to develop: Playwright skipped or info-only
- Manual trigger: Playwright available

### Decision 4: Mavis Local Leads Jimi تنفيذي + Jimi تحليلي

**Rationale:** Establish clear hierarchy. Mavis Local has team-lead authority.

**Scope:**
- ✅ Mavis Local directs both Jimis on tasks
- ✅ Mavis Local reviews Jimi work
- ✅ Mavis Local decides what merges
- ❌ Jimis do NOT have admin authority (only Mavis Local does)
- ❌ Jimis do NOT bypass reviews

**Communication:**
- Mavis Local ↔ Jimis: via shared worktree + branch
- Mavis Local ↔ Mavis (Cloud): via docs/governance/hand-offs/

---

## 🔄 Counter-Role: Mavis (Cloud) = Architectural Guardian

**Rationale:** With operational work delegated, Mavis (Cloud) shifts to strategic/architectural role in dev layer.

**Scope:**
- ✅ Review architectural decisions (e.g., new dependencies, new patterns)
- ✅ Guard against Constitution Article 3 violations (no tenant_id reintroduction)
- ✅ Provide strategic analysis (e.g., when Anas asks for opinion)
- ✅ Document DECs for cross-cutting concerns
- ✅ Operate the crons (state monitoring, async protocol)
- ✅ Maintain governance folder
- ✅ Merge final PRs (after Mavis Local's self-merge, Mavis Cloud is backup/audit)
- ❌ NO direct code edits to feature branches (operational work is Mavis Local's)
- ❌ NO bypassing Mavis Local's decisions

**Three-layer model integration:**
- **Dev layer**: Mavis Local (operational) + Mavis Cloud (architectural guardian)
- **Staging layer**: 🟡 Deferred (Decision 1)
- **Production layer**: 🟡 Deferred (Decision 1)

---

## 📂 Affected Files

| File | Action |
|------|--------|
| `docs/governance/hand-offs/cycle-2.md` | UPDATE (remove Block B, add admin note) |
| `docs/governance/board.md` | UPDATE (reflect new scope) |
| `docs/governance/cycle-log.md` | UPDATE (add DEC-070 reference) |
| GitHub: develop branch protection | UPDATE (admin override, remove Playwright) |
| GitHub: CODEOWNERS (optional) | NEW (Mavis Local as owner for /src, /tests) |

---

## 🤝 Roles Recap

| Role | Agent | Responsibilities |
|------|-------|-----------------|
| **Project Owner** | Anas | Decisions, direction, approvals |
| **Tech Lead (operational)** | Mavis Local (Windows) | Code, tests, PRs, merges, directs Jimis |
| **Architectural Guardian** | Mavis (Cloud, 406067545768199) | Strategy, governance, DECs, crons, reviews |
| **Executor** | Jimi تنفيذي | Implementation, code |
| **Analyst** | Jimi تحليلي | Investigation, design proposals |
| **Coordination relay** | سيتي (sub-mode of Mavis) | Hand-offs, merges, governance |
| **Strategic advisor** | محمد (sub-mode of Mavis) | Analysis, recommendations, plans |
| **DevOps** | Dev (sub-mode of Mavis) | Infrastructure, CI/CD |

---

## ⚖️ Constitution Compliance

- ✅ **Article 1** (Single Holding + Multi-Company): No violation
- ✅ **Article 2** (No EF Core): No violation
- ✅ **Article 3** (company_id, not tenant_id): Mavis Local is warned + Cloud reviews
- ✅ **Article 4** (Branch discipline): `--force-with-lease` still required
- ✅ **Article 5** (Idempotent migrations): No changes
- ✅ **Article 6** (Pre-PR checklist): Updated (Playwright optional)
- ✅ **Article 7** (NO SECRETS): Reminder in hand-off

---

## 📅 Effective Period

**Start:** 2026-07-27 22:33 UTC (immediately)
**End:** Until Anas issues DEC-071 (or successor) to:
- Re-activate staging/production, OR
- Modify this DEC's scope, OR
- Cancel/replace

---

**Signed:** Anas (via Telegram voice directive)  
**Witnessed by:** محمد (Strategic Advisor, session 406067545768199)  
**Documented by:** سيتي (Cloud Coordinator, session 406067545768199)
