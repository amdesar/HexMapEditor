# Raport HexMapEditor 0.7.0

## Wynik i baza

Zaimplementowano zakres0.7.0 na potwierdzonym czystym `43708ee` (kod0.6.4 `45f3481`). Aplikacja0.7.0, główny EXE FileVersion0.7.0.0. Kod/EXE/testy w lokalnym commicie **7bf009e**. Dokumentacja w kolejnym lokalnym commicie; jego identyfikator podaje odpowiedź końcowa. Końcowy Git sprawdzony po zapisie dokumentacji. Nie wykonywano push, tagu ani Release'a, migracji lub sprzątania prywatnych danych.

SHA-256 głównego EXE i końcowej kopii UI: **6CE97CC8341266679D0E2603462086408545085216AA91D5633B57007B523600**. Wszystkie materiały prób pod `tests/_work`. Źródła dawnych testów zachowane. Nie uruchamiano Run-AllTests, dawnych runnerów hurtowo, audytów WPF/DPI/grup lub pełnej przenośności.

## 1. Zakończenie gumki

Przyczyna blokady: MouseUp synchronicznie wykonywał Exclude na powierzchniach, budowę tekstu konturów, pełną walidację i aktualizację historii/dirty na UI. Poprawiony wcześniej podgląd nie usuwał tego kosztu. Kapsuły gestu były też przekazywane do każdej przecinanej kreski bez lokalnego filtra ich obwiedni.

Naprawa: wszystkie próbki/zakręty pozostają; osobno dla kreski wybierane są kapsuły przecinające jej obwiednię. Dokładny wynik przygotowuje jeden worker MTA na gest, na odłączonym modelu/punktach/kopiach gradientów. Przez wątek przechodzą wyłącznie stringi i zamrożone Freezables; ścieżki GDI worker tworzy i zwalnia sam. Nie ma kolejki operacji lub workerów na próbkę. Serializacja/walidacja wyniku poza UI. UI stosuje cały wynik tylko przy zgodnej referencji dokumentu/listy Ink i rewizji, z jedną historią. Niezmienione kreski wracają jako poprzednie współdzielone wersje; zmieniony gradient zachowuje dawną drogę i fazę. Prepared Ink serialization służy sprawdzeniu dirty bez ponownej serializacji całej mapy na zastosowaniu.

Widoczne Przetwarzanie… i Anuluj wymazywanie; konfliktujące kontrolki disabled, skróty/metody zapisu, historii i zmiany dokumentu dają jawny stan oczekiwania. Cancel/błąd/stale nie zmieniają części dokumentu lub historii. Zamknięcie anuluje, spóźniony wynik nie może trafić do innej mapy. Token sprawdzany między kreskami; pojedynczy natywny boolean nie jest przerywany w połowie. Nie zmieniono tolerancji, powierzchni gumki, Fit, zapisu ani budżetów.

### Pomiar tej samej ograniczonej fixture

24kreski×320punktów, szerokość40świata, zakrzywiony gest140próbek/średnica42. To reprezentatywna próba, nie maksimum dokumentu i nie twierdzenie o odtworzeniu dokładnie3s użytkownika.

| Operacja | 0.6.4 synchronicznie | 0.7.0 |
|---|---:|---:|
| Budowanie kapsuł podczas gestu | 21,09ms | zachowane próbki; nie ponawiano pomiaru |
| Contains/broadphase geometrii | 214,36ms | 47,36ms worker |
| Końcowe Exclude | 476,38ms | 231,19ms worker |
| Serializacja | 35,43ms | 41,37ms worker |
| Walidacja wyniku | 18,26ms | 18,64ms worker |
| Pełne dawne FinishInk na UI | 809,90ms | zastąpione enqueue96,28ms i późniejszym apply77,65ms |
| Odłączona migawka | część starego kosztu | 2,26ms, wewnątrz enqueue |
| Historia+dirty+UpdateUI | wewnątrz FinishInk | 7,18ms, wewnątrz apply |

W rzeczywistej pętli WinForms podczas powodzenia23tyknięcia timera15ms, największy odstęp128,08ms. Potwierdza postęp UI podczas obliczeń; nie jest FPS lub obietnicą zerowego opóźnienia. Enqueue/apply nadal mają ograniczony koszt UI. Większy dokładny boolean może liczyć dłużej w tle. Baseline i późniejsze pomiary wykonano lokalnie, bez sztucznych progów.

## 2. Zoom/pan mapy wewnątrz płótna

