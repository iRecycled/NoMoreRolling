# decompile.ps1
# Decompiles Gamble With Your Friends' Assembly-CSharp.dll into a versioned folder
#
# Usage (from game root in any terminal):
#   .\decompile.ps1
#   .\decompile.ps1 -Force        # re-decompile even if version already exists
#   .\decompile.ps1 -DiffOnly     # skip decompile, just show diff between two latest

param(
    [string]$GamePath    = ".",
    [string]$AppManifest = "c:\SteamLibrary\steamapps\appmanifest_3892270.acf",
    [switch]$Force,
    [switch]$DiffOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Paths
$GameDll     = "$GamePath\Gamble With Your Friends_Data\Managed\Assembly-CSharp.dll"
$VersionsDir = "$GamePath\.decompiled-versions"
$CurrentDir  = "$GamePath\.decompiled"

function Write-Header($text) {
    Write-Host ""
    Write-Host "--- $text ---" -ForegroundColor Cyan
}

function Write-Ok($text)   { Write-Host "  OK: $text" -ForegroundColor Green  }
function Write-Warn($text) { Write-Host "  WARN: $text" -ForegroundColor Yellow }
function Write-Fail($text) { Write-Host "  FAIL: $text" -ForegroundColor Red    }

# Check paths
Write-Header "Checking paths"

if (-not (Test-Path $AppManifest)) {
    Write-Fail "Steam app manifest not found: $AppManifest"
    exit 1
}

if (-not (Test-Path $GameDll)) {
    Write-Fail "Assembly-CSharp.dll not found: $GameDll"
    exit 1
}

Write-Ok "Paths verified"

# Get version info
Write-Header "Detecting version"

$manifestContent = Get-Content $AppManifest -Raw
if ($manifestContent -match '"buildid"\s+"(\d+)"') {
    $BuildId = $Matches[1]
} else {
    Write-Fail "Could not parse buildid from app manifest"
    exit 1
}

$DllHash = (Get-FileHash $GameDll -Algorithm MD5).Hash.Substring(0, 8)
$Version = "$BuildId-$DllHash"

Write-Ok "Steam Build ID: $BuildId"
Write-Ok "DLL Hash: $DllHash"
Write-Ok "Full Version: $Version"

$OutputDir = "$VersionsDir\$Version"

# Create versions directory
$null = New-Item -ItemType Directory -Force -Path $VersionsDir

# Migrate existing decompile if needed
if ((Test-Path $CurrentDir) -and -not (Test-Path "$VersionsDir\*")) {
    Write-Header "First run - archiving existing decompile"
    $unknownDir = "$VersionsDir\initial-unknown"
    Move-Item $CurrentDir $unknownDir
    Write-Ok "Archived to initial-unknown"
}

# Decompile if needed
if ((Test-Path $OutputDir) -and -not $Force -and -not $DiffOnly) {
    Write-Header "Decompilation"
    Write-Ok "Version $Version already decompiled - skipping (use -Force to redo)"
} elseif (-not $DiffOnly) {
    Write-Header "Decompiling Assembly-CSharp.dll"

    $ilspyCmd = Get-Command ilspycmd -ErrorAction SilentlyContinue
    if (-not $ilspyCmd) {
        Write-Host "  ilspycmd not found - installing..." -ForegroundColor Yellow
        dotnet tool install ilspycmd -g
        $env:PATH = "$env:USERPROFILE\.dotnet\tools;$env:PATH"
        $ilspyCmd = Get-Command ilspycmd -ErrorAction SilentlyContinue
        if (-not $ilspyCmd) {
            Write-Fail "ilspycmd still not found after install"
            exit 1
        }
    }
    Write-Ok "ilspycmd found"

    $null = New-Item -ItemType Directory -Force -Path $OutputDir

    Write-Host "  Decompiling (this takes 30-60 seconds)..." -ForegroundColor Cyan
    $timer = [System.Diagnostics.Stopwatch]::StartNew()
    ilspycmd $GameDll --project -o $OutputDir 2>&1 | Out-Null
    $timer.Stop()

    if ($LASTEXITCODE -ne 0) {
        Write-Fail "ilspycmd failed with code $LASTEXITCODE"
        exit 1
    }
    Write-Ok "Decompiled in $([int]$timer.Elapsed.TotalSeconds)s"
}

# Update current decompile folder
Write-Header "Updating .decompiled"
Write-Host "  Syncing from $OutputDir..."
robocopy $OutputDir $CurrentDir /MIR /NP /NFL /NDL | Out-Null
Write-Ok ".decompiled is now at version $Version"

Write-Header "Complete"
Write-Host "  Source code: $CurrentDir"
Write-Host "  All versions: $VersionsDir"
Write-Host ""
