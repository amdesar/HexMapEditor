# HexMapEditor 0.8.0

Baza sprawdzona lokalnie: HEAD **6e55ccf**, implementacja **92fdf11**. Początkowy Git czysty. Ukończono menu/szybki pasek, wspólne pola liczbowe, lokalny ciemny motyw i rename pliku. FileVersion/ProductVersion **0.8.0.0**. Map11/preset4, struktura Library i limity bez zmian. Bez push, tagu i Release'a.

## Polecenia: kompletne przeniesienie wejść

| Dawne wejście | Menu 0.8.0 | Szybki pasek / uwagi |
|---|---|---|
| Nowa | Plik → Nowa… | ikona; dotychczasowa pamięciowa Nowa edytora |
| Otwórz | Plik → Otwórz… | ikona; istniejąca walidacja i pytanie o zmiany |
| recent startu | Plik → Ostatnie projekty | ta sama historia30 i dostępne5; start pozostaje |
| Zapisz | Plik → Zapisz | ikona, Ctrl+S |
| Zapisz jako | Plik → Zapisz jako… | Ctrl+Shift+S |
| zamknięcie Windows | Plik → Zakończ | Alt+F4 / standardowa ramka |
| Cofnij/Ponów | Edycja → Cofnij/Ponów | dwie ikony, Ctrl+Z / Ctrl+Y |
| nowe rename | Edycja → Zmień nazwę pliku… | tylko dokument ze ścieżką |
| Rozmiar | Mapa → Rozmiar mapy… | ta sama transakcja/ostrzeżenie |
| toggle orientacji | Mapa → Orientacja → Pointy Top / Flat Top | dwa stany jednego modelu |
| Tło | Mapa → Tło mapy… | istniejący edytor i historia |
| Widok | Widok → Warstwy | wszystkie7 warstw w dotychczasowej kolejności |
| etykiety | Widok → Osie | Osie / Współrzędne w heksach / Brak współrzędnych; żaden dawny tryb nie zginął |
| jakość | Widok → Jakość → Wysoka / Niska | jeden przycisk High/Low, ten sam stan |
| zoom100 | Widok → Powiększenie | bieżący procent z dropdownem, presety i Niestandardowe… |
| dropdown Dopasuj | Widok → Dopasuj widok → mapę/wszystko | dwie osobne, rozróżnialne ikony |
| Biblioteka | Biblioteka → Otwórz główny folder biblioteki | wyłącznie Eksplorator, bez komunikatu sukcesu |
| nowe O programie | Pomoc → O programie… | ikona, autor, wersja/data ze wspólnych metadanych |

Szybki pasek: Nowa, Otwórz, Zapisz / Cofnij, Ponów / zoom, Dopasuj mapę, Dopasuj wszystko / High–Low. Ikonowe polecenia mają tooltipy i tylko rzeczywiście istniejące skróty. Jedna funkcja `ExecuteCommand` dla menu i szybkich przycisków, operacje danych używają istniejących metod. Skróty zapisu również trafiają do dispatchera. Historia tekstu heksa i WPF zachowuje właściwe ścieżki, zwykły TextBox swoje Undo; menu pamięta edytor sprzed przejęcia fokusu. Nie przypisano globalnego ShortcutKeys Ctrl+Z do historii mapy. Ctrl+Shift+Z nadal bez operacji. Dialogi lokalne mają własną historię i fokus.

Warstwy: Tło → Tereny → Ikony → Obiekty → Ograniczenie widoczności → Mgła → Rysik. Submenu warstw pozostaje otwarte przy przełączaniu, stan pochodzi z istniejących flag. Menu/toolbar korzystają ze wspólnej dostępności Undo/Redo; blokady gumki/zapisu zachowane, bez disabled całych paneli i pełnego UpdateUI przy wheel.

Foldery Biblioteki ustalono z rzeczywistego EnsureDirectories: **Tereny**, **Presety terenów**, **Grafiki terenów**, **Ikony**, **Obiekty**, **Stany obiektów**, **Tła**, **Mapy**, **Eksport obrazów**, **Eksport tekstu**. Odpowiadają Terrains, Terrains/Presets, Terrains/Graphics, Icons, Objects, Objects/States, Backgrounds, Hexmaps, Exports/Images, Exports/Text. Sources są wewnętrznym magazynem, nie oddzielną kategorią użytkową. Standardowe katalogi inicjalizowane dotychczasową metodą; otwarcie przez ProcessStartInfo/UseShellExecute, bez cmd. Selektory i zarządzanie grafikami w edytorach pozostają.

