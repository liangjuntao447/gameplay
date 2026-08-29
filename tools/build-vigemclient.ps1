# Builds the OFFICIAL ViGEmClient.dll from source and copies it to $OutDir.
# $SourceDir should be a checkout of nefarius/ViGEmClient (the workflow fetches
# it with actions/checkout to avoid git auth prompts). If $SourceDir is empty
# it clones the public repo. vcpkg (microsoft) is only used as a fallback.
param(
    [string]$OutDir = '.',
    [string]$SourceDir = ''
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$env:GIT_TERMINAL_PROMPT = '0'   # never let git hang asking for credentials
$tmp = if ($env:RUNNER_TEMP) { $env:RUNNER_TEMP } else { $env:TEMP }

# --- obtain source ---
if (-not $SourceDir -or -not (Test-Path $SourceDir)) {
    $SourceDir = Join-Path $tmp 'ViGEmClient'
    if (-not (Test-Path $SourceDir)) {
        Write-Host "Cloning nefarius/ViGEmClient into $SourceDir ..."
        git clone --depth 1 https://github.com/nefarius/ViGEmClient.git $SourceDir
    }
}

$dll = $null

# --- Attempt 1: direct CMake build (ViGEmClient is a plain Windows library) ---
Write-Host '=== Attempt 1: direct CMake build ==='
try {
    cmake -B (Join-Path $SourceDir 'build') -S $SourceDir -A x64
    cmake --build (Join-Path $SourceDir 'build') --config Release
    $dll = Get-ChildItem (Join-Path $SourceDir 'build') -Recurse -Filter 'ViGEmClient.dll' -ErrorAction SilentlyContinue |
        Select-Object -First 1
} catch { Write-Host "direct CMake failed: $($_.Exception.Message)" }

# --- Attempt 2: build with microsoft/vcpkg toolchain (in case deps are needed) ---
if (-not $dll) {
    Write-Host '=== Attempt 2: CMake + microsoft/vcpkg toolchain ==='
    $vc = Join-Path $tmp 'vcpkg'
    if (-not (Test-Path (Join-Path $vc 'vcpkg.exe'))) {
        git clone --depth 1 https://github.com/microsoft/vcpkg.git $vc
        Push-Location $vc
        try { & .\bootstrap-vcpkg.bat } finally { Pop-Location }
    }
    cmake -B (Join-Path $SourceDir 'build2') -S $SourceDir -A x64 `
        -DCMAKE_TOOLCHAIN_FILE="$vc\scripts\buildsystems\vcpkg.cmake"
    cmake --build (Join-Path $SourceDir 'build2') --config Release
    $dll = Get-ChildItem (Join-Path $SourceDir 'build2') -Recurse -Filter 'ViGEmClient.dll' -ErrorAction SilentlyContinue |
        Select-Object -First 1
}

if (-not $dll) { throw 'Could not build ViGEmClient.dll (both attempts failed).' }

$dest = Join-Path $OutDir 'ViGEmClient.dll'
Copy-Item $dll.FullName $dest -Force
Write-Host "ViGEmClient.dll copied to $dest (from $($dll.FullName))"
