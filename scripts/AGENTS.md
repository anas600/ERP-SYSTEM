# 🤖 AGENTS.md — `scripts/` (Build / Utility Scripts)

> **Build & utility scripts for ERP-SYSTEM.** These scripts drive the 3-Layer Model (Sprint 13+) and orchestrate CI/MVP operations.

**Last updated:** 2026-08-01 (Sprint 15 — added the auto-rebuild scripts; created this AGENTS.md as part of the Child DOX Index entry that was previously "TO CREATE").

---

## Scope

This directory holds scripts that:
- **Drive CI / GitHub Actions** (`smoke-test.sh`, `workflow-test.sh`)
- **Operate on the local database** (`dbq.py`, `gen_seed_1year.js`)
- **Generate reports / matrices** (`generate-rbac-matrix.py`, `daily-status-report.py`)
- **Handle backups / retention** (`pg-dump.sh`, `r2-upload.sh`, `restore-from-r2.sh`, `retention-*.sh`)
- **Health probes / heartbeat** (`health-ping.sh`, `local-verify.sh`, `local-integration.sh`)
- **Visual tours** (`visual-tour.mjs`, `visual-tour-2.mjs`)
- **Misc utility** (`check_seed.js`, `dec053-policy-map.sh`, `data-archive-t2.sh`)

### Sprint 15 addition

| File | Type | Purpose |
|------|------|---------|
| `scripts/rebuild-mvp-docker.ps1` | PowerShell | Layer 2 (mvp-docker) clean rebuild + smoke test |
| `scripts/watch-develop-and-rebuild.ps1` | PowerShell | Poll develop branch, detect new SHA, trigger rebuild |

These two new scripts are **Windows PowerShell** (because they drive Docker Desktop on the Mavis Local machine). The existing scripts in this directory are mostly bash/Python/Node targeting Mac/Linux/CI. **Both worlds coexist**: bash scripts for CI/cloud, PowerShell for local Windows automation.

---

## Conventions

### Naming
- Bash / sh: `kebab-case.sh` (e.g., `smoke-test.sh`, `health-ping.sh`)
- Python: `kebab-case.py`
- Node: `kebab-case.js`
- PowerShell: `PascalCase.ps1` or `kebab-case.ps1` (Windows convention) — see the new Sprint 15 scripts for the chosen style.

### Shebang
- Bash: `#!/usr/bin/env bash`
- Python: `#!/usr/bin/env python3` (when executable)
- PowerShell: `#Requires -Version 5.1` or comment header only (not executable on Unix)

### Comments
- Each script starts with a top-comment block explaining: purpose, when to use, what it does NOT do, prerequisites, output.
- The block is mandatory (consistency for the team).

### State files
Scripts that maintain state (e.g., `last-develop-sha` for the watcher) write to `.mavis/` (the Mavis state directory), **not** into `scripts/`. `scripts/` is code-only.

### Logging
Scripts that produce multi-line output write to a dedicated log in `.mavis/` (e.g., `.mavis/rebuild-log.txt`). Stdout is for the operator who's running the script directly.

---

## How to run

Most scripts are designed to be run from the repo root:

```bash
./scripts/<name>.sh          # bash
python ./scripts/<name>.py   # python
node ./scripts/<name>.js     # node
powershell -File ./scripts/<name>.ps1  # powershell (from any shell)
```

A few scripts assume CWD = `scripts/` (the older ones). Read the top-comment block before running.

---

## Sprint 15 specifics — the auto-rebuild flow

The auto-rebuild is driven by **two PowerShell scripts + one cron**:

```powershell
# 1. Manual: rebuild the MVP right now
powershell -File scripts/rebuild-mvp-docker.ps1

# 2. Manual: check for new SHA and rebuild if needed
powershell -File scripts/watch-develop-and-rebuild.ps1

# 3. Cron: every 5 min, run the watcher
# (cron defined via Mavis scheduler — see mavis tool)
```

State files:
- `.mavis/last-develop-sha` — last seen develop SHA (touching this manually triggers a re-rebuild)
- `.mavis/rebuild-log.txt` — append-only log of every rebuild attempt

Safety: the watcher is a no-op when the SHA is unchanged. The rebuild script always does a clean install (`docker compose down -v`).

See `docs/workflow/sprint-15-auto-rebuild.md` for the full design.

---

## Child DOX

No child directories. `scripts/` is a flat namespace.

If we add a subdirectory (e.g., `scripts/windows/` for Windows-only PowerShell scripts in the future), it gets its own AGENTS.md.

---

_Last updated: 2026-08-01 by Mavis (Muhammad mode). DOX applied._
