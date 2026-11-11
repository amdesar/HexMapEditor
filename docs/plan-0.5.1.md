# Plan 0.5.1

Baza sprawdzona 11 listopada 2026. HEAD `3485ef9`, gałąź master, katalog roboczy czysty. Główny EXE pozostaje wersją 0.5.0 do ukończenia zakresu.

## Etapy

1. Model pełnych źródeł i niezależnych kadrów, wspólne przekształcenia względne, limity i buforowanie. Dwa podglądy z obrotem. Celowana próba modelu kadrowania.
2. Pule wariantów terenu, wspólny edytor, trwały wybór wyglądu i deterministyczne próby losowania oraz regionów.
3. Biblioteka źródeł i definicji, rozpoznawanie identycznej pozycji, pliki i historia z ochroną konfliktów. Przenośne formaty mapy/presetu.
4. Wygląd mgły z pulą oraz jej aktualizacja w jednej transakcji.
5. Statyczne tło, kadrowanie, efekty i Widok.
6. Wybrane próby zwykłego EXE, dokumentacja, oznaczenie 0.5.1 i końcowy build.

## Punkt wznowienia

Etap 1 zaimplementowany. GraphicSource zachowuje pełne oryginalne bajty, GraphicDefinition dwa kadry i Linked, CropMath stosuje względne delty. Nowy dialog ma dwa podglądy i suwak 0–360°. Zmieniono roboczy zapis na mapę 8 i preset 3. Główny EXE i oznaczenie aplikacji nadal 0.5.0.

PASS crop w tests/_work/patch-051-crop-836fc11b18144fe2aa8d491b2984adfb. Obejmuje oryginalne bajty, niezależność kadrów, połączenie bez resetu, względne zmiany, zapis i pokrycie po obrocie. Brak Case w runnerze 051 zakończył się błędem użycia przed utworzeniem katalogu i kompilacją. Runner 050 także wymaga Case, jego historyczne testy modelu wymagają dostosowania do nowego formatu przy osobnym audycie.

Etap 2 zaimplementowany. Pula FillVariant miesza kolory i definicje grafiki, ma wspólny edytor kafelków i 1–32 pozycje. Heks zapisuje wybraną pozycję i migawkę wyglądu. Produkcja używa Random, test kontrolowanego generatora. Regiony opierają się na TerrainId, a mgła dodatkowo na początkowym Fog.

PASS pool w tests/_work/patch-051-pool-c3c23a8eeeb8484b9ff53f973c5012d8. Sprawdzono mieszane warianty, powtórzenie sąsiadów, brak kolejnego losowania odwiedzonego pola, dokładne Undo/Redo, region i osadzenie źródła puli.

Etapy 3–5 zaimplementowane. Pełne źródła w Sources, rozpoznanie duplikatu z decyzją, warianty mgły i aktualizacja istniejącego wyglądu, statyczne tło z myszowym kadrowaniem i buforowanymi efektami.

PASS fog w patch-051-fog-1afb8c89bfab42c8b0168cfb5b517b36. PASS library w patch-051-library-42cef3563ac5420288506f8c1d03f200. PASS background w patch-051-background-71d569a95e73483888ca8f468fc4cd4e. PASS portable w patch-051-portable-a034a98ea52441ceadb2b8f1d1ff974e. Katalogi są pod tests/_work.

Po tych wynikach poprawiono cache na LRU 256 kafelków (64 MiB), aby mapa z ponad 128 definicjami nie czyściła całego cache. Rozszerzony library przeszedł w patch-051-library-cfb5e1645f524c6999a6d4d220ae6982, obejmując rzeczywistą ścieżkę Importuj jako nową w izolowanym katalogu.

Dodano GraphicSchema wymagający jawnych pól nowego formatu. PASS library po tej walidacji w patch-051-library-efc0f04b3ba149c5ba9830713a5226cb i PASS portable w patch-051-portable-af84b712d0fe4be596a376bcf1b6daf6. Nie powtarzać tych prób bez wpływającej zmiany.

