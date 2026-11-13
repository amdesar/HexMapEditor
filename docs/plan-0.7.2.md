# Plan HexMapEditor 0.7.2

## Baza i granice

Potwierdzony HEAD1f2bc36, kod092c584; Git czysty. Odczytano raport0.7.1 i spec41, kod startu/Nowa/writera/dirty oraz backlogi. Brak AGENTS.md w repo. Nie czytamy prywatnego MRU/Library/map; dane prób wyłącznie tests/_work. Bez kampanii renderera i innych zaakceptowanych obszarów, sprzątania, push/tagu/Release'a.

## Etapy

1. Rozdzielić Nowa: start zapisuje nowy plik w istniejącym LibraryPaths.Hexmaps (Library/Hexmaps obok EXE), po walidacji nazwy i create-only, zachowując dialog parametrów po kolizji/błędzie. Edytor przyjmuje mapę bez ścieżki i z jawnym stanem wymaganego pierwszego zapisu, bez fikcyjnej historii.
2. Autozapis: stałe5min od otwarcia/utworzenia/udanego zapisu, jedno oczekiwanie. Tani zegar sprawdza dirty/path i bezpieczny stan (gest/modal/gumka/edycja/saving). Przygotowanie odłączonej migawki i wspólny walidator/serializer/writer. Zabezpieczyć zewnętrzne wersje, błąd bez ciasnego retry i brak konfliktu z ręcznym SaveAs/zmianą dokumentu. UI bez pełnego UpdateUI po sukcesie.
3. Autor obok zachowanej dużej nazwy; wersja/data w obecnym drugim wierszu. Jedno jawne źródło ReleaseMetadata, data13.11.2026. Żadnego linku/przycisku GitHub ani sieci. Dopisać tylko backlog0.13 oraz preferencje po1.0 odrębne od layoutu0.12.
4. Kilka jawnych Case: start/path/kolizje, pamięciowy dokument, kontrolowany zegar autozapisu/safety/roundtrip, layout. Finalny0.7.2.0/hash i krótki zwykły EXE, ewentualnie jeden rzeczywisty5min. Dokumentacja/raport/localcommity/status.

## Decyzje wstępne

Katalog istnieje jednoznacznie w LibraryPaths.Hexmaps i CreateSaveDialog; nie tworzymy nowego magazynu map. LibraryFiles chroni bibliotekę bajtami, ale writer map nie śledzi dotąd zewnętrznej wersji. Autozapis musi otrzymać fingerprint otwartego/zapisanego pliku i create/replace-safe sprawdzenie. Domyślny preset/HighLow/WPFprep/startlifecycle pozostają. Bez nowego map/preset formatu.

## Punkt wznowienia

Wykonane odczyty i plan. Implementacja/testy jeszcze nie rozpoczęte. Niepewna operacja: brak. Własnych procesów0.7.2 nie uruchomiono; przed UI inventory. Nie oznaczono EXE jako0.7.2.

## Etap1 — implementacja ścieżek i autozapisu (jeszcze przed testami)

StartPath/CreateInHexmaps używa Library/Hexmaps, odrzuca błędne/reserved/path traversal, sprawdza kolizję case-insensitive i create-only File.Move. Ten sam NewMapDialog przy kolizji/błędzie pozostaje z parametrami. Nowa edytora adoptuje pamięciowo; needsFirstSave jest odrębną flagą UX (nie dane/history/dirty), caption/pytanie uwzględniają ją. Dotychczasowy SaveAs zachowany. Autor/date wstawione z ReleaseMetadata13.11.2026, bez zmiany dużej nazwy/przycisków.

Autozapis: monotoniczny zegar5min, timer1s z tanimi guardami, jedno pending; reset po sukcesie/open/filecreate. Przygotowanie migawki metadata/punktów naUI bez kopiowania pełnych payloadów string; workerMTA waliduje/serializuje i przygotowuje savedState. Przed publikacją UI sprawdza dokument/rewizję/ścieżkę, udziela zgody jednemu workerowi. Podczas pracy wąska logiczna blokada konfliktujących wejść/poleceń, bez Disabled drzew/layoutu; Close/Escape anuluje przygotowanie z jawnym statusem (należy ponowić zamknięcie po zakończeniu). Worker używa wspólnego PrepareProjectBytes/WritePreparedProject. Fingerprint SHA256 bajtów odczytanego/zapisanego pliku, checked park→recheck→Move→cleanup z rollback, nie backup-system. Konflikt suspend do ręcznego rozwiązania/SaveAs, inny błąd nie częściej niż kolejny okres5min. Sukces nie kasuje Undo i nie wywołuje UpdateUI. Bez map/preset formatu zmian. To decyzje implementacyjne do weryfikacji, nie zaliczone próby.

