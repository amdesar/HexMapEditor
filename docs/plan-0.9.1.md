# Plan 0.9.1

Baza potwierdzona: 7791556, Git czysty przed rozpoczęciem; raport i końcowy plan0.9.0 przeczytane. Brak AGENTS.md w repozytorium. Stan końcowy: zaimplementowana i celowanie sprawdzona wersja0.9.1.0; szczegóły w ostatnim punkcie i raporcie. Poniższe etapy zachowują historię decyzji.

## Etapy

1. Odtworzenie biblioteka/osadzony preset, reguła źródła panelu przy otwarciu, drafty; aktywne sekcje częściowego zamykania i pytanie o mapę.
2. Różnicowe odświeżanie quick,0/1/6/7/12 bez placeholderów; dwa wybory kosza, usuwanie biblioteki konflikt-safe i listy sesji.
3. Kompaktowy stały panel bez globalnego scrollbara: wspólne krawędzie, parametry w jednym rzędzie, minimum wyliczone dla maksymalnego układu na FullHD100%.
4. Lokalne formularze/styl disabled/switch labels, radio-switche rodzaju, ikony i focus terenów; kwadratowe kadrowanie importu ikon/obiektów z pełnym otoczeniem.
5. Celowane Case i zwykły EXE we własnym tests/_work; wersja0.9.1.0, dokumentacja/backlog i jeden lokalny commit po pełnym ukończeniu.

## Decyzje / ograniczenia

Map11/preset5 i struktura Library bez zmiany, jeśli import kończy się istniejącym PNG IconAsset. Panel po odczycie korzysta z poprawnego lokalnego presetu po ID; osadzone wyglądy pomalowanych pól i instancji pozostają niezależne. W sesji drafty nie są podmieniane przy każdym wyborze. Zamknięcie częściowe zachowuje pominięte drafty aż do rzeczywistego zamknięcia. Bez ingerencji w odebraną ramkę/geometrię, bez pełnej kampanii i prywatnych danych.

Powyżej plan początkowy. Aktualny stan i zakończenie poniżej; wpisy etapów pozostawiono jako historię decyzji i zależnych prób.

## Etapy 1–4 i testy celowane — zapis wcześniejszego etapu

Odtworzenie na0.9.0: baseline73c8c46f9a4643699f61f785a866d544 potwierdza plik biblioteki z nowymi terenami/0ikon, ale panel z osadzonymi terenami/12ikon. Nie było utraty danych mapy. AdoptLibraryPanel przy przyjęciu dokumentu wybiera poprawny lokalny preset poID; w sesji SelectPreset zachowuje draft. TryLoadProject odświeża katalog dla Open/MRU. Preset uszkodzony pomijany z ostrzeżeniem, osadzony fallback zachowany, bez importu doLibrary. Open016c8f4fe23e4b6abcc99d30e4f0d649 PASS: obie kolejności zapisu, aktualizacja katalogu, fallback/brak pliku, uszkodzony katalog z komunikatem logicznym, draft i cleanmap. Modalnego ostrzeżenia uszkodzonego pliku jeszcze nie klikano wEXE.

Częściowe zamykanie: switche aktywne, allOff blokuje, tekst o pominiętych sekcjach; pytanie mapy zawiera nazwę. Close57ca5ed352704d02975734165c68e8bb PASS: częściowy zapis A→CancelB, A pozostaje dirty z draftem, kolejna procedura pytaA ponownie, błąd/anulowanie zapisu stop. Bez zmiany writera/formatów.

Quick: różnicowa aktualizacja zachowuje przyciski/miniatury poID, usuwa tylko brakujące, przesuwa istniejące;0/1/6/7/12 bez placeholderów i wysokość0/1/2rzędy. RestoreTerrainOnly nie odświeża ikon/obiektów. Lewy kosz ma dialog wyboru quick/library; brak pliku kopii wyłącza library. PPM tylkoquick. Usunięcie biblioteczne czyści zainicjalizowane zestawy sesji dopiero po poprawnym przejściu LibraryFiles. Quick5586292c8151459f90c80faa00d63087 PASS: delta usunięcia1/0 tworzeń; terrainrestore0/0, błąd konfliktu bez czyszczenia, wszystkie drafty bezID, pliki presetów i wystąpienia zachowane, restorekopii bez odtworzenia pliku, analog obiektu. Nie jest to dowód braku pojedynczej migającej klatki.

