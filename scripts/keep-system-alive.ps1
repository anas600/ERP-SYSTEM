# Sprint 52b — Keep the dev system alive.
# Both BE (5001) and FE (3000) need to be running for browser testing.
# If either dies, restart it. Run this in a background task or schedule it.

$ErrorActionPreference = 'Stop'
$BeDir = 'C:\Users\Anas\.minimax-agent\projects\ERP-Holding-sprint-21'
$BeLog = 'C:\Users\Anas\AppData\Local\Temp\sprint52b-be.log'
$BeErr = 'C:\Users\Anas\AppData\Local\Temp\sprint52b-be-err.log'
$FeLog = 'C:\Users\Anas\AppData\Local\Temp\sprint52b-fe.log'
$FeErr = 'C:\Users\Anas\AppData\Local\Temp\sprint52b-fe-err.log'

function Test-Port {
    param([int]$Port)
    $c = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
    return $null -ne $c
}

function Start-Be {
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Starting BE on 5001..."
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ASPNETCORE_URLS = 'http://localhost:5001'
    Start-Process -FilePath 'dotnet' `
        -ArgumentList 'run','--project',"$BeDir\src\backend\Host\ERP-SYSTEM.csproj" `
        -RedirectStandardOutput $BeLog -RedirectStandardError $BeErr -PassThru | Out-Null
}

function Start-Fe {
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Starting FE on 3000..."
    Start-Process -FilePath 'cmd' `
        -ArgumentList '/c',"cd $BeDir\src\frontend && npm start" `
        -RedirectStandardOutput $FeLog -RedirectStandardError $FeErr -PassThru | Out-Null
}

Write-Host "Keep-alive monitor started at $(Get-Date -Format 'HH:mm:ss')"
while ($true) {
    if (-not (Test-Port 5001)) {
        Write-Host "[$(Get-Date -Format 'HH:mm:ss')] BE is down. Restarting..."
        Start-Be
        Start-Sleep -Seconds 25
    }
    if (-not (Test-Port 3000)) {
        Write-Host "[$(Get-Date -Format 'HH:mm:ss')] FE is down. Restarting..."
        Start-Fe
        Start-Sleep -Seconds 15
    }
    Start-Sleep -Seconds 60
}
