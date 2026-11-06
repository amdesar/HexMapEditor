# HexMapEditor 0.2.3 — raport

6 listopada 2026. Praca w `C:\ALTERNATYWNA PARTYCJA\HexMapEditor-Codex`, na ukończonej 0.2.2. Na początku czysty master `b0fd2b6e31d034ab5c4ff8aa794ef32f24d09052`, origin `https://github.com/amdesar/HexMapEditor.git`; po fetch nadal zgodny z origin/master, bez potrzeby synchronizacji. Nie znaleziono dodatkowego AGENTS.md w repozytorium ani nadrzędnych katalogach. Przeczytano README, raport 0.2.2, obowiązujące fragmenty specyfikacji i kod/testy zmienianych obszarów. Nie rozpoczynano ponownie prac 0.2.2.

## Ukończone zmiany

1. Czytelne pliki presetów. `PresetFiles` wybiera bezpieczną nazwę z `Name`, a `PresetCatalog.Write` nadal waliduje ID i logiczne nazwy. Zachowuje Unicode i spacje, normalizuje Unicode, zastępuje znaki niedozwolone/kontrolne `_`, usuwa końcowe kropki/spacje, chroni nazwy urządzeń Windows także przed kropką (CON, AUX, COM1, LPT² itd.), pusty wynik zastępuje Preset. Rdzeń do 80 znaków, ścieżka docelowa do 210 z rezerwą dla pliku roboczego .NET. Zbyt długa lokalizacja daje błąd przed zmianą danych. Nazwa nie może wprowadzić separatora, ścieżki absolutnej ani wyjścia poza katalog.
2. Kolizje nazw plików są porównywane bez rozróżniania wielkości liter, także z istniejącymi katalogami/innymi plikami. Nazwy po normalizacji dostają ` (2)`, ` (3)` itd. Przykład: Kosmos? i kosmos* mają odrębne bezpieczne pliki. Konflikt logicznej nazwy presetu albo ID nadal wymaga decyzji — nie jest cicho nadpisywany. Istniejący własny plik jest znajdowany przez ID wewnątrz, również pod dawną nazwą z hashem.
3. Robocze przemianowanie nie dotyka dysku. Dopiero świadomy zapis nadpisania porządkuje nazwę jednego aktywnego pliku. Nowe bajty są najpierw zapisywane i Flush(true) do nieskanowanego `.tmp`. Dotychczasowy plik przenoszony jest bez flagi nadpisania, zachowując stare bajty, potem atomowe File.Replace zatwierdza zawartość. Przy błędzie zastąpienia nazwa jest przywracana; gdy sam rollback zawiedzie, komunikat wskazuje zachowany plik. Nie ma momentu z dwoma aktywnymi plikami presetu. ID nie zależy od nazwy i nie zmienia się przy przemianowaniu. Przy starcie nie ma masowej migracji nazw. Undo/Redo nie zmienia ani nazw, ani zawartości plików biblioteki.
4. Odśwież przy selektorze ma ikonę, polską podpowiedź i nazwę dostępności. Ręczny refresh korzysta z `PresetCatalog.Load`, bez migracji lub konkurencyjnego importera. Zastępuje jedynie odczytaną listę biblioteki; `map.Presets`, aktywne ID, robocze wersje, wybór terenu, gwiazdka i historie pozostają. Nowy plik staje się dostępny bez automatycznego zastosowania. Porównanie poprzedniego/nowego zapisanego stanu aktywnego ID pozwala zgłosić zniknięcie lub zewnętrzną zmianę. Projekt zachowuje własną kopię; wskaźnik odzwierciedla brak pliku/różnicę. Ponowne odczyty nie mnożą wpisów. Błędy/kolizje są zgłaszane; katastrofalny błąd odczytu zachowuje poprzednią listę. Nie dodano obserwatora systemu plików.
5. UI konsekwentnie używa preset: Presety, Nowy preset / Zmień nazwę oraz Zapisz preset / Usuń preset, w dwóch wierszach po dwa przyciski. Zmieniono także dialogi i komunikaty, bez przemianowywania klas/pól formatu. Teren pozostał terenem.
6. Lewy panel nadal 220 px, jedna kolumna. Zawartość wykorzystuje szerokość klienta z 6 px bocznego odstępu i pomniejsza się po pojawieniu pionowego scrolla. Przybornik, selektor, działania i paleta mają wspólną szerokość. Wiersze 32 zamiast 42 px i odstępy 4 zamiast 6 px: sześć terenów 216 zamiast 288 px. Osobne kolumny akcji mają 24×28 px; nazwa zajmuje pozostałą szerokość. Zachowano pełne podpowiedzi, skracanie, F2/Delete, dostępność, Dodaj i istniejące kontrolki przy niezwiązanych zmianach.
7. Kosz terenu rysowany lokalnie w czerwieni; hover i fokus pozostają standardowymi stanami przycisku. Tooltipy są krótkie: Edytuj teren (F2), Usuń teren (Delete). Nazwa celu nadal jest w potwierdzeniu i dostępności.
8. Ctrl+Shift+V w `dock.PreviewKeyDown` jest obsługiwane tylko przy fokusie `Box`. Wywołuje istniejące `Paste(true)` i ustawia Handled, zapobiegając dalszemu wklejeniu. Menu pokazuje Ctrl+Shift+V. Ctrl+V i RTF nie zmieniły ścieżki; brak tekstu pozostaje no-op. Jedna operacja historii opisu, bez mapy.

