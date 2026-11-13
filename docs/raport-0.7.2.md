# HexMapEditor 0.7.2

Ukończona mała aktualizacja na bazie sprawdzonego HEAD `1f2bc36` (implementacja 0.7.1: `092c584`). Aplikacja 0.7.2, FileVersion 0.7.2.0. Map11/preset4 i limity pozostają. Zachowane zaakceptowane WPF/start lifecycle, gumka, zoom, gradienty, High/Low i 12 kolorów; bez ponownego audytu tych obszarów. Przy rozpoczęciu Git był czysty.

## Dwie ścieżki Nowa

Istniejąca produkcyjna reguła `LibraryPaths.Hexmaps` wskazuje `Library/Hexmaps` obok EXE. Dla głównego programu jest to **C:\ALTERNATYWNA PARTYCJA\HexMapEditor-Codex\Library\Hexmaps**; odizolowana kopia korzysta ze swojego katalogu. Nie powstał drugi magazyn map.

Ze startu zatwierdzenie dotychczasowego NewMapDialog zapisuje początkowy plik bez SaveFileDialog, a dopiero sukces daje ścieżkę/MRU/czysty stan i przejście do edytora. Nazwy są walidowane jako pliki Windows, w tym reserved device names, znaki, końcowa kropka/spacja, długość, rozszerzenie i ograniczenie do katalogu. Kolizja case-insensitive wymaga nowej nazwy; dialog zachowuje parametry, bez sufiksów i nadpisania. Wspólny writer ma create-only na publikacji, nie tylko wstępne File.Exists. Błąd/Cancel pozostawia działający start.

Nowa z edytora tworzy mapę w pamięci bez ścieżki/pliku/MRU. Jawne `needsFirstSave` chroni także nietknięty nowy dokument; nie dodaje fikcyjnej historii ani danych heksa. Pierwsze Zapisz/Ctrl+S ma istniejący dialog. Cancel zachowuje dokument, sukces przypisuje plik i clean; kolejne Ctrl+S zapisuje ten sam plik. Zapisz jako i ochrona poprzedniej niezapisanej mapy pozostają.

## Autozapis

Interwał produkcyjny **5 minut / 300000 ms**, monotoniczny zegar. Udane otwarcie, utworzenie pliku i zapis ręczny/automatyczny zaczynają nowy okres. Malowanie nie przesuwa terminu. Tyknięcie co sekundę jest tanim sprawdzeniem flag, nie serializacją. Zapis dotyczy tylko dirty z przypisaną ścieżką; brak pliku nie uruchamia dialogu ani automatycznego nadawania nazwy.

Gest, modal, gumka, resize panelu i aktywna sesja edycji odraczają jedno żądanie. Odłączona migawka kopiuje metadane/punkty/parametry, współdzieli niezmienne duże payloady; zasoby wybrane do dokumentu są już przypięte niezależnie od biblioteki. Jeden worker MTA przygotowuje wspólną walidację, serializację, zapisany stan i IO. UI sprawdza dokument/rewizję/ścieżkę przed zgodą na publikację. Konfliktujące edycje, historia i ręczny zapis podczas pojedynczego zadania dostają status i pozostają zablokowane logicznie; nie ma równoległego SaveAs ani kolejki. Escape/zamknięcie anuluje oczekujące przygotowanie. Zamknięcie należy ponowić po zakończeniu; rozpoczętego publikowania pliku nie przerywamy w połowie.

Sukces aktualizuje punkt zapisu i tytuł, zachowuje stos Undo, nie tworzy kroku i nie wywołuje pełnego UpdateUI; status „Zapisano automatycznie”. Błąd pozostawia dirty, niemodalny komunikat i retry najwcześniej po kolejnych 5 minutach. Zmiana/zniknięcie pliku wstrzymuje autozapis do ręcznego rozwiązania/Zapisz jako. Nie ma konfiguracji, autozapisu presetów/Library ani backupów.

### Ochrona pliku i kompromis transakcji

Wspólny serializer/walidator/writer obsługuje obie ścieżki i autozapis. SHA-256 i długość bajtów otwartego/zapisanego pliku są fingerprintem poza formatem. Same-path Save również wykrywa konflikt. Nowy plik publikowany create-only po Flush(true). Przy chronionej aktualizacji writer sprawdza cel, przenosi go do własnej nazwy transakcji, ponownie sprawdza faktycznie przeniesione bajty i publikuje create-only. Obcy plik utworzony w wyścigu nie zostaje nadpisany. Błąd próbuje rollback; gdy obcy cel go blokuje, poprzednie bajty zostają w wskazanym pliku transakcji. Potwierdzony jawny overwrite przez Zapisz jako zachowuje Flush/Replace.

