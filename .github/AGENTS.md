# 🔄 AGENTS.md — .github/

> **GitHub workflows + templates.** Read root AGENTS.md first.

**Last updated:** 2026-07-29 (DOX framework applied)

---

## Purpose

GitHub Actions workflows, branch protection, and PR templates.

## Ownership

| Role | Owner |
|------|-------|
| **Authoring** | Dev (DevOps mode) |
| **Approval** | Anas (production workflows) / Mavis Local (dev) |

## Local Contracts

### Required CI Checks (6, per Constitution Article 4)
1. **Backend Tests (.NET 9.0)** — `dotnet test`
2. **Frontend Build (Next.js 14)** — `npm run build`
3. **CodeQL** — security scan
4. **TruffleHog** — secret scan
5. **Analyze (javascript-typescript)** — code quality
6. **Analyze (csharp)** — code quality

### Optional
- **Playwright E2E** — not required for merge (per Constitution Article 11).

## Work Guidance

### Adding a Workflow
1. Create `.github/workflows/<name>.yml`.
2. Use pinned action versions (`@v4.1.0`, not `@main`).
3. Document triggers and required secrets in workflow header.
4. Add to required checks if it's a new mandatory check.

## Verification

- [ ] All workflows have valid YAML.
- [ ] All actions pinned to versions.
- [ ] No secrets hardcoded (use `${{ secrets.X }}`).
- [ ] PR template updated if new required fields needed.

## Child DOX Index

| Path | Scope | Status |
|------|-------|--------|
| `.github/workflows/` | CI/CD workflows | Active |

---

_Last updated: 2026-07-29 by Mavis (Muhammad mode) — DOX framework applied_
