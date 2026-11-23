# Builds dist\Purgely-Setup-<version>.exe
# Usage: powershell -ExecutionPolicy Bypass -File tools\build-installer.ps1
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$iscc = @(
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) { throw 'Inno Setup 6 not found. Install it with: winget install JRSoftware.InnoSetup' }

Write-Host '1/3 Running tests...'
dotnet test Purgely.slnx -c Release
if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }

Write-Host '2/3 Publishing self-contained single-file exe...'
if (Test-Path publish) { Remove-Item publish -Recurse -Force }
dotnet publish src/Purgely.App -c Release -r win-x64 --self-contained `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -o publish
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }

Write-Host '3/3 Compiling installer...'
& $iscc /Q installer\Purgely.iss
if ($LASTEXITCODE -ne 0) { throw 'Inno Setup failed.' }

Get-ChildItem dist\*.exe | Sort-Object LastWriteTime | Select-Object -Last 1 |
    ForEach-Object { Write-Host "Done: $($_.FullName) ($([math]::Round($_.Length / 1MB, 1)) MB)" }
