# Raport HexMapEditor 0.6.2

12 listopada 2026. Baza: ukończone0.6.1, HEAD sprawdzony jako `a00807e` (kod `0bcbee2`). Początkowo czysty master; zachowano historię. Bez prywatnej Library/map, migracji, sprzątania, pełnego zestawu, push/tagu/Release'a.

Gotowy główny `HexMapEditor.exe`: aplikacja **0.6.2**, FileVersion **0.6.2.0**. Zwykła kopia użyta do prób:
`tests/_work/patch-062-fixture-c4aa4569b48040449c7898eb3c1520f9/HexMapEditor.exe`.
Oba pliki mają SHA-256 **700360F96794009C1B73BC0C1D577DE3484AF75EA3CF309475F0CDFF9238BA56**, sprawdzony również po zakończeniu UI. Biblioteka i mapa tej kopii są wyłącznie własnymi danymi testowymi.

## Gumka — przyczyna i wynik

Poprzedni algorytm nie odejmował powierzchni. Powiększał promień o połowę grubości kreski (`(diameter + stroke.Width) / 2`), wycinał odcinki osi i odrzucał dotknięte kropki. Przy grubych nakładających się kreskach znikały ich całe przekroje, również poza kołem podglądu. Sama korekta promienia tego nie naprawiłaby.

Teraz klik odejmuje koło o rzeczywistej średnicy narzędzia. Drag odejmuje ciągłą kapsułę między kolejnymi próbkami, czyli sumę kół bez luk. Wszystkie przecinane rysunki tracą wyłącznie tę powierzchnię. Pozostałe warstwy są nietknięte, a otwór odsłania ich rzeczywisty obraz.

Nienaruszona kreska nadal jest polilinią z grubością. Dopiero wymazywanie zamienia ją w zamknięte kontury z otworami i ewentualnie rozłącznymi częściami. Użyto istniejącego WPF `Geometry.Combine(Exclude)` i kanonicznego opisu powierzchni; renderowanie konturów odbywa się w GDI+ z antyaliasingiem. Nie tworzymy dokumentu/edytora WPF, bitmapy całego świata ani pozornego malowania tłem. Tolerancja operacji wynosi minimum0,0005 jednostki, zależnie od grubości /65536; krawędź pikselowa podlega antyaliasingowi.

Wersje powierzchni są niezmiennymi migawkami. Jeden gest daje jeden główny Undo/Redo; Redo odtwarza wynik, nie przelicza wymazywania. PPM Wyczyść wszystko pozostaje jednym krokiem. Pusta pozostałość jest usuwana; Fit korzysta z rzeczywistych obwiedni pozostałych konturów.

### Rozmiar, format i budżety

- Wspólne UI Rysik/Gumka: całkowite **1–200**, domyślnie5, krok1 i uchwyt8px/1. Wpisywanie działa. Przeliczenie ekran × DPI/96 / zoom raz na gest; zapis światowy pozostaje ułamkowy.
- Mapa **10** zamiast9: jawne `InkStroke.Surface`, null dla pełnej polilinii, kanoniczne kontury dla wyniku gumki. Jest to autorytatywna geometria, a nie cache. Nowe dane są walidowane przed podmianą dokumentu. Bez zgadywania/migracji map9 lub kasowania starych plików.
- Preset3, grafika2, biblioteki ikon/obiektów/teł i pozostałe formaty bez zmiany. Pełne źródła, deduplikacja i przenośny model zasobów pozostają.
- Nadal5000 powierzchni,10000 punktów kontrolnych pojedynczej,200000 dokumentu,2mln współdzielonych punktów historii i300 wpisów. Dodatkowo2Mi znaków pojedynczego opisu powierzchni,16Mi dokumentu,64Mi zachowanych wersji w historii. To limity znaków; tekst UTF-16 zajmuje do dwukrotności w bajtach, osobno od geometrii i narzutu obiektów. Nie zwiększano budżetów obrazów/źródeł/cache ani mapy256MiB. Przekroczenie odrzuca cały gest z komunikatem.

