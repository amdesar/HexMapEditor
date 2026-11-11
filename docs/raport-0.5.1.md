# Raport HexMapEditor 0.5.1

## Wynik i baza

Ukończono zakres 0.5.1 na bazie `3485ef9` (zawiera `9c7a295`). Zachowano rozpoczęte zmiany przy wznowieniu; nie powtarzano ukończonych etapów ani historycznych kampanii. Główny `HexMapEditor.exe` ma FileVersion 0.5.1.0, SHA-256 `4FF030486AB5744CB76EA63A322D1005F73F9C6D361FE5E9FC796D62602F2F66`. Brak nowych zależności/usług. C#, WinForms/GDI+ i istniejący WPF pozostają. Nie zmieniano naprawy WPF ani globalnego WS_EX_COMPOSITED.

## Źródła i kadry

Pełne oryginalne bajty nieruchomego PNG/JPEG/BMP/GIF pozostają w GraphicSource. ID treści SHA-256 jest indeksem; deduplikacja sprawdza również długość i bajty. GraphicDefinition przechowuje dwa niezależne środki X/Y w jednostkach źródła, Zoom i Angle oraz Linked. Logiczne ID pozycji biblioteki jest odrębne od tożsamości źródła i kadru. Plik źródłowy zostaje zamknięty po imporcie, nie zachowujemy ścieżki autora.

Dwa podglądy Pointy Top / Flat Top: przeciąganie lewym lub środkowym przyciskiem, zoom kółkiem, suwak 0–360° i reset. Klik podglądu wyznacza aktywny kadr. Połączenie nie wyrównuje wcześniejszych ustawień; stosuje względny obrót, wspólny mnożnik skali i deltę znormalizowaną względem obrazu. Granice pokrycia uwzględniają obrót; wspólne powiększenie może być konieczne przy obrocie, a ruch zatrzymuje się na granicy jednego z kadrów. Ponowna edycja otwiera pełne źródło i zapisane transformacje. Przycisk Kadruj przy wariancie działa także z osadzonej mapy bez lokalnej biblioteki. Zmiana orientacji wybiera właściwy kadr, bez samoczynnych 30°.

Biblioteka `Library/Terrains/Graphics/terrain-Nazwa.json` ma format 2, logiczne ID, kolejność i definicję. `Sources/source-SHA256.json` zawiera wspólne niezmienne źródło. Świadoma edycja/usunięcie pozycji korzysta z LibraryFiles i historii, bez niszczenia zastosowanych wariantów dokumentu. Źródła pozostają w magazynie po Undo/usunięciu wpisu do ponownego wykorzystania, bez automatycznego czyszczenia. Limit magazynu obejmuje również takie zachowane źródła. Nie tworzono migracji dawnych wycinków.

Identyczna definicja wymaga bajtowo zgodnego źródła i obu kadrów/Linked. Dialog pokazuje nazwy/miniatury oraz Użyj istniejącej / Importuj jako nową / Anuluj. Nowa pozycja ma niezależne ID i czytelny sufiks, współdzieląc źródło. Inny kadr nie jest identyczną definicją. Nazwa sama nie wykrywa duplikatu. Zatwierdzony import jest operacją biblioteczną, której późniejsze anulowanie edycji terenu nie wycofuje.

## Pule i historia

Teren ma 1–32 warianty kolor/grafika/mieszanka; kafelki, plus, edycja typu i usuwanie poza ostatnim. Pierwszy wariant jest stabilnym pełnym tłem kafelka terenu. Nazwa ma biały pogrubiony tekst na ciemnym podkładzie, skracanie i tooltip z liczbą wariantów. Akcje oraz złoty wybór pozostają.

Random wybiera niezależnie z równym prawdopodobieństwem; brak cyklu i wymuszania równowagi. Zastosowany heks zachowuje ID wybranego wariantu i migawkę kolor/grafika/kadr. Nie losujemy w redraw/zoom/orientacji/odczycie/Redo ani ponownie na odwiedzonym heksie w jednym geście. Wspólny stos zachowuje jeden gest/wypełnienie i limit 300. Edycja puli przyszła, aktualizacja starych pól jest świadoma. Regiony używają logicznego TerrainId, a mgła dodatkowo początkowego Fog. Wyglądy nie rozbijają jednego rodzaju; odrębne ID nie łączą się przez podobny kolor/obraz. Tekst, ikony i obiekty pozostają niezależne.

## Mgła i tło

Prawy klik narzędzia mgły otwiera wspólny edytor wyglądu. Domyślnie szarość. Wariant jest losowany przy rzeczywistym nałożeniu i zapamiętany. Nieprzezroczysta baza pod grafiką zapobiega ujawnianiu przez alpha. Domyślnie wyłączone Zastosuj wygląd do obecnie zasłoniętych heksów zmienia tylko wygląd, jednym Undo z dokładnym wynikiem losowania. Ochrona aktywnych odkrywców i podgląd pozostają.

