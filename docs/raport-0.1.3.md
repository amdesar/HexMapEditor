# HexMapEditor 0.1.3 — raport weryfikacji

Data: 5 listopada 2026. Format mapy: **1**.

## Stan początkowy i zakres

Praca w głównym folderze `HexMapEditor-Codex`, na czystym `master` z commitem `94cf89cedaf23378830a4ca85736634f72dd8c6a` (0.1.2). Po `git fetch origin` lokalna gałąź i `origin/master` były zgodne. Fast-forward nie był potrzebny. Nie przepisywano historii.

Zmieniono potwierdzanie nadpisania, katalogi biblioteki i dialogu zapisu, domyślne wymiary nowej mapy i nazwy orientacji. Zachowano geometrię, hit-testing, Dopasuj, format 1 i historię malowania 0.1.2. Nie wdrożono funkcji 0.2.0.

## Zapis

Natywny `SaveFileDialog` nie pyta o nadpisanie. Po zamknięciu dialogu program ustala pełną końcową ścieżkę z rozszerzeniem `.hexmap` i dla istniejącego pliku pokazuje jeden standardowy `MessageBox` Tak/Nie/Anuluj, domyślnie Nie. Odmowa/anulowanie kończą próbę bez zmiany stanu projektu. Zapisz do bieżącego pliku nie pyta ponownie.

Zapis zachowuje plik tymczasowy w katalogu celu, `Flush(true)` oraz `File.Replace`/`File.Move`. Dla celu, którego nadpisania nie potwierdzono, pojawienie się pliku przed końcem zapisu powoduje błąd zamiast cichego zastąpienia. Wydzielona metoda `TrySaveTo` pozwala testować błędy i stan projektu bez zamykania okien komunikatów; produkcyjna ścieżka nadal pokazuje standardowy komunikat błędu.

## Wyniki automatyczne

- Build `HexMapEditor.exe`: zakończony poprawnie lokalnym kompilatorem .NET Framework, bez pobierania zależności.
- `Run-HistoryTests.ps1`: **11/11** testów historii z 0.1.2 przeszło. Dostosowano lokalizację własnych plików testowych do katalogu tymczasowego poza projektem.
- `Run-TechnicalTests.ps1`: **12/12** testów metod i konfiguracji kontrolek przeszło. Pokrywają końcowe ścieżki i liczbę potwierdzeń dla `mapa.hexmap`, `mapa.txt`, `mapa.txt.hexmap`, `mapa.HEXMAP`; odmowę i anulowanie; nowy cel i zwykłe Zapisz; blokadę pliku, brak katalogu i zachowanie stanu po błędzie; brak zgody na zastąpienie nowo powstałego celu; jawne katalogi dialogu; nową instancję formularza; tworzenie i zachowanie zawartości biblioteki; inny katalog roboczy; błąd tworzenia biblioteki; domyślne wymiary i zakresy; zgodność nazw orientacji ze szczytem wielokąta, statusem i identyfikatorami danych.

Pierwszy przebieg w piaskownicy dostał odmowę dostępu przy przenoszeniu pliku w wirtualizowanym folderze tymczasowym. Nie został zaliczony. Powyższe wyniki uzyskano poza piaskownicą w systemowym folderze tymczasowym. Testy nie wymagają uprawnień administratora przy zwykłym lokalnym uruchomieniu.

## Wykonane testy rzeczywistego okna

Uruchomiono zbudowany EXE i obsługiwano natywne okna Windows przez Computer Use. Wszystkie mapy kontrolne były własnymi plikami w jednym unikatowym katalogu `%TEMP%`, poza projektem.