## Dwa wiersze, ramka i akcenty

Standardowa ramka Windows zachowuje resize/przesuwanie/minimize/maximize/close, systemowe menu i double-click; brak custom chrome. Best-effort `DwmSetWindowAttribute` dark caption20, fallback19, a przy braku DLL/entry/wsparcia zwykły pasek. Nie dodano runtime. Tytuł: **HexMapEditor — nazwa pliku/mapy \***; gwiazdka pochodzi z dirty lub needsFirstSave. Usunięto powtórzoną nazwę ze środka dawnego paska.

MenuStrip i ToolStrip z overflow zajmują dwa stałe wiersze nad całym płótnem i panelami. Brak zawijania w trzeci rząd i reorganizacji przez użytkownika. Minimum **1180×748** pozostaje; wysokość wierszy wynika też z fontu. Przy mniejszej dostępnej szerokości narzędzia przechodzą do wbudowanego overflow, bez zmniejszania ikon i rozciąganych odstępów. Sprawdzono overflow przy240px w kontroli oraz minimum i skalowanie kontrolek125%; nie jest to test fizycznego monitora125%.

Zapis dirty/needsFirstSave: **#D77A68**, ikona i ramka, delikatne ciemne tło. Low: **#4FA7A0**, tekst i taka sama geometria ramki/tła. Clean/High neutralne, bez pulsowania i bez złota. Sukces ręcznego/automatycznego zapisu odświeża caption/akcent zgodnie ze stanem, błąd nie gasi sygnału. Hover/focus/disabled zachowane. Dopasowano pressed/selected gradienty nagłówków menu do ciemnej palety; nie zmieniono zaakceptowanego Jednolity/Gradient.

Zoom: presety **25/50/75/100/125/150/200/300/400%**, wszystkie mieszczą się w dotychczasowym zakresie kamery. Niestandardowe **1–12800%**, dwie cyfry ułamka; krok1%, czułość4 logiczne px/krok. Widoczny caption zaokrągla procent jak dotąd. Zmiana z menu/paska zachowuje środek płótna, nie dirty; kółko zachowuje poprzedni zakres/zakotwiczenie. Nie ma pełnego UpdateUI na każdy wheel. Dopasuj wszystko używa dotychczasowej obwiedni widocznych warstw.

## Rename, autozapis i fingerprint

Plik przenoszony w tym samym folderze, bez SaveAs/kopii/serializacji mapy. Dialog pokazuje starą i nową nazwę. Wspólna walidacja Windows z tworzeniem projektu: znaki, reserved names, długość, rozszerzenie, traversal i ograniczenie do katalogu. Kolizje case-insensitive odmawiają overwrite. Zmiana wyłącznie wielkości liter używa prywatnej nazwy pośredniej i drugiego Move.

Sprawdzana jest istniejąca wersja pliku, następnie faktycznie przeniesione bajty. Błąd próbuje rollback. Jeśli obcy plik blokuje rollback, dane pozostają pod podaną w błędzie nazwą, bez nadpisywania obcej ścieżki. Dwa rename case-only mają krótki etap pośredni; nie obiecujemy odporności na każdą awarię zasilania. Po sukcesie currentPath, tytuł i MRU wskazują nowy plik, stary MRU jest usuwany bez duplikatu; fingerprint bajtów pozostaje poprawny. Nie zmienia się treści pliku, jego bazowego MapData.Name ani historii. Widoczna nazwa dokumentu wynika z nowej ścieżki. Clean pozostaje clean, dirty nadal wymaga zapisu.

Rename jest blokowany podczas aktywnego autojob/gumki i kończy gest przed przeniesieniem; ponowny guard chroni asynchroniczne zakończenie. Inkrement rewizji nie tworzy kroku danych. Następny autozapis używa nowego currentPath, bez odtwarzania starego. Sam rename nie resetuje okresu5min. Pamięciowy dokument ma niedostępne polecenie z tooltipem wyjaśniającym pierwszy zapis. Nie przebudowano writera0.7.2. Nowa ze startu, pamięciowa Nowa z edytora, needsFirstSave/MRU/autozapis oraz pytania przy porzuceniu zachowane.

