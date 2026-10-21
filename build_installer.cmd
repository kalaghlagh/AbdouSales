@echo off
chcp 65001 >nul
setlocal
cd /d "%~dp0"

rem ---- locate Inno Setup 6 (ISCC.exe) in the usual places ----
set "ISCC="
if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if not defined ISCC if exist "%ProgramFiles%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"
if not defined ISCC if exist "%LocalAppData%\Programs\Inno Setup 6\ISCC.exe" set "ISCC=%LocalAppData%\Programs\Inno Setup 6\ISCC.exe"

if not defined ISCC (
  echo Inno Setup 6 was not found.
  echo Download it from https://jrsoftware.org/isdl.php and install it, then run this again.
  if not "%NOPAUSE%"=="1" pause
  exit /b 1
)

if not exist "AbdouSales\bin\Release\net8.0-windows\win-x64\publish\AbdouSales.exe" (
  echo Publish output not found. Run publish.cmd first ^(or use build_all.cmd^).
  if not "%NOPAUSE%"=="1" pause
  exit /b 1
)

"%ISCC%" "Installer\AbdouSales.iss"
if errorlevel 1 (
  echo.
  echo *** INSTALLER BUILD FAILED ***
  if not "%NOPAUSE%"=="1" pause
  exit /b 1
)

echo.
echo ==========================================================
echo   Installer created:
echo   %CD%\Installer\Output\AbdouSales_Setup.exe
echo ==========================================================
if not "%NOPAUSE%"=="1" pause
