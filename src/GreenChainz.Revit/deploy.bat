@echo off
REM GreenChainz Revit Plugin - Deployment Script
REM Run this after building to install the plugin for testing

set REVIT_VERSION=2026
set ADDIN_FOLDER=%APPDATA%\Autodesk\Revit\Addins\%REVIT_VERSION%
set BUILD_OUTPUT=bin\x64\Debug\net8.0-windows

echo.
echo ========================================
echo GreenChainz Revit Plugin Deployment
echo ========================================
echo.

REM Check if build output exists
if not exist "%BUILD_OUTPUT%\GreenChainz.Revit.dll" (
    echo ERROR: Build output not found at %BUILD_OUTPUT%
    echo Please build the solution first (x64 configuration).
    echo.
    echo Try: dotnet build -c Debug
    pause
    exit /b 1
)

REM Create addins folder if it doesn't exist
if not exist "%ADDIN_FOLDER%" (
    echo Creating Revit addins folder...
    mkdir "%ADDIN_FOLDER%"
)

REM Copy DLL and dependencies
echo Copying plugin files...
copy /Y "%BUILD_OUTPUT%\GreenChainz.Revit.dll" "%ADDIN_FOLDER%\"
copy /Y "%BUILD_OUTPUT%\Newtonsoft.Json.dll" "%ADDIN_FOLDER%\"

REM Copy .addin manifest
copy /Y "GreenChainz.Revit.addin" "%ADDIN_FOLDER%\"

echo.
echo ========================================
echo Deployment Complete!
echo ========================================
echo.
echo Files deployed to: %ADDIN_FOLDER%
echo.
echo Next steps:
echo 1. Close Revit if it's running
echo 2. Restart Revit
echo 3. Look for "GreenChainz" tab in the ribbon
echo.
echo Optional: Set environment variables for live SDA data:
echo   set AUTODESK_CLIENT_ID=your_client_id
echo   set AUTODESK_CLIENT_SECRET=your_client_secret
echo.
pause
