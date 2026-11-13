# Plan i punkt wznowienia 0.8.0

Baza potwierdzona: HEAD 6e55ccf, kod 92fdf11. Git przy rozpoczęciu czysty. Brak AGENTS.md/innych instrukcji repo wykrytych przez rg. Przeczytano raport 0.7.2 oraz bieżące implementacje; specyfikacja 42 opisuje chronione ścieżki Nowa/autozapisu. Prywatne Library/MRU/mapy nie są fixtures.

## Etapy

1. Inwentaryzacja, wspólny ScrubNumber i lokalny ciemny motyw własnych kontrolek; zachowanie zakresów/live-edit/historii. Bez ingerencji w WPF i systemowe dialogi.
2. Dwa wiersze: MenuStrip Plik/Edycja/Mapa/Widok/Biblioteka/Pomoc i niezwijany ToolStrip z overflow. Wspólny dispatcher i stan, zoom zachowujący środek, dwa akcenty. Standardowa ramka Windows z best-effort DWM dark caption. Tytuł dokumentu bez powtórzonej nazwy na pasku.
3. Rename pliku w tym samym folderze z walidacją, collision/case-only/rollback, fingerprint/MRU i ochroną autozapisu. Bez zapisu dirty i fikcyjnej historii. Autor po rzeczywiście zmierzonej nazwie.
4. Celowane Case: wspólna kontrolka + reprezentatywne historie, menu/zoom/layout/motyw, rename/autosave. Finalny EXE dopiero po zakresie, zwykła byte-identical kopia w tests/_work. Dokumentacja/checklista/commity/status, bez pełnej kampanii.

## Inwentaryzacja wejść

Dotychczasowa góra: Nowa, Otwórz, Zapisz, Zapisz jako, Undo/Redo, folder Library, Tło, Orientacja (toggle), etykiety (Axes/InCells/None), zoom100, Widok (Tło/Tereny/Ikony/Obiekty/Ograniczenie/Mgła/Rysik), HighLow, Dopasuj mapę/wszystko, Rozmiar. Lista instancji i biblioteki w panelach pozostają. Preserve wszystkich trzech trybów współrzędnych przez submenu Osie (nie gubimy InCells).

## Inwentaryzacja liczb — stan przed zastąpieniem

| Okno/pole | Zakres/krok/precyzja | Podgląd i historia |
|---|---|---|
| NewMapDialog kolumny/wiersze | 1–200 / 1 / 0 | roboczy dialog, utworzenie po OK |
| ResizeDialog góra/dół/lewo/prawo | -199–199 / 1 / 0 | wynik rozmiaru live, jeden commit po OK |
| TableSizeDialog wiersze/kolumny | 1–100 / 1 / 0 | roboczy, WPF tabela dopiero po OK |
| Przybornik rozmiar/szerokość i wysokość | 1–15 / 1 / 0 | ustawienie narzędzia bez historii |
| Rysik/gumka wspólna liczba | 1–200 / 1 / 0, domyślnie5, 8px/jedn. | ustawienie narzędzia, bez map history |
| Instancja rozmiar | 8–1024 / 1 / 0 | LiveChangeObject, scalanie sesji |
| Instancja promień Fog/pola widzenia | 0–20 / 1 / 0 | LiveChangeObject |
| Grupa promień Fog/pola widzenia | 0–20 / 1 / 0 | LiveChangeGroup |
| Grupa rozmiar nadpisania/delta | -1016–1024 / 1 / 0 | ograniczenia efektywnych członków pozostają |
| IconEditDialog promień | 0–20 / 1 / 0 | lokalny draft, commit po OK |
| Picker RGB R/G/B | 0–255 / 1 / 0 | zsynchronizowany draft HEX/HSV, commit po OK |

TrackBar (kadr obrót0–360, gradient kąt0–360, tło krycie0–100/nasycenie0–200/blur0–24) to już graficzne suwaki, a nie pola wpisu: zachowujemy gesty i limity. HEX to tekst. Nowy custom zoom otrzyma wspólną kontrolkę ułamkową; zakres zgodny z kamerą. Inwentaryzację sprawdzić końcowym rg, bez testu każdej kontrolki osobno.

