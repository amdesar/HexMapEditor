# HexMapEditor 0.5.4

Wersja aplikacji **0.5.4**, FileVersion **0.5.4.0**. Baza: czysty master `0e8920f`, zawierający ukończoną 0.5.3; lokalny origin/master wskazywał tę samą historię. Bez migracji, odzyskiwania Library, push, tagu i Release'a. Prywatna Library/mapy i okna użytkownika nie były danymi testowymi ani celem sprzątania.

## Zmiany

### Puste pola

Nowe CellData mają dokumentowe ID `map-unpainted`, wersję `map-unpainted-v1` i pozycję `map-unpainted-fill`, wygląd #777F91, nazwę „Niepomalowany”. Nie są neutralnym terenem presetu i nie są usuniętym heksem lub Czerń/Pustka. Dotyczy też nowych brzegów po rozszerzeniu. Wcześniej konstruktor używał ID default-neutral, więc jawna aktualizacja neutralnego obejmowała również pustą mapę.

Malowanie neutralnym zapisuje jego rzeczywistą tożsamość, także przy identycznym kolorze. Wiadro odróżnia regiony po istniejących TerrainId/VariantId. Wybór presetu nie przypisuje go do pustych pól. Stan początkowy jest też właściwym punktem odniesienia ostrzeżeń o danych. Preset nie może zadeklarować zarezerwowanych ID, a odczyt sprawdza ich spójną reprezentację. Zachowano rozróżnienie historycznych wersji i aktualizację po ID wariantów z 0.5.3.

### Edytor wariantów i lokalne Undo/Redo

Wspólny edytor terenu/mgły ma kafelki i plus88×88, proporcjonalny cover i przewijanie. Stały czerwony × na kontrastowym tle usuwa tylko daną pozycję bez pytania i bez otwarcia edycji. Ostatnia jest chroniona; tooltip i dostępność wyjaśniają blokadę. Menu prawego przycisku używa tej samej ścieżki. Podczas próby okna glify były słabo czytelne; poprawiono rodzica kafelka na panel z sąsiednimi kontrolkami, zastosowano rysowany krzyż i istniejące lokalne ikony historii.

`AppearanceHistory` przechowuje100 operacji wyłącznie otwartego edytora. Obejmuje dodanie/usunięcie i zatwierdzone zmiany koloru, typu/grafiki/kadru. Kopiuje listy, ID i transformacje, współdzieląc niezmienne pełne źródła. Nie ma kopii obrazów per krok. Undo usunięcia przywraca pozycję i oba kadry; Redo jest unieważniane tylko nową zmianą. Brak zmiany i Anuluj nie dają wpisu. UI nie miało zmiany kolejności, więc nie dodawano nowej funkcji sortowania.

Ctrl+Z, Ctrl+Y/Ctrl+Shift+Z i przyciski dotyczą lokalnej puli. Pole nazwy zachowuje zwykłe tekstowe Undo, a modalne dzieci nie kierują klawiszy do nadrzędnego stosu. Główna historia mapy nie jest uruchamiana. OK puli przekazuje dotychczasową jedną transakcję dokumentu; Anuluj i ponowne otwarcie zerują lokalny stos. Lokalne cofanie nie usuwa zatwierdzonego importu ani innych plików biblioteki — cofa przypisanie do wariantu. Globalna historia LibraryFiles i jej ochrona konfliktów pozostają bez przebudowy.

### Mozaika

Gotowy podgląd przedstawia dwa rzędy regularnych miniheksów przyciętych do przycisku. Pojemność reprezentacji to12 odrębnych pozycji. Pula do12 pokazuje każdą przynajmniej raz, większa ma deterministyczny podzbiór; uzupełnienie miejsc jest stabilne. Powtórzone grafiki nadal są osobnymi pozycjami. Kolejność reprezentantów wyznacza hash TerrainId/ID pozycji, a uzupełnienie hash zawartości puli. Nazwa i repaint nie zmieniają układu, orientacja wybiera właściwy kadr i geometrię. Nie zużywa się Random malowania i nie zapisuje dodatkowego losowego stanu w mapie. Cache jest własnością kafelka, zastępowana bitmapa jest zwalniana; źródło jest dekodowane raz na odrębny kadr przy budowaniu mozaiki, nie co Paint. Wymiary wiersza, złoty wybór, tekst z obrysem i akcje pozostały.

