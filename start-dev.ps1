# ERP-SYSTEM — Start Dev Environment
# استعمل: .\start-dev.ps1
#
# هذا السكريبت يشغّل كل اللي تحتاجه لتطوير محلياً:
# 1. يتأكد PostgreSQL شغّال
# 2. يطلق الـ Backend (http://localhost:5000)
# 3. يطلق الـ Frontend (http://localhost:3000)
# 4. يفتح المتصفح تلقائياً على صفحة تسجيل الدخول

$ErrorActionPreference = 'Continue'
$ProjectRoot = $PSScriptRoot

Write-Host "=== ERP-SYSTEM Dev Launcher ===" -ForegroundColor Cyan
Write-Host ""

# --- 1. PostgreSQL ---
Write-Host "[1/4] التحقق من PostgreSQL..." -ForegroundColor Yellow
$pgService = Get-Service postgresql-x64-15 -ErrorAction SilentlyContinue
if ($null -eq $pgService) {
    Write-Host "  X خدمة PostgreSQL غير موجودة! ثبّت PostgreSQL 15 أولاً." -ForegroundColor Red
    exit 1
}
if ($pgService.Status -ne 'Running') {
    Write-Host "  -> تشغيل PostgreSQL..." -ForegroundColor Yellow
    Start-Service postgresql-x64-15
    Start-Sleep -Seconds 2
}
Write-Host "  OK PostgreSQL شغّال على :5432" -ForegroundColor Green

# --- 2. إيقاف العمليات القديمة ---
Write-Host "[2/4] تنظيف العمليات القديمة..." -ForegroundColor Yellow
Get-Process -Name "ERPSystem.Host" -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowTitle -eq "" -and $_.StartTime -lt (Get-Date).AddMinutes(-2) } | Stop-Process -Force -ErrorAction SilentlyContinue
Get-NetTCPConnection -LocalPort 5000 -State Listen -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue }
Get-NetTCPConnection -LocalPort 3000 -State Listen -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue }
Start-Sleep -Seconds 2
Write-Host "  OK تم تنظيف المنافذ 5000 و 3000" -ForegroundColor Green

# --- 3. Backend (.NET) ---
Write-Host "[3/4] تشغيل Backend (.NET على :5000)..." -ForegroundColor Yellow
$env:ASPNETCORE_ENVIRONMENT = "Development"
$backendDir = Join-Path $ProjectRoot "src\backend\Host"
if (-not (Test-Path (Join-Path $backendDir "bin\Debug\net9.0\ERPSystem.Host.dll"))) {
    Write-Host "  -> dotnet build (أول تشغيل، قد يستغرق 1-2 دقيقة)..." -ForegroundColor Yellow
    Push-Location $backendDir
    dotnet build 2>&1 | Select-Object -Last 5
    Pop-Location
}
Start-Process -FilePath "dotnet" -ArgumentList "run","--no-build","--urls=http://localhost:5000" `
    -WorkingDirectory $backendDir `
    -RedirectStandardOutput "$env:TEMP\backend.log" `
    -RedirectStandardError "$env:TEMP\backend.err.log" `
    -PassThru -NoNewWindow | Out-Null
Write-Host "  -> انتظار بدء الـ Backend..." -ForegroundColor Yellow
$ready = $false
for ($i = 1; $i -le 30; $i++) {
    Start-Sleep -Seconds 1
    try {
        $r = Invoke-WebRequest -Uri "http://localhost:5000/health" -UseBasicParsing -TimeoutSec 2
        if ($r.StatusCode -eq 200) { $ready = $true; break }
    } catch {}
}
if ($ready) {
    Write-Host "  OK Backend شغّال على http://localhost:5000" -ForegroundColor Green
} else {
    Write-Host "  X Backend لم يبدأ بعد 30 ثانية. افحص $env:TEMP\backend.err.log" -ForegroundColor Red
}

# --- 4. Frontend (Next.js) ---
Write-Host "[4/4] تشغيل Frontend (Next.js على :3000)..." -ForegroundColor Yellow
$frontendDir = Join-Path $ProjectRoot "src\frontend"
if (-not (Test-Path (Join-Path $frontendDir ".env.local"))) {
    "NEXT_PUBLIC_API_URL=http://localhost:5000" | Set-Content -Path (Join-Path $frontendDir ".env.local") -Encoding UTF8
}
Start-Process -FilePath "cmd" -ArgumentList "/c","npm run dev" `
    -WorkingDirectory $frontendDir `
    -RedirectStandardOutput "$env:TEMP\frontend.log" `
    -RedirectStandardError "$env:TEMP\frontend.err.log" `
    -PassThru -NoNewWindow | Out-Null
Write-Host "  -> انتظار بدء الـ Frontend..." -ForegroundColor Yellow
$readyFE = $false
for ($i = 1; $i -le 30; $i++) {
    Start-Sleep -Seconds 1
    try {
        $r = Invoke-WebRequest -Uri "http://localhost:3000" -UseBasicParsing -TimeoutSec 2
        if ($r.StatusCode -eq 200) { $readyFE = $true; break }
    } catch {}
}
if ($readyFE) {
    Write-Host "  OK Frontend شغّال على http://localhost:3000" -ForegroundColor Green
} else {
    Write-Host "  ! Frontend لم يبدأ بعد 30 ثانية (قد يحتاج وقت أطول). راجع $env:TEMP\frontend.log" -ForegroundColor Yellow
}

# --- ملخص ---
Write-Host ""
Write-Host "=== النظام جاهز ===" -ForegroundColor Green
Write-Host ""
Write-Host "الروابط:" -ForegroundColor Cyan
Write-Host "  Frontend:  http://localhost:3000"
Write-Host "  Swagger:   http://localhost:5000/swagger"
Write-Host "  Health:    http://localhost:5000/health/ready"
Write-Host ""
Write-Host "بيانات الدخول الجاهزة:" -ForegroundColor Cyan
Write-Host "  Email:     anas@demo.local"
Write-Host "  Password:  Demo1234"
Write-Host "  Tenant:    DemoCompany"
Write-Host ""
Write-Host "لفتح المتصفح تلقائياً:" -ForegroundColor Cyan
Write-Host "  Start-Process http://localhost:3000/login"
Write-Host ""
Write-Host "لإيقاف النظام: .\stop-dev.ps1" -ForegroundColor Yellow