Pierwsza kompilacja do tests/_work/HexMapEditor-072-compile.exe powiodła się przed ostatnimi drobnymi guardami; główny EXE nadal0.7.1. Pozostałe: celowane testy/ocena safety, ewentualne poprawki, wersjaAssembly/finalbuild/UI/docs/commity. Niepewna operacja: brak. Własnych okien0.7.2 jeszcze nie uruchomiono.

## Etap2 — celowane metody/kontrolki

PASS creation-5d7a4ebce0be41258ff621a2e4bd3214: rzeczywisty handler NewMapDialog, zapis Library/Hexmaps bez SaveFileDialog, kolizja case-insensitive z zachowaniem kolumn/nazwy, retry, invalid/reserved/traversal, create-only refusal, IO bez zamknięcia dialogu, Cancel. PASS editor-60f93d1cdb414fe88c7d3f43ba60fa31: produkcyjny NewProject z rzeczywistym NewMapDialog, pamięciowy untamperedNeedsSave, Cancel porzucenia i pierwszegoSave, pierwszy/ponownySave, brak autosave bez ścieżki. PASS autosave-b84e49f32eef436793936a434a62ef90: kontrolowany zegar300000ms, defergesture/modal/pendinggumka, jedno żądanie, worker/sharedwriter/savedState, blokada manualsave/history, brak UpdateUI, Undo zachowane, conflict/IO/cancel/stale; nie było realnego czekania5min. PASS controls-cd29a0ca89de457689688eec99bbd596: title28bold bounds120,32,281,51; autor409,58,80,19; version124,83,167,19, identyczny font/color autora i wersji; fixed/noGitHub.

Przegląd dodał tylko guard tekstowego Undo podczas autozapisu oraz poprawił reserved stem CON .txt. Osobny PASS safety-fbf30a4b261146d691b25ca8dc21787a: odłączone metadata i wspólny immutable payload, map11 z grafika/kadrami+gradientFog+Ink+tło i dedup1source, brak odtworzenia znikniętego pliku, konflikt ręcznegoSave bez overwrite i jawny SaveAs do innego celu. Nie ponawiano zaliczonych przypadków po niezależnych zmianach.

Wszystkie nowe dane/logi w tests/_work. Budżety/formats bez zmiany. Model autozapisu blokuje konfliktujące edycje na czas pojedynczego workera, więc nie ma równoległego manualnegoSaveAs czy zmiany mapy; identyfikator/rewizja/path sprawdzane ponownie przed pozwoleniem na publikację. Close/Escape anuluje przygotowanie, operację zamknięcia należy ponowić po zakończeniu; w trakcie samego atomowego IO czekamy na wynik. Nie ma kolejki. Dopiero UI zwykłego EXE i ewentualne realne5min pozostały, wraz z dokumentacją/backlogami/wersjąAssembly/finalbuild/commity. Główny EXE nadal0.7.1. Ostatnia niepewna operacja: brak; własne procesy testów zakończone, zwykłych0.7.2 jeszcze nie uruchamiano.

## Etap3 — finalny kandydat i trwający rzeczywisty interwał

Rootbuild0.7.2.0 i byte-identical kopia tests/_work/ui-072-a8064f959d7e4af692f0a9623ddb4663/HexMapEditor.exe, SHA256 CDC4C395A6E820628A8463E22E1936AD2B878C3835AEB23C7EFE7C91C5D5C244. Guard runnera bez Case sprawdzony raz: czytelny błąd, brak nowego folderu/prób. Ordinary EXE: autor/data/nazwa/style/fixed/recent obejrzane; Nowa ze startu stworzyła Library/Hexmaps/Nowa mapa.hexmap bez SaveFileDialog, clean iMRU. Po restarcie kolizja tego samego domyślnego imienia daje komunikat i zachowuje dialog; zmieniono nazwę na Próba startu0.7.2 i utworzono poprawnie. Nowa z edytora Pamięciowy0.7.2: brak pliku/MRU, gwiazdka już na nietkniętej mapie. AltF4 wywołał pytanie; Cancel zachował mapę. CtrlS→Cancel pierwszegoSave zachował mapę, następnyCtrlS zapisał we wskazanym domyślnym folderze. Błędy CUA bounds/capture zostały odświeżone przed kolejną akcją, nie powtarzano niepewnego wpisywania nazwy.

