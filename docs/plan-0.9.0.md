# Plan 0.9.0

Baza potwierdzona: HEAD041215a, Git czysty. Raport i plan0.8.3 przeczytane; brak AGENTS.md w repozytorium. Nie używamy prywatnych danych. Główny EXE pozostaje0.8.3 do ukończenia całości.

## Etapy
1. Dwie poprawki odbiorcze: tekst zoomu i statyczna nazwa rename.
2. Wspólna geometria figur, obrót/kąt i historia ustawień; celowane metody.
3. Model trzech sekcji presetów, częściowy zapis/przywracanie, robocze wersje i zamykanie; szybki dostęp24/12 oraz startery.
4. Układ panelu, grafiki i niebieskie oznaczenia; testy integracji i izolowany EXE.
5. Wersja0.9.0, główny EXE, dokumentacja i jeden lokalny commit po ukończeniu.

## Decyzje / stan
Nie zmieniać geometrii hybrydy. Zachować wspólne StampAt dla wszystkich materiałów. Nowe parametry figur wyłącznie sesyjne; historia ustawień bez dirty. Rozszerzenie presetu wymaga sprawdzenia serializacji i osadzania zasobów przed wyborem wersji formatu; mapę zmieniać tylko przy konieczności.

Wykonano: odczyt bazy i bezpośrednich wejść panelu/presetów/geometrii. Pozostałe: implementacja i testy etapów1–5. Próby: brak. Ostatnia operacja: zapis planu, wynik pewny. Brak własnych procesów/dialogów. Nie utworzono commita.

## Etap 1–2 w toku

- Dwie poprawki tekstowe: nowy SingleLineButton zoomu (osobny jednoliniowy render bez mutacji Text podczas Paint), szerokość tekstowej części +20px; EllipsisNameLabel w rename z SingleLine/VerticalCenter/EndEllipsis. Bez zmian hybrydy. Próba wizualna nadal wymagana.
- BrushGeometry rozszerzono o Line/Triangle/Octagon, obrót i kąt; ograniczenie1–100. Kandydaci ograniczeni do mapy + jednego wirtualnego pierścienia dla outline. Linia korzysta z Nearest. Kwadrat/prostokąt/koło zachowują wymiar połowy=size×24.24871 świata; regularne5/8 mają taki promień. Trójkąt: ramiona=size×48.49742, centroid w środku, czubek do góry, kąt1–179. Hex korzysta z metryki ułamkowej axial po obrocie, przy0 zachowuje pierścienie.
- ScrubNumber dostał opcjonalny normalizer tekstu/wartości i suffix, używany wyłącznie przy nowych kątach. BrushAngles obsługuje bardzo długie liczby przez modulo cyfr, nie clamp przed wrap. Historia ToolNumbers obejmuje Rotation/Apex bez danych mapy. UI parametrów jest prototypem do dalszego layoutu; jeszcze nie uznać panelu za ukończony.
- Wskazane wybory ikon/obiektów/terenów/stanów i obiektu na mapie oraz narzędzia skierowano do ChoiceStyle. Pozostałe kolory mapy niezmieniane; inwentaryzacja pozostałych zaznaczeń nadal potrzebna.
- Build `tests/_work/patch-090-dev/HexMapEditor.exe` PASS. `Run-Patch090Tests.ps1 -Case geometry` PASS, log `tests/_work/patch-090-geometry-8a2011c5ff64490c9174e6d100aee3a7/results.log`. Metody: figury/fill/outline/obroty/obie orientacje/środek poza mapą/odległy stempel/kąt oraz duże liczby i scrub. Nie jest to test zwykłego EXE, integracji historii ani wyglądu. Nie uruchomiono starej kampanii.

Pozostałe: cały model presetów3 sekcji, częściowy zapis/przywracanie/close, quick access i startery, końcowy layout/grafiki, integracje i zwykły EXE, wersja/dokumentacja/commit. Główny EXE i metadane nadal0.8.3. Ostatnia operacja pewna: geometry PASS; brak własnych procesów/dialogów. Żadna publikacja lub commit nie były podejmowane.

## Bieżący punkt wznowienia (historyczny — najnowszy stan na końcu)

Poniższa sekcja opisuje stan poprzedniej sesji. **Aktualny stan jest w sekcji „Integracja presetów i panelu” na końcu pliku.**

Dodano niezależny sesyjny model `QuickAccess.cs`:24 unikalne ID,12 widocznych, Used widocznego nie rusza kolejności, Used rezerwy/nowego promuje, Remove domyka, Restore przyjmuje tylko12 i czyści rezerwę. **Model nie jest jeszcze podłączony do panelu ani presetów.** `Run-Patch090Tests.ps1 -Case quick` PASS: `tests/_work/patch-090-quick-2ec3ac68c5724d1f87a2b6f1f0c113a6/results.log`. Nie powtarzano geometry, bo po PASS nie zmieniano jego implementacji (runner/test rozbudowany o oddzielny Case).

