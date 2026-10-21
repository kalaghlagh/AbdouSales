@echo off
chcp 65001 >nul
setlocal
cd /d "%~dp0"

echo ==========================================================
echo   Publishing: Release ^| win-x64 ^| Self-contained
echo ==========================================================
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
  echo *** .NET 8 SDK was not found. Install it from https://dotnet.microsoft.com/download/dotnet/8.0 ***
  if not "%NOPAUSE%"=="1" pause
  exit /b 1
)

rem Remove the old publish output so no stale files end up inside the installer
if exist "AbdouSales\bin\Release\net8.0-windows\win-x64\publish" rmdir /s /q "AbdouSales\bin\Release\net8.0-windows\win-x64\publish"

dotnet publish "AbdouSales\AbdouSales.csproj" ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  -p:PublishSingleFile=false ^
  -p:PublishTrimmed=false ^
  -o "AbdouSales\bin\Release\net8.0-windows\win-x64\publish"

if errorlevel 1 (
  echo.
  echo *** PUBLISH FAILED - read the error above ***
  if not "%NOPAUSE%"=="1" pause
  exit /b 1
)

echo.
echo ==========================================================
echo   Done. Output folder:
echo   %CD%\AbdouSales\bin\Release\net8.0-windows\win-x64\publish
echo   Run AbdouSales.exe from there to test.
echo ==========================================================
if not "%NOPAUSE%"=="1" pause