## Kometa

Przyczyną koralików i jaśniejszych miejsc były osobne dyski próbek i wielokąty, nakładane kolejno SourceOver. Odsunięte próbki pokazywały dyski/łączenia, a pokrycia sumowały alpha.

Nowy renderer łączy kolejne próbki ciągłymi segmentami o interpolowanej szerokości i alpha. Pokrycie przy połączeniach jest maksimum, nie sumą; nie rysujemy osobnych dysków próbek. Szerokość szybciej maleje ku ogonowi, według potęgi2,3 pozostałego życia. Każdy piksel komety jest składany na płótno raz w danym kafelku. Bufor256×256ARGB i tablica pokrycia mają około512KiB łącznie, są używane ponownie i zwalniane przy zamknięciu, bez bitmapy świata lub kolejki starych klatek.

Zachowano750ms,128próbek, głowę20px przy DPI96 bez obwódki, osobny kolor, lokalne ukrywanie/przywracanie kursora, łączenie żywych kliknięć i brak starego łącznika po wygaśnięciu/kontekście. Timer33ms działa tylko gdy potrzeba. Brak dirty, historii, zapisu i wpływu na Fit. Test obrazu potwierdza ciągłość rzadkich próbek i brak pików alpha; **nie zaliczono wizualnej płynności ciągłego PPM**, którego API drag nie obsługuje.

## Jedna ograniczona próba panelu

W0.6.1 zmierzono koszt natywnego Show prawej gałęzi około140–150ms. W ścieżce Kursora `ChooseObject` wykonywał pełne UpdateUI i Show jeszcze w MouseDown, przed `BeginObjectMove`. To konkretna blokująca praca na początku pierwszego przeciągnięcia. Przy otwartym panelu nie występował ten koszt pokazania.

Rozdzielono wybór i rozpoczęcie ruchu od ujawnienia zamkniętych szczegółów. W MouseDown zmienia się wybór/obrys i od razu można przesuwać; panel jest otwierany po rozstrzygnięciu kliknięcia lub zakończeniu ruchu. Sprzątanie stanu gestu poprzedza końcowy UpdateUI. Rączka nadal nie otwiera automatycznie szczegółów; panel już otwarty zachowuje dotychczasowe zachowanie. Testy obejmują obiekt i grupę, końcową pozycję, jeden wpis, Undo/Redo i powtórne kliknięcie.

Pomiary MouseDown (jedna krótka seria, pierwsza z JIT): pojedynczy zamknięty6,06ms, otwarty0,30ms; grupa zamknięta0,19ms, otwarta0,26ms. Nie są pomiarami całego odrysowania. W zwykłym EXE pierwszy drag przesunął obiekt i otworzył panel po geście. Narzędzie pokazuje zrzut po akcji, więc **nie potwierdzono atomowości separatora i treści ani ciągłej płynności**. Istniejący UpdateUI już przygotowuje wartości/layout przed Visible; natywne pokazanie może nadal mieć etapy. Dalsza poprawka kompozycji jest odłożona do przebudowy panelu, podobnie jak header/right-edge jitter. Nie zmieniano WPF, globalnego WS_EX_COMPOSITED, nie dodawano DoEvents, timerów lub sztucznych pauz.

## Tło podczas zmiany rozmiaru

Klucz cache zawierał viewport, a każda zmiana rozmiaru wywoływała Render bez przygotowanego obrazu: ponowne dekodowanie źródła, cover, filtr nasycenia i blur. Dodano cache jednego rozkodowanego źródła. ResizeBegin używa wyniku do320px; ResizeEnd odrzuca lekki wynik i renderuje dokładny do2048px. Jeden najnowszy rozmiar, bez kolejki. Cover/centrowanie pozostają w tym samym rendererze, zależą od właściwego viewportu, nie zaokrąglonego rozmiaru bufora. Źródło i parametry nie są modyfikowane, resize nie tworzy dirty/historii. Oba bufory zwalniane przy zmianie, dokumencie i Dispose.

