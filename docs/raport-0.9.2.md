# HexMapEditor 0.9.2 — raport

Data przygotowania:15.11.2026 (`ReleaseMetadata.Date`). Baza db52f96 potwierdzona lokalnie, Git czysty przed pracą. Aplikacja0.9.2; FileVersion i ProductVersion **0.9.2.0**. Brak push, tagu i Release’a.

## Wykonany zakres

| Punkt | Wynik |
|---|---|
| Biblioteki ikon/obiektów | Importuj / Edytuj / Usuń / Zamknij; jeden wybór, bez multiusuwania. Klik zaznacza do zarządzania, dwuklik/Enter na kafelku zatwierdza użycie. Zamknij/Escape nie wybiera zasobu. Edytuj/Usuń bez pozycji disabled. |
| Edycja i usuwanie | Wspólne istniejące TryChangeLibraryIcon/TryChangeObjectTemplate i LibraryFiles; rename/podmiana zachowują ID. Nowy plik przechodzi walidację i kwadratowy draft kadru. Anulowanie draftu nie zapisuje. Usuwanie wymaga istniejącego ostrzeżenia; błąd pliku nie czyści sesji. |
| Import między kategoriami | Trwały odpowiednik od pełnego źródłowego ID i kierunku, bez zmian schematu. Powtórzenie/restart używa istniejącego wpisu i jego ręcznej edycji. |
| Wiadro ikon | Połączony region dotychczasowego terenu/wariantu i stanu ikony startu. Puste, inna tożsamość, wybrana tożsamość odpowiednio malowane/zastępowane/usuwane. |
| Zapis presetu | Wspólny niebieski ChoiceStyle wynika z WorkingPresetDirty aktualnego presetu, identycznego źródła jak status/zapis. Inny draft i dirty mapy pozostają niezależne. |
| Kadr ikon/obiektów | Kompaktowy klient640×660, kadr520² przy100%, wynik512²; Obrót0° zachowuje przesunięcie i skalę. |
| Kadr terenu | Dotychczasowy klient640×590 i heks; szachownica, pełne otoczenie, przyciemnienie poza kadrem, niebieski obrys. Bez zmiany CropMath i zapisanej transformacji. |
| Zapis zmienionego kadru | Nadpisz istniejącą / Utwórz nową z własną nazwą / Anuluj. Kolizja bez sufiksu i bez overwrite obcej pozycji. Wynik automatycznie wybrany. |
| Warianty terenu | Klient540×430, minimum zewnętrzne554×469 przy100%; pełne5×2 bez zmniejszenia88px kafelków/fontu, krok z marginesami96px, padding8px i miejsce na scrollbar. Dalsze warianty przewijane. |
| Scrollbary | Wszystkie cztery wskazane obszary zachowują natywne scrollbary. Nie było istniejącego prostego wspieranego mechanizmu ich lokalnej kolorystyki; nie dodano własnego scrollbara, podmiany kontrolki ani globalnego hooka. |
| Odebrana baza | Hybrydowy tytuł/hit-testy, cały layout lewego panelu, figury, liczby, switche, zapis/autozapis, WPF, rysik/gumka i High/Low nie są przebudowane. |
| Backlog | Nowy zakres0.9.2 zastępuje plan multiusuwania. Zbiorcze usuwanie odłożone bez numeru; oryginały/recrop0.13, kolory narzędzi w przyszłych preferencjach, pozostała kosmetyka i pomysły po GM bez realizacji. |

## Tożsamość importu i zasoby

Dotychczasowa konwersja tworzyła losowe ID, a model nie miał trwałych danych pochodzenia. Zastosowano istniejące pole ID: `from-object-`+SHA256 pełnego ID źródła dla ikony, `from-icon-`+SHA256 dla obiektu. Nazwa i bitmapa nie są kluczem. Ręczna zmiana nazwy/grafiki odpowiednika nie zmienia ID ani nie jest nadpisywana podczas reimportu. Usunięty odpowiednik może powstać ponownie z tym samym wyliczonym ID. Nie skanowano dawnych duplikatów i nie scalamy wcześniejszych losowych kopii; pierwszy import takiego źródła według nowej reguły może utworzyć jeden nowy odpowiednik. To ograniczona reguła dla konwersji kategorii, nie ogólna deduplikacja plików.

