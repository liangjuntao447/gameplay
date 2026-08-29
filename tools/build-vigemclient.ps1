# Builds the OFFICIAL ViGEmClient.dll from source and copies it to $OutDir.
# $SourceDir should be a checkout of nefarius/ViGEmClient (fetched with
# actions/checkout). BUILD_SHARED_LIBS=ON forces a DLL (the default is a
# static .lib, which we don't want). vcpkg (microsoft) is only a fallback.
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

# --- Attempt 1: direct CMake build, as a SHARED (DLL) library ---
Write-Host '=== Attempt 1: direct CMake build (BUILD_SHARED_LIBS=ON) ==='
try {
    cmake -B (Join-Path $SourceDir 'build') -S $SourceDir -A x64 `
        -DBUILD_SHARED_LIBS=ON -DCMAKE_WINDOWS_EXPORT_ALL_SYMBOLS=ON
    cmake --build (Join-Path $SourceDir 'build') --config Release
    $dll = Get-ChildItem (Join-Path $SourceDir 'build') -Recurse -Filter 'ViGEmClient.dll' -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if (-not $dll) {
        Write-Host 'No ViGEmClient.dll in attempt 1; listing what was built:'
        Get-ChildItem (Join-Path $SourceDir 'build') -Recurse -Include '*.dll','*.lib','*.pdb' -ErrorAction SilentlyContinue |
            ForEach-Object { Write-Host "  $($_.FullName)" }
    }
} catch { Write-Host "direct CMake failed: $($_.Exception.Message)" }

# --- Attempt 2: CMake + microsoft/vcpkg toolchain, also shared ---
if (-not $dll) {
    Write-Host '=== Attempt 2: CMake + microsoft/vcpkg toolchain (BUILD_SHARED_LIBS=ON) ==='
    $vc = Join-Path $tmp 'vcpkg'
    if (-not (Test-Path (Join-Path $vc 'vcpkg.exe'))) {
        git clone --depth 1 https://github.com/microsoft/vcpkg.git $vc
        Push-Location $vc
        try { & .\bootstrap-vcpkg.bat } finally { Pop-Location }
    }
    cmake -B (Join-Path $SourceDir 'build2') -S $SourceDir -A x64 `
        -DBUILD_SHARED_LIBS=ON -DCMAKE_WINDOWS_EXPORT_ALL_SYMBOLS=ON `
        -DCMAKE_TOOLCHAIN_FILE="$vc\scripts\buildsystems\vcpkg.cmake"
    cmake --build (Join-Path $SourceDir 'build2') --config Release
    $dll = Get-ChildItem (Join-Path $SourceDir 'build2') -Recurse -Filter 'ViGEmClient.dll' -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if (-not $dll) {
        Write-Host 'No ViGEmClient.dll in attempt 2; listing what was built:'
        Get-ChildItem (Join-Path $SourceDir 'build2') -Recurse -Include '*.dll','*.lib','*.pdb' -ErrorAction SilentlyContinue |
            ForEach-Object { Write-Host "  $($_.FullName)" }
    }
}

if (-not $dll) { throw 'Could not build ViGEmClient.dll (both attempts failed). See the built-file listing above.' }

$dest = Join-Path $OutDir 'ViGEmClient.dll'
Copy-Item $dll.FullName $dest -Force
Write-Host "ViGEmClient.dll copied to $dest (from $($dll.FullName))"
