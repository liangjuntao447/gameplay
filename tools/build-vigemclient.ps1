# Builds the OFFICIAL ViGEmClient.dll from source.
# The default ViGEmClient target (with ViGEmClient_DLL=ON) produces a DLL with
# NO exports. The ViGEmClientShared target produces a DLL with the export
# configuration (its output is also named ViGEmClient.dll). So we build BOTH
# and then pick whichever DLL actually exports vigem_alloc (verified via dumpbin).
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

function Test-ViGEmExports($dllPath) {
    try {
        $out = (& dumpbin /exports $dllPath 2>&1 | Out-String)
        return $out -match 'vigem_alloc'
    } catch { return $false }
}

# --- configure + build BOTH targets ---
Write-Host '=== CMake configure: -DViGEmClient_DLL=ON ==='
cmake -B $buildDir -S $SourceDir -A x64 -DViGEmClient_DLL=ON 2>&1 | ForEach-Object { Write-Host "  $_" }

Write-Host '=== build default ViGEmClient target ==='
cmake --build $buildDir --config Release 2>&1 | ForEach-Object { Write-Host "  $_" }

Write-Host '=== build ViGEmClientShared target (has export config) ==='
cmake --build $buildDir --config Release --target ViGEmClientShared 2>&1 | ForEach-Object { Write-Host "  $_" }

# --- pick any DLL that actually exports vigem_alloc ---
Write-Host '=== scanning for a DLL that exports vigem_alloc ==='
$dll = $null
Get-ChildItem $buildDir -Recurse -Filter '*.dll' -ErrorAction SilentlyContinue | ForEach-Object {
    if (-not $dll -and (Test-ViGEmExports $_.FullName)) { $dll = $_ }
}

if (-not $dll) {
    Write-Host '=== no DLL with vigem_alloc export; what was built: ==='
    Get-ChildItem $buildDir -Recurse -Include '*.dll','*.lib' -ErrorAction SilentlyContinue | ForEach-Object { Write-Host "  $($_.FullName)" }
    throw 'No ViGEmClient DLL with a vigem_alloc export was produced.'
}

Copy-Item $dll.FullName $out -Force
Write-Host "DLL built: $out (from $($dll.FullName))"
Write-Host "exports vigem_alloc: $(Test-ViGEmExports $out)"
