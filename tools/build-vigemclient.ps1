# Builds the OFFICIAL ViGEmClient.dll from source and copies it to $OutDir.
# Used by the GitHub Actions workflow (and can be run locally with MSVC+CMake).
# Tries vcpkg (Nefarius fork) first, then a direct CMake build as a fallback.
param([string]$OutDir = '.')

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$tmp = if ($env:RUNNER_TEMP) { $env:RUNNER_TEMP } else { $env:TEMP }
$vc  = Join-Path $tmp 'vcpkg'

# --- ensure vcpkg (Nefarius fork, has the vigemclient port) ---
if (-not (Test-Path (Join-Path $vc 'vcpkg.exe'))) {
    Write-Host "Cloning nefarius/vcpkg into $vc ..."
    git clone --depth 1 https://github.com/nefarius/vcpkg.git $vc
    Push-Location $vc
    try { & .\bootstrap-vcpkg.bat } finally { Pop-Location }
}

$dll = $null

# --- Attempt 1: vcpkg port ---
Write-Host '=== Attempt 1: vcpkg install vigemclient:x64-windows ==='
try {
    & (Join-Path $vc 'vcpkg.exe') install vigemclient:x64-windows
} catch { Write-Host "vcpkg install error (ignored): $($_.Exception.Message)" }
$dll = Get-ChildItem (Join-Path $vc 'installed') -Recurse -Filter 'ViGEmClient.dll' -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match 'x64' } | Select-Object -First 1

# --- Attempt 2: direct CMake build of the source ---
if (-not $dll) {
    Write-Host '=== Attempt 2: direct CMake build ==='
    $src = Join-Path $tmp 'ViGEmClient'
    git clone --depth 1 --recurse-submodules https://github.com/nefarius/ViGEmClient.git $src
    cmake -B (Join-Path $src 'build') -S $src -A x64
    cmake --build (Join-Path $src 'build') --config Release
    $dll = Get-ChildItem (Join-Path $src 'build') -Recurse -Filter 'ViGEmClient.dll' -ErrorAction SilentlyContinue | Select-Object -First 1
}

if (-not $dll) { throw 'Could not build ViGEmClient.dll (vcpkg and direct CMake both failed).' }

$dest = Join-Path $OutDir 'ViGEmClient.dll'
Copy-Item $dll.FullName $dest -Force
Write-Host "ViGEmClient.dll copied to $dest (from $($dll.FullName))"