Przyczyna: cache pełnego viewportu był unieważniany przy każdej zmianie kamery; nawet mały pan od nowa rasteryzował widoczne gradientowe drogi. To inne obciążenie niż samo dopisywanie nowej kreski, poprawione0.6.4.

Całkowity pan przesuwa piksele cache w LockBits/memmove i dokładnie dorysowuje odsłonięte pasy. Podczas serii zoomu przekształcany jest poprzedni cache, nowe pasy rysowane z danych; po zakończeniu pełny dokładny obraz. Prostokąt PointF zachowuje ułamkową geometrię. Timer120ms tylko wykrywa koniec serii koła, nie działa jako stałe odrysowywanie. Brak kolejki kamer, gigantycznego rastra świata lub trwałego obniżania rozdzielczości. Chwilowy podgląd zoomu może być mniej ostry; nie jest zapisywany jako dokładny cache. Pamięć cache≤5,8mln pikseli PArgb, scratch256×256 — nadal poniżej dawnego24MiB. Większy viewport ma fallback bez tego cache, nie zaniżoną jakość.

Pomiar: viewport1000×640,8×240punktów, szerokość90świata, x co16; zoom.25/.5/1/1.46; osiem panów po(12,8). Dodatkowa kreska400świata narysowana na oddaleniu, później zoom. Czas renderera, nie systemowego przesuwania okna i nie ocena płynności UI.

| Operacja (ms) | Jednolity0.6.4 →0.7.0 | Gradient0.6.4 →0.7.0 |
|---|---:|---:|
| Pierwszy render |14,45 →14,92|89,24 →58,99|
| Warm |1,97 →1,90|1,26 →1,19|
| Pan8 |23,40 →11,34|560,61 →15,51|
| Cztery zoomy interakcyjne |10,46 →24,18|212,31 →36,75|
| Dokładny obraz po zoomie |1,32 →2,74|1,18 →27,97|
| Dodatkowa duża kreska+zoom |6,65 →13,35|105,80 →77,73|

Dawny final był już cache hit po synchronicznych zoomach, nowy final wykonuje odroczony dokładny render. Jawny narzut prostego jednolitego przypadku; nie deklarujemy przyspieszenia wszystkiego. Pusta warstwa12rysowań3,20→2,91ms. Po panie porównanie ze świeżym dokładnym renderem: maks. różnica RGB0 jednolity/1gradient. Wstępny BitBlt dawał błędne piksele, został odrzucony; jego szybkie liczby nie są wynikiem końcowym.

## 3. Grube gradienty

Odtworzono dyski na łuku140próbek, szerokość100, zoom112%: pełny jednolity dysk próbki pozostawiał widoczny inny kolor przy szerokim łączeniu. Zamiast niego rysowany jest tylko zewnętrzny odkryty sektor round-join. Kolorowe korpusy mają zakładkę1,5piksela urządzenia zapobiegającą pinholom; rzeczywista wektorowa sylwetka pozostaje bez zmiany. Późniejszy fragment nadal na wierzchu. Cykl300px,80%paleta/20%powrót, faza od drogi i kolor pozostałości po gumce zachowane. Nie ma wygładzania całego wejścia, dodatkowych punktów ani pełnych stemplowanych dysków. Obraz przed/po obejrzany; końcowa asercja0izolowanych pinholi. Rzadszy łuk22próbek przy113,7% bez przerw w punktach drogi. To ograniczona kontrola, nie kampania okręgów.

## 4. High/Low i uśrednianie

Wysoka/Niska jakość na górnym pasku, przy małej szerokości drugi rząd zamiast kolizji. Domyślnie High i reset wyłącznie po udanym Nowa/Otwórz. Sesja procesu, bez trwałych ustawień, map/presetów, dirty lub historii. Przyszłe ustawienia lokalne również nie będą tego utrwalać.

Low pomija tło bez zmiany flagi Widoku; grafiki/gradienty heksów i mozaiki przedstawia kolorem reprezentatywnym. Solid pozostaje bez zmiany. Rysik/wskaźnik korzystają z pierwszego koloru, pomijając kolorowy render; nowa kreska nadal zapisuje pełną definicję i drogę, więc High odtwarza cały przebieg. Edytory i biblioteki mają zawsze pełne źródła. Ikony/obiekty/opisy, geometria, kolejność warstw i logiczne maski bez zmian. High unieważnia właściwe cache — nie zachowuje bitmap Low.