### Jeden podgląd kadrowania

Dialog ma jedną powierzchnię608×408 i wybór Pointy Top / Flat Top, startujący w orientacji mapy. Przełącznik nie zmienia mapy. Nowy import do pierwszej zmiany orientacji aktualizuje oba zestawy parametrów wspólnymi deltami i ograniczeniami pokrycia. Pierwsza zmiana rozłącza bez resetu, późniejsze nie łączą. Nie ma przycisku ponownego łączenia. Niewidoczny kadr to wyłącznie parametry, bez drugiej kontrolki i odrysowań.

Ponowna edycja jest niezależna od początku, nawet gdy stary zasób ma Linked=true. Nowy import lub zmieniony kadr zapisuje Linked=false. OK całkowicie niezmienionego starego kadru zachowuje jego ID/metadane, aby samo usunięcie nieużywanego Linked nie tworzyło niewidocznego kroku. Przy następnej edycji i tak nie działa ono jako połączenie. Nie usunięto pełnych źródeł ani transformacji obu orientacji. Jednokrotnie dekodowany podgląd1024, ruch, kółko, obrót/reset i wynik z pełnych danych pozostają.

### Tło i nasycenie

Zewnętrzne powierzchnie mają równe365×310 i tę samą górną linię. Lewa pokazuje narzędzie kadrowania, prawa zachowuje proporcje rzeczywistego płótna. Nie zmieniono głównego cover, centrowania, statyczności pan/zoom, zakresu obszaru roboczego, uchwytów, krycia, blur, resetu100%, Anuluj ani historii.

Wcześniej każda zmiana nasycenia powtarzała skalowanie/cover i filtry większego podglądu, mimo buforowanego źródła. Podczas gestu suwaka wykorzystujemy cache geometrii cover160px i mały wynik efektów, zawsze dla najnowszej wartości. Jedno oczekujące odświeżenie, bez kolejki klatek, wątku i timera. Po MouseUp/KeyUp wykonywany jest dokładny renderer większego podglądu. Zatwierdzany dokument zachowuje źródło i dokładne ustawienia, nie ten bufor.

Lokalny pomiar20 kolejnych zmian nasycenia10…181 przy blur8 i obrazie1024×768: **598,29 ms przed → 25,74 ms po**. To suma pracy metod/kontrolek w jednym krótkim scenariuszu, nie FPS, statystyka wielu przebiegów ani czas całej obsługi Windows. Nie obiecujemy stałej płynności.

### Alpha grafiki a Fog

Sprawdzona ścieżka 0.5.3 jawnie rysowała kolor pod grafiką mgły oraz pomijała część warstw pod zasłoniętym polem. Raporty nie były dowodem obserwacji użytkownika. Wprowadzono uzgodnioną regułę: rzeczywiste warstwy są rysowane pod mgłą, grafika jest nakładana z alpha **bez koloru bazowego**; tylko wariant koloru wypełnia nieprzezroczyście. Obiekty też mogą prześwitywać przez grafikę alpha. Siatka pozostaje.

Logical Fog jest niezależne od pikseli. Szczegóły, lista, próg geometryczny i hit-test nadal maskują heksy/obiekty według Fog. Obrysy wyboru/grupy nadal są przycinane logicznie. Alpha nie odsłania danych w edytorze i nie wyłącza ochrony odkrywców. Podgląd bez mgły działa dotychczasowo. Zaktualizowano konkretną historyczną asercję nieprzezroczystej bazy i dodano rozróżnienie obrazu/logiki; nie skasowano testu.

## Formaty i limity

