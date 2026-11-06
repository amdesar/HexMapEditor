# Raport HexMapEditor 0.3.0

Data: 6 listopada 2026. Baza: ukończona 0.2.3, czysty `master` na `34956edaa00b820cc7f9ff8ea12b97dfd94d8c75`; origin `https://github.com/amdesar/HexMapEditor.git`. Początkowo HEAD i lokalna referencja origin/master były zgodne. Nie pobierano nowych referencji w tym zadaniu i nie wykonywano synchronizacji. Nie znaleziono dodatkowego AGENTS.md w sprawdzonym repozytorium/ścieżce nadrzędnej. Przeczytano README, specyfikację i raport 0.2.3. Zachowano istniejącą aplikację oraz poprawki 0.2.x; bez push, tagu i Release'a.

## Wykonane funkcje

- Pięć sekcji lewego panelu z nagłówkami/separatorami. Tereny przewijają się we własnym elastycznym obszarze. Ikony mają podgląd do 16 pozycji w czterech rzędach i pełny selektor. Obiekty pozostają wyłącznie małym placeholderem.
- Treść panelu 220 px i nowy dołączony pasek 18 px po prawej. Płótno zaczyna się za całym zespołem; Dopasuj, hit-testing i przeliczenie odsłanianego pasa prawej nakładki używają właściwych współrzędnych. Zwinięcie nie zmienia dokumentu/historii; kompensacja początku płótna zachowuje położenie mapy na ekranie.
- Cztery ikonowe akcje presetów w jednym rzędzie, refresh przy selektorze. Zapis `Kosmos.json`, bez `.terrainset`, z dotychczasowymi ID, walidacją, kolizjami i świadomym przemianowaniem. Bez nowych migracji, skanowania starego katalogu ani kopii migracyjnych. Skan *.json jest ogólny i nierekurencyjny; nie ma osobnego trybu starych nazw.
- Oddzielone narzędzie i materiał. Kliknięcie terenu/ikony wskazuje materiał bez edycji i zmiany narzędzia. Pędzel ma rysunek pędzla, ołówek pozostaje do edycji. Wskazanie materiału i ramki aktualizują wybór.
- Osiem wspólnych kształtów, rozmiar 1–15, drugi wymiar prostokąta, Wypełnienie/obrys i podgląd zasięgu. Działają na terenach, ikonach i mgle; istniejące Wiadro ignoruje ustawienia odcisku.
- Biblioteka ikon z własnymi pięcioma sylwetkami, importem bitmap, miniaturami, pełnym wyborem i potwierdzanym usunięciem do Kosza. Jedna duża proporcjonalna ikona na heks, niezależna od terenu. Usunięcie bibliotecznej kopii nie usuwa zasobu z mapy. Znacznik pierwszej inicjalizacji zapobiega ponownemu dodawaniu usuniętych startowych ikon.
- Istniejące Wiadro rozszerzone o ikony: region wariantu terenu; tryb dodaj/zastąp albo usuń wybraną ikonę ustalony na starcie.
- Dwa narzędzia mgły: wspólny odcisk oraz wypełnienie regionu koloru terenu. Nieprzezroczyste zasłonięcie po narysowaniu danych, przed roboczą siatką. Nazwa/opis/teren w szczegółach są maskowane, edycja i historia tekstowa ukrytego heksa zablokowane; realna treść pozostaje.
- Wspólne Undo/Redo mapy/palet/ikon/mgły/rozmiaru. Jeden gest/region to jedna operacja; odwiedzone pola nie przełączają się ponownie. Zmiany warstw przywracają tylko właściwe pola i nie cofają tekstu. Crop zachowuje ID/dane, ostrzega także o ikonach/mgle i liczy rozłączne kombinacje raz.
- Format mapy **3** z osadzonymi użytymi ikonami. Walidacja przed podmianą dokumentu; zapisuje się użyte zasoby, a nie całą bibliotekę. Nowa/Otwórz zerują dokumentowe historie, wybór materiału ikonowego i cache zasobów. Bez nowych zależności lub usług.

## Kształty, regiony i zasoby