Średnia: deterministyczne64×64środki próbek tylko wewnątrz właściwego heksa. Grafika uwzględnia orientację, zapisany kadr, obrót/przesunięcie/skalę; RGB ważone alpha, alpha to reprezentatywne pokrycie wnętrza. Całkowicie transparentna grafika pozostaje transparentna. Gradient ma średnią przestrzenną z położeń punktów, kierunku i projekcji heksa, nie średnią samych stopów. Cache≤256samych kolorów, klucz od zastosowanego wyglądu/kadru/orientacji, nie TerrainId lub współczesnej biblioteki. Dekodowanie źródła tylko na cache miss, bitmapa od razu Dispose. Bez utrzymywania pełnego obrazu dla koloru. To ograniczona aproksymacja próbkowaniem, nie pikselowo dokładna całka obrazu.

## 5. Kolory sesji i zaznaczenia

Jedna wspólna historia12solidRGB w2×6 dla obu pickerów. OK jednolitego koloru dodaje/move-front bez duplikatów; gradient, Anuluj i ruchHSV nie dopisują. Próbka wybiera roboczy kolor lub aktywny punkt gradientu. Puste disabled z tooltipem, zapisane mają HEX. Bez plików, głównego Undo lub automatycznego dopisywania stopów. Restart zeruje pulę. Jednolity/Gradient mają ciemnoniebieskie tło i ramkę, również CheckedBackColor (natywne rysowanie początkowo nadpisywało samo BackColor). Hover/focus/disabled i czerwone usuwanie zachowane. Dialog tła nie posiada analogicznego przełącznika typu — jego istniejące akcje nie zostały przebudowane.

## 6. Start i ostatnie projekty

Lekki StartWindow: istniejąca ikona, HexMapEditor/wersja, Otwórz/Stwórz nowy,5dostępnych recent lub pusty stan. Library nie jest ładowana przed wyborem. Wspólny NewMapDialog/CreateProjectData oraz wyodrębniony istniejący walidator ReadProjectData, nie drugi format odczytu. Edytor przygotowuje bibliotekę/layout przed Show. Argument mapy przechodzi walidację przed konstruktorem edytora, bez modelu tymczasowej pustej mapy; błąd przechodzi do startu. Anuluj pozostaje na starcie. Guard nie pozwala zagnieździć kilku startowych akcji, NewProject edytora zachowuje RunModal i osłonę wejścia. Bez Sleep/DoEvents/globalnej kompozycji lub zmian WPF.

**`recent-projects.json` obok EXE**, poza Library, max30pełnych znormalizowanych ścieżek/max256KiB. Windows case-insensitive, spacje i polskie znaki. Aktualizacja wyłącznie po sukcesie odczytu/zapisu/Zapisz jako. Atomowy Flush+Replace, błąd nie blokuje mapy/startu. Pliki missing pozostają w historii; wybieranych5pierwszych dostępnych spośród30, bez sortowania dat/scanningu. Przywrócona ścieżka znów może być widoczna. Tooltippełna ścieżka, przycisk nazwa mapy. Klik ponownie wykonuje walidację. Dostępność w tle: jeden checker lokalny i dwa UNC, bez blokowania UI; wolne sieciowe wyniki mogą oczekiwać na timeout systemu, nie tworzymy nieograniczonej kolejki. Brakujące/niepoprawne dane historii nie naprawiają ani nie modyfikują map.

## 7. Formaty, testy i ograniczenia

Map11/preset4 bez zmiany. Brak nowych danych autorytatywnych: Low, cache, MRU i historia kolorów pozostają poza projektem. Budżety źródeł/geometrii/300historii nie zwiększone. Źródła i Surface nadal przenośne w dotychczasowym zapisie. [Plan wydań](plan-wersji.md) zawiera uzgodnione0.8–0.13.x i1.0tylko wersje; przyszła zgodność wstecz od1.0, bez dzisiejszej implementacji migracji. Merge heksów i import osadzonych zasobów do Library dopiero po1.0. Żadnego backlogu nie zrealizowano przy okazji.

### Metody/render/kontrolki — bieżące próby

Każdy runner wymaga jednego Case. Osłona bez Case sprawdzona raz: błąd użycia, bez budowania/uruchamiania prób.