## Stan

Wykonane: odczyty Git/raportu i wstępna inwentaryzacja. Pozostała implementacja wszystkich etapów i celowane testy. Ostatnia niepewna operacja: brak. Własnych procesów/dialogów 0.8.0 nie uruchomiono. Główny EXE nadal ukończony 0.7.2.

## Etap1 — implementacja robocza, jeszcze nie zaliczona

Dodano wspólny ScrubNumber (bez spinnerów, próg drag, klik do tekstu, Enter/Escape/Tab, zakres/krok/precyzja, DPI przemieszczenia, zdarzenia transakcji) i zastąpiono pola z tabeli. Osobne NumberDrag przy narzędziach usunięte z UI; stara klasa pozostaje dla historycznych źródeł testów. Object/group LiveNumber rozpoczyna odrębną sesję, zachowuje merge; Cancel/no-op przywraca też gałąź Undo/Redo. To wymaga jeszcze testu integracji. TrackBar pozostają.

Nowa góra: menu i ToolStripControlHost z overflow, wspólny dispatcher, wszystkie stare wejścia, trzy tryby etykiet, shared zoom1–12800% z centrum. Rzeczywisty zakres wheel1–12800% bez podnoszenia. Dwa akcenty Save/Low i tytuł dokumentu. DWM attr20/19 best-effort na standardowej ramce. Dodano DarkForm/DarkTextBox i lokalny motyw pól/list/Combo, bez gałęzi WPF. Rozmiar minimum1180×748 zachowany. Nie zmieniono danych, autozapisu czy renderera.

Rename: wspólna walidacja PathForName, File.Move bez overwrite, case-only przez własną nazwę pośrednią, recheck fingerprint po przeniesieniu, rollback. MRU.Renamed usuwa starą ścieżkę bez zmiany formatu. Dirty/bazowa nazwa danych mapy pozostają, nazwa widoczna pochodzi ze ścieżki. Timer nie jest resetowany przez sam rename; następny autosave używa nowego currentPath i tej samej potwierdzonej wersji bajtów. Podczas autojob rename blokowany.

Pierwsza kompilacja wykryła wyłącznie błąd składni lambdy Dispose w AboutDialog; poprawiono, kompilacja do tests/_work/HexMapEditor-080-compile.exe PASS. Po tej kompilacji dodano jeszcze mierzenie odstępu autora i poprawki; to nie finalny EXE. Pozostałe: przegląd/scoped testy, ewentualne poprawki, motyw/gesty zwykłego EXE, backlogs/docs, wersja/build i commity. Brak zaliczonych Case; niepewna operacja: brak. Brak własnych okien.

## Etap2 — celowane kontrole integracji

PASS numbers-cda77a85... po poprawie testowej inicjalizacji (pierwszy focus tworzył sesję przed podłączeniem liczników; nie był to błąd anulowania). Po zależnej zmianie CancelDrag/disabled/CausesValidation i rozszerzeniu o Enter/Tab/capture/przepisanie rozmiaru → pierwsza kreska PASS numbers-29ed355a723a47af816f7e99f7382c3b. Sprawdzono próg, drogę i limity, polski ułamek/przecinek, precyzję bez cichego round, invalid Tab, Enter, utratę capture/disable, dark NewMap oraz prawdziwy LiveChangeObject: jeden Undo/Redo, Cancel i no-op bez historii. Pierwszy gest po wpisaniu 40 miał więcej niż jeden punkt i rozmiar40. Klasa ScrubNumber ma jeszcze dodaną wąską blokadę CtrlZ/Y podczas samego drag (tekst zachowuje native Undo); tę zmianę należy sprawdzić celowanym numbers, nie innymi kampaniami.

