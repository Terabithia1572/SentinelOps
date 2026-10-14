# ==============================================================================
# SentinelOps — PowerShell Database Snapshot Backup Script (Windows/Cross-Platform)
# ==============================================================================
[CmdletBinding()]
param(
    [string]$HostName = "localhost",
    [int]$Port = 5432,
    [string]$UserName = "postgres",
    [string]$Database = "sentinelops",
    [string]$OutputDir = "$PSScriptRoot\..\backups"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

$timestamp = (Get-Date).ToUniversalTime().ToString("yyyyMMdd_HHmmss")
$backupFile = Join-Path $OutputDir "sentinelops_$timestamp.dump"

$pgDump = "pg_dump"
$searchPaths = @(
    "C:\Program Files\PostgreSQL\18\bin\pg_dump.exe",
    "C:\Program Files\PostgreSQL\17\bin\pg_dump.exe",
    "C:\Program Files\PostgreSQL\16\bin\pg_dump.exe",
    "C:\Program Files\PostgreSQL\13\bin\pg_dump.exe"
)
foreach ($p in $searchPaths) {
    if (Test-Path $p) {
        $pgDump = $p
        break
    }
}

Write-Host "[INFO] Initiating backup of '$Database' using $pgDump..." -ForegroundColor Cyan
$sw = [System.Diagnostics.Stopwatch]::StartNew()

& $pgDump -h $HostName -p $Port -U $UserName -d $Database -F c -b -f $backupFile

$sw.Stop()
$fileSizeKb = [Math]::Round((Get-Item $backupFile).Length / 1KB, 2)
Write-Host "[SUCCESS] Backup complete in $($sw.ElapsedMilliseconds) ms. File: $backupFile ($fileSizeKb KB)" -ForegroundColor Green