To dwa atomowe przeniesienia z krótką nieobecnością docelowej nazwy, nie pojedynczy atomowy Replace dla sprawdzanej wersji. Nie gwarantujemy ochrony przed każdą awarią zasilania/dysku. Nie jest to system odzyskiwania ani seria kopii zapasowych. Duży dokument nadal wymaga skopiowania metadanych na UI; nie zmierzono czasu tej operacji przy maksymalnych limitach. Walidacja/serializacja/IO są na workerze. Nie podniesiono budżetów ani nie osłabiono walidacji.

## Autor, data i backlog

Pierwszy wiersz: duże, niezmienione **HexMapEditor**, obok **by amdesar** stylem starego napisu wersji i przy jego linii pisma. Drugi: **Wersja 0.7.2 • 13.11.2026**, w dotychczasowej pozycji i stylu. Autor nie dodaje trzeciego wiersza i nie przesuwa przycisków/recent. Jedynym źródłem jawnej daty jest `src/ReleaseMetadata.cs`, stała `Date`; nie data uruchomienia/mtime. Ikona bez zmian.

Brak stałego przycisku/linku GitHub, sieci, logowania, zmian remote i aktualizatora. Plan 0.13.0 zachowuje offline-friendly sprawdzanie w tle, ręczne sprawdzenie, możliwość pominięcia i ochronę danych. Szczegóły Release: link albo osobne okno — wybór niezatwierdzony, tylko zapisany. Szerszy panel preferencji po 1.0 (autozapis, wartości/kolory, własny domyślny preset, ewentualne skróty/maksymalizacja) jest oddzielony od lokalnego układu 0.12.0. Bez konfiguratora i odblokowania chronionego Domyślnego.

## Testy metod i kontrolek

Runner `tests/Run-Patch072Tests.ps1` wymaga jednego Case. Guard bez Case sprawdzono raz: czytelny błąd, bez uruchomienia prób i nowego katalogu. Nie uruchamiano Run-AllTests, pełnej kampanii ani hurtowo dawnych runnerów. Każdy poniższy Case wykonany raz; nie powtarzano zaliczonych prób po niezależnej zmianie.

| Case | Wynik i zakres | Folder pod tests/_work |
|---|---|---|
| creation | PASS: rzeczywisty handler NewMapDialog/ConfigureStart, katalog, create-only, map11/MRU, case-insensitive collision i zachowanie parametrów/retry, extension/reserved/traversal, IO i Cancel | patch-072-creation-5d7a4ebce0be41258ff621a2e4bd3214 |
| editor | PASS: produkcyjne NewProject z dialogiem, pamięciowy nowy, brak MRU/pliku/historii, pytanie nietkniętego, Cancel porzucenia i pierwszego Save, pierwszy/kolejny Save, brak autozapisu bez ścieżki | patch-072-editor-60f93d1cdb414fe88c7d3f43ba60fa31 |
| autosave | PASS: kontrolowany zegar, termin 300000 ms, defer gest/modal/gumka, jedno żądanie, worker/writer/clean, Undo zachowane, blokada ręcznego Save/historii, zero pełnych UpdateUI; konflikt, IO, cancel i stale przed publikacją | patch-072-autosave-b84e49f32eef436793936a434a62ef90 |
| controls | PASS: tytuł 28 bold bez zmiany, położenie autora/wersji, wspólny font/kolor małych napisów, data, fixed start i brak GitHub | patch-072-controls-cd29a0ca89de457689688eec99bbd596 |
| safety | PASS: odłączone metadane i wspólne payloady, krótki map11 round-trip mieszanych źródeł/kadrów/Fog/Ink/tła z dedup, obcy/utracony cel bez overwrite, ręczny konflikt i jawny SaveAs | patch-072-safety-fbf30a4b261146d691b25ca8dc21787a |

Kontrolki obsługiwane przez testowy handler i kontrolowany zegar nie są ręczną próbą zwykłego EXE. Safety sprawdza zależności migawki/writera, nie powtarza pełnego odbioru formatów/rendererów.

## Zwykły finalny EXE

Build głównego `HexMapEditor.exe` zakończony powodzeniem. FileVersion **0.7.2.0**. Kopia w `tests/_work/ui-072-a8064f959d7e4af692f0a9623ddb4663` identyczna bajtowo z głównym: SHA-256 **CDC4C395A6E820628A8463E22E1936AD2B878C3835AEB23C7EFE7C91C5D5C244**. Obsługa przez [Computer Use](C:/Users/admin/.codex/plugins/cache/openai-bundled/computer-use/26.1106.51428/skills/computer-use/SKILL.md), wyłącznie własne dane.

