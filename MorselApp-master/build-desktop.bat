@echo off
echo ============================================
echo WhatsApp Sender Desktop Build Script
echo ============================================
echo.

:: Set paths
set PROJECT_DIR=%~dp0
set BACKEND_DIR=%PROJECT_DIR%backend
set API_SOURCE=E:\SaasUntilNoe - Copy - Copy (5)\WhatsAppSender.API
set AUTOMATION_SOURCE=E:\FrontendApplication\WhatsAppApi\WhatsAppWebAutomation

:: Create backend folder
echo [1/6] Creating backend folder...
if not exist "%BACKEND_DIR%" mkdir "%BACKEND_DIR%"

:: Build API as self-contained executable
echo [2/6] Building WhatsApp Sender API...
cd /d "%API_SOURCE%"
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "%BACKEND_DIR%"
if errorlevel 1 (
    echo ERROR: Failed to build WhatsApp Sender API
    pause
    exit /b 1
)

:: Build Automation as self-contained executable
echo [3/6] Building WhatsApp Web Automation...
cd /d "%AUTOMATION_SOURCE%"
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "%BACKEND_DIR%"
if errorlevel 1 (
    echo ERROR: Failed to build WhatsApp Web Automation
    pause
    exit /b 1
)

:: Go back to project directory
cd /d "%PROJECT_DIR%"

:: Build Angular app
echo [4/6] Building Angular application...
call npm run build
if errorlevel 1 (
    echo ERROR: Failed to build Angular application
    pause
    exit /b 1
)

:: Build Electron installer
echo [5/6] Building Electron installer...
call npm run electron:build
if errorlevel 1 (
    echo ERROR: Failed to build Electron installer
    pause
    exit /b 1
)

echo.
echo [6/6] Build completed successfully!
echo.
echo Installer location: %PROJECT_DIR%release
echo.
pause
