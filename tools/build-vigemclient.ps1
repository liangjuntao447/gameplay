# Builds the OFFICIAL ViGEmClient.dll from source.
# ViGEmClient's CMakeLists has: option(ViGEmClient_DLL ... OFF).
# Setting -DViGEmClient_DLL=ON makes it build a SHARED library (ViGEmClient.dll)
# instead of the default STATIC .lib. Falls back to the ViGEmClientShared target.
param(
    [string]$OutDir = '.',
    [string]$SourceDir = ''
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$env:GIT_TERMINAL_PROMPT = '0'
$tmp = if ($env:RUNNER_TEMP) { $env:RUNNER_TEMP } else { $env:TEMP }

# --- obtain source ---
if (-not $SourceDir -or -not (Test-Path $SourceDir)) {
    $SourceDir = Join-Path $tmp 'ViGEmClient'
    if (-not (Test-Path $SourceDir)) {
        Write-Host "Cloning nefarius/ViGEmClient into $SourceDir ..."
        git clone --depth 1 https://github.com/nefarius/ViGEmClient.git $SourceDir
    }
}

$buildDir = Join-Path $SourceDir 'build'
$out = Join-Path $OutDir 'ViGEmClient.dll'

# --- primary: CMake with ViGEmClient_DLL=ON (builds a SHARED ViGEmClient.dll) ---
Write-Host '=== CMake configure: -DViGEmClient_DLL=ON ==='
cmake -B $buildDir -S $SourceDir -A x64 -DViGEmClient_DLL=ON 2>&1 | ForEach-Object { Write-Host "  $_" }
cmake --build $buildDir --config Release 2>&1 | ForEach-Object { Write-Host "  $_" }

$dll = Get-ChildItem $buildDir -Recurse -Filter 'ViGEmClient.dll' -ErrorAction SilentlyContinue | Select-Object -First 1

# --- fallback: the project also exposes a ViGEmClientShared target ---
if (-not $dll) {
    Write-Host '=== fallback: build ViGEmClientShared target ==='
    cmake --build $buildDir --config Release --target ViGEmClientShared 2>&1 | ForEach-Object { Write-Host "  $_" }
    $dll = Get-ChildItem $buildDir -Recurse -Filter 'ViGEmClientShared.dll' -ErrorAction SilentlyContinue | Select-Object -First 1
}

if (-not $dll) {
    Write-Host '=== no DLL produced; what was built: ==='
    Get-ChildItem $buildDir -Recurse -Include '*.dll','*.lib' -ErrorAction SilentlyContinue | ForEach-Object { Write-Host "  $($_.FullName)" }
    throw 'Could not produce ViGEmClient.dll'
}

Copy-Item $dll.FullName $out -Force
Write-Host "DLL built: $out (from $($dll.FullName))"