## Wszystkie zastąpione pola liczbowe

| Okno/pole | Min–max | Krok / precyzja | Podgląd i transakcja |
|---|---|---|---|
| NewMap: kolumny, wiersze | 1–200 | 1 / 0 | robocze, dane mapy po Utwórz |
| Resize: góra, dół, lewo, prawo | -199–199 | 1 / 0 | live rozmiar, commit z dotychczasowym ostrzeżeniem |
| Wstaw tabelę: wiersze, kolumny | 1–100 | 1 / 0 | roboczy dialog, bez przebudowy WPF |
| Przybornik: rozmiar/szerokość, wysokość | 1–15 | 1 / 0 | ustawienie narzędzia, bez historii mapy |
| Rysik/gumka wspólny rozmiar | 1–200, default5 | 1 / 0 | ustawienie ekranowe, 8px/krok, bez map history |
| Instancja: rozmiar | 8–1024 | 1 / 0 | live właściwość, jedna sesja/drag |
| Instancja: promień Fog i widzenia | 0–20 | 1 / 0 | live właściwość |
| Grupa: promień Fog i widzenia | 0–20 | 1 / 0 | nakładka, baza bez zmiany |
| Grupa: rozmiar/delta | -1016–1024 | 1 / 0 | dotychczasowe ograniczenia trybu i członków |
| Edycja ikony: promień | 0–20 | 1 / 0 | draft, OK |
| Picker koloru: R, G, B | 0–255 | 1 / 0 | wspólny draft HEX/RGB/HSV |
| Niestandardowy zoom | 1–12800% | 1 / 2 | draft, widok po OK, bez dirty |

Jedna implementacja `ScrubNumber`, bazowo8 logicznych px na istniejący krok (zoom4), z przeliczeniem DPI drogi. Klik bez drag otwiera tekst; drag zaczyna się dopiero po progu, ruch zależy od drogi, powrót zmniejsza. Capture kończy MouseUp w kontrolce; nie uruchamia gestu płótna. Enter zatwierdza poprawną liczbę przed Form.AcceptButton, Escape przywraca początek, Tab waliduje. Tekst ma natywne zaznaczanie/Ctrl+A/Undo; strzałki/kółko zmieniają tylko z fokusem. Błędna wartość pozostaje do poprawy, bez częściowego zastosowania. Separator zgodny z kulturą, testowany pl-PL; bez nowej konwersji wykładnika/grupowania. Całkowite UI nie zaokrągla zapisanej geometrii świata.

LiveNumber otwiera osobną sesję; próbki scalają istniejącą akcję, Cancel/no-op odwraca prowizoryczną akcję, w tym jej delty Fog, i przywraca gałąź Undo/Redo. Zakończony drag daje jeden krok. Edytory modalne pracują na draftach, ustawienia narzędzi bez historii mapy. Ctrl+Z/Y w samym drag nie uruchamia globalnej historii; tekst zachowuje swoje. Utrata capture/dezaktywacja/disable kończy anulowaniem gestu. Zachowano kolejność canvas.Focus przed inicjalizacją gestu — pierwsza kreska po wpisie nie staje się kropką.

Wyjątki: graficzne TrackBar obrotu kadru0–360, kąta gradientu0–360 i efektów tła (krycie0–100, nasycenie0–200, blur0–24) pozostają suwakami. HEX to tekst, nie liczba do scrub. WPF i systemowe Open/Save nie są zastępowane. Stara klasa NumberDrag pozostaje wyłącznie dla historycznego testu, nie jest używana przez UI. Dostosowano bez uruchamiania stare źródła testów do typu ScrubNumber oraz jawnego commit w Groups041; nie usunięto implementacji testów.

## Ciemny motyw — zakres i wyjątki