Następny konkretny etap: rozszerzenie TerrainPreset o dwie listy startowe i serializacji PresetCatalog (MapModel.cs), obsługa starego4 i nowego schematu; osadzone IconAsset sprawdzić pod kątem współdzielenia i budżetów. Następnie UI zapisu/przywracania i quick access oraz close. Przeczytane bezpośrednie wejścia: PresetOperations.cs, PresetHistory.cs, GraphicSchema.cs, IconLibrary.cs/IconAssets.cs. Nie trzeba czytać ponownie raportu0.8.3 ani bazy Git. Obecny PresetCatalog zapisuje jawnie tylko tereny i TerrainAssets/GraphicSources; ReadFile ma whitelistę pól. PresetFileChange/LibraryFiles zapewniają conflict-safe publikację i historię — zachować. Obecne map.Presets są wersjami roboczymi, ale wybór/edycja dotychczas oznacza dirty mapy; wymaga świadomego rozdzielenia zgodnie z promptem. Nie zmieniano jeszcze formatu ani presetów, aby nie pozostawić niedziałającego częściowego zapisu.

UI parametrów wymaga dalszej korekty etykiet/układu/visibility; nie uznawać go za odbiór. Nowe niebieskie oznaczenia nie obejmują jeszcze kompletnej inwentaryzacji zaznaczeń heksów. Grafiki i startery nietknięte. Rysik/gumka panel oraz2×6 i układ presetów jeszcze do zrobienia. Nie wykonano zwykłej próby EXE ani końcowego builda0.9.0. Źródła kompilują się w runnerze quick. Ostatnia operacja pewna: quick PASS. Brak własnych procesów/dialogów; główny EXE0.8.3. Nie ma commita0.9.0, nie ogłaszać ukończenia.

## Integracja presetów i panelu — wcześniejszy etap

- Preset5: Icons/Objects po12, wspólna tabela IconAssets, referencje AssetId, kopie definicji i walidacja. ReadFile obsługuje4 i5, brak sekcji w4 oznacza puste listy. Map11 rozszerzone opcjonalnymi danymi wewnątrz presetów i dotychczasową tabelą IconAssets. MapForSave dołącza potrzebne obrazy zestawów; ValidateMap attach. Header SavedMapState pomija palety/wybór presetu — dirty mapy pozostaje zależne od jej danych.
- SavePresetDialog ma3 switche i disabled+tekst przy Off; zamykanie blokuje je w All. Merge odczytuje aktualny plik dla sekcji Off; expected version nadal chroni publikację. Nowy preset nie dodaje neutralnego. Restore dialog startuje Off, odczytuje aktualny plik przed zmianą, ChangePalette nie dotyka heksów. Close po pytaniu mapy przechodzi po draftach; Cancel/nieudany zapis zatrzymują. Do testów integracji dodano wąskie hooki potwierdzenia/zapisu.
- QuickAccess podłączony per preset. Oddzielne definicje per sesyjny zestaw, PPM usuwa z listy, placeholders do12. Promocja po udanym umieszczeniu obiektu/ikony oraz dodaniu nowego zasobu, bez promocji przy samym wyborze. Nie nadpisuje plików biblioteki.
- StarterSets generuje20 ikon i12 obiektów z uzgodnionymi nazwami; Default ma12+12. Nowe markery v2 dodają brakujące wpisy z nowymi ID przez istniejący writer bez nadpisania cudzych wpisów. Wymagany wizualny odbiór małej skali.
- Panel poszerzony do280px +18toggle, wiersze4×3, zasoby40×28 w2×6, operacyjne48×26. Przybornik kurczy się, gdy nie korzysta z parametrów. Presety: akcje/wybór+refresh+restore/status3linie. Nowe symbole narzędzi, plus, restore. Pozostałe wybrane zaznaczenie heksa/marquee także ChoiceStyle.
- Build developerski PASS przed ostatnimi drobnymi zmianami hooków. Case presets PASS `patch-090-presets-a9cecf2dd17a4b6dbdecd6278bd722fe`: preset5 dedup/round-trip, odczyt4, merge sekcji, map11, dirty i rzeczywiste TryCreatePreset. To jeszcze nie dowód konfliktów/close/pełnej integracji. geometry/quick nie powtarzano.