Mapa8, preset3, grafika biblioteczna2 — **bez zmiany**. ID pustej powierzchni i oba kadry mieszczą się w istniejącej strukturze. Nie zgadujemy, które starsze szare pola były puste: ich zapisane ID zachowują dotychczasowy sens. Nie ma konwerterów, masowych zmian bibliotek ani odzyskiwania poprzedniej Library. Pełne użyte źródła, deduplikacja, historyczne migawki i przenośność danych pozostają. Limity 0.5.3 nie zostały zwiększone. Nie przeprojektowano gwiazdki, zapisu presetów lub zamykania; backlog UX pozostaje.

## Sprzątanie tests

Sprawdzono git ls-files, runnery/generatory i rozmiary. Wszystkie główne podfoldery tests to jeden `_work`; poza nim są śledzone źródła/runnery/README. Runnery tworzą świeże GUID i generują swoje wejścia, nie odwołują się do dawnych wyników. Nie było reparse point w tests/_work ani junction tests. Odczyt procesów nie wykazał edytora/testów/kompilatora; własne okna przed drugim etapem porządków potwierdzono osobno listą Computer Use.

| Zakres | Przed | Po zakończeniu prób i porządków |
|---|---:|---:|
| tests/_work | 172127873 B /164,15 MiB | 11130335 B /10,61 MiB |
| całe tests | 172466483 B /164,48 MiB | 11487924 B /10,96 MiB |

Początkowe odchudzenie:66 plików /161013264 B (153,55 MiB):

| Kategoria | Liczba | Bajty |
|---|---:|---:|
| Generowane BMP, także próbka granicy32 MiB | 10 | 61866471 |
| Testowe EXE i własna kopia UI0.5.3 | 37 | 24654848 |
| Własne mapy kontrolne | 6 | 25536323 |
| Generowane presety, źródła i biblioteka prób0.5.3 | 13 | 48955622 |

Usunięto też37 odtwarzalnych plików /9466149 B utworzonych już w tym zadaniu (próby054 i bezpośredni051-fog), po zakończeniu procesów. Zachowano wszystkie źródła testów, runnery/Run-AllTests, oczekiwania w kodzie, dokumentację i małe wyniki. Niejednoznaczne migration-022, backup-przed-043 i starsze ręczne zestawy pozostają. Nie twierdzimy, że każdy z nich jest zbędny. Nie zmieniano generatorów tylko po to, by uzasadnić usunięcie. Dane054 odtwarza jawny Case; presentation generuje Fixture054.hexmap/Stripes.png do UI. Żadnego szerokiego git clean, kasowania tests/Library lub podążania przez linki. Każda usuwana ścieżka była zweryfikowana pod workspace/tests/_work, używano LiteralPath.

Manifesty i małe podsumowania pozostały lokalnie w `tests/_work/cleanup-054-f8ed7966e8d34f7dbf3848a13dda8317` (removed.csv, removed-current.csv, summary.json, final.json). Rozmiar może wzrosnąć po kolejnych świadomych próbach; nie gwarantujemy stałego rozmiaru projektu. Nie odtwarzano dawnych danych ani nie uruchamiano pełnych testów po sprzątaniu.

## Testy metod i kontrolek

Nowy `tests/Run-Patch054Tests.ps1` wymaga jawnego pojedynczego Case. Brak Case raz sprawdzony — błąd użycia przed buildem. Źródła i skrypty zostają w Git, wyniki są ignorowane w _work. Nie wykonano Run-AllTests ani dawnych kampanii.

