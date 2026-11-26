# Budowanie i pakowanie HexMapEditor

Na Windows z pełnym .NET Framework 4.8 albo 4.8.1 uruchom poniższe polecenia z katalogu repozytorium. Do instalatora potrzebny jest Inno Setup 6 oraz jego kompilator `ISCC.exe`.

```powershell
.\Build.ps1 -Output 'tests/_work/build-0.13.0/HexMapEditor.exe'
.\packaging\Build-Packages.ps1 -AppExe 'tests/_work/build-0.13.0/HexMapEditor.exe' `
    -InnoCompiler 'C:\ścieżka\do\ISCC.exe' -Output 'tests/_work/build-0.13.0'
```

Skrypt buduje instalator i Portable z jednego EXE. Do paczek trafiają wyłącznie pliki wymagane do dystrybucji oraz licencja kodu. Grafiki startowe generuje edytor. Przy samodzielnym uruchomieniu bez znacznika instalacji EXE używa danych obok siebie. Próby uruchamiaj z odrębnej kopii w `tests/_work`, żeby nie korzystać z roboczej Library.

Portable aktualizuje się ręcznie po zamknięciu edytora. Rozpakuj nowe ZIP osobno i skopiuj do istniejącej kopii pliki z paczki. Zachowaj Library, własne mapy, MRU i ustawienia.

Wersja 0.13.0 używa kanału testowego GitHub bez poświadczeń. Plan dla 1.0.0 przewiduje domyślny kanał stabilny. [Raport 0.13.0](raport-0.13.0.md).

## Po publikacji pre-release'u

Tuż po opublikowaniu obu paczek zaktualizuj tekst o dostępności i obie odznaki README. Każdy link ma używać dokładnego tagu i nazwy załącznika wydania. Dla 0.13.0 wstaw [gotowy fragment README](raport-0.13.0.md#5-fragment-readme-po-publikacji). Pobierz Setup i Portable bez logowania, aby sprawdzić oba linki, i porównaj sumy SHA-256 z raportem.