- `eraser-profile` baseline `105b7533…`: wskazane pomiary0.6.4. Późniejszy profile`25fb1d57…` mierzył tylko enqueue i zamknął formę; nie jest dowodem ukończenia async.
- `eraser-async` **PASS**, `patch-070-eraser-async-79adccb9c2dd4409bd7d3c4954302b0b`: realna pętla komunikatów, dokładny sukces/niezmieniony oryginał, jedno Undo/Redo, map11roundtrip, blokada save/history, cancel, inny dokument/rewizja i błąd walidacji bez częściowej zmiany. Timingi błędnych faz początkowo pokazywały poprzedni sukces; zresetowano diagnostykę, nie używamy ich jako pomiarów błędu.
- `camera` baseline`e9439517…`; końcowe **PASS** `patch-070-camera-3f2f196167394ac0a11d62e0359927fb`: dane/tabela powyżej, obraz pan i grube/rzadsze łączenia. Nieudane wersjeBitBlt i pierwszy pinhole zachowane jako diagnostyka, nie zaliczone wyniki.
- `quality` **PASS** `patch-070-quality-b3cde1fcffd44001ad7fff2c1a66434b`: alpha, spatialmean, ograniczenie256, cacheLow/High, dane/dirty/historia, nowy gestLow/faza, map11, reset po sukcesie i brak resetu po błędzie. Wcześniejsze dodatkowe przycięcie maską zaniżało alpha próbkowania; usunięte przed PASS.
- `picker` **PASS** po CheckedBackColor: `patch-070-picker-7623eba1235e4c5aafbf4490223066f0`: wspólna12/duplikaty/move/cancel/gradient/aktywna próbka/2×6/empty/styl. Wstępna próba nie klikała niewidocznego formularza; test poprawiono przez Show.
- `start` **PASS** `patch-070-start-de5623ed16c44fbd81d7d85904354661`:30/5, missing/restore, case, restart/corrupt, wspólny odczytmap11/argumentbezplaceholdera i reentrancja.
- `integration` **PASS** `patch-070-integration-d2b759853be544128b6fecfb0f5c71ab`: faktycznie różne czerwony/niebieski kadry, odrzucenie nieobsługiwanej bardzo długiej ścieżki pozostawia czytelną historię, obie maski nadal logiczne wLow, mozaika kafelka ta sama.
- `fixture` `c878ec0a…`: generator własnej mapy/plamy/źródła, nie test funkcjonalny. Pełne ID i chronologia w planie.

### Zwykły EXE, oddzielnie od metod

Wszystkie kopie uruchomione z własnego `patch-070-fixture-c878ec0a20474ebb92d6523b590a050d`; hash root/kopii zgodny przy każdej podmianie. Nie testowano prywatnych map/Library i nie zamykano okien użytkownika.

1. Kandydat99A43FE2…: ekran startowy, Nowy→Anuluj, Otwórz→Escape, MRU→własna mapa. High/Low: tło, średnie heksy/mozaika/pierwszy kolor kresek, powrótHigh bez gwiazdki. Po zgodzie at-action gumka50przez plamę `(445,275)→(1080,800)`, jedno Undo/Redo i przezroczysty pas. Następna kreska, zoom146→130%, pan mapyRączką. OK jednolitego#FFD04A i ponowne otwarcie pokazały próbkę. Zapis/close/inventorypuste.
2. Po wyłącznie korekcie checked fill,6F872A…: restartMRU i pusta historia kolorów; rzeczywisty Nowy ze startu; ciemny checked fill/ramka; Tęcza i kreska80wLow `(420,400)→(1200,490)`, Highprzywrócił pełny cykl. Własny zapis. Po nowej zgodzie gumka80 `(800,350)→(800,545)`: otwór, niezmienione kolory boków, Undo. Następna gradientowa kreska, zoom i pan. Zapis/close/inventorypuste.
3. Ostatnia zmiana dotyczyła wyłącznie startowego guardu i przywrócenia RunModal dla Nowa edytora. Dokładny finalny **6CE97CC8…**: restart z2własnymi recent, Anuluj systemowego Otwórz, recent→zapisana mapa/Highzrysunkami/otworem; czyste zamknięcie. Nie ponawiano poprawnych gestów gumki/kamery po niezwiązanej osłonie startu. Są one opisane na odpowiednich kandydatach, bez udawania nowej próby tego samego gestu w ostatnim pliku.