Punkt: jeden heks. Heksagon N: środek i N−1 pełnych pierścieni (N=3: 19 pól, obrys 12). Kwadrat/prostokąt wybierają centra w prostokącie; koło i foremny pięciokąt centra we właściwej figurze. Jednostka wymiaru to około 48,5 jednostki bazowej mapy, niezależnie od zoomu (promień heksa 28). Prostokąt ma osobną wysokość. Pięciokąt jest zwrócony wierzchołkiem w górę. Linie są poziome/pionowe względem ekranu, próbkowane po najbliższych centrach, długość (N−1) jednostek. Małe figury mogą dawać jeden heks. Obrys to członkowie mający sąsiada poza figurą, wyliczony przed przycięciem do mapy. To dyskretna aproksymacja, nie idealna figura euklidesowa.

Wiadra współdzielą istniejącą kolejkę i sześciu sąsiadów, bez rekurencji. Tereny/ikony porównują wariant terenu, mgła jego kolor pod spodem. Ikony i mgła nie dzielą regionu. Pole startowe określa tryb na cały gest/region. W usuwaniu ikon kasowana jest tylko wybrana ikona, inne pozostają. Teksty i teren nie są nadpisywane operacją warstwy.

IconAssets zawiera ID `icon-` + SHA-256 bajtów PNG, nazwę, wymiary i base64. Heks ma IconId oraz Fog. Ten sam zasób jest współdzielony przez wystąpienia; brak zewnętrznych ścieżek. Import nieruchomych PNG/JPG/BMP/GIF: do 8 MiB, 2048×2048, normalizacja do maksymalnie 512×512. Limity dokumentu/sesji: 128 zasobów, 2 MiB każdy i około 32 MiB PNG łącznie (44 MiB base64), mapa do 128 MiB. Nieużywane zasoby potrzebne Undo są zachowane w pamięci, pomijane w pliku; przy dojściu do limitu zapis/ponowne otwarcie zwalniają nieużywane. Kod zwalnia bitmapy i uchwyty źródła.

**Odczyt wyłącznie formatu 3**, bez konwersji map 1/2. Starsze i uszkodzone pliki pozostają na dysku bez zmian; nie zastępują otwartego projektu. Treść presetu nadal format 1, zmieniła się konwencja nazwy pliku. Zachowano bezpieczny zapis tymczasowy, Flush(true), atomowe zastąpienie i potwierdzenie końcowej ścieżki. Nie odzyskiwano usuniętej Library ani kopii migracji użytkownika.

## Wykonane testy metod/kontrolek — selektywnie

Nowy runner `tests/Run-FogIconTests.ps1` z filtrami `-Case`; nie wywołuje zbiorczego zestawu. Sprawdzono dziewięć grup konkretnych przypadków, wszystkie zakończone PASS i kodem 0:

1. Osiem deterministycznych geometrii przy rozmiarach 1/3 i wypełnieniu/obrysie, obie orientacje; pełne pierścienie i kierunki linii względem ekranu.
2. Nakładanie odcisków ikon, stały tryb w geście, jeden wpis Undo/Redo, zachowanie terenu/tekstu, brak edycji ikon przez ogon dwukliku dialogu.
3. Mgła przez gest: brak ponownego przełączania, jeden wpis historii, natychmiastowe maskowanie wcześniej otwartego opisu z tajną treścią, wyłączone pola, zachowanie rzeczywistych danych i Undo/Redo.
4. Ikonowe Wiadro na czterech spójnych polach małej mapy i usuwanie; mgła na tym regionie według koloru mimo innego wariantu jednego pola, bez uszkadzania ikon i opisu, Undo.
5. Import własnego PNG 40×20 bez blokady źródła, usunięcie źródła, zapis dwóch wystąpień jako jednego użytego zasobu z pominięciem nieużywanego, odczyt, zachowanie proporcji; uszkodzony zasób i format 2 odrzucone bez podmiany mapy. Potwierdzono zerowanie historii/zasobów przy Nowa/Otwórz.
6. Przycięcie pola z ikoną i mgłą: jedno liczenie, grupa Ikona+Mgła, Undo rozmiaru odtwarza ID/warstwy; starsze Undo ikony nie zmienia niezależnej mgły.
7. Renderowanie do bitmapy: piksele wnętrza są nieprzezroczystą szarością, bez prześwitywania czerwonego terenu, ikony i etykiety; przed mgłą widoczny jest jasny środek ikony.
8. Minimalny rozmiar formularza 1180×680 z dodatkowymi terenami: stałe sekcje mieszczą się, tylko terenowa lista przewija, ikony nie przesuwają się, płótno zaczyna się za 238/18 px, Dopasuj mieści granice, zwinięcie nie zmienia danych/historii.
9. Preset Kosmos.json, polska nazwa po zmianie, jeden aktywny plik/stałe ID i czytelny sufiks przy kolizji nazw po normalizacji.