Aplikacja **0.2.3**, EXE **0.2.3.0**, mapa **2**, odczyt **1**, preset **1**. Bez nowych zależności, usług i pobierania przy uruchamianiu. Bez push, tagu i Release'a.

## Krótki przegląd prawego nagłówka

Sprawdzono `SetRightWidth`, `PositionOverlays`, `BuildDetailsPanel`, `InitializePanelResize` i dokowanie przycisków. Nie znaleziono własnej pętli wielokrotnego ustawiania pozycji tych przycisków. Jedno SetRightWidth wywołuje PositionOverlays; przyciski należą do dockowanego nagłówka WinForms, a ich pozycje przelicza układ po zmianie bounds rodzica. Zgłoszony lekki przeskok nie ma tu potwierdzonej prostej przyczyny do usunięcia. Nie maskowano go opóźnieniem ani nie przebudowano nagłówka. **Pozostaje bez zmian i nie jest oznaczony jako naprawiony.** Nie powtarzano rozbudowanej diagnostyki smug; ich naprawa i regresja odrysowania pozostają.

## Automatyczne testy

Celowany `Run-ConvenienceTests.ps1`: 7/7 po poprawieniu dwóch błędów przygotowania samego testu (deklaracja zmiennych oraz kolejność tworzenia błędnego pliku względem FindFile). Testy obejmują:

- Unicode, niedozwolone/reserved/trailing/puste/długie nazwy i granice katalogu;
- kolizje normalizacji i wielkości liter, suffix, powtórny zapis bez duplikatu;
- odczyt starego hasha bez przemianowania na starcie, świadomy zapis, zmiana samej wielkości liter i stałe ID;
- wstrzyknięty błąd zastąpienia/rollback oryginalnych bajtów i zajęty cel bez nadpisania;
- nowy przenośny plik, powtórny refresh, robocze zmiany, wybór, kontrolki, gwiazdka/historia, zewnętrzna zmiana/zniknięcie, błędny plik i konflikt ID;
- dwie pary przycisków i układ palety z pionowym przewijaniem przy minimalnym oknie;
- schowek Unicode + konkurencyjny RTF, Ctrl+Shift+V jako pojedyncza historia, brak przechwycenia Ctrl+V, brak wykonania poza opisem i brak kasowania przy pustym schowku. Test przywraca poprzedni schowek.

Końcowy zbiorczy `tests/Run-AllTests.ps1` uruchomiono **raz**: **111/111**, kod wyjścia **0**. History 11, Technical 12, Gesture 11, Feature 35, Patch 21, Maintenance 14, Convenience 7. Zmieniono jedno stare oczekiwanie rozmiaru kafelka na nową responsywną szerokość i wysokość 32; nie osłabiono testu zachowania kontrolek. Log: `tests/_work/all-274bb94cce5b46708c4e2861e57b246f/results.log` (ignorowany). Na końcu ujednolicono jeszcze pojedyncze słowo „zestawu” → „presetu” w dawnym komunikacie błędu migracji i przebudowano EXE; nie zmieniło to logiki ani oczekiwań testów i nie powtarzano całej kampanii.

## Rzeczywiste próby zwykłego EXE

Własna, odizolowana biblioteka obok identycznej kopii głównego EXE w `tests/_work/ui-023-510986ec0329497a9097eedc7143b648/`. Materiał przygotował `tests/Create-ConvenienceUi.ps1`; bez prywatnej Library i Worda.

- Nowy preset Kosmos żółty → dokładnie `Kosmos żółty.terrainset.json`. Robocza nazwa Kosmos po zmianie pozostawiła dawną nazwę pliku i status różnicy. Świadomy Zapisz preset / Nadpisz zmienił plik na `Kosmos po zmianie.terrainset.json`, bez drugiej kopii.
- Podczas działania skopiowano własny plik Galaktyki żółte do testowego Presets. Odśwież pokazał go na liście, pozostawiając aktywny preset i jego robocze przemianowanie. Wybrano nowy plik bez restartu; paleta poprawnie pokazała 19 terenów. Po późniejszym Undo nazwy/wyboru zapisany plik pozostał pod nową nazwą.
- Duże okno i rzeczywiste minimum 1180×680: cztery przyciski w 2×2, refresh przy liście, równe marginesy, zwarta paleta, widoczne pełne podstawowe napisy. Długie nazwy są skracane; pionowo przewinięto listę do końca i z powrotem do Dodaj. Nie zaobserwowano poziomego scrolla ani obcięcia kolumn akcji. Czerwone kosze, hover i rzeczywiste krótkie tooltipy obu akcji były widoczne. Zachowano nazwę projektu i Rozmiar.
- W aktywnym opisie wpisano i skopiowano własne „Zażółć gęślą jaźń — 0.2.3”, ustawiono kursor na końcu i naciśnięto Ctrl+Shift+V. Dodana jedna kopia; jeden Ctrl+Z usunął tylko wklejenie, Ctrl+Y odtworzył. Menu pokazało pełny skrót. Nie zmienił się teren ani wybór presetu.
- Zakończono własne edycje przez Undo do czystego stanu i zamknięto okno. Nie usuwano presetów przez UI, nie ponawiano testu Kosza ani innych zakończonych scenariuszy 0.2.2.

