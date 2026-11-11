# HexMapEditor 0.5.3

Zakończona lokalna poprawka na bazie `ea067f0` (0.5.2). Zastano czysty master, zgodny z lokalnym origin/master. Zachowano wcześniejsze commity; bez push, tagu, Release'a, migracji i dostępu testów do prywatnej Library. Wersja aplikacji 0.5.3, FileVersion 0.5.3.0.

## Przyczyny i naprawy

**Regiony.** `RegionState` wpisywał TerrainId w pole Variant; Flood tracił zastosowaną wersję. Dlatego nowsza niebieska przegroda tej samej definicji nie zatrzymywała wiadra na dawnym różowym terenie. Migawka regionu przechowuje teraz TerrainId i VariantId, a Flood wymaga obu. Wiadro mgły dodaje początkowy Fog. Nie porównujemy pikseli, samego koloru ani nazwy. Losowane pozycje jednej wersji pozostają jednym rodzajem regionu. Dwa odrębne tereny nie łączą się przez wygląd. Dotychczasowe iteracyjne sąsiedztwo i migawka początku gestu pozostają.

**Wersja i pozycja puli.** Wykorzystano istniejące TerrainId, VariantId/Revision oraz FillVariant.Id/FillVariantId. Sama nazwa i przestawienie kolejności nie nadają nowego VariantId; zmiana zawartości puli tak. Weryfikacja zgodności wersji przy odczycie nie wymaga identycznej historycznej nazwy.

`Zachowaj rozmieszczenie wariantów` jest domyślnie zaznaczone i zależne od `Zastosuj do bieżącej mapy`. Aktualizacja nadal dotyczy wyłącznie edytowanego TerrainId+VariantId. Dopasowanie po stabilnym ID pozycji zastępuje jej wygląd bez losowania, również kolor↔grafika i po zmianie kolejności. Nowa pozycja nie zajmuje starych pól. Usunięta pozostawia na nich dawną migawkę, wersję i zasoby, nie wraca do edytowalnej puli. Bez zachowania układu operacja jawnie losuje z aktualnej puli. Undo/Redo przechowuje dokładne delty, nie losuje w Redo. Analogiczna opcja mgły zmienia wyłącznie wygląd już zasłoniętych heksów. Nie zmieniano mechaniki ochrony odkrywców.

Identyczne grafiki pozostają odrębnymi pozycjami z osobnymi ID i równym prawdopodobieństwem. Współdzielenie źródła nie scala losów.

**Lagi.** Krótki pomiar wykazał wielokrotne dekodowanie Base64/porównywanie bajtów tego samego niezmiennego źródła w budżecie oraz ponowną generację miniatur identycznej grafiki. Źródła współdzielą niezmienny obiekt; porównanie referencji jest szybkie, natomiast różne obiekty nadal porównują długość/bajty po indeksie SHA-256. Miniatury puli są buforowane w dialogu i zwalniane po jego zamknięciu. Snapshoty i walidacja odczytu pozostały.

Oba podglądy kadru rysowały pełną rozdzielczość metodą HighQualityBicubic. W tej próbie nie odtworzono osobnej, specyficznej dla prawego widoku przyczyny — koszty obu były podobne. Teraz źródło podglądu ma maksymalnie 1024 px i jest dekodowane raz, a rysowanie jest biliniowe. Matematyka przesunięcia nadal odnosi się do pełnego źródła. Transformacje, pokrycie i względne łączenie pozostają. Invalidate scala odrysowania do aktualnego stanu; brak pracowników asynchronicznych i kolejki nieaktualnych bitmap. Wyniki mapy/zapisu nadal powstają z pełnych danych.

**Wejście po dialogu.** Wcześniejsze warunki świeżego MouseDown i zwolnienia myszy nie rozróżniały dodatkowego Down, który został zakolejkowany podczas pracy po zamknięciu dialogu. Dodano watermark czasu zakończenia operacji modalnej i edycji. Filtr własnego płótna odrzuca starsze Down/double-click oraz drugi Down w systemowym czasie i prostokącie dwukliku kończącego przycisku puli. Reszta istniejącej ochrony gestów/przechwycenia pozostaje. Nie dodano globalnej zwłoki, timera ani Application.DoEvents. Nie twierdzimy, że odtworzono dokładnie wszystkie warunki opóźnienia obserwowane na komputerze użytkownika; sprawdzono konkretną ścieżkę dwukliku i brak aktualizacji heksów.