Quick nadal unikalne24/12. Udany import może wybrać/promować istniejący wpis. Operacja plikowa korzysta z istniejącej kontroli konfliktu i historii. Biblioteczne usunięcie czyści wszystkie zainicjalizowane robocze listy/rezerwy dopiero po sukcesie pliku. Nie usuwa wystąpień/źródeł mapy, osadzonych kopii ani zapisanych plików presetów i nie oznacza presetów dirty. Undo/Redo sesji odtwarza plik oraz listy; granice odzyskania pozostają takie jak w istniejącym ostrzeżeniu, bez Kosza i bez obietnicy odzyskania po zamknięciu.

Nowe przyciski bibliotek wywołują te same handlery co panel. Aktywna biblioteka jest właścicielem ich dialogów i potwierdzeń. Ujawnioną w próbie zależność złego owner MainWindow poprawiono zamiast tworzyć drugie kasowanie. Zamknięcie pickera po zarządzaniu przywraca wcześniejszy wybór materiału/narzędzia w dozwolonym stanie; jawne operacje biblioteczne pozostają wykonane.

## Wiadro ikon i historia

RegionState przechowuje przed zmianą stan ikony: null albo IconEntryId, z fallback IconId dla danych bez tożsamości wpisu. Flood uwzględnia ten warunek tylko dla wiadra ikon. Dotychczasowe ograniczenie terenu/wariantu i sąsiedztwo pozostają. Gest zachowuje stan startu i tryb malowania/usuwania; przesuwanie nie przełącza go na przeciwny stan. Każda istniejąca ikona ogranicza wypełnianie pustego regionu, również bariera z kilku różnych ID. Jedna logiczna akcja historii, zwykłe dirty mapy i brak zmian innych warstw.

Przycisk Zapisz preset nie dostał nowego dirty ani zmiany dostępności. UpdatePresetStatus stosuje ChoiceStyle do istniejącego WorkingPresetDirty. Zapis częściowy pozostawia wyróżnienie, gdy pominięte sekcje są niezapisane; restore/pełny zapis i przełączenie aktualizują stan. Górny zapis mapy bez zmiany.

## Kadrowanie, zapis i skutki

CropPreview współdzieli wyłącznie szachownicę i rysowanie otoczenia. Kopia transformacji jest przeskalowana na większy obszar podglądu, oryginał pozostaje nietknięty. Wynik nadal wyznaczają dotychczasowe CropMath, kwadrat512×512 ARGB dla ikon/obiektów i heks terenu. Zachowane alpha, contain, pan/zoom/obrót oraz budżety. Podgląd kwadratu520×520 jest większy od wyniku, z widocznym otoczeniem. Obrót0° zeruje tylko Angle.

Pochodzenie wyboru bibliotecznego jest zapamiętywane w sesji przez słabą referencję definicji do ID logicznego wpisu. Dla osadzonych definicji szukany jest dokładny wcześniejszy wygląd, nie podobna nazwa. Wiele identycznych wpisów wymaga jawnego wskazania celu; nazwa celu nie jest przypadkowo zastępowana nazwą pierwszego dopasowania. Samodzielna osadzona grafika bez odpowiednika pozostaje draftem bez automatycznego importu do Library.

Nowa pozycja wymaga nazwy1–200 znaków i przechodzi istniejące reguły plików; konflikt nazwy/ścieżki odrzucany bez dopisywania2/3. Nadpisanie zachowuje logiczne ID, istniejący writer kontroluje bajty pliku. Stary wpis nie jest usuwany przed udanym zapisem. Wybór nowego/nadpisanego kadru wraca do wywołującego formularza.

Bezpośredni zapis w bibliotece zmienia tylko pozycję biblioteki, bez zmiany istniejących terenów, historycznych definicji czy heksów. W edycji terenu kadr trafia do wariantu; dotychczasowe Zastosuj do bieżącej mapy On/Off oraz zachowanie rozmieszczenia nadal decydują o aktualizacji mapy. Kopie danych nie pozwalają na propagację przez wspólną mutowalną referencję/cache.

Operacja biblioteczna jest osobną operacją sesyjnej historii. Anuluj przed jej zapisem pozostawia wpis; późniejsze Anuluj formularza nadrzędnego odrzuca jego draft, ale nie cofa już zatwierdzonej niezależnej operacji plikowej. Można użyć istniejącego Undo w sesji. Nie dodano nowego systemu propagacji ani recrop zapisanych PNG ikon/obiektów.

Map11/preset5 bez zmiany, nadal odczyt preset4. Ikony biblioteczne2, obiekty1, grafiki terenów2. Nie zwiększono limitów plików/zasobów/historii. Ikony/obiekty nadal wejście8MiB i2048², wynik512²; pełna bitmapa źródła tylko podczas importu i Dispose na zamknięciu. Nie przechowujemy nowych oryginałów ani nowej struktury Library.

## Testy metod, renderowania i kontrolek

