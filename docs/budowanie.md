# Budowanie i pakowanie HexMapEditor

Na Windows z pełnym .NET Framework 4.8 albo 4.8.1 uruchom poniższe polecenia z katalogu repozytorium. Do instalatora potrzebny jest Inno Setup 6 oraz jego kompilator `ISCC.exe`.

```powershell
.\Build.ps1 -Output 'tests/_work/build-0.12.3/HexMapEditor.exe'
.\packaging\Build-Packages.ps1 -AppExe 'tests/_work/build-0.12.3/HexMapEditor.exe' `
    -InnoCompiler 'C:\ścieżka\do\ISCC.exe' -Output 'tests/_work/build-0.12.3'
```

Skrypt buduje instalator i Portable z jednego EXE. Do paczek trafiają wyłącznie pliki wymagane do dystrybucji oraz licencja kodu. Grafiki startowe generuje edytor. Przy samodzielnym uruchomieniu bez znacznika instalacji EXE używa danych obok siebie. Próby uruchamiaj z odrębnej kopii w `tests/_work`, żeby nie korzystać z roboczej Library.

Portable aktualizuje się ręcznie po zamknięciu edytora. Rozpakuj nowe ZIP osobno i skopiuj do istniejącej kopii pliki z paczki. Zachowaj Library, własne mapy, MRU i ustawienia.

Wersja 0.12.3 używa kanału testowego GitHub bez poświadczeń. Plan dla 1.0.0 przewiduje domyślny kanał stabilny. [Raport i odbiór aktualizacji](raport-0.12.3.md).
