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
#    Sprint 14: admin user is created by the bootstrap service (no manual seed in smoke test).
#    Credentials match the defaults in .env.example (BOOTSTRAP_DEFAULT_ADMIN_EMAIL / PASSWORD).
$ADMIN_EMAIL = if ($env:BOOTSTRAP_DEFAULT_ADMIN_EMAIL) { $env:BOOTSTRAP_DEFAULT_ADMIN_EMAIL } else { "admin@erp.local" }
$ADMIN_PASSWORD = if ($env:BOOTSTRAP_DEFAULT_ADMIN_PASSWORD) { $env:BOOTSTRAP_DEFAULT_ADMIN_PASSWORD } else { "ChangeMe1234!" }

Test-Step "API: POST /api/auth/login (bootstrap admin)" {
    $body = @{
        email = $ADMIN_EMAIL
        password = $ADMIN_PASSWORD
    } | ConvertTo-Json
    try {
        $r = Invoke-WebRequest -Uri "$API_URL/api/auth/login" -Method POST -Body $body -ContentType "application/json" -UseBasicParsing -ErrorAction Stop
        return $r.StatusCode -eq 200
    } catch {
        # 401 may mean the bootstrap admin isn't created yet (first run takes time) — retry once
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

# 5. Database is "clean" (1 company = bootstrap Holding, no local-docker seed data, +bootstrap admin)
Test-Step "DB: clean (companies = 1 bootstrap Holding, no seed data)" {
    $env:PGPASSWORD = if ($env:POSTGRES_PASSWORD) { $env:POSTGRES_PASSWORD } else { "erp_mvp_password" }
    try {
        $companies = (docker exec erp-mvp-postgres psql -U erp -d erp_system -t -A -c "SELECT count(*) FROM companies;" 2>&1).Trim()
        # Exactly 1 company (the bootstrap Holding)
        return $companies -eq "1"
    } catch {
        return $false
    }
}

# 6. Sprint 14: bootstrap admin user was created by the env-driven service (no manual SQL)
Test-Step "DB: bootstrap admin user exists (no manual seed)" {
    $env:PGPASSWORD = if ($env:POSTGRES_PASSWORD) { $env:POSTGRES_PASSWORD } else { "erp_mvp_password" }
    try {
        $count = (docker exec erp-mvp-postgres psql -U erp -d erp_system -t -A -c "SELECT count(*) FROM users WHERE email = '$ADMIN_EMAIL';" 2>&1).Trim()
        return $count -ge "1"
    } catch {
        return $false
    }
}

# 7. Swagger is NOT reachable in Production (intentionally disabled for security)
Test-Step "API: Swagger disabled in Production (intentional)" {
    try {
        $r = Invoke-WebRequest -Uri "$API_URL/swagger" -UseBasicParsing -ErrorAction Stop
        # Should return 404 or redirect (not 200) — Swagger is Development-only
        return $r.StatusCode -ne 200
    } catch {
        # 404 (or connection error) is the expected behavior
        return $true
    }
}

# 8. Sprint 14 P0c: admin user has Admin role + dashboard returns 200 (not 403)
#    Regression guard: if the Admin role is missing from user_roles, /api/dashboard/summary
#    returns 403 (ReadAccess policy requires Admin/Accountant/ProjectManager/Viewer).
#    This catches: forgot to assign role, JWT role claim missing, role name typo, etc.
Test-Step "API: dashboard/summary returns 200 (Admin role assigned)" {
    try {
        $body = @{
            email = $ADMIN_EMAIL
            password = $ADMIN_PASSWORD
        } | ConvertTo-Json
        $loginResp = Invoke-RestMethod -Uri "$API_URL/api/auth/login" -Method POST -Body $body -ContentType "application/json" -UseBasicParsing -ErrorAction Stop
        $token = $loginResp.accessToken
        $holdingId = $loginResp.holdingCompanyId
        if (-not $token) { return $false }

        $headers = @{ Authorization = "Bearer $token" }
        if ($holdingId) { $headers["X-Company-Id"] = $holdingId }

        $dashResp = Invoke-WebRequest -Uri "$API_URL/api/dashboard/summary" -Headers $headers -UseBasicParsing -ErrorAction Stop
        return $dashResp.StatusCode -eq 200
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
    Write-Host "   Login: $ADMIN_EMAIL / (env: BOOTSTRAP_DEFAULT_ADMIN_PASSWORD)" -ForegroundColor Green
    exit 0
} else {
    Write-Host "❌ $fail check(s) failed. Check 'docker compose logs' for details." -ForegroundColor Red
    exit 1
}