**UI.** Kafelki mają pogrubioną nazwę w jednym białym/czarnym kolorze, z przeciwnym obrysem i bez ciemnego pasa. Wybór koloru opiera się na buforowanej jasności koloru albo 32 próbkach środkowego pasma miniatury; nie jest liczony co Paint. Zachowano wysokość, cover, złoty wybór, skracanie i akcje.

Edytor tła dodaje cztery środkowe uchwyty do narożników, z kursorami i minimum kadru 2%. Środkowy uchwyt zmienia tylko właściwą granicę. Podgląd jest podpisany i ma proporcje canvas.ClientSize, tę samą logikę cover/centrowania/efektów w mniejszej rozdzielczości. Źródło podglądu i wynik filtrów są buforowane; samo krycie nie przelicza filtrów. Jedno oczekujące BeginInvoke pokazuje najnowsze ustawienia, bez timera i stosu klatek. Nasycenie ma reset 100%. Nie zmieniono właściwego cover, kamery ani obszaru tła na mapie. Widok: Tło, Tereny, Ikony, Obiekty, Mgła.

Komunikat nadpisania presetu mówi teraz o sesyjnym Undo/Redo plików, limicie 300 i ochronie konfliktów, bez obietnicy historii po restarcie. Kontrola ID grafik została oddzielona od importu ich zawartości: pominięty nieobsługiwany syntetyczny wpis nie blokuje nowej grafiki. Nie naprawiamy go ani nie osłabiamy walidacji rzeczywiście wybieranego zasobu. Naprawę presetów 0.5.2 zachowano. WPF i ograniczenie WS_EX_COMPOSITED pozostawiono bez zmian.

## Limity i pamięć

Jednostki MiB = 1024² bajtów. Stałe znajdują się w `src/GraphicLimits.cs`.

| Zakres | 0.5.2 → 0.5.3 | Powód / znaczenie |
|---|---|---|
| Pojedyncze pełne źródło terenu/mgły/tła | 16 → 32 MiB | Żądany import; oryginalne bajty pliku |
| Wymiary źródła | 4096×4096 → bez zmian | Bez podnoszenia maksymalnej bitmapy |
| Źródła dokumentu lub presetu | 64 → 96 MiB, nadal 64 szt. | Umiarkowany budżet kilku dużych źródeł, deduplikacja |
| Magazyn źródeł biblioteki, też nieaktywne | 64 → 192 MiB, nadal 64 szt. | Oddzielny budżet dysku, nie stałego RAM katalogu |
| Definicje grafiki/kadrów | 256 → bez zmian | Odrębne od liczby źródeł |
| Aktywne wpisy biblioteki grafik terenów | 128 → bez zmian | Egzekwowane również przed nowym zapisem |
| Pozycje jednej puli | 32 → bez zmian | Także powtórzone grafiki to osobne pozycje |
| Mapa (plik UTF-8) | 128 → 256 MiB | Zapas na Base64 źródeł (96 MiB daje około 128 MiB), heksy, palety i opisy |
| Preset (plik UTF-8) | 112 → 160 MiB | Ten sam budżet źródeł z zapasem serializacji i metadanych |
| Pojedynczy JSON źródła biblioteki | 23 MiB → Base64(32 MiB)+64 KiB | Około 42,73 MiB; opakowanie dopuszczonego źródła |
| Cache wyników kadrów mapy | 256 wyników 256² → bez zmian | Około 64 MiB ARGB, wspólny LRU kluczy orientacji |
| Źródło podglądu kadru/tła | pełne do 4096 → do 1024 px | Około 4 MiB ARGB na bufor; pełne źródło pozostaje w danych |
| Wynik tła na płótnie | do 2048 px → bez zmian | Do około 16 MiB na wynik, plus robocze bufory efektów |

