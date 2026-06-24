# ERP-SYSTEM — Stop Dev Environment
# استعمل: .\stop-dev.ps1

Write-Host "=== إيقاف ERP-SYSTEM ===" -ForegroundColor Yellow

# إيقاف Backend
Get-Process -Name "ERPSystem.Host" -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object {
    $cmdLine = (Get-CimInstance Win32_Process -Filter "ProcessId = $($_.Id)" -ErrorAction SilentlyContinue).CommandLine
    $cmdLine -like "*ERPSystem*"
} | Stop-Process -Force

# إيقاف Frontend
Get-Process -Name "node" -ErrorAction SilentlyContinue | Where-Object {
    $cmdLine = (Get-CimInstance Win32_Process -Filter "ProcessId = $($_.Id)" -ErrorAction SilentlyContinue).CommandLine
    $cmdLine -like "*next*" -or $cmdLine -like "*npm*"
} | Stop-Process -Force

Get-NetTCPConnection -LocalPort 5000 -State Listen -ErrorAction SilentlyContinue | ForEach-Object {
    Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue
}
Get-NetTCPConnection -LocalPort 3000 -State Listen -ErrorAction SilentlyContinue | ForEach-Object {
    Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue
}

Start-Sleep -Seconds 2

$still5000 = (Test-NetConnection -ComputerName localhost -Port 5000 -InformationLevel Quiet -WarningAction SilentlyContinue) -eq $true
$still3000 = (Test-NetConnection -ComputerName localhost -Port 3000 -InformationLevel Quiet -WarningAction SilentlyContinue) -eq $true

if (-not $still5000 -and -not $still3000) {
    Write-Host "OK تم إيقاف كل العمليات على :5000 و :3000" -ForegroundColor Green
} else {
    Write-Host "! بعض العمليات لا تزال شغّالة. أعد تشغيل السكريبت أو أعد تشغيل الجهاز." -ForegroundColor Yellow
}

# PostgreSQL يبقى شغّال عمداً (StartType=Automatic)
Write-Host ""
Write-Host "PostgreSQL بقي شغّالاً (للحفاظ على البيانات)." -ForegroundColor Cyan
Write-Host "لإيقافه: Stop-Service postgresql-x64-15"