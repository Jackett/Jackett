:: Helper script to fix
:: https://github.com/Jackett/Jackett/issues/10068

@echo off
:: Allow the script to run (and work) from other directories
cd /d "%~dp0"

:: Wait until the updater ends
:LOOP
>nul 2>&1 tasklist | find /i "JackettUpdater.exe" || (
    echo JackettUpdater is still running
    >nul ping -n 2 127.0.0.1
    goto LOOP
)

:: Start Jackett Tray
start "" "%~0\..\JackettTray.exe" --UpdatedVersion yes
exit /b