Lokalne porównanie na1200×800PNG, nasycenie65, blur6, cztery viewporty1300–1339×800: poprzednia pełna ścieżka **267,33ms**, render z przygotowanym źródłem i buforem320 **116,75ms** łącznie. Cztery rzeczywiste zmiany rozmiaru Form z layoutem i rysowaniem w teście kontrolek: **317,82ms**. Nie porównujemy tego ostatniego wyniku wprost z samym rendererem, nie obiecujemy stałego FPS. Końcowy dokładny bufor jest pikselowo zgodny w pobranych próbkach z bezpośrednim dokładnym rendererem. W zwykłym EXE przeprowadzono zmianę szerokości z filtrowanym tłem; ciągłe nadążanie za myszą i przejście jakości wymagają odbioru użytkownika.

## Pozostałe UI i backlog

- Globalna Biblioteka inicjalizuje standardowe brakujące katalogi i otwiera właściwy folder przez ProcessStartInfo UseShellExecute/Verb=open, bez cmd i argumentów konkatenowanych ze ścieżką. MessageBox tylko przy błędzie.
- Ograniczenie widoczności ma konturowe oko; wiadro dodatkowy znak/podkreślenie. Reszty ikon nie przebudowano.
- Biblioteka grafik ma kompaktowe4kolumny: około4×120px, marginesy i scrollbar. Natywny spacing/ArrangeIcons aktualizowane przy pokazaniu i resize. Ręczna zmiana rozmiaru i ograniczenie do obszaru ekranu pozostają.
- Dół grupy: ramka/kolor → Rozwiąż grupę → Usuń instancje grupy. Kolumna Mgła domyślnie185px, bez elastycznego rozszerzania; maskowanie/ellipsis i dotychczasowe tooltipy pozostają.
- Dialog tła: Z biblioteki… → Z pliku… → Usuń tło (czerwony tekst/ramka, hover/focus/disabled); nadal usuwa tylko tło mapy. MMB i granic historii nie zmieniano.
- Dodano [backlog lokalnych ustawień](backlog-ustawienia-lokalne.md), jawnie powiązany z przyszłą przebudową góry/Plik: okna, panel, biblioteki i kolumny niezależne od map, reset, fallback monitora/DPI, bez obecnej implementacji/profili. Dopisano wygląd uchwytów, X/Y lub szerokość/wysokość i drugi uchwyt lewego panelu. [Kolejka folderów](backlog-import-z-folderow.md) zachowana, niewdrożona.
- Źródła pola widzenia grup: **obecnie nieodtwarzalne, bez ustalonej przyczyny**; użytkownik potwierdził działanie również w0.6.0. Nie prowadzono ponownej diagnozy ani kampanii źródeł/grup. Zachowano regułę Obiekty OFF, bazową maskę i niezależny trwały Fog.

## Weryfikacja metod, renderowania i kontrolek

Tylko jawne pojedyncze Case `tests/Run-Patch062Tests.ps1`; brak Case odrzucany przed kompilacją/tworzeniem przebiegu (kontrola kodu, bez ponownej próby historycznego runnera). Nie Run-AllTests ani stare patch-runnery. Dane każdego przebiegu w osobnym tests/_work.