- **empty PASS:** aktualizacja neutralnego nie zmienia nowych pustych pól; faktycznie namalowany neutralny aktualizuje się i stanowi odrębny region przy tym samym kolorze. Wybór presetu nie przypisuje go do pól, wiadro nie przekracza pomalowanego neutralnego, Undo/Redo i round-trip zachowują ID. Nowe brzegi po resize są niepomalowane.
- **editor PASS:** kwadraty/plus/× i ochrona ostatniego; Undo usunięcia odtwarza ID, pozycję, grafikę, oba kadry i współdzielone źródło. Edycja jest jednym lokalnym krokiem, no-op nie kasuje Redo, nowa gałąź kasuje. Limit100 i nowy dialog z pustym stosem; główne komórki/historia pozostają bez zmian do zatwierdzenia. Zatwierdzona pula daje jeden wpis główny. Podstawowa wspólna próba mgły.
- **crop PASS:** jeden CropSurface, inicjalizacja nowego importu w bieżącej orientacji, wspólne zmiany do pierwszego przełączenia bez resetu, potem niezależne; re-edit niezależny mimo dawnego Linked, Anuluj i no-op. Walidacja pokrycia, pełne źródło i obie transformacje po zapisie/odczycie presetu.
- **presentation PASS:** deterministyczna reprezentacja sześciu i ograniczenie32 do12, brak tasowania po nazwie/kolejności, repaint/wybór nie korzysta z Random malowania. Równe panele tła i canvas2:1, dokładna zgodność końcowego nasycenia z rendererem. Przezroczysta mgła pokazuje czerwony teren, ale Fog pozostaje, szczegóły/lista/hit-test nadal maskują.
- **background-perf:** pomiar przed/po opisany wyżej. Nie przeprowadzono pełnego benchmarku.
- **051-fog PASS, tylko ten Case:** dostosowana asercja alpha, aktywny odkrywca, delta/Undo/Redo mgły i podstawowa aktualizacja wyglądu. Wywołanie jawnego reroll dostosowano do zmiany domyślnego preserve z0.5.3. Nie powtarzano innych sześciu przypadków051.

Logi PASS: patch-054-empty-9d8887d591bf44bd9702f7a93ebc9c0e, editor-a18d3da3bb4c47fc874a69b87cf4f368, crop-7df0fc614ae848c1b6cce72269531005, presentation-919c6c148e2a4c2398ac67d0a6f33684 (wszystkie z patch-054-), patch-051-fog-3cbef34d9a614a91b915b4df299fd61c. Pierwszy setup empty nie inicjował biblioteki w niewidocznym MainWindow; pierwszy setup presentation odwracał listę bez odświeżenia aliasu pierwszego wariantu. Poprawiono te harnessy. Powtórzono tylko nieudane lub bezpośrednio zmienione przypadki. Nie przedstawiamy wcześniejszych wyników jako ponownie wykonanych.

## Rzeczywiste próby zwykłego EXE

Własna kopia: `tests/_work/ui-054-8be96cf91c704c8c93dbdcb9d6ae2562`, wyłącznie własne dane. Końcowy główny EXE i kopia miały identyczny SHA-256: `D0AD9527F96E5E54451A13031D4FF5A69B12C18A78DEDF3700BF5E80A785B8BE`.

Sprawdzono czytelną mozaikę, kwadratowe kafelki/plus/× i scroll do dolnej części. Ponowna edycja grafiki: kółko, drag, suwak Pointy do92°, przełączenie Flat pozostawiło jego0°. Zatwierdzenie kadru i wariantu, Ctrl+Z przywrócił poziome pasy, Ctrl+Shift+Z przywrócił obrót. Anuluj puli zostawiło mapę bez gwiazdki/globalnego Undo. Tło: równe powierzchnie, poprawne proporcje, drag nasycenia do16% i reset100%; końcowy wynik zmienił się prawidłowo, Anuluj zachowało dokument.

Te gesty wykonano na przedostatnim buildzie F5FD…; później poprawiono wyłącznie niezmienione OK starego Linked. Po tej korekcie zbudowano końcowy D0AD… i sprawdzono zależną ścieżkę: otwarcie kadru, OK bez ingerencji, OK wariantu — oba przyciski historii lokalnej były disabled według UIA; zatwierdzenie puli nie dodało gwiazdki ani globalnej historii. Nie powtarzano niezmienionych gestów tylko z powodu tego no-op. Nie jest to samo uruchomienie EXE przedstawione jako pełna próba.

