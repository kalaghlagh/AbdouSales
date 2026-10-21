@echo off
chcp 65001 >nul
setlocal
cd /d "%~dp0"

rem One click: publish the app, then build the installer with Inno Setup.
set NOPAUSE=1

call publish.cmd
if errorlevel 1 (
  echo.
  echo *** Stopped: publish failed. ***
  pause
  exit /b 1
)

call build_installer.cmd
if errorlevel 1 (
  echo.
  echo *** Stopped: installer build failed. ***
  pause
  exit /b 1
)

echo.
echo All done. Your installer: %CD%\Installer\Output\AbdouSales_Setup.exe
pause
