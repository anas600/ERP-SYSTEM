#Requires -Version 5.1
<#
.SYNOPSIS
    Sends a message to the configured Telegram chat via the Mavis bot.

.DESCRIPTION
    Sprint 16 — Per Anas 2026-08-01 04:44 UTC directive. Sends a one-line
    notification to Anas's Telegram. Used by the auto-rebuild watcher to
    ping on success/failure.

    The bot token is read from the Mavis credentials file (auto-discovered).
    The chat ID is read from .mavis/telegram-chat.json (gitignored) or the
    Mavis agent config dir. Both can be overridden via -BotToken / -ChatId.

    Exit codes:
      0 = message sent successfully
      1 = bot token or chat ID not configured
      2 = Telegram API returned an error
      3 = network / HTTP error

.PARAMETER Message
    The message text to send. Markdown is NOT supported (kept simple for reliability).

.PARAMETER BotToken
    Optional override for the Telegram bot token. Default: auto-discover from
    C:\Users\Anas\.minimax\credentials\mavis\telegram.json

.PARAMETER ChatId
    Optional override for the Telegram chat ID. Default: auto-discover from
    .mavis/telegram-chat.json (gitignored) or
    C:\Users\Anas\.minimax\agents\mavis\config\telegram-chat.json

.PARAMETER Quiet
    Suppress console output.

.EXAMPLE
    # Send a simple message
    powershell -File scripts/notify-telegram.ps1 -Message "✅ Sprint 15 rebuild succeeded"

.EXAMPLE
    # With overrides
    powershell -File scripts/notify-telegram.ps1 -Message "Hello" -ChatId "2095951462"

.NOTES
    - The bot token is in the Mavis platform's credentials (gitignored, outside the repo).
    - The chat ID is per-user. To find yours: message the bot, then call
      https://api.telegram.org/bot<TOKEN>/getUpdates and look for the chat.id.
    - Rate limit: Telegram allows ~30 messages/sec to the same chat. We send at most
      a few per day, so this is a non-issue.

    PowerShell note: Telegram API may take a few seconds; we use a 10s timeout.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Message,
    [string]$BotToken,
    [string]$ChatId,
    [switch]$Quiet
)

# ============ Auto-discover bot token ============

if (-not $BotToken) {
    $candidates = @(
        "C:\Users\Anas\.minimax\credentials\mavis\telegram.json",
        "C:\Users\Anas\.minimax\telegram-channel.yaml"
    )
    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            try {
                if ($candidate -like "*.json") {
                    $BotToken = (Get-Content $candidate -Raw | ConvertFrom-Json).credentials.botToken
                } elseif ($candidate -like "*.yaml") {
                    # Simple YAML parse: look for "botToken: <value>"
                    $content = Get-Content $candidate -Raw
                    if ($content -match "botToken:\s*`"?([^`"\s]+)`"?") {
                        $BotToken = $Matches[1]
                    }
                }
                if ($BotToken) { break }
            } catch {
                # Try next candidate
            }
        }
    }
}

if (-not $BotToken) {
    Write-Host "Telegram bot token not found. Set it in C:\Users\Anas\.minimax\credentials\mavis\telegram.json" -ForegroundColor Red
    exit 1
}

# ============ Auto-discover chat ID ============

if (-not $ChatId) {
    $candidates = @(
        # Project-local (gitignored)
        (Join-Path (Get-Location) ".mavis/telegram-chat.json"),
        # Agent-level (cross-project)
        "C:\Users\Anas\.minimax\agents\mavis\config\telegram-chat.json"
    )
    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            $ChatId = (Get-Content $candidate -Raw).Trim()
            if ($ChatId) { break }
        }
    }
    # Try env var as last resort
    if (-not $ChatId -and $env:Mavis_TELEGRAM_CHAT_ID) {
        $ChatId = $env:Mavis_TELEGRAM_CHAT_ID
    }
}

if (-not $ChatId) {
    Write-Host "Telegram chat ID not found. Create .mavis/telegram-chat.json with the chat ID." -ForegroundColor Red
    exit 1
}

# ============ Send the message ============

$uri = "https://api.telegram.org/bot$BotToken/sendMessage"
try {
    $response = Invoke-RestMethod -Uri $uri -Method Post -Body @{
        chat_id = $ChatId
        text    = $Message
    } -TimeoutSec 10 -ErrorAction Stop

    if ($response.ok) {
        if (-not $Quiet) {
            Write-Host "✅ Telegram message sent (chat_id=$ChatId)" -ForegroundColor Green
        }
        exit 0
    } else {
        Write-Host "Telegram API returned ok=false: $($response.description)" -ForegroundColor Red
        exit 2
    }
} catch {
    Write-Host "Telegram send failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 3
}
