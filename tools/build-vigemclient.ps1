# Builds the OFFICIAL ViGEmClient.dll from source, FORCING the vigem_* exports.
# Strategy (most reliable): let CMake compile the source into a STATIC
# ViGEmClient.lib (CMake knows all includes/defines), then link that .lib into
# a DLL with link.exe using a .def generated from the ACTUAL symbol names in the
# .lib (read via dumpbin). This does not depend on the source's export macro or
# on guessing include paths.
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

function Test-ViGEmExports($p) {
    try { return ((& dumpbin /exports $p 2>&1 | Out-String) -match 'vigem_alloc') } catch { return $false }
}

# --- 1) CMake compiles the source into a STATIC .lib (default: static target) ---
Write-Host '=== CMake build (static ViGEmClient.lib) ==='
cmake -B $buildDir -S $SourceDir -A x64 2>&1 | ForEach-Object { Write-Host "  $_" }
cmake --build $buildDir --config Release 2>&1 | ForEach-Object { Write-Host "  $_" }
$lib = Get-ChildItem $buildDir -Recurse -Filter 'ViGEmClient.lib' -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match 'Release' } | Select-Object -First 1
if (-not $lib) {
    Write-Host '=== no ViGEmClient.lib produced; what was built: ==='
    Get-ChildItem $buildDir -Recurse -Include '*.lib','*.dll' -ErrorAction SilentlyContinue | ForEach-Object { Write-Host "  $($_.FullName)" }
    throw 'CMake did not produce ViGEmClient.lib'
}
Write-Host "Static lib: $($lib.FullName)"

# --- 2) read the ACTUAL vigem_* symbol names from the .lib ---
Write-Host '=== reading vigem_* symbols from the .lib ==='
$names = (& dumpbin /symbols $lib.FullName 2>&1 |
        Select-String 'External.*vigem' |
        ForEach-Object { ($_.Line -split '\|')[1].Trim() } |
        Select-Object -Unique)
if (-not $names) {
    Write-Host 'No vigem_* symbols found via dumpbin; using plain-name fallback list.'
    $names = @(
        'vigem_alloc','vigem_connect','vigem_disconnect','vigem_free',
        'vigem_target_add','vigem_target_remove','vigem_target_free',
        'vigem_target_x360_alloc','vigem_target_x360_update'
    )
}
Write-Host "Symbols: $($names.Count)"
$names | ForEach-Object { Write-Host "  $_" }

# --- 3) generate .def from those names ---
$defBody = 'LIBRARY ViGEmClient' + "`r`n" + 'EXPORTS' + "`r`n" +
    (($names | ForEach-Object { '  ' + $_ }) -join "`r`n")
$defFile = Join-Path $tmp 'vigem_exports.def'
Set-Content -Path $defFile -Value $defBody -Encoding Ascii
Write-Host "def: $defFile"

# --- 4) link the .lib into a DLL with the .def ---
Write-Host '=== link ViGEmClient.lib -> ViGEmClient.dll ==='
& link.exe /DLL "/DEF:$defFile" $lib.FullName setupapi.lib advapi32.lib ole32.lib "/OUT:$out" 2>&1 |
    ForEach-Object { Write-Host "  $_" }

if (Test-ViGEmExports $out) {
    Write-Host "DLL built with exports: $out"
    Write-Host "exports vigem_alloc: True"
    exit 0
}

# --- diagnostics ---
Write-Host '=== diagnostics: dumpbin /exports of built DLL ==='
if (Test-Path $out) { & dumpbin /exports $out 2>&1 | ForEach-Object { Write-Host "  $_" } }
Write-Host '=== diagnostics: dumpbin /symbols (sample) of the lib ==='
(& dumpbin /symbols $lib.FullName 2>&1 | Select-String 'vigem' | Select-Object -First 25) |
    ForEach-Object { Write-Host "  $($_.Line)" }
throw 'Could not produce a DLL that exports vigem_alloc.'
