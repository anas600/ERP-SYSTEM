# Sprint 15 — Auto-rebuild mvp-docker on develop push

**Goal:** Per **Anas 2026-08-01 03:35 UTC** — automate the Layer 1→2 workflow. When a new commit lands on `develop`, automatically rebuild `mvp-docker` and run the smoke test. Notify on success/failure.

**Why:** Today the workflow is manual:
1. Local Team pushes PR → merges to develop
2. Admin Team sees the merge (via cron or manual check)
3. Admin Team runs `cd mvp-docker && docker compose up -d --build`
4. Admin Team runs `smoke-test.ps1`
5. Admin Team reports to Anas
6. Anas opens the browser

Auto-rebuild eliminates steps 2-5. The watcher detects the new commit, rebuilds, runs the smoke test, and writes the result to a log file + (future) Telegram notification. Anas only needs to open the browser.

**Branch:** `feature/sprint-15-auto-rebuild` (off `origin/develop @ 01b4223`).

---

## Design

### Components

| File | Purpose | Runs on |
|------|---------|---------|
| `scripts/rebuild-mvp-docker.ps1` | The actual rebuild: `docker compose down -v` + `up -d --build` + smoke test. Logs everything. | Windows (PowerShell) |
| `scripts/watch-develop-and-rebuild.ps1` | The watcher: polls `git ls-remote origin develop`, detects new SHA, calls the rebuild script. | Windows (PowerShell) |
| `.mavis/last-develop-sha` | State file: the last seen develop SHA. | (data file) |
| `.mavis/rebuild-log.txt` | Append-only log of every rebuild attempt. | (data file) |
| Cron `mvp-auto-rebuild-on-develop-push` | Runs the watcher every 5 min. | Mavis scheduler |

### Why PowerShell (not bash)?

The watcher drives **Docker Desktop on Windows** (which is what Mavis Local uses). PowerShell is the native shell on Windows. The existing `mvp-docker/smoke-test.ps1` is also PowerShell — consistency matters. The bash scripts in `scripts/` (e.g., `smoke-test.sh`) are for CI/Mac/Linux and not relevant to the local-machine auto-rebuild.

### Flow

```
[every 5 min — cron]
        │
        ▼
watch-develop-and-rebuild.ps1
        │
        ├── fetch origin develop (git ls-remote)
        ├── read .mavis/last-develop-sha
        ├── if SHA == last: exit (nothing to do)
        ├── write "rebuild started at <timestamp> for SHA <new>" to log
        ├── run rebuild-mvp-docker.ps1
        │       │
        │       ├── cd mvp-docker
        │       ├── docker compose down -v  (Layer 2 purity)
        │       ├── docker compose up -d --build
        │       ├── wait for /api/health/live (up to 90s)
        │       ├── run smoke-test.ps1
        │       └── return 0/1
        │
        ├── if rebuild succeeded: update .mavis/last-develop-sha
        ├── if rebuild failed: write to log, leave .mavis/last-develop-sha unchanged (so we retry next tick)
        └── exit
```

### Safety properties

1. **Idempotent on success**: if the rebuild succeeds, the SHA file is updated. Next tick sees the same SHA and does nothing.
2. **Self-healing on failure**: if the rebuild fails, the SHA file is NOT updated. Next tick (5 min later) sees the same new SHA and tries again. Eventually succeeds (or Anas notices in the log).
3. **No mid-merge rebuild**: the watcher waits 10s after detecting a new SHA before triggering the rebuild. If a new SHA comes in during the delay, the timer restarts. (Avoids catching a SHA that gets immediately replaced by a force-push.)
4. **Layer 2 purity**: `docker compose down -v` always — no incremental state. Every rebuild is a clean install.
5. **No background work**: the cron fires the watcher synchronously. If a rebuild is in progress, the next tick waits for it to finish (or for the new SHA to settle).

### What this does NOT do (out of scope for Sprint 15)

- Telegram notification (Sprint 16+)
- Auto-merge develop to main (forbidden by Constitution)
- Parallel rebuilds (Sprint 16+)
- Branch other than develop (only watches develop)
- Pre-merge rebuild (only post-merge, when the SHA is stable)

---

## Implementation order

1. `scripts/rebuild-mvp-docker.ps1` (the worker)
2. `scripts/watch-develop-and-rebuild.ps1` (the orchestrator)
3. Test the rebuild script manually (verify it works on current state)
4. Test the watcher manually (simulate a new SHA)
5. Set up the cron
6. Update `scripts/AGENTS.md` and `AGENTS.md` (Child DOX)
7. Update `CHANGELOG.md`

---

## Verification

- [ ] `rebuild-mvp-docker.ps1` exits 0 on a clean rebuild + passing smoke test
- [ ] `rebuild-mvp-docker.ps1` exits 1 on a failing smoke test (and leaves the containers running for debugging)
- [ ] `watch-develop-and-rebuild.ps1` is a no-op when the SHA is unchanged
- [ ] `watch-develop-and-rebuild.ps1` triggers the rebuild when the SHA changes
- [ ] The cron runs every 5 min without manual intervention
- [ ] `.mavis/rebuild-log.txt` accumulates one entry per rebuild attempt
- [ ] `.mavis/last-develop-sha` is updated only on success
- [ ] `dotnet build` for the BE is unaffected (no BE code changes)
- [ ] CI is unaffected (no new required checks added)

---

## Open questions for Anas (post-sprint, not blocking)

1. Should the watcher also work on the `main` branch? (Probably no — main is LOCKED per the branch architecture reset.)
2. Should the rebuild run during specific hours only (e.g., 08:00-22:00)? (Cron has `active_hours` support if needed.)
3. Telegram notification — bot token or user ID? (Defer to Sprint 16.)

---

_Last updated: 2026-08-01 by Mavis (Muhammad mode). Admin Team is the implementer._
