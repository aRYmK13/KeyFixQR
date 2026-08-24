#Requires -Version 5.1
<#
  KeyFix QR build script
    1. Restore + test
    2. Publish self-contained win-x64 single-file
    3. Build portable ZIP
    4. Build Inno Setup installer (installs Inno Setup via winget if missing)

  Output: dist\KeyFixQR-Setup.exe , dist\KeyFixQR-Portable.zip
#>
param(
    [switch]$SkipTests,
    [switch]$SkipInstaller
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$dist = Join-Path $root 'dist'

Write-Host '=== KeyFix QR build ===' -ForegroundColor Cyan

Write-Host '[1/5] Restoring...' -ForegroundColor Yellow
dotnet restore "$root\KeyFixQR.sln"
if ($LASTEXITCODE -ne 0) { throw 'restore failed' }

if (-not $SkipTests) {
    Write-Host '[2/5] Running tests...' -ForegroundColor Yellow
    dotnet test "$root\tests\KeyFixQR.Tests" -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'tests failed' }
} else {
    Write-Host '[2/5] Tests skipped' -ForegroundColor DarkYellow
}

Write-Host '[3/5] Publishing self-contained win-x64...' -ForegroundColor Yellow
dotnet publish "$root\src\KeyFixQR" -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true -p:DebugType=None `
    -o "$dist\publish" --nologo
if ($LASTEXITCODE -ne 0) { throw 'publish failed' }

Write-Host '[4/5] Building portable ZIP...' -ForegroundColor Yellow
New-Item -ItemType Directory -Force -Path "$dist\portable-stage" | Out-Null
Copy-Item "$dist\publish\KeyFixQR.exe" "$dist\portable-stage\" -Force
Copy-Item "$dist\PORTABLE-README.txt" "$dist\portable-stage\" -Force
Compress-Archive -Force -Path "$dist\portable-stage\*" -DestinationPath "$dist\KeyFixQR-Portable.zip"

if (-not $SkipInstaller) {
    Write-Host '[5/5] Building installer...' -ForegroundColor Yellow
    $iscc = @(
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
        'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
        'C:\Program Files\Inno Setup 6\ISCC.exe'
    ) | Where-Object { Test-Path $_ } | Select-Object -First 1

    if (-not $iscc) {
        Write-Host '   Inno Setup not found; installing via winget...' -ForegroundColor DarkYellow
        winget install --id JRSoftware.InnoSetup -e --silent `
            --accept-source-agreements --accept-package-agreements
        if ($LASTEXITCODE -ne 0) { throw 'winget install of Inno Setup failed' }
        $iscc = "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
        if (-not (Test-Path $iscc)) { throw 'ISCC.exe still not found after install' }
    }

    & $iscc "$root\installer\keyfixqr.iss"
    if ($LASTEXITCODE -ne 0) { throw 'installer compile failed' }
}

Remove-Item "$dist\portable-stage" -Recurse -Force -ErrorAction SilentlyContinue

Write-Host ''
Write-Host '=== DONE ===' -ForegroundColor Green
Get-ChildItem $dist -File | ForEach-Object {
    Write-Host ("  {0}  ({1:N1} MB)" -f $_.Name, ($_.Length / 1MB))
}