Rozkodowany obraz 4096² ARGB zajmuje około 64 MiB, niezależnie od liczby skompresowanych bajtów. Podczas walidacji/importu dochodzą Base64, bajty i bufory robocze; nie jest to gwarancja działania nieograniczonej liczby obrazów. Zwiększenie budżetu dyskowego nie zwiększa cache. Katalog po pełnej walidacji zwalnia bajty źródeł i zachowuje metadane, a wybrany zasób przypina pełną zweryfikowaną migawkę do dokumentu. Użyte źródła dokumentu oraz bajty potrzebne historii nadal mogą zajmować pamięć. Nie czyścimy nieaktywnych źródeł i prywatnych danych automatycznie. Niezależne dotychczasowe limity importu ikon/obiektów pozostają bez zmian (8 MiB/2048, wynik do 512).

## Formaty i zasoby

Mapa **8**, preset **3**, biblioteczna grafika terenu **2** — bez podniesienia numerów. Istniejące pola już zapisują TerrainId, VariantId/Revision, FillVariantId, migawki, pełne źródła i oba kadry. Naprawiono użycie tych pól. Nie dodano migracji dawnych formatów. Zapis zbiera zasoby potrzebne również historycznym wyglądom i paletom, nie całą bibliotekę. Biblioteka nadal używa czytelnych terrain-Nazwa.json i współdzielonych Sources/source-SHA256.json; ścieżki katalogu źródłowego nie są zależnością mapy.

## Wykonane testy metod i kontrolek

Nowy runner `tests/Run-Patch053Tests.ps1` wymaga jawnego pojedynczego Case. Brak Case sprawdzony raz: błąd użycia przed buildem/próbami. Nie uruchamiano Run-AllTests, dawnych zestawów ani pełnej macierzy. Źródła testów pozostają w Git, wszystkie dane/EXE/logi w ignorowanym tests/_work.

- `regions`: odtworzony FAIL przed naprawą; PASS po — 15×10 różowy teren i pełna niebieska przegroda nowej wersji, region 70 pól.
- `boundaries` PASS: mieszane wyglądy jednej wersji, graficzna przegroda nowszej, osobny TerrainId przy tym samym wyglądzie, początkowy Fog, usunięcie palety bez utraty regionu, bucket Undo/Redo, zapis/odczyt ID i źródła, nazwa i kolejność bez nowej wersji.
- `updates` PASS: ID po zmianie kolejności/dodaniu/usunięciu/zmianie typu, zachowane dawne wersje i usunięte migawki, wyłączone apply, jawny reroll z kontrolowanym Random i dokładne Redo, podstawowa aktualizacja wyglądu mgły/Undo/Redo bez nowych zasłonięć, round-trip. Dwie identyczne grafiki pozostają odrębnymi losami; powrót na pole w jednym geście nie losuje ponownie.
- `local` PASS: metody kontrolek kadru/łączenia, wszystkie cztery środkowe uchwyty tła, proporcja canvas 2:1, reset nasycenia, zależne checkboxy, kontrast jasny/ciemny i stabilny kolor dla grafiki, rysowanie kafelka, znacznik czasu i przestrzenna osłona dwukliku, obce MouseUp/Clicks2 bez edycji oraz pierwszy nowy gest. To testy kontrolek, nie pomiar płynności.
- `safety` PASS: rzeczywisty import BMP dokładnie 32 MiB (własny obraz z dopełnieniem), zapis/odczyt presetu i zgodność bajtów, odrzucenie 32 MiB+1 przed odczytem. Budżety dokument/biblioteka i zapas serializacji sprawdzone metodami, bez produkowania wielkiej biblioteki. Pominięta syntetyczna grafika formatu 1 pozostała niezmieniona, nowy import i Undo/Redo pliku przeszły. Katalog zachowuje leniwe źródło, dokument przypiętą migawkę. Krótki smoke nowego presetu i kopii Domyślnego przeszedł tą samą ścieżką aplikacji co przyciski.

Pierwsza próba pomiaru miała błąd argumentu refleksji; pierwszy test ukrytej kontrolki używał niedziałającego tam PerformClick. Poprawiono wyłącznie harness, właściwe powtórzenia przeszły. Nie przedstawiamy ich jako błędów aplikacji.