| Case i końcowy przebieg | Faktyczny zakres i wynik |
| --- | --- |
| `eraser`, `patch-062-eraser-dd2f83a9449d4aab959366bb6691236b` | PASS: gęsta plama, grube kreski, cienka kreska i inna kropka; zoom0,73/1,46. Porównanie pikseli wnętrza/poza kołem z marginesem AA, ciągły pas przy rzadkich próbkach, identyczny render serializowanej powierzchni, rzeczywista obwiednia skrajnej pozostałości. Rzeczywiste Begin/Move/Finish gumki, jeden Undo/Redo, Clear/Undo/Redo, brak zmiany heksów, map10 round-trip, całkowite UI i szerokość świata większa niż200. |
| `comet`, `patch-062-comet-801287de60cb4c23bd6aab8991af6807` | PASS: obraz rzadkich punktów bez przerw, alpha centrum bez piku na łączeniu, szybszy taper, wygaśnięcie750ms. Kontrolowane Begin/PointerAt/Finish, timer, kursor i kontekst/wygaśnięcie, brak zmiany dokumentu/historii. To nie gest PPM zwykłego okna. |
| `panel`, `patch-062-panel-f9ecd7cf776349e2b216606e36fc3084` | PASS: pojedynczy/grupa, zamknięty/otwarty panel. Brak synchronicznego otwierania w MouseDown, dokładny ruch, jeden wpis, Undo/Redo, klik otwiera/powtórny odznacza. Lokalne czasy powyżej, bez oceny płynności. |
| `background-ui`, `patch-062-background-ui-502bef8993ad434d82094e64c68f6830` | PASS: cache źródła, lekki/dokładny bufor, końcowe próbki pikseli, bez dirty/historii, bezpieczny ProcessStartInfo folderu;4kolumny, stała Mgła, kolejność grupy i czerwone akcje tła. Pomiary powyżej. |

Techniczne pierwsze próby niezaliczone nie są ukryte: fixture gumki początkowo używał nazwanego koloru zamiast wymaganego HEX; nowy handler Dispose miał brak średnika, a wywołanie EnsureDirectories brak argumentu — poprawiono przed zaliczonymi próbami. Fixture panelu początkowo nakładał obiekty i trafiał inny, górny; rozstawiono dane testowe. Pierwszy test kompaktowego widoku wykrył2kolumny przed natywnym ułożeniem; dodano ArrangeIcons/aktualizację spacing i sprawdzono poprawkę. Podczas oznaczania wersji poprawiono błędny BOM AssemblyInfo, poprzedni EXE został zachowany. Fixture UI początkowo nie miał wymaganego TemplateId; poprawiono wyłącznie fixture i zapisano skutecznie. Dawnych testów nie usuwano: konkretne nieaktualne asercje cięcia osi, minimum0,5 i map9 dostosowano do nowej semantyki, **bez ich uruchamiania**.

## Faktyczne próby zwykłego finalnego EXE

Własna mapa `tests/_work/patch-062-fixture-c4aa4569b48040449c7898eb3c1520f9/Próba 0.6.2.hexmap`. Po sprawdzeniu rzeczywistych okien nie było HexMapEditor; uruchomiono wskazaną identyczną kopię EXE. Nie zamykano okien użytkownika.

1. Wczytano fixture: wielowarstwowa żółta plama, inna kropka i cienka niebieska kreska, obiekty/grupa, filtrowane tło. Pierwsze wywołanie drag zostało wstrzymane przez wykryty input użytkownika; stan odczytano ponownie, wynik nie był zaliczany. Następny rzeczywisty drag pojedynczego obiektu przy zamkniętym panelu przesunął go o zadane45×24px ekranu i otworzył szczegóły. Po zamknięciu panelu pozycja była widoczna; zapis potwierdził X470,920441/Y146,490891 zamiast440/130.
2. Gumka ustawiona przez pole na40. Po **nowej zgodzie użytkownika w chwili akcji** klik przez kropkę i plamę wyciął okrąg zgodny z obrysem. Następny drag wyciął pas z zaokrąglonymi końcami ze wszystkich warstw, odsłaniając siatkę. Ctrl+Z cofnęło cały pas (okrąg poprzedniego gestu pozostał), Ctrl+Y odtworzyło ten sam pas. Zaobserwowano rzeczywisty obraz, nie sam licznik segmentów.
3. Górna Biblioteka otworzyła nowe okno Eksploratora. Pasek adresu potwierdził dokładnie Library tej kopii tests/_work, ze spacjami i polskimi znakami w ścieżce nadrzędnej. Brak informacyjnego MessageBox. Zamknięto wyłącznie to własne okno Eksploratora.
4. Próba bezpośredniego trafienia prawej ramki nie rozpoczęła resize. Użyto systemowego menu Rozmiar, wskazano prawą krawędź, następnie wykonano drag aktywnej krawędzi; szerokość ujęcia zmieniła się1415→1243px. Tło miało nasycenie65/blur6/krycie75, końcowy cover/obraz pozostały poprawne. Zrzuty po akcji nie dowodzą ciągłego nadążania za kursorem ani braku1–3px przeskoku w każdej klatce.
5. Zapisano wyłącznie własną mapę (gwiazdka zniknęła), a następnie zamknięto własne okno. Odczyt pliku potwierdził format10,12rysunków, w tym8 powierzchni po wymazaniu. Sprawdzono brak obu własnych okien. Nie wykonywano kolejnego szerokiego sprzątania; artefakty tests/_work pozostają ignorowane.