Wyłącznie jawne pojedyncze Case `tests/Run-Patch092Tests.ps1 -Case …`. Źródła testów są wersjonowane, wyniki pod tests/_work. Nie uruchomiono Run-AllTests, historycznych runnerów ani pełnej kampanii.

| Case / ostatni wynik | Zakres |
|---|---|
| libraries — PASS45cef09edff7406493be83b37ce8f24d | Oba kierunki, powtórzenia, ponowny odczyt w nowej instancji, ręczna edycja, różneID przy tej samej nazwie, unikalny quick, single-select/Close, routowanie Enter/dwuklik, konflikt pliku, istniejące usunięcie/Undo/Redo i odtworzenie odpowiednika po usunięciu; mapa/presety zachowane. |
| bucket — PASS8cc91baafb524982b644ce81a4da9103 | Produkcyjne BeginMaterialStroke/VisitBucketRegion/FinishStroke: pełna i mieszana bariera, obie strony, usunięcie jednej, zastąpienie bariery, usunięcie całości, granica terenu, Undo/Redo, niezmienione inne warstwy. |
| crop — PASS87338da0bfa7477095d9afd43be7c617 | Kompaktowy dialog/reset tylko kąta, Anuluj i niezmieniony oryginał; rendering poza dawnym kwadratem; overwrite/new/kolizje nazwy i obcej wersji, brak propagacji do mapy/palety, ApplyOff/On, pełne5×2; wybór nazwy właściwego celu przy wielu identycznych wpisach. |
| status — PASS96206d594f5542c2a4cc31cf6c3c60de | Czysty/niezapisany bieżący, inny draft, częściowy zapis, restore i przełączenie, niezależność dirty mapy. |

Powtórzenia wyłącznie zależne: libraries po naprawie rzeczywistej aktywacji natywnego Button (376abe94…) oraz po poprawieniu owner dialogów (45cef09e…); crop po lokalnej poprawce wyboru nazwy celu nadpisania przy wielu identycznych dopasowaniach (87338da0…). Bucket i status nie powtarzane, bo późniejsze zmiany nie dotykały tych mechanizmów. Pierwotny crop0dd548… i libraries6e6da… także PASS. Build ma dotychczasowe ostrzeżenia CS0649 test hooks; brak błędów kompilacji.

## Zwykły EXE — osobna próba

Wyłącznie własna kopia i dane w `tests/_work/patch-092-fixture-b25054ebc74243188347ffa1c0acf048`. Mapa8×7 PointyTop, jednolity teren graficzny i pionowa bariera Osada w kolumnieD, własny preset11wariantów i własny PNG640×320 z alpha. Narzędzie Computer Use; nie metody symulujące ręczny gest.

- Obejrzano cztery przyciski obu bibliotek i disabled bez wyboru. Rzeczywisty Enter na obiekcie oraz dwuklik na ikonie wybierają zasób; zamknięcie bez wyboru nie zatwierdza przypadkowej pozycji.
- Obiekt Postać niebieska zaimportowano jako ikonę, potem odpowiednik przemianowano i podmieniono grafikę z własnego PNG. W kwadratowym kadrze wykonano pan/zoom, obrót108° i Obrót0°. Po rzeczywistym restarcie i ponownym imporcie oryginalnego źródła wróciła ręcznie edytowana nazwa/grafika; plik ma nadal pojedynczy ID `from-object-864f995158b5b4dd7869137503e2d43104bdc47bb326b2e2c468581baf898cdc`.
- Wiadrem z Lotnisko/krzyżem wykonano pełną sekwencję obu stron bariery, usunięcia lewej, ponownego wypełnienia, zastąpienia bariery oraz usunięcia połączonej całości. Ctrl+Z/Y, Ctrl+S i późniejsze otwarcie własnej mapy. Bez klikania wszystkich kombinacji metodowego testu.
- Sprawdzono rzeczywiste pełne5×2 przy11wariantach i scrollbarze; kadr terenu pokazuje otoczenie, szachownicę i niebieski heks. Zapisano nową pozycję „Nowy kadr próby092”, automatycznie wybraną w wariancie, a teren zatwierdzono z ApplyOff. Mapy nie przemalowano; przycisk presetu stał się niebieski, po jego ręcznym zapisie neutralny.
- Własny edytowany odpowiednik usunięto z biblioteki, zamknięto picker i sprawdzono Ctrl+Z/Y/Z: kafelek i plik wracają, znikają, wracają; status presetu przez cały czas czysty. Potwierdzenie jest widoczne nad aktywną biblioteką po poprawce owner.
- Na finalnych bajtach dodatkowo sprawdzono bezpośredni kadr istniejącej grafiki w bibliotece, wybór nadpisania i pozostawienie wyniku wybranego. Mapa oraz preset nadal czyste. To krótka zależna próba po ostatniej poprawce nazwy celu, bez powtarzania pozostałych zaliczonych ścieżek.