Własne okna zamknięto; listy procesu po zamknięciu były puste. Nie zamykano okien użytkownika. Nie było oczekującego usuwania przez UI ani wymaganej zgody; × wywołano w testach kontrolki, w oknie oceniono jego czytelność. Nowe odtwarzalne dane/EXE usunięto dopiero po zamknięciu.

## Ograniczenia

Narzędzie wykonało drag/suwak, lecz udostępnia zrzut po geście, nie ciągłe nagranie. Nie deklarujemy płynności, FPS ani braku każdej pominiętej klatki na podstawie tych obrazów. Pomiar nasycenia dotyczy pracy metod, nie subiektywnego odbioru.

Nie wykonano pełnej matrycy puli/źródeł, DPI, wydajności dużych map, starych audytów WPF, historycznych scenariuszy regionów i wszystkich bibliotek. Nowy import/synchronizację oraz logiczną alpha sprawdzono metodami; w zwykłym oknie reprezentatywna próba kadrowania dotyczyła ponownej edycji. Nie wykonano pełnej macierzy routingu klawiatury wszystkich modalnych pickerów i pól tekstowych ani manualnego kliknięcia ×. Historyczne testy dwóch paneli/ponownego łączenia pozostają źródłami poprzedniej semantyki i wymagają osobnego dostosowania, nie dodania niewidocznego renderera. Nie wymagano ponownie potwierdzonej przez użytkownika kampanii zapisu mapy/presetu lub recrop po odczycie — nowy round-trip dotyczył tylko zmienionych ID/transformacji.

## Checklista użytkownika

### I. Nowości i poprawki0.5.4

- Nowa mapa: edytuj neutralny z aktualizacją — puste pola pozostają; namalowany neutralny zmienia się, Wiadro rozróżnia regiony. Sprawdź nowe brzegi po resize.
- Dodaj/edytuj/usuń wariant przez ×, cofnij/przywróć lokalnie. Sprawdź zachowane ID/kadr, limit ostatniego, Ctrl+Y i Ctrl+Shift+Z, a następnie Anuluj/ponowne otwarcie.
- Oceń mozaikę i nazwy, brak tasowania przy hover/zmianie nazwy i właściwy kadr po orientacji.
- Nowy import: obróć przed pierwszym przełączeniem, sprawdź drugi kadr; potem edytuj niezależnie. Re-edit zawsze od początku niezależny. Oceń płynność pojedynczego podglądu.
- Tło: równe panele, proporcje, drag nasycenia/reset; zaakceptowane pan/zoom i cover pozostają. Oceń płynność na własnym źródle.
- Grafika mgły z alpha: obraz prześwituje, ale logiczne szczegóły/lista pozostają maskowane; odkrywcy nadal chronią zakres.

### II. Nadal niepotwierdzone

- Dokładne Undo/Redo rozmieszczenia po jawnej aktualizacji — dopóki użytkownik nie potwierdzi osobno.
- Trzy decyzje duplikatu oraz niezależna edycja kopii.
- Przenośność do czystej kopii — **świadomie odłożona**, bez żądania nowej kampanii teraz.

## Git i wynik

Build lokalnym Build.ps1; główny HexMapEditor.exe0.5.4.0 pozostaje śledzony. Commit implementacji, EXE i celowanych testów: `f62314633c8a725d14a178be2b51cb34dcd18fa6`. Po nim pozostały wyłącznie README, specyfikacja, plan, raport i tests/README. Osobny commit tych pięciu plików dokumentacji jest końcowym commitem podanym w odpowiedzi. Docelowy stan po nim: czysty master, dwa lokalne commity przed sprawdzonym lokalnym origin/master. Library, dane prób, logi i testowe EXE są ignorowane, przegląd staged wykluczył je z commita. Bez push, tagu i Release'a. Nie deklarujemy kompleksowego przetestowania aplikacji.