Layout:276px+18uchwyt, przyciski48×26 zgap5px, wspólne krawędzie z6×40px kafelkami;4×3 zachowane. Parametry pojedynczy rząd, tekst lub przy trójkącie symbole z tooltipami po rzeczywistym pomiarze. Stałe minimum wyliczone z192tool+114preset+126minteren+120+120quick+góra/dół+ramka =1180×806 naDPI96. AutoScrollkolumnyoff, terenreszta. Controls525a88c9f55d4a558d448e6ead3ad458 PASS: maksymalnyukład/minimum,brakscrollbara,marginy,krawędzie,parametry-360°,wyrównanieNameDialog,radio-switchjednaopcja,disabledtext. PierwszecontrolsFAIL c403… za mały wewnętrznyobszar-360; drugi49e9… różnica2pxkrawędzi; oba lokalnie naprawione, zależne powtórzeniePASS. Fizyczny ekran stanowiska2560×1440,DPI96 (100%); próba okna o geometriiFullHD/minimum planowana, bez nazywania rozdzielczości DPI.

Formularze:EditorDialog centruje nativeTextBox wgPreferredHeight whost,AppearanceName podobnie;SwitchDrawing jednoliniowe i czytelneDisabled;EditorButton wdotykanychdialogach wspólnie rysujedisabled i destruktywnyczerwonyaktywny. FillVariantDialogradio-switche,wierszKolor/Gradient/Grafika bezpustegowiersza;Import/Biblioteka/Kadruj. TytułyKolorRysika/Wskaźnika, większy odstępgradiencie,ListaObiektówDarkFormCaption zfallback. ComboBox beznowejkontrolki,zostajeobsługiwanystylnatywny. Poprawionesymbolehand/brush/eraser/eye i focusklawiaturyterenu beznatywnejkropkowanejlinii.

Import:SquareImportDialog wspólny dla ikon/obiektów/podmianypliku. ReadImport zachowuje8MiB/2048/formaty nieruchome; pełny bitmapwRAM tylko na czasdraftu. CropTransform i CropMath.Draw bez zmiany mechaniki kadrowania terenów; własneContain/pan/zoom/angle bez wymuszaniaCover. Wyjście512²ARGB, podglądkadr560²+przyciemnioneotoczenie. Crop a2af8e0336964257815d3620a0cef4e6 PASS:Contain,alpha,pan/zoom/37°,deterministycznyPNG,Cancel bezResult,oryginałbajtowoniezmieniony. WspólnyTerrainCrop/CropMath niezmieniony, brak nowego recropzasobów.

Pozostałe: krótka zwykła sesjaUI wewłasnejkopii, ostatni przegląd bez rozszerzania, wersja0.9.1.0/buildgłówny po weryfikacji, dokumentacja/nowyplanwydań, jedencommit. Główny EXE nadal0.9.0. Własnych zwykłychprocesów nie uruchamiano0.9.1; testowe okna metod byłyDispose, inventory sprawdzić przedUI. Ostatnia operacja pewna: open/close/quickPASS. Brak nierozstrzygniętej operacji i brak znanej blokady. Nie kończyć na etapie/planu.

## Punkt przed końcową próbą — zapis historyczny 14.11.2026

Etapy 1–4 zaimplementowane. Zwykła sesja EXE potwierdziła panel z biblioteki, układ i minimum 1180×806 przy 96 DPI, parametry, puste zestawy, radio-switche, pełne etykiety, disabled i czerwony Usuń punkt. Import ikony: pan/zoom/133° w kwadratowym podglądzie z alpha; import obiektu: domyślne dopasowanie. Usunięcie quick i biblioteki we własnych danych wykonane za zgodą użytkownika. Częściowe zamykanie: A zapis tylko ikon → pytanie B → Anuluj; oba drafty pozostały. Ponowny wybór A przez rzeczywistą listę potwierdził Roboczy teren A i niezapisany status. Następnie oba własne drafty świadomie zapisano i zamknięto kopię.