Następne: testy integracji częściowego zapisu/restore/close i UI, korekty z wyników. Zwykły finalny EXE, wersja/dokumentacja/pojedynczy commit nadal przed nami. Ostatnia operacja pewna: zapis hooków i checkpointu. Brak własnych otwartych procesów/dialogów. Główny EXE nadal0.8.3. Żadnego rollbacku ani publikacji częściowego wydania.

## Kontrole integracji i pierwsza ocena okna

Presets PASS `patch-090-presets-8c13cd22f566481e9659332f7d2f3562`: produkcyjne Create/Save/Restore, zachowany draft przy częściowym zapisie, niezmienione heksy po restore, konflikt pliku i błędny odczyt bez utraty danych; No/Cancel/błąd zapisu zatrzymują close we właściwym miejscu. Rozszerzenie o wcześniejszy udany zapis A i Cancel na B PASS w `patch-090-presets-b0ac833c5b2746578cb408e616c2c8ea`. Powtórzenia wyłącznie po dodaniu tych asercji i korekcie starterów.

Controls PASS `patch-090-controls-0aeb664587b147988e62a7ced8c54cca`:4×3,2×6, rzeczywista wewnętrzna szerokość tekstu12800%, obrót400→40 i Undo, brak zmian heksów. Użyty tam fixtureDefault do własnej kopii `tests/_work/patch-090-ui`. Rzeczywiste okno pokazuje nowe narzędzia i12+12, sześć pełnych terenów w zwykłym rozmiarze. Wykryto zasłonięte przyciski Presetów przez DockTop wyboru — zmieniono ten wiersz na DockNone. Ujednolicono przyciski zarządzania48×26. Konieczne powtórzenie zależnego layoutu. Kopię testową zamknięto AltF4, czysta mapa. Nie dotykano okien użytkownika.

Quick definicje są teraz per preset (nie wspólne po ID pomiędzy draftami), wybór z biblioteki jawnie przyjmuje wybraną definicję. Default startery uzupełnione, poprawiono poziomy środek rysunków domków. Nie przebudowujemy miniaturek quick przy każdym UpdateUI — tylko zmiana presetu/listy. Ostatnia zmiana pewna: ta osłona odświeżania. Pozostałe: końcowa kontrola UI i brakujących scenariuszy; wersja/dokumentacja/finalny build/commit. Brak znanej blokady, główny EXE nadal0.8.3; własny proces powinien być zamknięty — sprawdzić inventory przed kolejnym uruchomieniem.

## Finalizacja — aktualny punkt wznowienia

Zakończono implementację: trzy sekcje presetów5 i odczyt4, map11 z osadzonymi źródłami, partial save/restore, per-preset drafty i szybki dostęp24/12, close map→presety, startery20+12, układ i niebieskie wybory. Metadata0.9.0.0, data14.11.2026. Źródła hybrydowej ramki nietknięte.

Aktualny panel ma298px +18toggle (wcześniejszy280 był etapem przejściowym). Przy minimum cała kolumna przewijalna, teren ma dostępne co najmniej36px. Wykryty controls FAIL c398… naprawiono lokalnie; controls82c3ed23e7df44fbb8803dd5a7f33e18 PASS: minimum,4×3/2×6, wspólna szerokość pola rysika, rzeczywiste użycie niewidocznej ikony promuje, sam wybór nie promuje, visible12 bez rezerwy; kontrolowane skalowanie125% (nie fizyczny monitor). Nie powtarzać geometry/quick bez zmiany ich algorytmu.

Restore ma snapshot quick list i definicji w istniejącym EditAction, więc Undo/Redo przywraca również rezerwę. Presets1ed9798ac7234303bb16d6e64502b631 PASS po tej zmianie. Ostatni presets ec81bcb68b42448faa3025b02c8fd193 PASS po dodatkowej ochronie częściowej kopii presetu bez lokalnego pliku: pominięte sekcje źródłowego draftu pozostają. Poprzednia dodatkowa asercja4cc5… trafiła na celowo uszkodzony plik wcześniejszej próby w tym samym folderze; wydzielono osobny podfolder fixture, test przeszedł. Walidacja null/ID/źródeł zachowana. MRU opening otrzymało tę samą ochronę draftów co New/Open (ostatnia lokalna poprawka).

Rzeczywiste UI wykonano na własnych kandydatach: 12800% i reset, długa statyczna nazwa rename,6pełnych terenów/12+12 starterów, pusty preset bez neutralnego, dodanie terenu bez mapdirty, obrót400→40°, próbny stempel, partialSaveAllOff i tekst, SaveTerrainOnly, restoreOff/Cancel, przełączanie draftów i Cancelclose. final2: minimum/hide/show, widoczna ikona po użyciu nie przeskakuje, zapis mapy nie wymusza zapisu presetu. Własne wcześniejsze procesy zamknięte czystym AltF4; nie zamykano okien użytkownika.