### Krótkie pomiary przed/po

Ten sam własny obraz BMP 1024² (~3 MiB), osiem pozycji wskazujących tę samą grafikę, 15×10 pól. Jedno wykonanie przed i po; wyniki lokalne, bez statystyki i gwarancji FPS.

| Operacja metod/kontrolek | Przed | Po |
|---|---:|---:|
| CheckTextureBudget, 150 pól | 9645,08 ms | 2,90 ms |
| UpdateTerrain, 150 pól | 7,71 ms | 6,73 ms |
| ChangeFogAppearance, 150 pól | 10137,89 ms | 7,25 ms |
| Otwarcie puli 8 identycznych grafik | 268,58 ms | 35,26 ms |
| 10 DrawToBitmap Pointy Top | 93,05 ms | 29,65 ms |
| 10 DrawToBitmap Flat Top | 91,45 ms | 27,89 ms |

Pomiar samego UpdateTerrain nie obejmuje osobno zmierzonego CanAccept/CheckTextureBudget. Porównanie mgły obejmuje walidację budżetu wewnątrz metody. To nie pełny czas od kliknięcia do końca Windows Paint. Największy koszt był w powtarzanym badaniu źródła, nie w samym przypisaniu 150 pól. Efekty tła oceniono funkcjonalnie; nie wykonano osobnego benchmarku filtrów.

Logi: patch-053-perf-64e8adbad4574bf78629915d54446597 (przed), patch-053-perf-2417db52f039451ab31f47e92b550aff (po), boundaries-b7766b0c8d8b44caa606d0f4bd946be3, updates-7fd4f6df5fff408e9db10de36bdab430, local-32ae22bb17d148b7a9db8d9cc80d491c, safety-b2f55d8506e646d0b6efe2d58d860388, wszystkie z prefiksem patch-053- w tests/_work. Wcześniejsze zaliczone próby nie były powtarzane bez zależnej zmiany.

## Rzeczywiste próby zwykłego końcowego EXE

Zwykła kopia w `tests/_work/ui-053-146e55a1ff174b8a98897bda610a7233` była identyczna bajtowo z głównym EXE: SHA-256 `2B29C665B8C011475EE40F368FB08A6280FC2132433D39CC34EBF1137EE498B7`. Bez kompilacji diagnostycznej i prywatnej biblioteki.

1. Wiadro mieszanej puli w różowym regionie: wypełniło lewych 70 pól, pozostawiło niebieską przegrodę 10 i prawych 70 pól. Rzeczywisty kolorowy wynik widoczny w oknie.
2. W dialogu zmieniono granatowy wariant na #88BBFF, zaznaczono aktualizację przy domyślnym zachowaniu rozmieszczenia. Zapisane mapy przed/po: 29 czerwonych na tych samych miejscach, 41 niebieskich na tych samych miejscach z nowym kolorem, 80 pozostałych nietkniętych; zero rozbieżności.
3. Po wznowieniu nie było poprzedniego okna; wynik ostatniej kopii i porównanie plików ustalono przed dalszą pracą. Otworzono tę samą kopię i własną mapę, bez powtarzania 1–2. Kadry: zoom, drag lewego i prawego widoku, suwak obrotu do 132°, następnie Anuluj. Oba końcowe podglądy odświeżone. Nie ma nagrania klatek podczas drag — nie deklarujemy potwierdzonej stałej płynności na podstawie końcowych zrzutów.
4. Zmieniono nazwę bez aktualizacji mapy, a następnie zatwierdzono dialog dwuklikiem przy aktywnym Pędzlu. Porównanie pełnych payloadów heksów z zapisem po punkcie 2: **0 zmian**. Kolejny świadomy klik naniósł jeden graficzny heks, więc osłona nie blokuje następnego gestu.
5. Tło: środkowy prawy uchwyt zmienił prawą granicę; podgląd zachował proporcje płótna. Nasycenie 19%, reset 100%, rozmycie 8 px, krycie 58%; po zatwierdzeniu końcowy wynik był na płótnie. Sprawdzono kolejność menu Widok. To krótka próba kontrolek i wyników, bez filmowego pomiaru ruchu i bez ponownego audytu zaakceptowanej statyczności tła.
6. Własna mapa została zapisana, własne okno zamknięte, lista okien tego procesu po zamknięciu była pusta. Brak otwartego dialogu lub oczekiwania na zgodę.

