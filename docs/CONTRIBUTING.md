# Contributing to ERP-SYSTEM

> **Audience:** Anyone working on the repo — Anas, Mavis, Jimis, future contributors.
> **Phase:** 6.4+ (Multi-Company architecture live)

This document covers the local development workflow that all roles follow.
For the cross-team governance protocol (cycle hand-offs, DECs, crons), see
[`docs/governance/README.md`](./governance/README.md).

---

## 🚀 First-Time Setup

After cloning, run these commands once to wire up the local tooling:

### 1. Pre-commit hook (TruffleHog secret scan)

```bash
# Linux / macOS
git config core.hooksPath .githooks
chmod +x .githooks/pre-commit

# Windows (PowerShell)
git config core.hooksPath .githooks
```

This enables `.githooks/pre-commit` which is a cross-platform bash script that:

1. Scans staged files for common secret patterns (`password=`, `ghp_*`, `sk-*`, AWS keys, private keys, etc.) — runs in <100ms.
2. If `trufflehog` is on `PATH` (recommended), runs the deep entropy-based scan (~5s).
3. Blocks the commit if anything is found.

The hook uses POSIX bash (works on Linux, macOS, Windows with Git Bash, and CI containers). It does NOT require PowerShell.

**To install TruffleHog** (optional but recommended):

```bash
# Windows (winget — works on Windows 10+)
winget install --id TruffleSecurity.trufflehog

# macOS (Homebrew)
brew install trufflehog

# Linux (binary)
curl -sSfL https://raw.githubusercontent.com/trufflesecurity/trufflehog/main/scripts/install.sh | sh -s -- -b /usr/local/bin

# Or via Python (any platform, no admin)
pip install trufflehog
```

**Bypass for emergencies** (CI still scans):

```bash
git commit --no-verify
```

### 2. Environment variables

Copy the dev settings template and fill in real values:

```bash
# Linux / macOS
cp src/backend/Host/appsettings.Development.template.json src/backend/Host/appsettings.Development.json

# Windows
Copy-Item src\backend\Host\appsettings.Development.template.json src\backend\Host\appsettings.Development.json
```

> **Never commit `appsettings.Development.json`** — it is gitignored. It
> contains real Supabase/Neon passwords.

### 3. Verify the setup

```bash
# Backend builds
cd src/backend
dotnet build Host/ERP-SYSTEM.csproj

# Frontend builds
cd ../frontend
npm ci
npm run type-check
npm run build
```

---

## 🧪 Running Tests

### Backend (xUnit)

```bash
cd src/backend
# All tests (integration tests need a local Postgres — see ci-fast.yml)
dotnet test Tests/ERPSystem.Tests/ERPSystem.Tests.csproj

# Just the new Phase 6 test cases (no PG needed for the unit ones)
dotnet test --filter "FullyQualifiedName~HoldingBootstrap"
dotnet test --filter "FullyQualifiedName~UserCompany_Limits"
dotnet test --filter "FullyQualifiedName~CompanySwitcher_"
```

### Frontend (Jest / Playwright)

```bash
cd src/frontend
npm run test              # unit
npm run e2e               # Playwright (slow, optional per DEC-070)
```

---

## 🔁 Commit Conventions

We use **Conventional Commits** (enforced loosely by PR review):

| Type | When |
|---|---|
| `feat(...)` | New user-facing feature |
| `fix(...)` | Bug fix |
| `docs(...)` | Documentation only (AGENTS.md, CHANGELOG.md, hand-offs) |
| `refactor(...)` | Code change that neither fixes a bug nor adds a feature |
| `test(...)` | Adding or correcting tests |
| `chore(...)` | Maintenance (deps, configs, governance files) |
| `ci(...)` | CI/CD pipeline changes |
| `perf(...)` | Performance improvement |

**Scopes** are usually the module name: `identity`, `finance`, `ar`, `ap`,
`inventory`, `hr`, `payroll`, `procurement`, `projects`, `reports`,
`notifications`, `host`, `frontend`, `governance`, `tests`, `ci`, `docs`.

### Examples

```bash
git commit -m "feat(reports): add Trial Balance report"
git commit -m "fix(frontend): prevent XSS in invoice description field"
git commit -m "docs(governance): finalize cycle 2 - cycle-log + summary"
git commit -m "chore(deps): bump Next.js to 14.2.0"
git commit -m "test(cycle-2): 6.2 tests refactor (multi-company) + 3 new test cases"
```

---

## 🌿 Git Workflow

Per the governance protocol (see
[`docs/governance/README.md`](./governance/README.md)):

| Branch | Role | Push policy |
|---|---|---|
| `main` | Production | PR from `develop` only (admin merge) |
| `develop` | Integration | Direct push OK for solo owner, PRs from feature branches |
| `feature/*` | Working branches | Free |
| `fix/*` | Bugfix branches | Free |
| `hotfix/*` | Critical production fixes | Free |
| `docs/*` | Documentation-only changes | Free |
| `chore/*` | Maintenance | Free |

### Local hybrid dev pattern

1. Create a feature branch from `develop`:
   ```bash
   git checkout develop
   git pull origin develop
   git checkout -b feature/your-feature
   ```

2. Do the work, commit, push, open a PR to `develop`.

3. CI runs automatically (`.github/workflows/ci-fast.yml`):
   - Backend Tests (with real PG service container)
   - Frontend Build (type-check + lint + build)
   - CodeQL (csharp + js security)
   - TruffleHog OSS Scan (in addition to local pre-commit)
   - ~~Playwright e2e~~ (per DEC-070, not required)

4. Per **DEC-070** (Local Team Empowerment, 2026-07-27), Mavis Local has
   full admin authority on `develop` — self-merge using
   `gh pr merge <N> --squash --delete-branch --admin`.

---

## 🛡️ Security

- **Never** commit secrets, `.env` files, or credentials. The pre-commit
  hook + CI TruffleHog scan will block them, but defense in depth matters.
- **Never** disable the pre-commit hook for "speed" — it runs in <5s.
- **Never** push to `main` directly. Always go through a PR.
- **Never** touch `main` without explicit Anas approval.

If you accidentally commit a secret (e.g. `--no-verify` was used):

1. **Rotate the secret immediately** at the provider (GitHub, Supabase, etc.).
2. Remove it from the repo with `git filter-branch` or BFG.
3. Force-push (since history is rewritten, the secret is gone).
4. Notify the team in `docs/governance/hand-offs/` with a security incident hand-off.

---

## 📋 Local Tooling Reference

| Tool | Purpose | Install |
|---|---|---|
| `dotnet` 9.0 SDK | Backend | https://dot.net |
| `node` 20+ | Frontend | https://nodejs.org |
| `npm` 10+ | Package manager | (bundled with Node) |
| `psql` 15+ | Direct DB query | `winget install PostgreSQL.PostgreSQL.16` |
| `pgcli` 4+ | psql alternative (auto-complete) | `pip install pgcli` |
| `trufflehog` 3+ | Deep secret scan | `winget install TruffleSecurity.trufflehog` |
| `gh` 2+ | GitHub CLI | https://cli.github.com |

---

## 🤝 Governance Hand-Offs

When a cycle closes, the Tech Lead writes a hand-off in
`docs/governance/hand-offs/cycle-N-response.md` per the template in
`docs/governance/hand-off-template.md`. The coordinator (سيتي) reviews it,
merges if needed, and starts the next cycle.

For cycle work that affects a Phase, scope, or architecture, **file a DEC**
(`docs/DEC-NNN-...md`) and have it approved by Anas before starting.

---

**Last updated:** 2026-07-27 (Cycle 3 — T2)