- Autor/data/nazwa widoczne w dwóch wierszach, bez kolizji/trzeciego wiersza/GitHub; start i recent zachowane.
- Start → Nowa mapa: plik w własnym Library/Hexmaps bez SaveFileDialog, clean i MRU. Po ponownym uruchomieniu ta sama nazwa dała komunikat kolizji, zachowany dialog i udane utworzenie po zmianie nazwy na „Próba startu 0.7.2”.
- Nowa z edytora „Pamięciowy 0.7.2”: brak pliku i wpisu MRU potwierdzony odczytem własnego folderu, gwiazdka także bez malowania. Alt+F4 → pytanie → Anuluj zachowało dokument.
- Ctrl+S → pierwszy dialog → Anuluj zachowało dokument. Kolejny Ctrl+S zapisuje wybrany plik i clean. Pierwszy Save nie był symulowany przez test modelu.
- **Rzeczywiste pięć minut**: po ręcznym zapisie o 07:45:25 czasu Europe/Warsaw pomalowano jeden heks i nie używano Ctrl+S. O 07:50:25 plik otrzymał nowy zapis; odczyt wykazał jeden heks Równina. Wcześniej przed terminem pozostawał niezmieniony. Końcowe okno bez gwiazdki. To osobna rzeczywista próba timera, oprócz Case z kontrolowanym zegarem.
- Po próbie zamknięto własne czyste okno; końcowy inventory nie wykazał go. Okien użytkownika nie zamykano. Własne dane prób pozostawiono w tests/_work; bez szerokiego sprzątania. Prywatnych map, Library i głównego recent-projects.json nie czytano jako fixture ani nie zmieniano.

Jednorazowe błędy odczytu bounds/capture narzędzia obsłużono świeżym inventory/odczytem przed kolejną akcją, bez ślepego ponawiania niepewnego wpisywania.

## Ograniczenia i niewykonane próby

Nie wykonano pełnej regresji/przenośności, dawnych kampanii WPF/DPI/grup/gumki/zoomu/HighLow ani nagrania klatek. Nie deklarujemy pełnej płynności lub braku pojedynczego flasha na podstawie screenshotów. W zwykłym EXE nie symulowano awarii dysku/uprawnień ani zewnętrznego konfliktu podczas publikacji — te reguły sprawdzono metodami na własnych plikach. Nie benchmarkowano autozapisu dokumentu maksymalnej wielkości i nie wykonywano serii wielu prawdziwych interwałów; rzeczywisty interwał sprawdzono raz. Błąd i stale/cancel są celowanymi próbami ścieżki workera.

## Checklista użytkownika

- [ ] Nowa ze startu zapisuje w Library/Hexmaps bez wyboru ścieżki; clean i Ctrl+S.
- [ ] Kolizja nazwy, także inną wielkością liter: komunikat, zachowane parametry, bez overwrite/sufiksu.
- [ ] Błędna nazwa/błąd zapisu pozostawia działający start i możliwość poprawy.
- [ ] Nowa z edytora pozostaje bez pliku/MRU; także nietknięta wymaga decyzji o zapisie.
- [ ] Cancel Nowa/Otwórz/zamknięcia lub pierwszego zapisu zachowuje dokument; pierwszy sukces przypisuje plik, kolejne Ctrl+S bez dialogu.
- [ ] Dirty z plikiem zapisuje się po 5 minutach; praca nie odsuwa terminu, gest/modal/gumka odracza jedno żądanie.
- [ ] Bez ścieżki nie ma automatycznego pliku/dialogu; clean nie jest ponownie zapisywany.
- [ ] Sukces autozapisu usuwa gwiazdkę, zachowuje Undo i pokazuje status; błąd pozostawia dirty, konflikt wymaga ręcznej decyzji/SaveAs.
- [ ] Autor obok niezmienionej dużej nazwy; wersja/data w drugim wierszu; brak GitHub i aktualizatora.
- [ ] Wąska łatka zachowuje zaakceptowany renderer, High/Low i historię kolorów; bez potrzeby nowej pełnej kampanii.

## Git

Kod, główny EXE i celowane testy: **92fdf11** (`Implement HexMapEditor 0.7.2 project creation rules and guarded autosave`). Następny lokalny commit obejmuje README, specyfikację, plan wersji, backlog preferencji, plan i ten raport. Przed nim pozostały wyłącznie te własne zmiany dokumentacji; końcowy odczyt Git i identyfikator dokumentacji podaje podsumowanie zadania. Prywatny ignorowany MRU/Library i testowe _work zachowane. Bez push, tagu i Release'a.