**Niezaliczona** próba błędnego pliku w systemowym OpenFileDialog: helper odrzucił set_value i wskazywał SearchEditBox mimo kliknięcia Nazwa pliku. Nie wpisywano w niepotwierdzony fokus. Anulowano; błąd odczytu/bieżący dokument sprawdzono metodą, nie oknem błędu startu. Podczas niewłaściwego kierowania wejścia do właściciela pojawił się drugi własny dialogNowy; zidentyfikowano/anulowano dodatkowy i dodano guard. Nie uznajemy niepewnego kliknięcia za sukces.

API daje pojedynczy odcinek dragLPM, nie ciągły wieloodcinkowy/PPM. Brak ręcznej wizualnej oceny komety/grubego łuku/okręgów. Brak FPS i potwierdzenia braku flasha klatka po klatce; zrzuty potrafiły złapać animację zamykania systemowego modala. Nie przedstawiamy pomiaru timera lub końcowych bounds jako pełnej płynności. Argument mapy sprawdzony metodą/konstruktorem, nie osobnym ręcznym uruchomieniem z wiersza poleceń. Kontrola pliku boundary.json wykazała, że bardzo długa ścieżka została odrzucona przez .NET przed dojściem do256KiB — ta próba potwierdza bezpieczne niepowodzenie, nie osiągnięcie limitu bajtów. Ochrona wielkości zapisu jest jawnie w kodzie; nie wykonano osobnego testu jej granicy. Brak fizycznego testu odpinania sieci/nośnika, maksymalnych dokumentów i zabicia zasilania przy zapisieMRU; zastosowano ograniczenia i standardowy atomowy zapis. Brak pełnej regresji oraz kampanii przenośności (świadomie później).

## Checklista odbiorcza użytkownika

- [ ] Długi zakrzywiony gest gumki na większej plamie: dyskretny stan przetwarzania, responsywne UI/anulowanie, pełny wynik lub rollback; jedno Undo/Redo, otwór i zachowane kolory boków.
- [ ] Oddalenie → duże kreski → stopniowy zoom i **pan mapy w płótnie** wHigh; porównaj jednolity/gradient, cold/warm i stan po wyczyszczeniu. Bez wymagania stałegoFPS.
- [ ] Grubość80+ i zakręt/łuk przy niecałkowitym zoomie: brak dawnych dysków/pinholi, późniejsza część na wierzchu, bez globalnego wygładzania.
- [ ] High/Low: tło znika/wraca, właściwy średni kadr/alpha/gradient heksa oraz mozaika; solid bez zmiany. Ikony/obiekty i logiczne maski niezależne.
- [ ] Gradientowa kreska narysowana wLow odzyskuje pełny przebieg wHigh; pointerLow ma pierwszy kolor, faza nadal od drogi. Dane/gwiazdka/historia nie zmieniają się samym przełącznikiem.
- [ ] Nowa/Otwórz sukces resetujeHigh; Anuluj/błąd zachowuje bieżący tryb. Edytor/crop/biblioteka zawsze pełna jakość.
- [ ] Wspólna historia12w2×6: OKsolid/move-front/bezduplikatów, gradient/Cancelbez dopisywania, aktywny punkt, puste disabled, tooltipHEX; restart zeruje.
- [ ] Ciemny aktywny Jednolity/Gradient, czytelna ramka i focus; czerwone akcje/disabled zachowane.
- [ ] Start: ikona/wersja, Nowy/Otwórz, Cancel/błądbez zamknięcia; wejście do gotowego edytora bez niedokończonych kontrolek. Osobno oceń przejście w czasie.
- [ ] Do5dostępnych ostatnich spośród30; brakujący pomijany, przywrócony wraca; polskie ścieżki/spacje/case, zapis i restart, uszkodzony plikMRUnieblokuje pracy. Plik może zniknąć między listą a kliknięciem.
- [ ] UruchomienieEXEzargumentemmapy pomija start; błąd daje bezpieczny start. Brak skojarzeń rozszerzeń/instalatora.
- [ ] Dotychczas odłożona końcowa przenośność i testy całości pozostają na później; nie wracać do zamkniętych zgłoszeń grup/WPF/backlogów bez nowej regresji.

## Git i punkt końcowy

Kod/testy/EXE **7bf009e**; dokumentacja w następnym lokalnym commicie, identyfikator w odpowiedzi końcowej. Oczekiwany i sprawdzany końcowy status czysty. Zachowano wcześniejsze commity; brak zastanych zmian na początku. Własne okna/dialogi zamknięte i inventorypuste. Materiały dowodowe pozostają ignorowane w tests/_work, bez szerokiego czyszczenia. Nie deklarujemy kompleksowego przetestowania aplikacji.
