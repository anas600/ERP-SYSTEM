#Requires -Version 5.1
<#
.SYNOPSIS
    Rebuilds the mvp-docker stack (Layer 2 of the 3-Layer Model) from a clean state
    and runs the 8-check smoke test.

.DESCRIPTION
    Sprint 15 — Per Anas 2026-08-01 03:35 UTC directive. This is the "worker" half
    of the auto-rebuild pipeline:

        [watcher] ──► [rebuild-mvp-docker.ps1] ──► log + state update

    The script:
    1. Tears down the existing mvp-docker stack (including the volume — Layer 2 purity)
    2. Rebuilds the images (no cache) so any code change in develop is picked up
    3. Brings the stack up
    4. Waits for the API to be ready (up to 90s)
    5. Runs the 8-check smoke test (smoke-test.ps1)
    6. Writes the result to .mavis/rebuild-log.txt

    Exit codes:
      0 = rebuild + smoke test succeeded
      1 = smoke test failed (containers are left running for debugging)
      2 = rebuild itself failed (docker compose up failed)
      3 = docker is not running / not installed

.PARAMETER SkipSmokeTest
    If set, skip the smoke test. Useful when you just want to rebuild and inspect manually.

.PARAMETER SkipDown
    If set, skip the `docker compose down -v`. Use only when debugging — violates the
    "Layer 2 purity" principle.

.EXAMPLE
    # Standard rebuild + smoke test
    powershell -File scripts/rebuild-mvp-docker.ps1

.EXAMPLE
    # Rebuild only (no smoke test) — for debugging
    powershell -File scripts/rebuild-mvp-docker.ps1 -SkipSmokeTest

.EXAMPLE
    # Used by the watcher: rebuild + log
    powershell -File scripts/rebuild-mvp-docker.ps1 -Quiet

.NOTES
    - Run from the repo root.
    - Docker Desktop must be running.
    - The first build of a fresh image can take 15-20 minutes (NuGet + npm caches are cold).
      Subsequent rebuilds of cached layers are much faster.
    - The smoke test waits up to 90s for the API to be ready (the first run after a
      clean install includes the bootstrap, which takes ~20-30s).

    PowerShell note: docker compose writes progress to stderr, which trips $ErrorActionPreference.
    We use Start-Process to capture both streams cleanly without triggering PowerShell's
    "NativeCommandError" handling.
#>

[CmdletBinding()]
param(
    [switch]$SkipSmokeTest,
    [switch]$SkipDown,
    [switch]$Quiet
)

# Note: we deliberately do NOT set $ErrorActionPreference = "Stop" — docker compose
# writes progress to stderr and we don't want that to abort the script.

# ============ Paths ============

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$RepoRoot = Resolve-Path (Join-Path $ScriptDir "..")
$MvpDir = Join-Path $RepoRoot "mvp-docker"
$SmokeTest = Join-Path $MvpDir "smoke-test.ps1"
$LogFile = Join-Path $RepoRoot ".mavis/rebuild-log.txt"

# Ensure .mavis/ exists
$MavisDir = Join-Path $RepoRoot ".mavis"
if (-not (Test-Path $MavisDir)) {
    New-Item -ItemType Directory -Path $MavisDir -Force | Out-Null
}

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    $line = "[$timestamp] [$Level] $Message"
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

# Helper: run a docker command and capture exit code + combined output.
# Uses Start-Process to avoid PowerShell's NativeCommandError trip on stderr.
function Invoke-DockerCompose {
    param(
        [Parameter(Mandatory)][string[]]$Arguments
    )
    $tmpOut = [System.IO.Path]::GetTempFileName()
    $tmpErr = [System.IO.Path]::GetTempFileName()
    try {
        $proc = Start-Process -FilePath "docker" -ArgumentList $Arguments `
            -NoNewWindow -Wait -PassThru `
            -RedirectStandardOutput $tmpOut `
            -RedirectStandardError $tmpErr
        $exitCode = $proc.ExitCode
        $combined = ""
        if (Test-Path $tmpOut) { $combined += (Get-Content $tmpOut -Raw -ErrorAction SilentlyContinue) }
        if (Test-Path $tmpErr) { $combined += (Get-Content $tmpErr -Raw -ErrorAction SilentlyContinue) }
        return @{ ExitCode = $exitCode; Output = $combined }
    } finally {
        Remove-Item $tmpOut, $tmpErr -Force -ErrorAction SilentlyContinue
    }
}

# ============ 1. Prerequisite check ============

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Log "Docker is not installed or not on PATH" "ERROR"
    exit 3
}

$dockerInfo = & docker info 2>&1 | Out-String
if ($LASTEXITCODE -ne 0) {
    Write-Log "Docker Desktop is not running. Start it manually and retry." "ERROR"
    exit 3
}

# ============ 2. Tear down (if not skipped) ============

if (-not $SkipDown) {
    Write-Log "Tearing down mvp-docker stack (clean install)..." "INFO"
    Push-Location $MvpDir
    $result = Invoke-DockerCompose -Arguments @("compose", "down", "-v", "--remove-orphans")
    Pop-Location
    if ($result.ExitCode -ne 0) {
        Write-Log "docker compose down FAILED (exit=$($result.ExitCode)). Output: $($result.Output)" "ERROR"
        exit 2
    }
    Write-Log "docker compose down completed (exit=$($result.ExitCode))" "INFO"
} else {
    Write-Log "SkipDown=true — keeping existing containers" "WARN"
}

# ============ 3. Build + up ============

Write-Log "Building + starting mvp-docker stack (15-20 min first run, <2 min cached)..." "INFO"
Push-Location $MvpDir
$result = Invoke-DockerCompose -Arguments @("compose", "up", "-d", "--build")
Pop-Location
if ($result.ExitCode -ne 0) {
    Write-Log "docker compose up FAILED (exit=$($result.ExitCode)). Output: $($result.Output)" "ERROR"
    exit 2
}
Write-Log "docker compose up completed (exit=$($result.ExitCode))" "INFO"

# ============ 4. Wait for API (smoke test does its own wait, but log it) ============

if ($SkipSmokeTest) {
    Write-Log "SkipSmokeTest=true — skipping smoke test" "WARN"
    Write-Log "Rebuild done. Containers are running. Use 'docker compose logs' to inspect." "OK"
    exit 0
}

# ============ 5. Smoke test ============

Write-Log "Running smoke test (8 checks)..." "INFO"
Push-Location $MvpDir
& $SmokeTest
$smokeExit = $LASTEXITCODE
Pop-Location

if ($smokeExit -eq 0) {
    Write-Log "Smoke test passed. Layer 2 is ready to browse." "OK"
    exit 0
} else {
    Write-Log "Smoke test FAILED (exit=$smokeExit). Containers are left running for debugging." "ERROR"
    exit 1
}
