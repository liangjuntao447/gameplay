@echo off
REM ============================================================
REM  TouchCloudPad - ViGEmBus virtual gamepad driver installer.
REM  Requests admin rights, then auto-downloads the latest ViGEmBus
REM  from GitHub (winget does NOT carry ViGEmBus).
REM  ASCII-only so it runs under any Windows codepage.
REM ============================================================

REM --- self-elevate to administrator ---
net session >nul 2>&1
if errorlevel 1 (
  echo Requesting administrator privileges...
  powershell -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
  exit /b
)

echo.
echo Running as administrator.
echo Trying to auto-download and install the latest ViGEmBus from GitHub...
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1"
if errorlevel 1 goto page

goto done

:page
echo.
echo Auto-download failed. Opening the official download page...
echo Please download the latest .msi or .exe and run it as administrator.
start "" "https://github.com/nefarius/ViGEmBus/releases/latest"

:done
echo.
echo ============================================================
echo   PLEASE REBOOT to load the ViGEmBus driver.
echo   After reboot: run TouchCloudPad.exe, and use
echo   drivers\check.bat to confirm the driver service is present.
echo ============================================================
pause
