@echo off
setlocal enabledelayedexpansion
REM =====================================================================
REM  TouchCloudPad  build script
REM  Compiles the whole app with the .NET Framework 4.8 compiler (csc.exe)
REM  that ships with Windows. No SDK, no NuGet, no runtime to install.
REM  Output: TouchCloudPad.exe  (single small exe, runs on any Win10)
REM =====================================================================

set "FW=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319"
if not exist "%FW%\csc.exe" set "FW=%WINDIR%\Microsoft.NET\Framework\v4.0.30319"
set "CSC=%FW%\csc.exe"
set "WPF=%FW%\WPF"

if not exist "%CSC%" (
  echo [ERROR] .NET Framework 4.8 compiler not found.
  echo         Windows 10 includes it; if missing, enable it via:
  echo         Control Panel ^> Programs ^> .NET Framework 3.5/4.8.
  exit /b 1
)

cd /d "%~dp0"

echo Compiling TouchCloudPad.exe ...

"%CSC%" /nologo /target:winexe /platform:x64 /optimize+ ^
  /r:System.dll ^
  /r:System.Core.dll ^
  /r:System.Xml.dll ^
  /r:System.Runtime.Serialization.dll ^
  /r:"%FW%\System.IO.Compression.dll" ^
  /r:"%FW%\System.IO.Compression.FileSystem.dll" ^
  /r:"%FW%\System.Xaml.dll" ^
  /r:"%WPF%\WindowsBase.dll" ^
  /r:"%WPF%\PresentationCore.dll" ^
  /r:"%WPF%\PresentationFramework.dll" ^
  /out:"%~dp0TouchCloudPad.exe" ^
  "%~dp0src\*.cs"

if %ERRORLEVEL% NEQ 0 (
  echo.
  echo [ERROR] Build failed.
  exit /b 1
)

echo.
echo Build OK: %~dp0TouchCloudPad.exe
endlocal
