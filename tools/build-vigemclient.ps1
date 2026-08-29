# Builds the OFFICIAL ViGEmClient.dll from source.
# Strategy: configure with CMake first (generates any config headers and knows
# the include paths), then compile the .cpp files directly into a DLL with
# cl.exe + a .def file (ViGEmClient's CMake hard-codes a STATIC lib, so we
# produce the DLL ourselves). Falls back to common shared-target names, and
# prints diagnostics so we can fix the exact option if it still fails.
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
$out = Join-Path $OutDir 'ViGEmClient.dll'

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

# --- configure with CMake first (generates headers, records includes) ---
$buildDir = Join-Path $SourceDir 'build'
Write-Host '=== CMake configure ==='
try {
    cmake -B $buildDir -S $SourceDir -A x64 2>&1 | ForEach-Object { Write-Host "  $_" }
} catch { Write-Host "cmake configure failed (continuing): $($_.Exception.Message)" }

$built = $false

# --- Attempt 1: cl.exe direct DLL compile ---
$cl = Get-Command cl.exe -ErrorAction SilentlyContinue
if (-not $cl) {
    # try to add MSVC's bin to PATH via VCINSTALLDIR (set by msvc-dev-cmd)
    if ($env:VCINSTALLDIR) {
        $env:PATH = "$env:VCINSTALLDIR\bin\Hostx64\x64;$env:VCINSTALLDIR\bin;" + $env:PATH
        $cl = Get-Command cl.exe -ErrorAction SilentlyContinue
    }
}
if ($cl) {
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

    $clArgs = @('/nologo', '/O2', '/EHsc', '/LD', '/DUNICODE', '/D_UNICODE')
    $clArgs += $incs
    foreach ($s in $srcs) { $clArgs += "`"$s`"" }
    $clArgs += '/link', "/DEF:`"$defFile`"", "/OUT:`"$out`""
    try {
        & $cl.Source @clArgs 2>&1 | ForEach-Object { Write-Host "  $_" }
    } catch { Write-Host "cl.exe invoke failed: $($_.Exception.Message)" }
    if (Test-Path $out) { $built = $true }
} else {
    Write-Host '=== cl.exe NOT FOUND in PATH ==='
}

# --- Attempt 2: try common shared-target names via CMake ---
if (-not $built) {
    Write-Host '=== Attempt 2: CMake shared targets ==='
    foreach ($t in @('ViGEmClientShared', 'ViGEmClient_DLL', 'ViGEmClientDll', 'viGEmClient')) {
        cmake --build $buildDir --config Release --target $t 2>&1 | ForEach-Object { Write-Host "  $_" }
        $d = Get-ChildItem $buildDir -Recurse -Filter 'ViGEmClient.dll' -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($d) { Copy-Item $d.FullName $out -Force; $built = $true; break }
    }
}

# --- diagnostics if still failing ---
if (-not $built) {
    Write-Host ''
    Write-Host '=== COULD NOT PRODUCE ViGEmClient.dll — diagnostics ==='
    Write-Host "cl.exe: $(if ($cl) { $cl.Source } else { 'NOT FOUND' })"
    Write-Host '--- CMakeLists.txt (add_library / option / BUILD / set lines) ---'
    Get-Content (Join-Path $SourceDir 'CMakeLists.txt') -ErrorAction SilentlyContinue |
        Select-String -Pattern 'add_library|add_library\(|option\(|BUILD_SHARED|VCPKG|set\(.*SHARED|ViGEmClientShared|PROJECT' |
        ForEach-Object { Write-Host "  $($_.Line)" }
    Write-Host '--- CMake available targets ---'
    cmake --build $buildDir --target help 2>&1 | ForEach-Object { Write-Host "  $_" }
    Write-Host '--- files produced under build/ ---'
    Get-ChildItem $buildDir -Recurse -Include '*.dll','*.lib' -ErrorAction SilentlyContinue | ForEach-Object { Write-Host "  $($_.FullName)" }
    throw 'Could not build ViGEmClient.dll. Diagnostics printed above.'
}

Write-Host "DLL built: $out"
