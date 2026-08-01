#Requires -Version 5.1
<#
.SYNOPSIS
    Watches the develop branch on origin and triggers a clean rebuild of mvp-docker
    whenever a new commit is detected.

.DESCRIPTION
    Sprint 15 — Per Anas 2026-08-01 03:35 UTC directive. This is the "watcher" half
    of the auto-rebuild pipeline:

        [cron every 5 min] ──► [watch-develop-and-rebuild.ps1] ──► [rebuild-mvp-docker.ps1]

    The script:
    1. Reads the current develop SHA from `git ls-remote origin develop`
    2. Reads the last seen SHA from .mavis/last-develop-sha
    3. If they're the same, exits silently (nothing to do)
    4. If they differ:
       a. Waits 10s and re-reads the SHA (stability check — avoids catching a SHA
          that's mid-merge or about to be force-pushed)
       b. If still the same as the first read, triggers the rebuild
       c. On rebuild success: updates .mavis/last-develop-sha + writes to log
       d. On rebuild failure: leaves the SHA file unchanged (so we retry next tick)

    Designed to be run by a cron every 5 minutes. Also safe to run manually.

.PARAMETER Force
    If set, ignore the last-develop-sha file and always trigger a rebuild. Useful
    for one-off manual testing.

.PARAMETER Quiet
    Suppress console output (still logs to .mavis/rebuild-log.txt).

.EXAMPLE
    # Cron-driven (every 5 min)
    powershell -File scripts/watch-develop-and-rebuild.ps1 -Quiet

.EXAMPLE
    # Force a rebuild regardless of SHA
    powershell -File scripts/watch-develop-and-rebuild.ps1 -Force

.EXAMPLE
    # Manually trigger and watch the output
    powershell -File scripts/watch-develop-and-rebuild.ps1

.NOTES
    - Run from the repo root (the script uses $RepoRoot for paths).
    - The script is idempotent: if it runs multiple times in quick succession with
      no new commits, only the first one does work.
    - State file: .mavis/last-develop-sha (touch this file or change its contents to
      force a re-rebuild even if the remote SHA is the same).
#>

[CmdletBinding()]
param(
    [switch]$Force,
    [switch]$Quiet
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# ============ Paths ============

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$RepoRoot = Resolve-Path (Join-Path $ScriptDir "..")
$RebuildScript = Join-Path $ScriptDir "rebuild-mvp-docker.ps1"
$NotifyScript = Join-Path $ScriptDir "notify-telegram.ps1"
$StateFile = Join-Path $RepoRoot ".mavis/last-develop-sha"
$LogFile = Join-Path $RepoRoot ".mavis/rebuild-log.txt"

# Ensure .mavis/ exists
$MavisDir = Join-Path $RepoRoot ".mavis"
if (-not (Test-Path $MavisDir)) {
    New-Item -ItemType Directory -Path $MavisDir -Force | Out-Null
}

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    $line = "[$timestamp] [watcher] [$Level] $Message"
    Add-Content -Path $LogFile -Value $line
    if (-not $Quiet) {
        switch ($Level) {
            "ERROR" { Write-Host $line -ForegroundColor Red }
            "WARN"  { Write-Host $line -ForegroundColor Yellow }
            "OK"    { Write-Host $line -ForegroundColor Green }
            default { Write-Host $line }
        }
    }
}

# ============ 1. Read current develop SHA ============

Push-Location $RepoRoot
try {
    $currentSha = git ls-remote origin develop 2>&1 | ForEach-Object { ($_ -split "`t")[0] } | Select-Object -First 1
} catch {
    Write-Log "git ls-remote failed: $($_.Exception.Message)" "ERROR"
    Pop-Location
    exit 1
}
Pop-Location

if ([string]::IsNullOrWhiteSpace($currentSha)) {
    Write-Log "Empty SHA from git ls-remote (network issue?)" "ERROR"
    exit 1
}

# ============ 2. Read last seen SHA ============

$lastSha = $null
if (Test-Path $StateFile) {
    $lastSha = (Get-Content $StateFile -Raw).Trim()
}

# ============ 3. Compare ============

if (-not $Force -and $currentSha -eq $lastSha) {
    if (-not $Quiet) {
        Write-Host "No new commits (SHA=$currentSha). Nothing to do." -ForegroundColor DarkGray
    }
    exit 0
}

# New SHA detected (or forced)
$reason = if ($Force) { "FORCED" } else { "new commit" }
Write-Log "Detected $reason on develop. SHA=$currentSha (was: $lastSha)" "INFO"

# ============ 4. Stability check: wait 10s and re-read ============

if (-not $Force) {
    Write-Log "Stability check: waiting 10s before triggering rebuild..." "INFO"
    Start-Sleep -Seconds 10
    Push-Location $RepoRoot
    try {
        $stableSha = git ls-remote origin develop 2>&1 | ForEach-Object { ($_ -split "`t")[0] } | Select-Object -First 1
    } catch {
        Write-Log "git ls-remote (stability check) failed: $($_.Exception.Message)" "ERROR"
        Pop-Location
        exit 1
    }
    Pop-Location

    if ($stableSha -ne $currentSha) {
        Write-Log "SHA changed during stability check (was $currentSha, now $stableSha). Skipping this tick — next tick will pick it up." "WARN"
        exit 0
    }
    Write-Log "SHA stable at $stableSha. Triggering rebuild." "INFO"
}

# ============ 5. Trigger rebuild ============

Write-Log "Calling rebuild-mvp-docker.ps1..." "INFO"
$rebuildStart = Get-Date

# Use Start-Process for clean exit-code capture (avoids PowerShell's stderr quirks).
# We pipe the rebuild script's output to the host so the operator sees it (the
# rebuild script already runs in -Quiet mode to suppress its own progress).
$tmpOut = [System.IO.Path]::GetTempFileName()
$tmpErr = [System.IO.Path]::GetTempFileName()
try {
    $proc = Start-Process -FilePath "powershell" -ArgumentList @(
        "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $RebuildScript, "-Quiet"
    ) -NoNewWindow -Wait -PassThru `
      -RedirectStandardOutput $tmpOut `
      -RedirectStandardError $tmpErr
    $rebuildExit = $proc.ExitCode
    $rebuildOutput = ""
    if (Test-Path $tmpOut) { $rebuildOutput += (Get-Content $tmpOut -Raw -ErrorAction SilentlyContinue) }
    if (Test-Path $tmpErr) { $rebuildOutput += "`n--- STDERR ---`n" + (Get-Content $tmpErr -Raw -ErrorAction SilentlyContinue) }
} finally {
    Remove-Item $tmpOut, $tmpErr -Force -ErrorAction SilentlyContinue
}
$rebuildDuration = ((Get-Date) - $rebuildStart).TotalSeconds

if ($rebuildExit -eq 0) {
    # Success: update state file
    try {
        Set-Content -Path $StateFile -Value $currentSha -NoNewline
        Write-Log "Rebuild succeeded in $([math]::Round($rebuildDuration, 1))s. State updated to $currentSha." "OK"
        Write-Log "Next time Anas opens http://localhost:3000, the latest develop is running." "OK"

        # Sprint 16: notify on success
        $shortSha = $currentSha.Substring(0, [Math]::Min(7, $currentSha.Length))
        $msg = "✅ Sprint 16 auto-rebuild: success in $([math]::Round($rebuildDuration, 1))s. SHA=$shortSha. Open http://localhost:3000"
        & $NotifyScript -Message $msg -Quiet
        # Notify failures are non-fatal (don't block the state update)

        exit 0
    } catch {
        Write-Log "Rebuild succeeded but Set-Content on state file FAILED: $($_.Exception.Message)" "ERROR"
        exit 4
    }
} else {
    Write-Log "Rebuild FAILED (exit=$rebuildExit) after $([math]::Round($rebuildDuration, 1))s. State NOT updated — will retry next tick." "ERROR"
    $lastLines = ($rebuildOutput -split "`n" | Select-Object -Last 20) -join "`n"
    Write-Log "Last 20 lines of rebuild output:`n$lastLines" "ERROR"

    # Sprint 16: notify on failure
    $shortSha = $currentSha.Substring(0, [Math]::Min(7, $currentSha.Length))
    $msg = "❌ Sprint 16 auto-rebuild: FAILED (exit=$rebuildExit) after $([math]::Round($rebuildDuration, 1))s. SHA=$shortSha. Will retry next tick. Check .mavis/rebuild-log.txt"
    & $NotifyScript -Message $msg -Quiet

    exit $rebuildExit
}