Najpierw uruchomiono siedem nowych grup, następnie osobne renderowanie. Pojedyncze kontrole geometrii/renderowania powtórzono po zmianie algorytmu linii i wzmocnieniu bezpośredniej asercji; układ po kompensacji początku płótna oraz zwiększeniu miejsca na cztery rzędy ikon; import/odczyt po korekcie trybu dekodowania i dodaniu kontroli resetu. Nie powtarzano dawnych zestawów. Dziewięć to liczba różnych grup, nie liczba wszystkich wywołań runnera. Logi `tests/_work/fog-icons-*/results.log` pozostają lokalne, ignorowane; każdy przebieg sprząta własne EXE/dane/bibliotekę.

## Rzeczywiste próby zwykłego EXE

Computer Use, zwykła kopia głównego EXE w `tests/_work/ui-030-3487a0c08b524b05ad7f8dd688dccd33`, bez diagnostycznej kompilacji i prywatnej Library. SHA-256 badanej kopii i głównego EXE w chwili prób: `0F7907761399A30453212CB969F89A9626C5B90063616F09457FFAEE214A940F`. Po próbach kod działania pozostał taki sam; końcowy rebuild obejmuje również poprawkę komentarza i dokumentację/testy.

- Start: pięć sekcji, boczny pasek, ikonowe akcje presetów, pięć różnych startowych sylwetek.
- Import własnego przezroczystego PNG 96×48 przez systemowy dialog; proporcjonalna miniatura, wybór materiału bez gwiazdki i zmiany narzędzia.
- Pędzel ikon: przeciągnięcie C3→D3, duże proporcjonalne symbole; jedno Ctrl+Z usunęło oba i gwiazdkę, Ctrl+Y odtworzyło oba.
- Pędzel mgły na C3/D3: oba zasłonięte; jedno Undo odsłoniło obie ikony, Redo zasłoniło.
- Zaznaczenie zasłoniętego C3: ???, komunikat ukrycia, nieaktywna nazwa i opis zamiast starej treści.
- Zwinięcie/rozwinięcie: mapa zachowała położenie na ekranie, pasek pozostawał osiągalny; Dopasuj wykorzystało większe płótno (146%→171%). Samo rozwinięcie nie uruchamia ponownego Dopasuj.
- Zapis Warstwy.hexmap przez systemowy dialog. Kontrola pliku: format 3, jeden zasób i dwa wystąpienia z mgłą; poprawna nazwa projektu i brak gwiazdki.
- Zamknięcie programu, usunięcie wyłącznie własnego źródłowego PNG i jego pliku z własnej testowej biblioteki. Restart zwykłego EXE, otwarcie mapy dwuklikiem, bez gwiazdki/aktywnego Undo. Odkrycie C3 ujawniło prawidłową ikonę z mapy mimo braku źródła/kopii bibliotecznej; Undo przywróciło mgłę i stan zapisu.
- Pełna biblioteka otworzyła selektor; wybór Gwiazdy zmienił materiał bez zmiany mapy/narzędzia. Zakończono procesy przed sprzątaniem.

## Ograniczenia i niewykonane próby

Nie uruchomiono Run-AllTests ani równoważnej kampanii, dawnych zestawów regresji, Worda, kampanii formatowania/tabel, większego DPI ani dawnej diagnostyki prawego panelu. Historyczne wyniki 0.2.x nie są wynikami tego zadania. Stare testy map 1/2 i migracji/rozszerzeń pozostały w Git; ich oczekiwania zgodności nie obowiązują formatu 3 (opis w tests/README.md).

Próba ręcznego zmniejszenia okna przez Computer Use nie zmieniła faktycznie rozmiaru, więc **nie zaliczono interaktywnego minimum ani interaktywnego przewijania długiej listy terenów**. Minimum i izolację przewijania potwierdza test formularza/kontrolek, nie ręczny gest użytkownika. Wiadra ikon/mgły, przycinanie nowych warstw, komplet kształtów i obie orientacje sprawdzono metodami, bez pełnej interaktywnej macierzy. Kosza ikon nie zatwierdzano interaktywnie; użyto istniejącego mechanizmu RecycleFile i nie deklarujemy nowej próby jego działania. Nie wykonano testów na innym komputerze, pełnej sesji 200×200, dużego importu wszystkich formatów/granic limitów ani symulacji awarii zasilania. Nie jest to wersja kompleksowo przetestowana.