Końcowy build tests/_work/patch-090-release/HexMapEditor.exe PASS; SHA256 D70ECE46B0F2FA9F50B0D89EA80EBC91A9B9D068BEEA4007F94A9C16C8D5A83A. To kandydat do identycznej podmiany głównego EXE po smoke. Główny EXE nadal0.8.3. Własna mapa i MRU w tym samym katalogu; obecnie otwarty edytor tej kopii, test ochrony draftu przy MRU w toku. Ostatnia pewna operacja: otwarcie menu Plik/Ostatnie projekty po zmianie roboczej nazwy. Sprawdzić wynik przed powtórzeniem. Brak otwartych zapisów/buildów; brak commita0.9.0.

README/spec47/planwersji/backlog i raport0.9.0 przygotowane. Pozostałe: ukończyć zależny MRUsmoke, bezpiecznie zamknąć własną kopię, porównać/podmienić główny EXE, potwierdzić wersje/hash, dokończyć ten punkt wznowienia i jeden wymagany lokalny commit wszystkich własnych plików poza_work. Nie ma znanej blokady. Źródła testów/historyczne runnery pozostają, pełnej kampanii nie uruchamiano.

### Końcowa zależność bibliotek

MRU smoke w zwykłym release EXE PASS: edycja nazwy draftu bez mapdirty → Ostatnie projekty → pytanie z właściwą nazwą i all3 → Cancel zachowuje mapę/draft → CtrlZ przywraca czysty preset. Kopię zamknięto czystym AltF4. Główny EXE podmieniono atomowo na identyczne D70… i potwierdzono0.9.0.0; od tej chwili nie jest już0.8.3.

Ostatni przegląd integracji wykrył brak odświeżenia referencji aktywnego quick po jawnej edycji wpisu biblioteki (nie po samym przełączeniu draftów). Dodano przyjęcie definicji do bieżącego zestawu w ApplyIconLibrary/ApplyObjectLibrary, także dla Undo; pozostałe drafty bez live podmiany. Zależny controls powtórzony z nowymi asercjami rzeczywistych TryChange/Undo dla ikony i obiektu: PASS f2564975dc7d48c19055a1542e128594. Pozostałych Case nie powtarzano.

Rebuild release PASS, nowy SHA25613DAC209E1839BD3320EF819848F5D19C9D044F3BDDF3CABC00309DBBE03AD80. Krótki zależny smoke tej kopii w toku; własne okno startowe release, poprzednie procesy zamknięte. Po nim podmienić główny EXE na13DA… (główny obecnieD70…), końcowy status/hash i jeden commit. Ostatnia operacja pewna: build+start nowego release, nie powtarzać bez sprawdzenia procesu.

## Zakończenie 0.9.0 — stan końcowy

Wszystkie etapy zakresu wykonane, celowane próby zakończone. Krótki smoke ostatniego rebuilda PASS: zwykły start i otwarcie własnej mapy, poprawny layout; potem czysty AltF4. Osobno zakończony wcześniej zależny MRU/Cancel/Undo smoke, bez powtarzania niezależnych testów. Główny EXE atomowo podmieniony na identyczne bajty ostatniej sprawdzonej kopii: FileVersion/ProductVersion0.9.0.0, SHA25613DAC209E1839BD3320EF819848F5D19C9D044F3BDDF3CABC00309DBBE03AD80. Własne procesy 0.9.0:0; brak własnych okien/dialogów. Operacja podmiany i porównania ma wynik pewny, nie ma nierozstrzygniętej operacji build/IO/UI.

README, spec47, plan wersji, backlog i raport0.9.0 ukończone. Raport oddziela metody, symulowane skalowanie i prawdziwe EXE oraz zawiera checklistę i niewykonane próby. Nie wykonywano pełnej kampanii, fizycznej macierzy DPI ani przenośności. Główna ramka bez zmian. Nie dotykano prywatnej Library/map/MRU ani okien użytkownika. Tests/_work pozostaje ignorowane i poza commitem.

Wymagany jeden commit ma obejmować wszystkie własne źródła, EXE, testy i dokumentację (angielski tytuł). Identyfikator tego commita odczytywać przez git log -1 -- docs/plan-0.9.0.md; końcowy status zweryfikować po commicie. Brak osobnego commita etapu lub dokumentacji. Przy wznowieniu przed ponowieniem commita sprawdzić git log/status, aby nie utworzyć drugiego. Pozostały tylko operacja pojedynczego commita i odczyt końcowego Git; nie podejmować nowych prac/testów bez regresji.
