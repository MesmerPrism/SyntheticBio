@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "SYNTH_ROOT=%%~fI"
set "POLAR_ROOT=%SYNTH_ROOT%\..\PolarH10"

set "SYNTH_EXE=%SYNTH_ROOT%\artifacts\publish\SyntheticBio.App-win-x64\SyntheticBio.App.exe"
set "POLAR_BUILD_SCRIPT=%POLAR_ROOT%\tools\app\Build-Workspace-App.ps1"
set "POLAR_EXE=%POLAR_ROOT%\out\workspace-app\PolarH10.App.exe"
set "SYNTH_PIPE=polarh10-synth"

if not exist "%SYNTH_EXE%" (
    echo SyntheticBio publish output not found:
    echo   %SYNTH_EXE%
    exit /b 1
)

if not exist "%POLAR_BUILD_SCRIPT%" (
    echo PolarH10 workspace build script not found:
    echo   %POLAR_BUILD_SCRIPT%
    exit /b 1
)

echo Building PolarH10 workspace app...
powershell -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%POLAR_BUILD_SCRIPT%" -Configuration Release
if errorlevel 1 exit /b 1

if not exist "%POLAR_EXE%" (
    echo PolarH10 workspace build output not found:
    echo   %POLAR_EXE%
    exit /b 1
)

start "SyntheticBio" "%SYNTH_EXE%"
timeout /t 2 /nobreak >nul

set "POLARH10_TRANSPORT=synthetic"
set "POLARH10_SYNTHETIC_PIPE=%SYNTH_PIPE%"
start "PolarH10" "%POLAR_EXE%" --transport synthetic --synthetic-pipe %SYNTH_PIPE%

endlocal