Pierwszy proces został ręcznie zamknięty przez użytkownika podczas importu (potwierdził to), bez zapisanego zasobu. Wznowiono tylko niedokończoną próbę. Nie jest to crash aplikacji.

Znaleziono zależność Undo usunięcia bibliotecznego: plik wracał, ale lista quick nie. Dodano migawkę zainicjalizowanych list sesji w istniejącym kroku LibraryChange; konflikt-safe zapis pliku poprzedza zmianę list. Quick6426a422fbea43118fde7d1d5fe391a8 PASS dla Undo/Redo ikon i Undo obiektu. Test d4c… miał błędnie wstawioną zmianę wyboru presetu między delete i Undo; poprawiono wyłącznie kolejność testu.

Kandydat po tej poprawce zbudowany w fixture abd254…; SHA256 E6EE1469E99CC5B72046EF1DDE07B59733614375520F13F67AF5192F164B1544. Główny EXE nadal 0.9.0. Pozostałe: krótka zależna próba delete/Undo na nowych bajtach, publikacja tych samych bajtów jako główny EXE, dokumentacja i jeden commit. Nie powtarzać niezależnych PASS open/close/controls/crop. Fizyczny ekran 2560×1440 przy 100%; geometrię minimum sprawdzono, fizycznego monitora Full HD nie testowano. Bez nagrania nie deklarować braku pojedynczej migającej klatki.

Ostatnia operacja: build kandydata zakończony poprawnie. Brak niepewnej operacji. Własne okna testowe zamknięte, kolejną kopię trzeba uruchomić; nie zakładać stanu okien użytkownika.
## Zakończenie implementacji i weryfikacji

Wszystkie etapy1–5 zaimplementowane, testy celowane i pogrupowana sesja zwykłego EXE wykonane. Aktualny raport: raport-0.9.1.md. Ograniczenia: fizyczny ekran2560×1440/100%, geometria minimum1180×806 mieści się wFullHD100%, brak fizycznego monitoraFullHD i brak nagrania pojedynczych klatek flasha. Bez pełnej kampanii aplikacji.

Końcowe open cc169e02862d4d35805e0f1f87cd6620 PASS rozszerza fixture o pomalowany heks, ikonę i instancję oraz round-trip zachowanych źródeł. Powód powtórzenia: poprzednia fixture nie miała faktycznie pomalowanego heksa/obiektu, więc nie stanowiła wystarczającego dowodu tego wymagania. Produkcja bez zmiany.

W zwykłym kandydacie po poprawce Undo: import własnego obiektu, delete biblioteczny, Undo/Redo/Undo przywracają też pozycję listy; czysty status presetu. W końcowym przeglądzie poprawiono proporcje zmienionej miniatury niekwadratowej w nowej ścieżce różnicowej. Zależny quick da8167f3f0c54b5282fa0d6cd93467ba PASS, również16×32alpha i zachowanie kontrolki. Ponowiono wyłącznie build i krótki start/MRU/panel/zamknięcie na końcowych bajtach. Niezależne controls/crop/close bez powtórzenia.

Pierwsza publikacja kandydata miała błędny pusty parametr backupPath (poprzedniEXE został zachowany), poprawiono wywołanie NullString. Ostateczna publikacja przetestowanych bajtów zakończona, obaEXE0.9.1.0. SHA256 A1FF9F96D0CAE6EA0D64430B1CFB353B940F5387BC68A7C39396515DC030A61D. Aktualne README/specyfikacja/planwydań/backlog/raport gotowe. Żadnej zmiany map11/preset5/Library i żadnego wdrażania późniejszych etapów.

Własne okna/procesy: końcowa kopia zamknięta Alt+F4, inventory potwierdziło brak okien testowych. Nie zamykano okien użytkownika. Ostatnia niepewna operacja: brak; po publikacji potwierdzono zgodność hashów. Wydanie obejmuje jeden lokalny commit własnych plików; jego ID: git log -1 -- docs/raport-0.9.1.md oraz odpowiedź końcowa. Końcowy odczyt Git wykonywany po commicie. tests/_work, Library i MRU nie należą do commita. Nie pozostała znana nieukończona implementacja ani blokada; brak push/tagu/Release.