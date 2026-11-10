@echo off
setlocal
if not exist "%~dp0Build.ps1" (
    echo Blad: brak pliku Build.ps1.
    goto failed
)
echo Oczekiwana ikona: "%~dp0assets\app\icon.ico"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build.ps1"
if errorlevel 1 goto failed
echo Gotowe: "%~dp0HexMapEditor.exe" zostal przebudowany.
echo Eksplorator moze jeszcze pokazywac ikone z pamieci podrecznej.
if /i not "%~1"=="/nopause" pause
exit /b 0
:failed
echo Blad przebudowy. Sprawdz komunikat powyzej.
if /i not "%~1"=="/nopause" pause
exit /b 1