Próby funkcjonalne zwykłej kompilacji miały SHA-256 `728238679B48D4302A547E77A068B2F95AC6FFAEFE0BC97BE250AC8F17A875D3`. Po końcowej kosmetyce jednego starego komunikatu i buildzie identyczność głównego EXE/kopii potwierdzono SHA-256 **E666BD44D7DBF1192477F37E20C8A3CF8661909C0861531DF0C74A348E905688**; wykonano dodatkowo start końcowej zwykłej kopii bez ostrzeżeń i zamknięcie, bez ponawiania wszystkich powyższych prób.

Nie wykonywano nowej kampanii Worda/formatowania/tabel, wszystkich funkcji prawego panelu, wysokiego DPI, awarii zasilania ani wyścigu równoległych procesów zapisujących ten sam plik. Zewnętrzną zmianę/zniknięcie, błędne pliki i konflikty refresh sprawdzono metodami na własnych danych; w UI sprawdzono import poprawnego pliku i zachowanie roboczej nazwy. Przeskok prawego nagłówka pozostaje. Test metod skrótu nie jest przedstawiany jako próba fizycznych klawiszy — tę wykonano osobno.

## Biblioteka, pliki i dalsza praca

Usuniętej przez użytkownika Library i kopii nie odzyskiwano. Nie migrowano ani nie używano prywatnych presetów do testów. Wzorzec Domyślny pochodzi z kodu; pusta biblioteka jest prawidłowa. Dawny raport 0.2.2 opisuje historyczną migrację, nie aktualną obecność tych kopii. Nic z Library/tests/_work/bin nie jest śledzone; reguły ignorowania pozostają. Źródła/skrypty testów i główny EXE pozostają śledzone. Brak głównego bin.

Po potwierdzeniu zamknięcia okna i braku procesów testowych usunięto wyłącznie własny przebieg ui-023-510986ec0329497a9097eedc7143b648 (kopię EXE i trzy własne pliki presetów wraz z izolowaną strukturą). Pozostawiono ignorowany log zbiorczy. Materiał odtwarza śledzony skrypt. Nie sprzątano prywatnej biblioteki ani wcześniejszych przebiegów.

Zmiany: AssemblyInfo, HexMapEditor (wersja/napisy), MapModel + nowy PresetFiles (zapis), PresetOperations (refresh/napisy), EditorDialogs/EditorShell (UI), TerrainTile, RichNotes; PresetLibrary wyłącznie końcowy napis. Testy: ConvenienceTests, Run-ConvenienceTests, Create-ConvenienceUi, runner zbiorczy i oczekiwanie palety PatchTests. Dokumenty: README, specyfikacja 1.6, plan i ten raport. Nie obejmowano cudzych zmian (stan początkowy czysty).

Odłożone, bez wdrożenia: siedem heksów w schemacie rozmiaru, tymczasowe wyróżnienie zagrożonych pól, zakładki projektów, dalsza stylistyka/grupy ostrzeżeń, przebudowa opisów. Numer konkretnego wydania zakładek nie został narzucony.

Uruchamianie: główny `HexMapEditor.exe` lub dotychczasowy BAT. Build: `Build.ps1`. Regresje: `tests/Run-AllTests.ps1`; dane UI odtwarza `tests/Create-ConvenienceUi.ps1`. Końcowy commit/status podano po utworzeniu commita w odpowiedzi, bez samoodwołania dokumentu do własnego hasha.

## Checklista użytkownika

1. Nowy preset → nazwa z polskimi znakami → czytelny pojedynczy plik.
2. Zmień nazwę → plik jeszcze bez zmian; Zapisz preset / Nadpisz → nowa nazwa jednego pliku. Undo ma zmienić tylko kopię roboczą.
3. Skopiować cudzy poprawny plik do Presets, kliknąć Odśwież; bieżąca robocza paleta ma pozostać.
4. Sprawdzić 2×2 przyciski, marginesy, przewijanie i czerwony kosz na swoim DPI.
5. W opisie Ctrl+Shift+V, jedno Undo/Redo; Ctrl+V nadal formatowane.