| Obszar | Zmiana |
|---|---|
| NewMap, Name, SavePreset, TableSize, Resize i ostrzeżenie resize | ciemna forma, tekst/Combo i wspólne liczby; przyciski Cancel nie blokują wyjścia przez walidację błędnej liczby |
| Obiekt/grupa, stany, merge/ungroup/removal | ciemne zwykłe pola tekstowe/listy/Combo; nowe liczby; brak funkcjonalnej przebudowy panelu |
| AppearancePool/FillVariant, EditorColor/PaintPicker | ciemne własne pola, RGB ScrubNumber; istniejące próbki/podglądy i Jednolity/Gradient zachowane |
| Ikony, grafiki, duplikat, źródło, kadr | ciemna forma/teksty/listy/Combo; grafiki i maski podglądu bez przemalowywania |
| BackgroundDialog/Picker/Save | wspólna ciemna forma i zwykłe pola; suwaki oraz czerwone Usuń tło zachowane |
| Start/O programie/rename/custom zoom | ciemne własne obszary, wspólne metadane/ikona |

DarkTextBox ma ciemne tło, subtelną stałą ramkę i niebieskawy aktywny obszar, nadal zaznacza tekst. ScrubNumber ma własną ramkę hover/active/disabled. Theme nie schodzi do RichNoteEditor/ElementHost. Lista obiektów zachowuje wcześniejszy własny ciemny renderer. Zachowano świadomie białe/jasne kolory, obrazy i podglądy. Systemowe Open/Save/MessageBox, natywne strzałki/obramowania Combo, glyphy checkbox/radio i nagłówki TabControl nie są zamieniane na własne; część systemowego obrysu/default/focus może pozostać jasna. Nie deklarujemy „wszystko ciemne”. Bez WS_EX_COMPOSITED, zmiany frameworka lub kampanii WPF.

Autor: nazwa/font/rozmiar i drugi wiersz pozostają; po Shown mierzymy tekst w rzeczywistym DC, uwzględniamy GDI padding obu etykiet i około6px przerwy przy linii pisma. Usuwa to pusty slot, a nie zmniejsza dużą nazwę. Wersja/data z ReleaseMetadata, jawna data13.11.2026 bez godziny. Brak GitHub/przycisku aktualizatora.

## Testy metod i kontrolek

Runner `Run-Patch080Tests.ps1` wymaga jednego jawnego Case. Puste Case sprawdzono raz: błąd użycia, bez uruchomienia prób/folderów. Bez Run-AllTests i dawnych runnerów hurtowo.

- **numbers PASS** `patch-080-numbers-e5d63ff61ea3409f91d49ec03eefca03`: próg/click/drag, odwrócenie drogi, clamp, cancel/capture/disabled, pl-PL i precyzja, invalid Tab, Enter przez ProcessCmdKey przed rodzicem, brak wykładnika, NewMap theme; rzeczywisty LiveChangeObject jeden Undo/Redo oraz Cancel/no-op; pierwsza pełna kreska po wpisie rozmiaru oraz dokładny rollback Fog po Cancel/no-op promienia (bez nowego wpisu/dirty). Wcześniejszy błąd licznika inicjalnego focus testu poprawiono w fixture; późniejsze powtórzenia wynikały z zależnych zmian kontrolki i wykrytego błędu Enter.
- **commands PASS** `patch-080-commands-c0d412f6cb8d452ca9682e35b83f3785`: kategorie/wejścia/warstwy/foldery, 9 szybkich pozycji, overflow240px i dwa wiersze, centrum zoomu bez UpdateUI, wspólna jakość/needsFirstSave/akcenty, mierzony autor, minimum oraz symulowane Scale125%. Regresja Close(ItemClicked) → PerformClick potwierdza niezwolnione menu zoomu. Ostatnia zmiana później dotyczyła tylko palety pressed/selected; obejrzano ją w zwykłym EXE.
- **rename PASS** `patch-080-rename-4ac2b8b0f3ea4fa59ba42ddb0121cbf3`: clean/dirty bez zapisu, case-only, MRU, kolizja/traversal, blokada autojob, kontrolowany następny autozapis i ręczny Save pod nową ścieżką, obca/zniknięta wersja. Nie powtarzano po niezależnych poprawkach UI.

Kontrolowany zegar użył produkcyjnej ścieżki autozapisu; nie czekano kolejnych5min. Rzeczywisty5min test jest udokumentowany jako wcześniejszy0.7.2, nie nowy wynik0.8. Nie zmierzono FPS i nie testowano pełnej aplikacji.

