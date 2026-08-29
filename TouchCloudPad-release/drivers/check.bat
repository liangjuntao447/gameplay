@echo off
REM ============================================================
REM  TouchCloudPad - check whether the virtual gamepad is ready.
REM  ASCII-only output so it runs under any Windows codepage.
REM ============================================================
echo.
echo [1] ViGEmClient.dll  (client library, should sit next to the exe)
if exist "%~dp0..\ViGEmClient.dll" (
  echo     [OK] found (next to exe)
) else (
  if exist "C:\Windows\System32\ViGEmClient.dll" (
    echo     [OK] found (System32)
  ) else (
    echo     [X] NOT FOUND. Put ViGEmClient.dll next to TouchCloudPad.exe.
  )
)

echo.
echo [2] ViGEmBus driver service  (kernel driver)
sc.exe query ViGEmBus >nul 2>&1
if errorlevel 1 (
  echo     [X] ViGEmBus driver service NOT found
  echo         Driver is not installed or loaded.
  echo         Run install.bat as administrator, then REBOOT.
) else (
  echo     [OK] ViGEmBus driver service is present
  sc.exe query ViGEmBus | findstr /i "STATE"
)

echo.
echo Both [OK] = restart TouchCloudPad.exe; status should read "gamepad connected".
pause
