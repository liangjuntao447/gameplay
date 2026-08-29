# TouchCloudPad - installer helper.
#   1) Downloads + runs the official ViGEmBus driver (if not present).
#   2) Downloads the OFFICIAL ViGEmClient.dll and puts it next to the exe.
# ASCII-only so it runs regardless of the system codepage.
# Exits 0 if the driver is present/installed, 1 otherwise.

$ErrorActionPreference = 'Continue'
$ProgressPreference = 'SilentlyContinue'

$api = @{ 'User-Agent' = 'TouchCloudPad' }
$exeDir = (Join-Path $PSScriptRoot '..')   # drivers\.. -> TouchCloudPad folder

# ---------- 1) ViGEmClient.dll (official client library) ----------
Write-Host ''
Write-Host '[1/2] Downloading the OFFICIAL ViGEmClient.dll ...'
$vcOk = $false
try {
    $vc = Invoke-RestMethod -Uri 'https://api.github.com/repos/nefarius/ViGEmClient/releases/latest' -Headers $api -TimeoutSec 30
    $dll = $vc.assets | Where-Object { $_.name -match '\.dll$' -and $_.name -match 'x64|amd64' } | Select-Object -First 1
    if (-not $dll) { $dll = $vc.assets | Where-Object { $_.name -match '\.dll$' } | Select-Object -First 1 }
    if ($dll) {
        $dest = Join-Path $exeDir 'ViGEmClient.dll'
        Invoke-WebRequest -Uri $dll.browser_download_url -OutFile $dest -UseBasicParsing -TimeoutSec 120
        Write-Host "    [OK] saved $($dll.name) -> $dest"
        $vcOk = $true
    } else { Write-Host '    [X] no .dll asset found in release.' }
} catch {
    Write-Host "    [X] download failed: $($_.Exception.Message)"
}
if (-not $vcOk) {
    Write-Host '    Manual: download ViGEmClient.dll (x64) from'
    Write-Host '    https://github.com/nefarius/ViGEmClient/releases/latest'
    Write-Host '    and put it next to TouchCloudPad.exe.'
}

# ---------- 2) ViGEmBus driver ----------
Write-Host ''
Write-Host '[2/2] Checking / installing the ViGEmBus driver ...'
$drvOk = $false
try {
    sc.exe query ViGEmBus | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host '    [OK] ViGEmBus driver already running.'
        $drvOk = $true
    }
} catch { }

if (-not $drvOk) {
    try {
        $rel = Invoke-RestMethod -Uri 'https://api.github.com/repos/nefarius/ViGEmBus/releases/latest' -Headers $api -TimeoutSec 30
        $asset = $rel.assets | Where-Object { $_.name -match '\.(msi|exe)$' -and $_.name -notmatch 'arm' } | Select-Object -First 1
        if ($asset) {
            $dest = Join-Path $env:TEMP $asset.name
            Write-Host "    downloading $($asset.name) ..."
            Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $dest -UseBasicParsing -TimeoutSec 180
            Write-Host '    launching installer...'
            $p = Start-Process -FilePath $dest -Verb RunAs -PassThru
            if ($p) { $p.WaitForExit() }
            sc.exe query ViGEmBus | Out-Null
            if ($LASTEXITCODE -eq 0) { $drvOk = $true }
        }
    } catch {
        Write-Host "    [X] driver install failed: $($_.Exception.Message)"
    }
}

if ($drvOk) {
    Write-Host ''
    Write-Host 'Both done. REBOOT the PC, then run TouchCloudPad.exe.'
    exit 0
}
Write-Host ''
Write-Host 'Driver not loaded yet. If you just installed it, REBOOT now.'
exit 1