- Nowy, niezapisany projekt: Zapisz otworzył `Library/Hexmaps` obok EXE.
- Wpisanie `mapa.txt` przy istniejącym `mapa.txt.hexmap`: jedno potwierdzenie z końcową ścieżką. Nie pozostawiło plik kontrolny bez zmian. Ponowne Zapisz jako nadal proponowało `Library/Hexmaps`.
- Istniejący `mapa.hexmap`: jedno potwierdzenie; Tak zapisało mapę, bez drugiego pytania. Następne zwykłe Zapisz nie wyświetliło pytania.
- Zapisz jako po tym zapisie: zaproponował katalog bieżącego pliku, poza projektem.
- Pełne `mapa.txt.hexmap`: jedno potwierdzenie tej samej końcowej ścieżki. Escape/Anuluj pozostawiło plik kontrolny bez zmian.
- `upper.HEXMAP`: jedno potwierdzenie, udany zapis i brak pliku z podwójnym rozszerzeniem.
- Po zmianie orientacji próba zapisu do celu tylko do odczytu została zablokowana komunikatem samego dialogu Windows. Anulowanie zachowało gwiazdkę. Następne Zapisz trafiło do poprzedniego `upper.HEXMAP`; odczyt plików potwierdził nową orientację tam i niezmienioną mapę w celu tylko do odczytu.
- Pointy Top ma wierzchołek u góry, Flat Top poziomą krawędź u góry. Sprawdzono rysunek, przycisk, tooltip i status.
- Po otwarciu mapy 1×1 dialog Nowa mapa pokazał 15×10, etykiety obu zakresów 1–200 oraz nazwę Pointy Top.
- Po zamknięciu i ponownym uruchomieniu EXE nowy projekt ponownie proponował `Library/Hexmaps`. Powtórzony przypadek `mapa.txt` nadal ostrzegał o `mapa.txt.hexmap`.

Sesje testowe zamknięto. Własne pliki kontrolne usunięto po testach.

## Rola bin i zachowanie danych

W 0.1.2 skrypt testów kompilował `HexMapEditor.HistoryTests.exe` do `bin`. Główna aplikacja jest budowana do folderu projektu, nie do `bin`; katalog roboczy narzędzi w tym zadaniu również był głównym folderem projektu. W mojej wcześniejszej próbie interaktywnej wybrałem `bin` jawnie dla własnej mapy tymczasowej. Program nie miał kodu ustawiającego `bin` jako katalog map; `SaveFileDialog` w ogóle nie miał ustawionego `InitialDirectory`.

Brak jawnego katalogu i wcześniejszy wybór `bin` wyjaśniają możliwość jego późniejszego proponowania przez pamięć dialogów Windows; bez jawnej ścieżki znaczenie mógł mieć także katalog roboczy konkretnego uruchomienia. Nie odczytywano historycznego stanu tej pamięci ani katalogu roboczego każdej wcześniejszej sesji, więc nie przypisujemy każdej próby do konkretnego wpisu Windows.

Przed 0.1.3 w `bin` były też `Nowa mapa.hexmap` i `Nowa mapa.txt.hexmap`. Pozostawiono je na miejscu i nie zmieniono ich zawartości. Nie usuwano `bin`, nie migrowano map i nie dodano map testowych do repozytorium. Nowe testy kompilują własne EXE i zapisują własne dane w katalogu tymczasowym poza projektem.

Biblioteka jest tworzona względem EXE, a jej katalogi nie oznaczają gotowego importu, eksportu ani zarządzania zasobami.

## Ograniczenia weryfikacji

Nie wykonano testu na czystym komputerze bez internetu, pełnym dysku, przy awarii zasilania ani we wszystkich konfiguracjach DPI i systemach plików. Nie testowano interaktywnie odmowy uprawnień do tworzenia biblioteki; test metody używał kolizji z plikiem o nazwie `Library`. Inny katalog roboczy sprawdzono testem metod, a nie osobną sesją interaktywnego EXE.

Komunikat aplikacji po błędzie samego `File.Replace` nie był osobno obsługiwany w interaktywnym oknie: błąd i zachowanie stanu sprawdzono metodą z zablokowanym plikiem, a interaktywny przypadek pliku tylko do odczytu zatrzymał wcześniej natywny dialog Windows. Testy metod nie są przedstawiane jako pełne testy tego dialogu.
