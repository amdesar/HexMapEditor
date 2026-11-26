# Budowanie i pakowanie HexMapEditor

Na Windows z pełnym .NET Framework 4.8 albo 4.8.1 uruchom poniższe polecenia z katalogu repozytorium. Do instalatora potrzebny jest Inno Setup 6 oraz jego kompilator `ISCC.exe`.

```powershell
.\Build.ps1 -Output 'tests/_work/build-0.13.4/HexMapEditor.exe'
.\packaging\Build-Packages.ps1 -AppExe 'tests/_work/build-0.13.4/HexMapEditor.exe' `
    -InnoCompiler 'C:\ścieżka\do\ISCC.exe' -Output 'tests/_work/build-0.13.4'
```

Skrypt buduje instalator i Portable z jednego EXE. Do paczek trafiają wyłącznie pliki wymagane do dystrybucji oraz licencja kodu. Grafiki startowe generuje edytor. Przy samodzielnym uruchomieniu bez znacznika instalacji EXE używa danych obok siebie. Próby uruchamiaj z odrębnej kopii w `tests/_work`, żeby nie korzystać z roboczej Library.

Portable aktualizuje się ręcznie po zamknięciu edytora. Rozpakuj nowe ZIP osobno i skopiuj do istniejącej kopii pliki z paczki. Zachowaj Library, własne mapy, MRU i ustawienia.

Wersja 0.13.4 używa kanału testowego GitHub bez poświadczeń. Plan dla 1.0.0 przewiduje domyślny kanał stabilny. [Raport 0.13.4](raport-0.13.4.md).

## Publikacja pre-release’u

Commit wydaniowy zawiera już README z właściwym numerem i oboma odnośnikami. Opublikuj pre-release `v0.13.4` z paczkami o nazwach podanych w README. Po publikacji pobierz oba pliki bez logowania i porównaj SHA-256 z raportem.