Pierwszy kandydat ujawnił nieskuteczne DoubleClick/Enter standardowego Button — poprawiono lokalny ResourceTileButton (WM_LBUTTONDBLCLK i ProcessCmdKey), następnie obie rzeczywiste aktywacje przeszły. Druga zależność: native MessageBox był własnością MainWindow i chował się pod biblioteką. Użytkownik zamknął tę kopię z pytaniem; aktywny owner przekazywany teraz do wspólnych handlerów i próba przeszła. Nie dotyczyło to okien użytkownika ani prywatnych danych.

## Ograniczenia i niewykonane próby

Stanowisko dostępne przez narzędzie nie było fizycznym FullHD1920×1080. Kontrole i zrzuty odpowiadały100% (96DPI), okno edytora1426×934 mieściło się w większym pulpicie; **nie deklarujemy fizycznego odbioru FullHD** ani pełnej macierzy DPI. Nowe okna są mniejsze od obszaru FullHD, jednak użytkownik powinien potwierdzić wygląd na swoim stanowisku.

Nie wykonywano pełnej ręcznej macierzy obu bibliotek i wszystkich błędów IO; wspólne mechanizmy sprawdzono metodami, a jedną reprezentatywną integrację zwykłym EXE. Anulowanie importu i konflikty kadru/metodowa kontrola danych są odrębne od wizualnej próby. Nie wykonywano ponownie pięciominutowego autozapisu, ramki, figur, WPF, gumki, całego panelu i przenośności. Nie mierzono FPS ani ciągłej płynności/pojedynczego flasha na podstawie screenshotów. Nie ma deklaracji kompleksowej regresji.

## Krótka checklista użytkownika

- Biblioteka ikon/obiektów → klik pozycji: aktywne Edytuj/Usuń; dwuklik/Enter używa; Zamknij nie wybiera.
- Import tej samej pozycji z przeciwnej biblioteki, również po restarcie → jeden odpowiednik, bez utraty jego ręcznych zmian.
- Edytuj → rename/podmiana pliku/Anuluj → odpowiednio zachowane ID lub niezmieniony wpis.
- Usuń z biblioteki → jawne ostrzeżenie i brak pozycji; Undo/Redo w sesji → plik/listy odtworzone/usunięte, mapa i zapisane presety zachowane.
- Wiadro po dwóch stronach bariery ikon → oddzielne regiony; klik bariery zastępuje jej tożsamość; po połączeniu usuwana całość.
- Zmieniony bieżący preset → niebieski Zapisz preset; czysty inny preset → neutralny, niezależnie od mapy.
- Kwadratowy kadr → pan/zoom/obrót; Obrót0° zachowuje skalę i przesunięcie; wynik ma alpha i512².
- Kadr terenu → szachownica/otoczenie/niebieski heks; nowy wymaga nazwy, konflikt nie nadpisuje, Anuluj pozostawia wpis.
- Biblioteczny recrop → brak automatycznej zmiany mapy; edycja terenu → dotychczasowy switch ApplyOn/Off.
- Okno wariantów na FullHD100% → pięć pełnych kolumn i dwa pełne rzędy, dalsze przewijane, natywny scrollbar.

## Artefakt i Git

Główny `HexMapEditor.exe` oraz finalna kopia testowa mają identyczne bajty, SHA-256:
`1A5B41D42E7AEDC80B9D1C094BEF06E6548A759B04FD3485BD223CE2BFED075C`.
Sprawdzono FileVersion/ProductVersion0.9.2.0. Poprzedni poprawny0.9.1 zachowano w katalogu własnej próby przed atomowym przeniesieniem sprawdzonego kandydata. Nie przebudowywano artefaktu po końcowej próbie.

Jeden lokalny commit obejmuje ten raport, kod, główny EXE i źródła testów: `Implement HexMapEditor 0.9.2 library and crop fixes`. Jego identyfikator podaje końcowa odpowiedź i `git log -1`; nie wpisujemy do treści commita jego własnego, zależnego od tej treści hasha. Po commicie oczekiwany czysty status własnych zmian; wynik końcowego sprawdzenia w odpowiedzi. Dane tests/_work i prywatne pliki wykluczone. Brak uruchomionych własnych okien/procesów po próbach. Brak publikacji/push/tagu/Release’a.