Odciski nakładają się dla odebranych zdarzeń myszy, bez interpolacji między odległymi zdarzeniami; bardzo szybki ruch może zostawić odstępy. Figury są przybliżeniem na dyskretnej siatce. Maksymalny rozmiar i duże mapy nie przeszły kampanii wydajnościowej. Budżet ikon sesji obejmuje zasoby dla Undo. Mgła nie szyfruje danych pliku. Nie poprawiano znanego przeskoku prawego nagłówka ani edytora opisów (w tym stanów formatowania/pogrubienia bez zaznaczenia). Wszystkie funkcje odłożone są wyszczególnione w sekcji 24 specyfikacji; Obiekty pozostają świadomym placeholderem.

## Pliki i stan pracy

Nowe moduły: BrushGeometry.cs (odciski), IconAssets.cs (zasoby/import/biblioteka), IconEditing.cs (materiał/warstwy/historia/renderowanie), LeftSections.cs (sekcje). Zmiany dotychczasowych plików: HexMapEditor.cs, MapModel.cs, EditorOperations.cs, EditorShell.cs, PanelSizing.cs, PresetFiles.cs, PresetOperations.cs, RemovalGroups.cs i AssemblyInfo.cs. Zbudowano główny HexMapEditor.exe 0.3.0.0. Końcowy build zakończył się kodem 0; SHA-256: C8F9F7ACED714C3CDE3E6D5300A83C183EBA81E4F53E4BB6BBD92E0C8B190E60. Testy i instrukcje: FogIconTests.cs, Run-FogIconTests.ps1, Create-LayersUi.ps1, tests/README.md. Dokumentacja: README, specyfikacja, plan i ten raport.

Wszystkie dane prób znajdują się wyłącznie w ignorowanym tests/_work. Prywatna Library nie była materiałem testowym, nie wykonywano migracji ani odzyskiwania. Po potwierdzeniu zamknięcia okna usunięto wyłącznie własną testową kopię EXE, mapę i Library z ui-030-3487a0c08b524b05ad7f8dd688dccd33; pozostawiono krótki results.log. git check-ignore potwierdził reguły, git ls-files nie wykazał Library/_work/bin. Źródła/skrypty i główny EXE są śledzone; Library, _work i główny bin ignorowane. Końcowy identyfikator lokalnego commita i wynik git status są podane w podsumowaniu po zatwierdzeniu (sam raport wchodzi w ten commit); można je sprawdzić przez `git log -1 --oneline` i `git status --short`. Bez push, tagu i Release'a.

## Krótka checklista użytkownika

1. Uruchom główny HexMapEditor.exe. W minimalnym oknie przewiń długą listę terenów: inne sekcje i Dodaj pozostają dostępne. Zwiń/rozwiń bocznym paskiem i użyj Dopasuj.
2. Wybierz Pędzel, teren, potem ikonę: narzędzie pozostaje wybrane. Sprawdź osiem kształtów, prostokąt z drugim wymiarem, obrys, linie i obie orientacje.
3. Przeciągnij ikonę przez kilka pól z nakładającymi się odciskami; jedno Undo/Redo obejmuje gest. Powtórz tę samą ikoną od pola z ikoną — usuwa wybrany symbol. Zmiana terenu zachowuje ikonę i tekst.
4. Na małym obszarze jednego terenu sprawdź Wiadro ikon oraz Wypełnienie mgłą; mgła obejmuje ten sam kolor także przy różnych wariantach terenu.
5. Maluj/odkrywaj mgłę jednym gestem. Sprawdź ukrycie wcześniej otwartej nazwy i opisu oraz przywrócenie danych po odkryciu/Undo.
6. Zaimportuj własny PNG, wybierz z miniatur i pełnej listy. Zapisz mapę i otwórz sam plik przy braku źródła/bibliotecznej kopii. Sprawdź też anulowanie importu i usuwanie własnej ikony z biblioteki bez utraty mapowego symbolu.
7. Przytnij brzeg z ikoną/mgłą/tekstem: liczby i pełne współrzędne bez dubli, anulowanie bez zmian, potwierdzona operacja i Undo/Redo odtwarzają dane.
8. Zapisz własny preset jako czytelny .json, zmień roboczo nazwę i zapisz świadomie. Odśwież nie resetuje palety, a Undo nie zmienia plików. Stare mapy otwieraj nadal odpowiednią starszą wersją programu.