## Zwykły EXE i błędy wykryte podczas integracji

Wszystkie uruchomienia wyłącznie w `tests/_work/ui-080-ec7df5c055804d0f9ebd6ae1b9b89eaa`, z własną Library/MRU/mapami. Każdy kandydat był kopią identyczną bajtowo z ówczesnym głównym EXE. Końcowy SHA-256 obu: **6F7F3D91E69066B0A53CE9ECBE04A1B71BBE6D9BD6BF13F0A5DC841D9FBC52EA**. Obsługa przez [Computer Use](C:/Users/admin/.codex/plugins/cache/openai-bundled/computer-use/26.1106.51428/skills/computer-use/SKILL.md).

W pierwszym kandydacie Enter zamykał NewMap przez AcceptButton przed aktualizacją Value — rzeczywiście utworzono17 zamiast wpisanych15. Naprawiono przechwycenie ProcessCmdKey; następna próba zostawiła dialog i utworzyła wpisane16 kolumn. Wybór presetu zoomu ujawnił ObjectDisposedException ContextMenuStrip (CreateHandle/ReparentToDropDownOwnerWindow): Closed.Dispose wyprzedzało dispatch kliknięcia. Zmieniono na jeden popup żyjący razem z oknem. Nie zaliczano błędnych prób jako sukcesu; dodano regresje obu rzeczywistych ścieżek.

Po poprawkach:

- Start z poprawionym odstępem, ciemny NewMap, prawdziwy drag15→17 i wpis/Enter; utworzenie16×10 z plikiem bez SaveFileDialog. Te próby były na wcześniejszych kandydatach; kolejne zmiany popupu/palety nie unieważniały NewMap.
- W kopii **7931C27…** preset100% bez wyjątku, oba osobne dopasowania, Low turkus i bez dirty. Rysik5→7 drag16px, potem wpis20; pierwszy drag(510,300)→(750,350) dał pełną kreskę. Gwiazdka i koralowy Save.
- Alt+E/End/Enter otworzyło rename. Nowa nazwa „Próba po rename0.8.0”, dirty pozostało. Odczyt własnego pliku przed Save:0kresek, brak starej ścieżki i brak starego MRU. Ctrl+S:1kreska, clean i neutralny Save. Nie przedstawiamy tego jako test modelu.
- Po ostatniej zmianie samej tabeli kolorów kopia **1AD6B11…** ponownie otworzyła zapisany wynik przez recent; kreska zachowana, clean i High. Alt+E ma ciemny czytelny aktywny nagłówek, Escape zamyka menu. O programie rzeczywiście otwarte: ikona, amdesar, wersja/data; Escape zamyka.
- W **kopii 667BE0D6…** po zawężeniu parsera: recent → zapisany plik → Niestandardowe; wpis `92,5`, Enter pozostawia dialog z `92,50`, OK ustawia widok (caption92% po zaokrągleniu), bez dirty. Nie ponawiano innych zaakceptowanych wejść po zmianie jedynie odrzucania wykładnika.

Ostatnia lokalna korekta dotyczyła wyłącznie rollbacku live-number: przywracamy również delty Fog prowizorycznej akcji, co potwierdził zależny Case numbers. W **finalnej kopii 6F7F3D91…**, zgodnej z głównym EXE, otwarto własny projekt, dodano Wieżę ze startera i otwarto jej szczegóły. Drag rozmiaru32px zmienił30→34; jeden Ctrl+Z przywrócił30, Ctrl+Y34. Ctrl+S zapisało i zgasiło gwiazdkę/akcent. To faktyczna integracja kontrolki z dokumentową historią w finalnym EXE, nie samo uruchomienie. Nie ponawiano nieunieważnionych prób pozostałych poleceń po tej zmianie.

Własne czyste okna zamknięto; inventory potwierdziło brak własnych okien. Główny0.7.2 użytkownik zamknął sam przed buildem. Nie zamykano jego okien ani nie odrzucano danych. Odczyty narzędzia czasem miały stare indeksy lub błąd geometrii podczas przejścia; każdorazowo sprawdzano nowy inventory/screenshot przed kolejną akcją, bez ponawiania niepewnego tworzenia pliku. Własnych artefaktów nie czyszczono szeroko.

