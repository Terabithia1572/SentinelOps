# ==============================================================================
# SentinelOps — PowerShell Database Restore Script (Windows/Cross-Platform)
# ==============================================================================
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$BackupFile,
    [string]$HostName = "localhost",
    [int]$Port = 5432,
    [string]$UserName = "postgres",
    [string]$TargetDatabase = "sentinelops_restored"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $BackupFile)) {
    Write-Error "Backup file '$BackupFile' does not exist."
    exit 1
}

$pgRestore = "pg_restore"
$psql = "psql"
$searchPaths = @(
    "C:\Program Files\PostgreSQL\18\bin",
    "C:\Program Files\PostgreSQL\17\bin",
    "C:\Program Files\PostgreSQL\16\bin",
    "C:\Program Files\PostgreSQL\13\bin"
)
foreach ($dir in $searchPaths) {
    if (Test-Path "$dir\pg_restore.exe") {
        $pgRestore = "$dir\pg_restore.exe"
        $psql = "$dir\psql.exe"
        break
    }
}

Write-Host "[INFO] Creating target database '$TargetDatabase'..." -ForegroundColor Cyan
& $psql -h $HostName -p $Port -U $UserName -d postgres -c "CREATE DATABASE `"$TargetDatabase`";" 2>$null

Write-Host "[INFO] Restoring '$BackupFile' into '$TargetDatabase' using $pgRestore..." -ForegroundColor Cyan
$sw = [System.Diagnostics.Stopwatch]::StartNew()

& $pgRestore -h $HostName -p $Port -U $UserName -d $TargetDatabase --no-owner --clean --if-exists $BackupFile

$sw.Stop()
Write-Host "[SUCCESS] Restore complete in $($sw.ElapsedMilliseconds) ms into '$TargetDatabase'." -ForegroundColor Green