Tło na górnym pasku: pełne źródło, prostokątne kadrowanie myszą, krycie 0–100%, nasycenie 0–200%, rozmycie 0–24 px, Zatwierdź/Anuluj/usunięcie. Proporcjonalne cover z zapisanym kadrem, bez deformacji; kamera go nie przesuwa/skaluje. Rozmiar płótna odbudowuje bufor, Dopasuj nie uwzględnia tła. Efekty są pochodne i nie zmieniają źródła. Edycja ma jeden Undo, Widok → Tło jest chwilową flagą bez gwiazdki. Nie dodano biblioteki teł.

## Formaty i limity

Mapa **8**, preset **3**, grafika biblioteczna **2**. Osobne tabele pełnych GraphicSources i definicji TerrainAssets; odwołania z palet, heksów, mgły i tła, ze źródłem zapisanym raz. Cache i miniatury nie są obowiązkowym dublowanym zapisem. Zachowujemy źródła palet projektu, również jeszcze nienamalowanych pozycji. Nie dołączamy całej biblioteki. Ikony/obiekty nadal mają dotychczasowe osadzone zasoby i metadane.

Nowe formaty wymagają pełnych źródeł i danych puli. Brak konwersji/odczytu dawnych map/presetów/wycinków jest świadomy i zgodny ze zleceniem; stare pliki nie są usuwane. Walidacja wymaga jawnych pól, sprawdza referencje, źródła i skończone transformacje przed podmianą otwartego dokumentu. Bezpieczny zapis pozostaje.

Limity: plik źródła 16 MiB i 4096×4096; 64 źródła /64 MiB bajtów łącznie; 256 definicji, 128 aktywnych wpisów biblioteki, 32 warianty/pulę; mapa do 128 MiB, preset do 112 MiB. Po przekroczeniu błąd bez uszkodzenia dokumentu. Cache LRU do 256 wyników 256×256 (64 MiB), osobny klucz orientacji. Tło do 2048 w większym wymiarze bufora. Nie przetwarzamy pełnego obrazu dla każdego heksa w każdej klatce. Budżet nie oznacza nieograniczonej liczby importów; Sources zachowuje nieaktywne źródła.

## Faktyczne testy metod/kontrolek

Każdy runner dostał jeden Case; brak Case sprawdzono raz — błąd użycia przed kompilacją/utworzeniem artefaktów, bez uruchamiania innych prób. Runner 050 również wymaga Case. Nie uruchamiano Run-AllTests ani równoważnej kampanii.

- **crop PASS** — oryginalne bajty, niezależne kadry 90/0, połączenie bez resetu i względny obrót/zoom/ruch, przejście 0°, serializacja, ponowny kadr z pełnego źródła i pokrycie po obrocie. `patch-051-crop-836fc11b18144fe2aa8d491b2984adfb`.
- **pool PASS** — kontrolowany generator 0,0,1,2, mieszana pula, dozwolone powtórzenia, brak ponownego losowania odwiedzonych pól, dokładne Undo/Redo i zachowany tekst, region across wyglądy oraz osadzenie. `patch-051-pool-c3c23a8eeeb8484b9ff53f973c5012d8`.
- **fog PASS** — alpha z nieprzezroczystą bazą, chroniony sojusznik radius0, bucket i dokładne Undo/Redo, przyszła pula i jawna aktualizacja. `patch-051-fog-1afb8c89bfab42c8b0168cfb5b517b36`.
- **background PASS** — crop, nasycenie0, blur/opacity, te same piksele i bufor przy pan/zoom, Undo/Redo oraz flaga bez dirty. `patch-051-background-71d569a95e73483888ca8f468fc4cd4e`.
- **library PASS** — rzeczywista ścieżka trzech decyzji duplikatu z wstrzykniętą decyzją i własnym katalogiem, inny kadr, niezależne ID/jedno źródło, rename i dokładne bajty Undo/Redo, konflikt ścieżki i retry, reload kolejności. Po zaostrzeniu schematu: `patch-051-library-efc0f04b3ba149c5ba9830713a5226cb`.
- **portable PASS** — jedno źródło dla dwóch definicji, palety/heksy/mgła/tło, mapa/preset bez źródła i biblioteki, oba kadry; uszkodzony plik nie podmienia otwartego dokumentu. Po schemacie: `patch-051-portable-af84b712d0fe4be596a376bcf1b6daf6`.
- **controls PASS** — po błędzie znalezionym w UI: wywołanie rzeczywistej metody ruchu narożnika i render poprawnej miniatury Flat. `patch-051-controls-1bd93f3d6e5048f391ced97ac6e358d9`.

Katalogi wyników są w tests/_work; powyższych wcześniejszych wyników nie uruchamiano ponownie tylko z powodu wznowienia. Historyczne testy zachowano. Przypadki dawnych formatów nie były uruchamiane ani przedstawiane jako zgodne z nowym modelem.