PASS commands-9d77...; po zależnych zmianach trackowania ostatniego edytora/stanu przycisków oraz rozszerzeniu o minimum/symulowane125% PASS commands-1a570d6f55c54b41b776533765a80efa. Kategorie,9 szybkich poleceń,7 warstw,10 kategorii folderów, overflow240px, dwa wiersze bez nazwy, zoom-centre bez UpdateUI, akcent Low/needsFirstSave i resetHigh, mierzenie autora. 125% to Scale kontrolek w procesie, nie rzeczywista zmiana DPI systemu.

PASS rename-4ac2b8b0f3ea4fa59ba42ddb0121cbf3: clean/dirty bez zapisu, case-only, MRU bez starej ścieżki, kolizja/traversal, blokada autojob, następny kontrolowany autosave i ręczny Save na nową ścieżkę, obcy/zniknięty plik. Nie czekano ponownie5min i nie ruszano prywatnych danych. Zmiany późniejsze wyłącznie kontrolek/motywu/klawiszy, nie unieważniają tego wyniku.

Pozostałe: guard nowego runnera raz; ostatnia wąska próba skrótów number; wersja/finalbuild i zwykły EXE (start, menu, zoom, rename/Save, scrub/wpis), docs/backlogs i commity. Główny EXE nadal0.7.2. Niepewna operacja: brak. Własne procesy testów zakończone; zwykłego0.8.0 nie uruchamiano.

## Etap3 — kandydat i rzeczywisty błąd Enter w UI

Guard bez Case PASS (brak testów/artefaktów). Ostatni numbers-e7e745a6... PASS po blokadzie CtrlZ/Y w drag. Użytkownik zamknął główny0.7.2 na prośbę; CIM po zgodzie potwierdził brak procesu. Zbudowano kandydata0.8.0.0, własna kopia ui-080-ec7df5c055804d0f9ebd6ae1b9b89eaa. Wstępna kontrola startu wykazała jeszcze padding etykiety; autor po Shown jest mierzony z rzeczywistym DC i GDI paddingiem (6px między końcem nazwy i podpisem). Powiązany commands-c5e8947a162d42e6b2ed7477054a6615 PASS, bez ponawiania rename.

Kandydat SHA4251ED...: start wygląda poprawnie, ciemny NewMap, prawdziwy drag kolumn15→17. Wpis15 + Enter w zwykłym EXE ujawnił, że Form AcceptButton wygrywa przed TextBox.KeyDown: zapisano17, chociaż test samego OnKeyDown przechodził. To nie zaliczona próba poprawnego wpisu! Zamknięto wyłącznie własny czysty projekt (dane w tests/_work). Poprawiono ScrubNumber.ProcessCmdKey Enter przed parentem, test zastąpiono rzeczywistą ścieżką ProcessCmdKey z warunkiem niezamkniętego NewMapDialog. Wynik ostatniego numbers odczytać z wyjścia/logu przed ewentualnym powtórzeniem. UI start/menu/rename/scrub nadal wymaga końcowej kopii po tej poprawce. Poprzednie hash nie jest finalne. Backlogi zapisane, docs końcowe jeszcze niegotowe. Ostatnia operacja: zależny numbers po poprawce; własne okno zamknięte, potwierdzić inventory przed kolejnym uruchomieniem.

## Etap4 — poprawione integracje UI i kolejny finalny kandydat

PASS numbers-f82052a332854b1cb7a736741dbae830 po poprawce Enter; ten sam routing ProcessCmdKey i niezamknięty NewMap sprawdzony metodą. W zwykłym EXE SHA5B54EE... wpis16 + Enter pozostawił dialog, Utwórz dało mapę16×10, bez gwiazdki i bez SaveFileDialog. Low miał turkusowy napis/ramkę i nie zmieniał dirty. Próba wyboru100% ujawniła nowy ObjectDisposedException ContextMenuStrip (stack CreateHandle/ReparentToDropDownOwnerWindow): Closed.Dispose wyprzedzało dispatch kliknięcia. Nie zaliczać tej próby zoomu. Poprawka: jeden zoomPopup na życie okna, Dispose dopiero razem z nim. Powiązany commands-c0d412f6cb8d452ca9682e35b83f3785 PASS z Close(ItemClicked) → PerformClick i bez disposed.

