@echo off
setlocal
if not exist "%~dp0Build.ps1" (
    echo Blad: brak pliku Build.ps1.
    goto failed
)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build.ps1"
if errorlevel 1 goto failed
echo Gotowe: HexMapEditor.exe zostal przebudowany.
if /i not "%~1"=="/nopause" pause
exit /b 0
:failed
echo Blad przebudowy. Jesli istnial poprzedni EXE, zostal zachowany.
if /i not "%~1"=="/nopause" pause
exit /b 1