## Faktyczne próby zwykłego EXE

Wyłącznie zwykłe kopie aplikacji w `tests/_work/ui-051`, własna mapa `Proba051.hexmap` 8×6 i własny obraz pasów. Prywatna Library nietknięta. Próby funkcji były wykonywane podczas rozwoju z roboczym oznaczeniem 0.5.0; po samym podniesieniu wersji uruchomiono identyczną bajtowo kopię finalnego głównego EXE 0.5.1 i zamknięto ją. Nie przedstawiamy całej sekwencji jako ponowionej na finalnym numerze.

Sprawdzono: dodanie koloru do puli dwóch grafik, jeden gest z różnymi wyglądami i zapis/ponowny odczyt; pełny kafelek nazwy; ponowne otwarcie kadrowania osadzonego wariantu po odczycie. W dwóch podglądach rozłączono, obrócono Pointy do 91° (Flat 0°), połączono bez resetu, następnie Pointy181°/Flat90°. Kółko i przeciąganie stosowały wspólne względne zmiany. Zatwierdzono wariant i pulę. Sprawdzono mieszaną pulę mgły oraz nakładanie jednym przeciągnięciem.

UI wykryło wysunięte przyciski edytora puli — nadano początkową szerokość header/footer przed kotwiczeniem, ponowne otwarcie miało komplet widocznych kontrolek. Wykryto też miniaturę Flat dekodowaną jako Pointy; poprawiono wybór orientacji i sprawdzono kontrolką. Ruch narożnika tła ujawnił sumę left+width minimalnie >1 przez odejmowanie float. Zmieniono odejmowanie na double; nowy celowany test i powtórzenie tego narożnika w EXE przeszły bez wyjątku. W oknie sprawdzono nasycenie0, blur6, zatwierdzenie, pan i zoom z nieruchomym tłem. Dane zapisano, własne okna zamknięto.

## Ograniczenia i niewykonane próby

Nie przeprowadzono pełnego zestawu, kampanii DPI, Worda, dawnych funkcji WPF ani benchmarków dużych map/filtrów. Nie wykonano ręcznie wszystkich trzech decyzji duplikatu, wszystkich limitów importu, usuwania grafiki, wszystkich formatów obrazów i UI Undo każdej kombinacji. Te podstawowe ścieżki danych mają wyżej wymienione celowane testy metod, co nie jest pełnym testem okien. Nie oceniano płynności klatka po klatce. Buforowanie sprawdzono metodą i krótką obserwacją, bez obietnicy określonej wydajności.

Nie dodano wag, high/low detail, światła, widoku gracza, zakładek, eksportu zasobów ani ogólnego edytora obrazów. Biblioteczny Sources zachowuje nieaktywne źródła i może osiągnąć limit; nie ma automatycznego usuwania prywatnych danych. Stare małe wycinki nie zapewniają pełnego oryginału i nie są konwertowane.

## Checklista użytkownika

1. Własny preset: dodaj kolor i grafikę, sprawdź pełne kafelki i różne wyniki pędzla/wiadra; Undo/Redo powinno odtworzyć dokładnie wybór.
2. Rozłącz kadry, obróć jeden, połącz bez resetu, zmień zoom/obrót; zapisz mapę/preset, otwórz i ponownie kadruj pełne źródło.
3. Powtórz import: Użyj istniejącej / Importuj jako nową / Anuluj. Nowy ma niezależną edycję.
4. Prawy klik mgły: mieszana pula, opcjonalna aktualizacja starych heksów; alpha nie ujawnia terenu, odkrywca zachowuje ochronę.
5. Tło: przytnij myszą, krycie/nasycenie/blur; pan/zoom mapy go nie przesuwa. Widok → Tło nie zmienia zapisu.
6. Przenieś samą nową mapę lub preset do czystej kopii; grafiki i oba kadry mają działać. Zachowaj stare pliki dla odpowiedniej starszej aplikacji.

## Git i artefakty

Wszystkie dane prób i EXE testowe są ignorowane w tests/_work, bez prywatnych plików w commicie. Źródła testów są śledzone. Główny EXE, README, specyfikacja, plan i ten raport obejmują 0.5.1. Końcowy commit implementacyjny wskazuje Git/końcowa odpowiedź (bez samoodwołania hasha w zawartości commita). Nie wykonywano push, tagu ani Release'a. Stan po commicie podano w końcowej odpowiedzi.

Commit implementacji i EXE: `47ed57b6c49232dd3b758a67a54aa02ca32861e0`. Po nim `git status --short` był pusty, master o jeden commit przed lokalną referencją origin/master. Późniejszy commit dokumentacji tylko zapisuje ten wynik; jego hash podano w końcowej odpowiedzi. Nie pobierano nowych zależności i nie wysyłano historii.
