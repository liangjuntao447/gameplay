# Builds the OFFICIAL ViGEmClient.dll from source by compiling the .cpp files
# directly with cl.exe into a DLL (ViGEmClient's CMake hard-codes a STATIC
# library, so BUILD_SHARED_LIBS does not help). Exports are provided by a .def
# file, so we do not depend on the project's own export macros.
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

# --- exports .def (from the official ViGEmClient.h API) ---
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

# --- attempt 1: compile the .cpp files into a DLL with cl.exe ---
Write-Host '=== Attempt 1: compile DLL directly with cl.exe ==='
$srcs = Get-ChildItem $SourceDir -Recurse -Filter '*.cpp' -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\build' } | ForEach-Object { $_.FullName }
Write-Host "Source files: $($srcs.Count)"
$srcs | ForEach-Object { Write-Host "  $_" }

# include dirs: source root + every folder that contains .h files
$incs = @()
$incs += "/I`"$SourceDir`""
Get-ChildItem $SourceDir -Recurse -Directory -ErrorAction SilentlyContinue |
    Where-Object { (Get-ChildItem $_.FullName -Filter '*.h' -File -ErrorAction SilentlyContinue).Count -gt 0 } |
    ForEach-Object { $incs += "/I`"$($_.FullName)`"" }
Write-Host "Include dirs: $($incs.Count)"

if ($srcs.Count -gt 0) {
    $clArgs = @('/nologo', '/O2', '/EHsc', '/LD', '/DUNICODE', '/D_UNICODE')
    $clArgs += $incs
    foreach ($s in $srcs) { $clArgs += "`"$s`"" }
    $clArgs += '/link', "/DEF:`"$defFile`"", "/OUT:`"$out`""
    try {
        & cl.exe @clArgs 2>&1 | ForEach-Object { Write-Host "  $_" }
    } catch { Write-Host "cl.exe failed: $($_.Exception.Message)" }
    if (Test-Path $out) {
        Write-Host "DLL built: $out"
        exit 0
    }
}

# --- attempt 2: CMake with BUILD_SHARED_LIBS (fallback) ---
Write-Host '=== Attempt 2: CMake BUILD_SHARED_LIBS=ON (fallback) ==='
try {
    cmake -B (Join-Path $SourceDir 'build') -S $SourceDir -A x64 -DBUILD_SHARED_LIBS=ON -DCMAKE_WINDOWS_EXPORT_ALL_SYMBOLS=ON
    cmake --build (Join-Path $SourceDir 'build') --config Release
    $dll = Get-ChildItem (Join-Path $SourceDir 'build') -Recurse -Filter 'ViGEmClient.dll' -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($dll) { Copy-Item $dll.FullName $out -Force; Write-Host "DLL built: $out"; exit 0 }
} catch { Write-Host "CMake fallback failed: $($_.Exception.Message)" }

throw 'Could not build ViGEmClient.dll. See cl.exe / CMake output above.'