Własne czyste okno z wyjątkiem zamknięto przez jego Zamknij; po zakończeniu inventory puste. Root i kopia ui-080-ec7df5c055804d0f9ebd6ae1b9b89eaa przebudowane: SHA256 7931C27B533CD84354720B5272948011D96E9CA78DDFA39C018384C044D4C07A, wersja0.8.0.0. Potem nieudany patch dokumentacji nie zmienił pliku (nieudany match), build i Copy zakończyły się sukcesem; nie powtarzać. Pozostała finalna próba zoom/pasek/menu, rename/Save, Rysik scrub/wpis, docs/checklisty/commity. Nie trzeba powtarzać numbers/rename metod po zmianie wyłącznie popupu. Własnych procesów brak, ostatnia niepewna operacja: brak.

## Etap5 — zakończony odbiór okna i ostatnia zmiana wyłącznie palety menu

Kopia SHA7931C27... zgodna z ówczesnym root EXE: zwykły start i recent, preset100% działa bez wyjątku; dwa osobne dopasowania; Low turkus bez dirty; Rysik rozmiar5→7 przez16px drag, ręczny wpis20 i pierwszy pełny drag (510,300)→(750,350), bez kropki. Dirty: gwiazdka i koralowy zapis. Alt+E/End/Enter otworzyło Rename; zmiana na Próba po rename0.8.0 zachowała dirty. PrzedCtrlS plik nadal miał0kresek, stara ścieżka nie istniała, MRU miało tylko nową. CtrlS zapisało1kreskę i zgasiło gwiazdkę/akcent. To faktyczny zwykły EXE, nie test metod. Okno zapisano i zamknięto.

Dopiero po tej próbie poprawiono samą tabelę kolorów aktywnego/pressed nagłówka MenuStrip (wcześniej jasny prostokąt). Nie zmieniono handlerów/numeryków/rename/modelu. Źródła historycznych testów mają dostosowane wyłącznie casty NumericUpDown→ScrubNumber i Groups041 jawny commit sesji; nie uruchamiano starych runnerów, nie usuwano źródeł. Stary NumberDrag w ToolSize zachowany dla historycznego testu tej klasy, nie jest używany przez UI.

Finalny root i kopia: SHA256 1AD6B11F5BF58BF77AB19D55D55295662F9142D19BA271C243742B28014CA582, wersja0.8.0.0. W tej końcowej kopii ponownie otwarto zapisany plik przez recent: kreska odtworzona, clean, High. Alt+E pokazuje teraz czytelny ciemny nagłówek; Escape zamyka menu. Pomoc/O programie z prawdziwą ikoną, amdesar, wersją/datą otwarte i zamknięte Escape. Nie powtarzano nieunieważnionych funkcjonalnych prób po samej zmianie palety. Ostatnie AltF4 wysłane tylko własnemu czystemu oknu; końcowe inventory potwierdzić. Pozostałe wyłącznie docs/raport/README, diff/wersja/hash i lokalne commity. Niepewne: tylko wynik ostatniego zamknięcia; nie ponawiać go bez sprawdzenia.

## Etap6 — domknięta walidacja, dokumentacja i commity pozostają

Końcowy przegląd parsera zawęził NumberStyles do zwykłej liczby ze znakiem i separatorem kultury (bez wykładnika/grupowania). Nie wprowadzamy niejawnej konwersji1e2. Powiązany numbers-ae56cff3e71746bcbe3862ef6ba76c82 PASS z nową asercją; bez ponawiania commands/rename. Root i kopia finalna SHA256 667BE0D6EF4E527B0ED581327760668F42B71D92F2DCAD963567DBE331A9815C, FileVersion0.8.0.0. W tej samej finalnej kopii start/recent → zapisany plik, zoomNiestandardowe: polski wpis92,5, Enter pozostawił dialog z92,50, OK ustawiło właściwy widok (92% zaokrąglony caption), bez dirty. Poprzednie poprawne wejścia UI nie powtarzane po zawężeniu wyłącznie nieobsługiwanego zapisu wykładniczego. WysłanoAltF4 własnemu czystemu oknu; przed końcem potwierdzić inventory, nie ponawiać zamknięcia w ciemno.

