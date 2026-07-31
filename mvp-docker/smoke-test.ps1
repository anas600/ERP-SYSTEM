#!/usr/bin/env pwsh
# MVP Docker Smoke Test (Sprint 13, Layer 2 of 3-Layer Model)
# Verifies the containerized MVP is working end-to-end.
# Per Anas 2026-07-31 21:51 UTC directive.
#
# Usage:  cd mvp-docker && ./smoke-test.ps1
# Exits 0 on full success, 1 on any failure.

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$API_URL = if ($env:API_URL) { $env:API_URL } else { "http://localhost:5000" }
$FRONTEND_URL = if ($env:FRONTEND_URL) { $env:FRONTEND_URL } else { "http://localhost:3000" }
$WAIT_TIMEOUT_SECONDS = if ($env:WAIT_TIMEOUT_SECONDS) { [int]$env:WAIT_TIMEOUT_SECONDS } else { 90 }

$results = @()
$fail = 0

function Test-Step {
    param([string]$Name, [scriptblock]$Block)
    Write-Host "→ $Name ..." -NoNewline
    try {
        $result = & $Block
        if ($result) {
            Write-Host " ✓" -ForegroundColor Green
            $script:results += @{Name=$Name; Status="OK"; Detail=$result}
        } else {
            Write-Host " ✗" -ForegroundColor Red
            $script:results += @{Name=$Name; Status="FAIL"; Detail=""}
            $script:fail++
        }
    } catch {
        Write-Host " ✗ ($($_.Exception.Message))" -ForegroundColor Red
        $script:results += @{Name=$Name; Status="FAIL"; Detail=$_.Exception.Message}
        $script:fail++
    }
}

Write-Host ""
Write-Host "=== MVP Smoke Test ===" -ForegroundColor Cyan
Write-Host "API:      $API_URL"
Write-Host "Frontend: $FRONTEND_URL"
Write-Host "Timeout:  ${WAIT_TIMEOUT_SECONDS}s"
Write-Host ""

# 1. Wait for API to be ready
Write-Host "⏳ Waiting for API to become ready..." -NoNewline
$waited = 0
$ready = $false
while ($waited -lt $WAIT_TIMEOUT_SECONDS) {
    try {
        $r = Invoke-WebRequest -Uri "$API_URL/api/health/live" -UseBasicParsing -TimeoutSec 3 -ErrorAction Stop
        if ($r.StatusCode -eq 200) {
            $ready = $true
            break
        }
    } catch {}
    Start-Sleep -Seconds 2
    $waited += 2
    Write-Host "." -NoNewline
}
if ($ready) {
    Write-Host " ✓ (${waited}s)" -ForegroundColor Green
} else {
    Write-Host " ✗ TIMEOUT after ${WAIT_TIMEOUT_SECONDS}s" -ForegroundColor Red
    $fail++
}

# 2. Health endpoints
Test-Step "Health: /api/health/live" {
    $r = Invoke-WebRequest -Uri "$API_URL/api/health/live" -UseBasicParsing -ErrorAction Stop
    return $r.StatusCode -eq 200
}

Test-Step "Health: /api/health/ready" {
    $r = Invoke-WebRequest -Uri "$API_URL/api/health/ready" -UseBasicParsing -ErrorAction Stop
    return $r.StatusCode -eq 200
}

# 3. Login API (admin user — created by DefaultHoldingBootstrapHostedService on first run)
Test-Step "API: POST /api/auth/login (bootstrap admin)" {
    $body = @{
        email = "admin@erp.local"
        password = "Admin1234!"
    } | ConvertTo-Json
    try {
        $r = Invoke-WebRequest -Uri "$API_URL/api/auth/login" -Method POST -Body $body -ContentType "application/json" -UseBasicParsing -ErrorAction Stop
        return $r.StatusCode -eq 200
    } catch {
        # 401 means the bootstrap admin isn't created yet (first run takes time) — retry once
        Start-Sleep -Seconds 5
        try {
            $r2 = Invoke-WebRequest -Uri "$API_URL/api/auth/login" -Method POST -Body $body -ContentType "application/json" -UseBasicParsing -ErrorAction Stop
            return $r2.StatusCode -eq 200
        } catch {
            return $false
        }
    }
}

# 4. Frontend serves content
Test-Step "Frontend: GET /" {
    try {
        $r = Invoke-WebRequest -Uri $FRONTEND_URL -UseBasicParsing -TimeoutSec 10 -ErrorAction Stop
        return $r.StatusCode -eq 200 -and $r.Content -match "(?i)html|<!doctype"
    } catch {
        return $false
    }
}

# 5. Database is clean (no test data from local-docker seed)
Test-Step "DB: no local-docker seed (companies table empty)" {
    $env:PGPASSWORD = if ($env:POSTGRES_PASSWORD) { $env:POSTGRES_PASSWORD } else { "erp_mvp_password" }
    $query = "SELECT count(*) FROM companies;"
    try {
        $result = docker exec erp-mvp-postgres psql -U erp -d erp_system -t -A -c $query 2>&1
        # 0 = clean, anything else (other than 0) = contamination
        return $result -match "^0$"
    } catch {
        return $false
    }
}

# 6. Swagger / OpenAPI is reachable
Test-Step "API: /swagger" {
    try {
        $r = Invoke-WebRequest -Uri "$API_URL/swagger" -UseBasicParsing -ErrorAction Stop
        return $r.StatusCode -in @(200, 301, 302)
    } catch {
        return $false
    }
}

# Summary
Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
foreach ($r in $results) {
    $color = if ($r.Status -eq "OK") { "Green" } else { "Red" }
    $icon = if ($r.Status -eq "OK") { "✓" } else { "✗" }
    Write-Host "  $icon $($r.Name) [$($r.Status)]" -ForegroundColor $color
}
Write-Host ""
if ($fail -eq 0) {
    Write-Host "✅ All checks passed. MVP is ready to browse." -ForegroundColor Green
    Write-Host "   Open: $FRONTEND_URL" -ForegroundColor Green
    Write-Host "   Login: admin@erp.local / Admin1234!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "❌ $fail check(s) failed. Check 'docker compose logs' for details." -ForegroundColor Red
    exit 1
}