## Ograniczenia i niewykonane próby

Nie wykonano pełnej kampanii, przenośności do czystej kopii, DPI/WPF ani dużych benchmarków. Nie powtarzano grup ze źródłami, bibliotek/map bez zależnej zmiany. Round-trip nowych konturów wykonano metodami i rzeczywistym zapisem UI, **bez deklaracji ponownego otwarcia zapisu UI**. Nie wykonywano wizualnego ciągłego PPM komety (API drag obsługuje LPM); brak deklaracji jego płynności. Narzędzie ujmuje obraz po gestach, więc atomowość natywnego pokazania separatora i ciągły live resize pozostają do odbioru. Dalsza naprawa panelu wymagająca przebudowy kompozycji jest jawnie odłożona, nie rozszerzano tej jednej próby. Brak oczekujących zgód, dialogów lub własnych aktywnych procesów po próbie.

## Checklista odbiorcza użytkownika

- [ ] Gęsta plama i inna kropka: koło gumki40/200 usuwa tylko powierzchnię pod obrysem, również przy innym zoomie; pas nie ma luk.
- [ ] Undo/Redo gestu i PPM Wyczyść wszystko; zapis/odczyt nowych konturów oraz Fit po wymazaniu skraju.
- [ ] Wspólny całkowity rozmiar1–200 i uchwyt8px/1; nowe kreski ekranowe, stare nadal skalują się z mapą.
- [ ] PPM kometa: ciągła, bez koralików/jaśniejszych łączeń, szybko zwężana,750ms; kursor zawsze wraca, brak starego łącznika.
- [ ] Kursor, panel zamknięty: pierwszy drag obiektu/grupy zaczyna ruch przed otwarciem szczegółów; zwykły/powtórny klik pozostają. Ocenić osobno pozostały separator/header.
- [ ] Live resize z reprezentatywnym tłem: lżejszy obraz podczas ruchu, dokładny po końcu, ten sam cover i brak trwałej zmiany danych.
- [ ] Biblioteka otwiera folder; kompaktowe4kolumny grafik; oczy narzędzi, stała Mgła, kolejność ramka→Rozwiąż→Usuń i czerwone Usuń tło.
- [ ] Przenośność do czystej kopii nadal świadomie odłożona; nie wymaga się jej teraz ani ponownej kampanii grup.

## Git

Kod, finalny EXE i celowane testy: **`025ee1b`**. Dokumentacja tej wersji jest w następującym po nim lokalnym commicie. Gałąź master; końcowy stan roboczy po zapisaniu dokumentacji sprawdzany jako czysty. Testowe Library/mapy i EXE pozostają wyłącznie w ignorowanym tests/_work. Bez push/tagu/Release'a. Nie deklarujemy kompleksowego przetestowania aplikacji.