Pozostała wyłącznie dokumentacja końcowa, kontrola diff/hash/wersji i lokalne commity. Nie wznawiać implementacji/testów. Brak blokad funkcjonalnych/niezaliczonych Case poza uczciwie opisanymi ograniczeniami UI/DPI. README zaczęte, raport/spec43 jeszcze do napisania. Ostatnia niepewna operacja: tylko ostatnie zamknięcie; główny build i kopiowanie znany sukces.

## Etap7 — korekta rollbacku live-number i zamknięty zakres

Końcowy przegląd wspólnego bindingu do właściwości wykazał, że przywrócenie samej liczby i stosu nie wystarcza dla promienia odkrywcy: live-preview może zawierać delty Fog. Cancel/no-op teraz odwraca prowizoryczną akcję przez istniejące ApplyAction(false), potem przywraca Undo/Redo i sprawdza dirty. Nie zmienia reguł odkrywania; cofa całą roboczą transakcję, także jej delty. Dodano do tego samego numbers reprezentatywny prawdziwy promień z Fog: preview odsłania, Cancel oraz powrót do początku odtwarzają dokładną bazę i brak wpisu. PASS numbers-e5d63ff61ea3409f91d49ec03eefca03. Bez kampanii masek/grup; poprzednie testy rename/commands pozostają ważne.

Próba custom zoom w kopii667BE... zakończona i jej własne okno zamknięte, inventory[] potwierdzone. Po zmianie tylko powyższego rollbacku root i kopia ponownie zbudowane; aktualny SHA odczytany w wyjściu ostatniego builda (wpisać do raportu). Nie ponawiać wcześniejszych prób UI bez wpływającej zmiany. Do wykonania tylko uzupełnienie raportu o rollback/hash, końcowy diff i lokalne commity. Brak własnych procesów/dialogów; brak niepewnej operacji. Zakres implementacyjny i testy ukończone, nie ogłoszono jeszcze końcowego Git.

## Etap8 — końcowy hash i ostatnia rzeczywista kontrola bindingu

Finalny root/kopia SHA256 6F7F3D91E69066B0A53CE9ECBE04A1B71BBE6D9BD6BF13F0A5DC841D9FBC52EA. W tym EXE otwarto własny zapisany projekt przez recent, dodano własną Wieżę ze startera, otwarto szczegóły Kursorem. Nowe pole Rozmiar: drag32px dał30→34; jeden CtrlZ przywrócił30, CtrlY34. CtrlS zapisało, gwiazdka i akcent zgasły. To reprezentatywne dokumentowe pole na faktycznej końcowej kopii (nie sam start EXE), bez kampanii grup/mastek. Po zapisie AltF4 tylko własnego okna; potwierdzić brak przed końcowym Git. Nie powtarzać zakończonych etapów. Kod i wymagany zakres zamknięte; pozostałe dopisanie hasha/tego wyniku do raportu, commit kodu/testów/EXE oraz dokumentacji i status. Ostatnia niepewna operacja: wyłącznie to ostatnie zamknięcie.

## Ukończone

Kod/EXE/testy zapisane lokalnie w9ae0d8b. FileVersion0.8.0.0, hash finalny6F7F3D91E69066B0A53CE9ECBE04A1B71BBE6D9BD6BF13F0A5DC841D9FBC52EA, zgodny z własną kopią. Ostatnie inventory[] potwierdziło zamknięcie własnego okna po zapisaniu próby. Brak własnych procesów/dialogów, brak niepewnej operacji. Raport/spec43/README/plan wersji/backlogi uzupełnione; rollback liczby obejmuje też deltyFog. Wszystkie końcowe wyniki i ograniczenia są w raporcie, bez twierdzenia o pełnej regresji. git diff --check bez błędów, tylko ostrzeżenia CRLF. Pozostały jedynie lokalny commit dokumentacji i końcowy odczytGit. Nie uruchamiać ponownie prób ani builda przy wznowieniu tego ostatniego kroku. Bez push/tagu/Release'a.
