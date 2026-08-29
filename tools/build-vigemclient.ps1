# Builds the OFFICIAL ViGEmClient.dll from source, FORCING the vigem_* exports.
# Neither CMake target exports them (VIGEM_API is not dllexport by default), so
# we compile the .cpp files directly with cl.exe into a DLL and drive the
# exports with a .def file (linker exports by name, independent of the source's
# export macro). CMake is used first only to generate any config headers and a
# build dir for include paths; CMake-with-export-define is a fallback.
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

# --- CMake configure first: generates any config headers + build dir ---
Write-Host '=== CMake configure (for generated headers) ==='
try { cmake -B $buildDir -S $SourceDir -A x64 2>&1 | ForEach-Object { Write-Host "  $_" } } catch { Write-Host "  (cmake configure warn: $($_.Exception.Message))" }

# --- .def exports ---
$def = @'
LIBRARY ViGEmClient
EXPORTS
  vigem_alloc
  vigem_connect
  vigem_disconnect
  vigem_free
  vigem_target_add
  vigem_target_add_async
  vigem_target_ds4_alloc
  vigem_target_ds4_register_notification
  vigem_target_ds4_unregister_notification
  vigem_target_ds4_update
  vigem_target_ds4_update_ex
  vigem_target_ds4_update_ex_ptr
  vigem_target_free
  vigem_target_get_index
  vigem_target_get_pid
  vigem_target_get_type
  vigem_target_get_vid
  vigem_target_is_attached
  vigem_target_is_waitable_add_supported
  vigem_target_remove
  vigem_target_set_pid
  vigem_target_set_vid
  vigem_target_x360_alloc
  vigem_target_x360_get_user_index
  vigem_target_x360_register_notification
  vigem_target_x360_unregister_notification
  vigem_target_x360_update
'@
$defFile = Join-Path $tmp 'vigem_exports.def'
Set-Content -Path $defFile -Value $def -Encoding Ascii

# --- locate cl.exe ---
$cl = Get-Command cl.exe -ErrorAction SilentlyContinue
if (-not $cl -and $env:VCINSTALLDIR) {
    $env:PATH = "$env:VCINSTALLDIR\bin\Hostx64\x64;$env:VCINSTALLDIR\bin;" + $env:PATH
    $cl = Get-Command cl.exe -ErrorAction SilentlyContinue
}
if (-not $cl) { throw 'cl.exe not found (MSVC toolchain missing).' }

# --- Attempt 1: cl.exe + .def (guaranteed exports) ---
Write-Host "=== Attempt 1: cl.exe + .def -> $out (cl: $($cl.Source)) ==="
$srcs = Get-ChildItem $SourceDir -Recurse -Filter '*.cpp' -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\build' } | ForEach-Object { $_.FullName }
Write-Host "Source files: $($srcs.Count)"
$srcs | ForEach-Object { Write-Host "  $_" }

$incs = @("/I`"$SourceDir`"")
if (Test-Path $buildDir) { $incs += "/I`"$buildDir`"" }
Get-ChildItem $SourceDir -Recurse -Directory -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\build' -and (Get-ChildItem $_.FullName -Filter '*.h' -File -ErrorAction SilentlyContinue).Count -gt 0 } |
    ForEach-Object { $incs += "/I`"$($_.FullName)`"" }

$clArgs = @('/nologo', '/O2', '/EHsc', '/LD', '/DUNICODE', '/D_UNICODE', '/DVIGEMCLIENT_EXPORTS')
$clArgs += $incs
foreach ($s in $srcs) { $clArgs += "`"$s`"" }
$clArgs += '/link', "/DEF:`"$defFile`"", "/OUT:`"$out`""
try {
    & $cl.Source @clArgs 2>&1 | ForEach-Object { Write-Host "  $_" }
} catch { Write-Host "cl.exe invoke failed: $($_.Exception.Message)" }

if (Test-ViGEmExports $out) { Write-Host "DLL built with exports: $out"; exit 0 }

# --- Attempt 2: CMake + export define ---
Write-Host '=== Attempt 2: CMake -DVIGEMCLIENT_EXPORTS + ViGEmClientShared ==='
try {
    cmake -B $buildDir -S $SourceDir -A x64 -DViGEmClient_DLL=ON "-DCMAKE_CXX_FLAGS=/DVIGEMCLIENT_EXPORTS" 2>&1 | ForEach-Object { Write-Host "  $_" }
    cmake --build $buildDir --config Release --target ViGEmClientShared 2>&1 | ForEach-Object { Write-Host "  $_" }
    $dll = Get-ChildItem $buildDir -Recurse -Filter '*.dll' -ErrorAction SilentlyContinue |
        Where-Object { Test-ViGEmExports $_.FullName } | Select-Object -First 1
    if ($dll) { Copy-Item $dll.FullName $out -Force; Write-Host "DLL built with exports: $out"; exit 0 }
} catch { Write-Host "  (cmake fallback warn: $($_.Exception.Message))" }

# --- diagnostics: dumpbin of whatever DLL was built ---
Write-Host '=== diagnostics: dumpbin /exports of built DLL ==='
$candidate = Get-ChildItem $buildDir -Recurse -Filter '*.dll' -ErrorAction SilentlyContinue | Select-Object -First 1
if ($candidate) { Write-Host "File: $($candidate.FullName)"; & dumpbin /exports $candidate.FullName 2>&1 | ForEach-Object { Write-Host "  $_" } }
throw 'Could not produce a DLL that exports vigem_alloc.'