## Ograniczenia i niewykonane próby

Nie wykonano pełnych regresji, DPI, dużych map, benchmarków wszystkich źródeł/filtrów ani ponownej kampanii WPF/Worda. Nie diagnozowano naprawionych obszarów niezwiązanych ze zmianą. Ochrona aktywnych odkrywców nie była ponownie sprawdzana interaktywnie; jej ścieżka uzgadniania pozostała bez zmian, nowa próba regionu sprawdza początkowe Fog. Nie wykonano pełnej macierzy błędów I/O ani zajętości każdego pliku. Wybrane granice budżetu sprawdzono syntetycznie, granicę pojedynczego pliku rzeczywistym importem. Brak ciągłego nagrania/FPS ogranicza ocenę płynności, zwłaszcza zgłoszonej różnicy lewo/prawo.

Nie przedstawiamy przenośności jako potwierdzonej ręcznie przez użytkownika. Metody round-trip sprawdzają potrzebne źródła i parametry; dalsze odbiorcze próby poniżej pozostają jawnie niepotwierdzone przez niego. Nie testowano ponownie wszystkich trzech decyzji duplikatu w oknie ani alpha mgły. Artefakty/logi własnych prób zachowano ignorowane w tests/_work; nic z nich nie trafia do commita. Prywatne dane i okna użytkownika pozostawiono nietknięte.

## Checklista użytkownika

### I. Główne poprawki 0.5.3

- Pomaluj różowym, edytuj na niebieski bez aktualizacji, narysuj przegrody: Wiadro wypełnia tylko kliknięty region. Sprawdź też własną grafikę.
- W puli czerwony/niebieski zmień drugi wygląd z apply + zachowaniem rozmieszczenia: pozycje czerwonych pozostają. Dodaj/przestaw/usuń wariant; usunięte zachowują dawne pola. Odznacz zachowanie, jeśli świadomie chcesz przelosować.
- Sprawdź analogiczną opcję wyglądu istniejącej mgły i pozostawanie aktywnego odkrywcy poza zasłonięciem.
- Przesuń/obróć oba kadry; oceń płynność na swoim źródle. Połącz/rozłącz bez resetu.
- Zatwierdź teren bez aktualizacji: brak przypadkowego odcisku, następny świadomy gest działa. Oceń napisy na jasnym/ciemnym/mieszanym tle.
- Tło: środkowe uchwyty, właściwe proporcje podglądu, suwaki i reset 100%. Nowy preset i kopia Domyślnego nadal działają; komunikat nadpisania opisuje historię sesji.

### II. Dotychczas niepotwierdzone ręcznie przez użytkownika

- Zapis/odczyt mapy i presetu z mieszaną pulą, oboma kadrami, mgłą i tłem (tło jest częścią mapy, nie presetu).
- Ponowne kadrowanie pełnego źródła po odczycie mapy.
- Dokładne Undo/Redo rozmieszczenia po aktualizacji.
- Użyj istniejącej / Importuj jako nową / Anuluj oraz niezależność edycji kopii.
- Alpha grafiki mgły bez ujawniania terenu i ikon.
- Przenośność do czystej kopii — **świadomie odłożona ręczna próba**, bez wymagania jej teraz.

## Git i wynik

Główny HexMapEditor.exe zbudowano lokalnym Build.ps1. Commit kodu, EXE i celowanych testów: `b14c6fa1f735828d5e289df96f4e2ce96b1b74b0`. Po nim pozostały wyłącznie README, specyfikacja, plan i ten raport; osobny commit dokumentacji jest końcowym commitem podanym w odpowiedzi. Docelowy stan po nim: czysty master, dwa lokalne commity przed sprawdzonym lokalnym origin/master. Bez push, tagu i Release'a. Backlog UX: gwiazdka presetu, rozdzielenie zapisów mapy/presetu, szersze komunikaty zamykania i automatyczny zapis — bez zmian funkcjonalnych w tej łatce.
