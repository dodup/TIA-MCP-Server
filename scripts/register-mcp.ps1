<#
.SYNOPSIS
    Registers the TIA Portal Openness V21 MCP server in Antigravity.
.DESCRIPTION
    1. Ensures tool JSON schemas are exported to ~/.gemini/antigravity/mcp/tia-portal-v21/
    2. Adds 'tia-portal-v21' to ~/.gemini/config/mcp_config.json
#>

param(
    [string]$ExePath = "$PSScriptRoot\..\bin\Release\net48\TiaOpennessMcp.exe"
)

$resolvedExe = [System.IO.Path]::GetFullPath($ExePath)

if (-not (Test-Path $resolvedExe)) {
    Write-Error "Executable not found at: $resolvedExe"
    Write-Host "Please build the project first: dotnet build -c Release"
    exit 1
}

$userProfile = [Environment]::GetFolderPath('UserProfile')
$configPath = Join-Path $userProfile ".gemini\config\mcp_config.json"
$mcpDir = Join-Path $userProfile ".gemini\antigravity\mcp\tia-portal-v21"

# 1. Export tool schemas directly to the antigravity MCP directory
Write-Host "Exporting tool schemas to $mcpDir..." -ForegroundColor Cyan
if (-not (Test-Path $mcpDir)) {
    New-Item -ItemType Directory -Path $mcpDir -Force | Out-Null
}
& $resolvedExe --export-schemas $mcpDir

# 2. Update mcp_config.json
Write-Host "Updating $configPath..." -ForegroundColor Cyan

if (Test-Path $configPath) {
    $rawJson = Get-Content $configPath -Raw
    $config = $rawJson | ConvertFrom-Json
} else {
    $config = [PSCustomObject]@{
        mcpServers = [PSCustomObject]@{}
    }
}

if (-not $config.mcpServers) {
    $config | Add-Member -MemberType NoteProperty -Name "mcpServers" -Value ([PSCustomObject]@{})
}

$tiaServerConfig = [PSCustomObject]@{
    command = $resolvedExe
    args = @()
}

$config.mcpServers | Add-Member -MemberType NoteProperty -Name "tia-portal-v21" -Value $tiaServerConfig -Force

$updatedJson = $config | ConvertTo-Json -Depth 20
Set-Content -Path $configPath -Value $updatedJson -Encoding UTF8

Write-Host "==========================================================" -ForegroundColor Green
Write-Host " SUCCESS: TIA Portal V21 MCP Server registered!" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
Write-Host " Server ID:  tia-portal-v21"
Write-Host " Command:    $resolvedExe"
Write-Host " Config:     $configPath"
Write-Host " Schemas:    $mcpDir"
Write-Host ""
