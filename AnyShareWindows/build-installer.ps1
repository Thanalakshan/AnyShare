#Requires -Version 5.1
$ErrorActionPreference = "Stop"

$projectRoot = $PSScriptRoot
$projectFile = Join-Path $projectRoot "AnyShareWindows.csproj"
$publishDir = Join-Path $projectRoot "publish\win-x64"
$installerScript = Join-Path $projectRoot "installer\AnyShareSetup.iss"
$installerOutput = Join-Path $projectRoot "installer\output"

function Find-InnoSetupCompiler {
    $candidates = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
    )

    foreach ($path in $candidates) {
        if (Test-Path $path) {
            return $path
        }
    }

    return $null
}

Write-Host "Publishing AnyShare for Windows x64..." -ForegroundColor Cyan

if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

dotnet publish $projectFile `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishReadyToRun=true `
    -o $publishDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed."
}

$iscc = Find-InnoSetupCompiler
if (-not $iscc) {
    throw @"
Inno Setup 6 was not found.

Install it from https://jrsoftware.org/isinfo.php
Then run this script again to create installer\output\AnyShare-Setup-1.0.0.exe
"@
}

Write-Host "Building compressed installer..." -ForegroundColor Cyan

if (Test-Path $installerOutput) {
    Remove-Item $installerOutput -Recurse -Force
}

& $iscc $installerScript

if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup build failed."
}

$setupExe = Get-ChildItem -Path $installerOutput -Filter "AnyShare-Setup-*.exe" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $setupExe) {
    throw "Installer executable was not created."
}

Write-Host ""
Write-Host "Installer created:" -ForegroundColor Green
Write-Host $setupExe.FullName
