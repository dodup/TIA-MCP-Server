<#
.SYNOPSIS
    Registers TiaOpennessMcp.exe in Siemens TIA Portal Openness AllowList.
.DESCRIPTION
    Siemens TIA Portal Openness requires external applications to be whitelisted in
    the Windows Registry under HKLM:\SOFTWARE\Siemens\Automation\Openness\AllowList.
    This script computes the SHA-256 hash of TiaOpennessMcp.exe and registers it,
    preventing security popups and connection blocks.
#>

param(
    [string]$ExePath = "$PSScriptRoot\..\bin\Release\net48\TiaOpennessMcp.exe"
)

$resolvedPath = [System.IO.Path]::GetFullPath($ExePath)

if (-not (Test-Path $resolvedPath)) {
    Write-Error "Executable not found at: $resolvedPath"
    Write-Host "Please build the project first: dotnet build -c Release"
    exit 1
}

# Check for Administrator privileges
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "Elevating privileges to register in HKLM registry..." -ForegroundColor Cyan
    Start-Process powershell.exe -Verb RunAs -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`" -ExePath `"$resolvedPath`""
    exit 0
}

try {
    $exeName = [System.IO.Path]::GetFileName($resolvedPath)
    $bytes = [System.IO.File]::ReadAllBytes($resolvedPath)
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    $hash = [Convert]::ToBase64String($sha256.ComputeHash($bytes))
    $dateModified = (Get-Item $resolvedPath).LastWriteTime.ToString("yyyy/MM/dd HH:mm:ss.fff")

    $regPath = "HKLM:\SOFTWARE\Siemens\Automation\Openness\AllowList\$exeName"
    if (-not (Test-Path $regPath)) {
        New-Item -Path $regPath -Force | Out-Null
    }

    $entryPath = "$regPath\Entry"
    if (-not (Test-Path $entryPath)) {
        New-Item -Path $entryPath -Force | Out-Null
    }

    Set-ItemProperty -Path $entryPath -Name "Path" -Value $resolvedPath
    Set-ItemProperty -Path $entryPath -Name "DateModified" -Value $dateModified
    Set-ItemProperty -Path $entryPath -Name "FileHash" -Value $hash

    Write-Host "==========================================================" -ForegroundColor Green
    Write-Host " SUCCESS: Registered in Siemens TIA Portal Openness AllowList!" -ForegroundColor Green
    Write-Host "==========================================================" -ForegroundColor Green
    Write-Host " Executable:   $resolvedPath"
    Write-Host " DateModified: $dateModified"
    Write-Host " FileHash:     $hash"
    Write-Host " Registry:     $entryPath"
    Write-Host ""
} catch {
    Write-Error "Failed to register in Openness AllowList: $_"
    exit 1
}
