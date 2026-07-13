@echo off
setlocal

set "APPLICATION_DIRECTORY=%~dp0"
set "APPLICATION_EXE=%APPLICATION_DIRECTORY%AgentAura.Prototype.exe"
set "RUNTIME_DOWNLOAD_URL=https://dotnet.microsoft.com/download/dotnet/10.0/runtime"

if /i "%~1"=="--check-runtime" goto checkRuntime

call :hasRequiredRuntime
if not errorlevel 1 goto launch

echo.
echo Agent Aura requires the Microsoft .NET 10 Windows Desktop Runtime for x64.
echo Install it from Microsoft, then run Start-AgentAura.cmd again:
echo %RUNTIME_DOWNLOAD_URL%
echo.
set /p "OPEN_INSTALLER=Open the official Microsoft download page now? [y/N] "
if /i "%OPEN_INSTALLER%"=="y" start "" "%RUNTIME_DOWNLOAD_URL%"
exit /b 1

:launch
start "" "%APPLICATION_EXE%"
exit /b 0

:checkRuntime
call :hasRequiredRuntime
exit /b %errorlevel%

:hasRequiredRuntime
if /i not "%PROCESSOR_ARCHITECTURE%"=="AMD64" exit /b 1

set "DOTNET_HOST=%ProgramFiles%\dotnet\dotnet.exe"
if not exist "%DOTNET_HOST%" exit /b 1

"%DOTNET_HOST%" --list-runtimes 2>nul | findstr /r /c:"^Microsoft.WindowsDesktop.App 10\." >nul
if errorlevel 1 exit /b 1

exit /b 0