## Ograniczenia i niewykonane próby

Brak pełnej regresji, przenośności, dawnych kampanii gumki/zoomu/masek/bibliotek/WPF. Nie klikano każdego pola osobno — wspólna kontrolka i reprezentatywne integracje. Nie uruchomiono dostosowanych historycznych runnerów i nie potwierdzamy ich pełnego bieżącego wyniku. Nie wykonano rzeczywistej zmiany DPI/monitorów; test Scale125% i ekran roboczy to ograniczona kontrola układu. System bez DWM dark caption nie był fizycznie dostępny; fallback wynika z obsługi HRESULT/braku API, nie próby na drugim OS. Nie nagrywano klatek i nie deklarujemy ciągłej płynności/braku flasha na podstawie screenshotu. Nie powtarzano prawdziwego5min autozapisu; integracja rename używa kontrolowanego zegara. Nie wykonywano ręcznie konfliktu rename z równoczesnym zewnętrznym procesem; sprawdzono refusal/case-only/missing/foreign w metodach. Nie uruchamiano pełnej kampanii funkcji zachowanych.

Backlog tylko zapisany: [toasty po zakładkach, kopiowanie po1.0, źródłowe pliki i kolejka0.12, menu kart0.11](backlog-ui-po-0.8.md). Brak placeholderów zakładek/Ustawień/Aktualizacji, skanowania źródeł, zmiany Library, Clipboard lub sieci. Plan0.9–1.0 zachowany.

## Checklista odbiorcza

- [ ] Standardowa ciemna ramka i tytuł z nazwą/gwiazdką; dwa wiersze, bez powtórzonej nazwy.
- [ ] Sześć menu, wszystkie dawne wejścia, właściwe tooltipy/skróty i Escape.
- [ ] Plik Nowa/Otwórz/Zapisz/SaveAs zachowuje0.7.2; pamięciowa nowa i needsFirstSave.
- [ ] Menu/ikony Undo i Redo zgodne z kontekstem; tekst i lokalne dialogi zachowują historie, Ctrl+Shift+Z bez operacji.
- [ ] Resize/orientacja/tło prowadzą do dotychczasowych operacji; wszystkie7 warstw i trzy tryby współrzędnych.
- [ ] Zoompresety/custom z centrum, oba dopasowania, High/Low i resetHigh po Nowa/Otwórz.
- [ ] Koralowy Save tylko przy potrzebie zapisu, turkusowy Low; bez złota/migania.
- [ ] Wąskie okno/overflow, nieobcięte ikony i brak trzeciego wiersza.
- [ ] Rename clean/dirty, case-only, kolizja/Cancel/IO, MRU i następny Save/autosave pod nową ścieżką.
- [ ] Liczby: klik vs próg drag, wpis/ułamek/limity, Enter/Escape/Tab, capture/disable, jeden Undo/no-op/cancel.
- [ ] Pierwsza kreska po wpisie pełna; rozmiar1–200, default5, droga8px/krok.
- [ ] Ciemny NewMap i reprezentatywne pola/listy, czytelny focus/disabled; grafiki/próbki/WPF/systemowe dialogi bez niezamierzonego przemalowania.
- [ ] Autor bliżej nazwy z zachowaną baseline, druga linia wersja/data; O programie z tą samą ikoną/metadanymi.
- [ ] Biblioteka otwiera właściwe foldery, bez cmd i komunikatu sukcesu; zarządzanie zasobami pozostaje w edytorach.
- [ ] Bez nowych preferencji, zakładek, aktualizatora i zmian struktury Library; backlogi tylko udokumentowane.

## Git

Kod, EXE i testy: **9ae0d8b** (`Implement HexMapEditor 0.8.0 command bars, numeric editing and file rename`). Przed kolejnym lokalnym commitem pozostała wyłącznie własna dokumentacja: README, specyfikacja, plan wersji, backlogi, plan i raport0.8.0. Identyfikator commitu dokumentacji oraz końcowy odczyt Git podaje zakończenie zadania. Ignorowane Library, prywatny recent-projects.json i tests/_work pozostają poza commitami. Bez publikacji i deklaracji kompleksowego przetestowania.
