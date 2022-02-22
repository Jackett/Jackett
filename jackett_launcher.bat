:: Helper script to fix
:: https://github.com/Jackett/Jackett/issues/10068

@echo off

:: Wait until the updater ends
:loop
tasklist | find /i "JackettUpdater.exe" > nul 2>&1
if errorlevel 1 (
  goto continue
) else (
  echo JackettUpdater is still running
  ping -n 2 127.0.0.1 > nul
  goto loop
)

:: Start Jackett Tray
:continue
start "" "%0\..\JackettTray.exe" --UpdatedVersion yes