Trwa JEDNA próba rzeczywistych5min na tej pamięciowej mapie po ręcznym zapisie: początkowy LastWriteTimeUtc06:45:25,13.11.2026 (czas lokalny07:45:25 Europe/Warsaw). Termin około06:50:25UTC/07:50:25PL. Pomalowano jeden heks, dokumentdirty. Nie zapisywać ręcznie ani nie zmieniać dokumentu przed sprawdzeniem wyniku; nie zamykać własnego okna. Własny proces ma app path ui-072-a8064f..., główne oknoHexMapEditor0.7.2*. Ostatnia niepewna operacja: wynik pięciominutowego timera jeszcze nieznany. Pozostałe docs/backlog/raport, sprawdzenie realnego autoSave, zamknięcie własnego okna, commity/status.

## Etap4 — zakończona próba EXE i dokumentacja

Rzeczywisty interwał PASS: przed terminem plik bez zmian; LastWriteTimeUtc zmienił się z 06:45:25 na 06:50:25 dnia13.11.2026 (lokalnie 07:45:25→07:50:25). Odczyt własnego pliku wykazał jeden pomalowany heks Równina bez ręcznego Ctrl+S. Świeży odczyt okna potwierdził czysty tytuł i mapę „Pamięciowy 0.7.2” bez gwiazdki. Wysłano Alt+F4 wyłącznie do własnego okna; późniejsze inventory potwierdziło jego brak. Nie powtarzać tej próby. Brak własnych otwartych okien/dialogów.

Ostatni odczyt przed wznowieniem był ucięty przez limit wyjścia; był tylko odczytem Git/źródeł/UI. Jego stan ustalono świeżym odczytem, bez ponawiania operacji dyskowych. Weryfikacja źródeł dokumentu: wybór biblioteki grafiki używa DocumentCopy/Pin, tła Pin, odczyt mapy/presetu ma własne osadzone dane. Nie dodano spekulacyjnej zmiany modelu źródeł.

README, specyfikacja sekcja42, plan wersji, backlog preferencji oraz raport0.7.2 zaktualizowane. Opisano także kompromis checked writera: dwa atomowe rename z krótką nieobecnością celu, rollback bez nadpisania obcej wersji; bez gwarancji każdej awarii zasilania. Map11/preset4/limity bez zmian. Brak nowego linku/przycisku GitHub i sieci; aktualizator i preferencje tylko backlog.

Pozostałe wyłącznie końcowe odczyty diff/hash/FileVersion, lokalne commity własnej pracy i końcowy Git. Testów nie ponawiać po zmianie samej dokumentacji. Ostatnia niepewna operacja: brak. Własne dane zachowane pod tests/_work; bez sprzątania i bez modyfikowania prywatnych danych.

## Zakończenie

Zakres 0.7.2 ukończony. Kod/EXE/testy zapisane lokalnie w **92fdf11**. FileVersion/ProductVersion 0.7.2.0, root i kopia UI mają zgodny SHA-256 CDC4C395A6E820628A8463E22E1936AD2B878C3835AEB23C7EFE7C91C5D5C244. `git diff --check` bez błędów (jedynie informacyjne ostrzeżenia normalizacji CRLF). Odczyt istniejących pięciu results.log potwierdził PASS; nie uruchamiano ich ponownie. Raport oddziela kontrolowany zegar od rzeczywistego pięciominutowego zapisu i podaje ograniczenia.

Następna operacja: wyłącznie commit dokumentacji i kontrolny Git. Nie jest to kolejny etap implementacji ani punkt do ponownego uruchamiania prób. Ostatnia niepewna operacja: brak; nie ma własnych otwartych procesów/dialogów. Bez push/tagu/Release'a.