Próba UI wykryła wysunięte przyciski edytora puli. Ustawiono początkową szerokość jego header/footer przed dodaniem kotwiczonych kontrolek. Otwarty ponownie dialog miał widoczne przyciski. Ostatnią operacją był klik plusa w edycji terenu Pula, otwierający edycję nowego wariantu koloru. Nie zatwierdzono danych. Przy wznowieniu lista okien nie zawierała własnego procesu, więc należy ponownie uruchomić istniejący tests/_work/ui-051/HexMapEditor.exe i dokończyć próbę.

Własna mapa Proba051.hexmap zawiera 8×6 pól, dwa kadry jednego źródła w palecie i tło. Kopia ma robocze oznaczenie 0.5.0, główny EXE nadal 0.5.0. Pozostały próby zwykłego EXE, przegląd końcowych zmian, dokumentacja i końcowy build 0.5.1.

## Decyzje

Pełne oryginalne bajty źródła są współdzielone przez definicje kadrów. Wyniki renderowania są odbudowywanym cache 256 × 256. Źródło do 16 MiB i 4096 × 4096, budżet 64 źródeł / 64 MiB, 256 definicji, cache LRU 256 kafelków / 64 MiB. Biblioteka używa Sources/source-SHA.json oraz terrain-Nazwa.json. Sources jest niezmiennym magazynem współdzielonych bajtów, Undo zmienia wpis logiczny, a zachowane źródło może być wykorzystane ponownie. Pozostawiamy dawne pliki bez migracji.

### Uzupełnienie wznowienia
Otworzono ponownie zapisaną własną mapę ui-051/Proba051.hexmap. W UI sprawdzono dodanie koloru do puli terenu, jeden gest z różnymi wariantami oraz zapis/odczyt. Dwa podglądy: rozłączenie, obrót tylko Pointy do 91°, połączenie bez resetu, kolejny obrót do 181° daje Flat 90°; przesuwanie myszą i wspólny zoom kółkiem. Dodano bezpośrednie Kadruj przy wariancie, korzystające z osadzonego pełnego źródła także bez biblioteki. Sprawdzono mieszaną pulę mgły i jej nakładanie.
UI ujawniło błąd float w narożnikowym kadrowaniu tła (left+width minimalnie >1) oraz miniaturę Flat dekodowaną kadrem Pointy. Poprawiono oba. PASS nowego wyłącznie celowanego controls w patch-051-controls-1bd93f3d6e5048f391ced97ac6e358d9: rzeczywista metoda ruchu narożnika i render podglądu Flat. Nie powtarzano wcześniejszych przypadków.
Pozostało: próba poprawionego narożnika i statycznego tła w zwykłym EXE, dokumentacja, wersja/build 0.5.1 oraz końcowy przegląd. Własne okno ui-051 nadal otwarte, mapa zapisana po puli mgły. Główny EXE bez zmian.

### Ukończenie
Zakres zaimplementowany. Poprawiony narożnik tła ponownie sprawdzony w zwykłym EXE: bez wyjątku; nasycenie 0%, rozmycie 6 px i zatwierdzenie. Pan i zoom przesuwają heksy, a tło pozostaje w tym samym miejscu ekranu. Własna mapa zapisana, okno zamknięte. Ustawiono wersję 0.5.1/0.5.1.0 i zbudowano główny EXE. Identyczna bajtowo kopia uruchomiła okno 0.5.1 w tests/_work/ui-051, następnie zamknięta. SHA-256 końcowego EXE: 4FF030486AB5744CB76EA63A322D1005F73F9C6D361FE5E9FC796D62602F2F66.
Niepewnych operacji ani własnych otwartych procesów/dialogów już nie ma. Pozostaje wyłącznie zapis raportu/przegląd i lokalny commit. Siedem celowanych przypadków oraz osłona Case opisane w raporcie; nie uruchamiano pełnego zestawu. Prywatna Library nietknięta. Dane i wyniki własnych prób pozostają ignorowane w tests/_work.
