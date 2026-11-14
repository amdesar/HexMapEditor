# HexMapEditor 0.9.1 — raport wykonania

Baza: **7791556**, potwierdzona lokalnie; przed pracą Git czysty. Aplikacja oraz FileVersion/ProductVersion **0.9.1.0**, jawna data wydania **14.11.2026** w `ReleaseMetadata`. Map11/preset5, odczyt terenowego preset4 i struktura Library bez zmian. Jeden lokalny commit obejmuje własny kod, główny EXE, źródła testów i dokumentację; bez `_work` i prywatnych danych. Bez push, tagu i Release'a.

## Panel i wysokość

Przyczyną naprzemiennego scrollbara była konkurencja zewnętrznego AutoScroll z minimalną wysokością całej tabeli oraz obszarem terenów wypełniającym resztę. Kolumna jest teraz tabelą Dock=Fill bez AutoScroll; stałe sekcje mają wyliczone wysokości, a tereny dostają resztę. Przewijanie dotyczy wyłącznie terenów.

Panel ma **276 px + uchwyt18** przy100%, zamiast298+18. Sześć kafelków40×28 z marginesami2 zajmuje264px. Operacyjne przyciski48×26 mają stały odstęp5px i wspólne skrajne krawędzie z kafelkami; czteroprzyciskowy rząd pozostawia piątą pozycję pustą. Przybornik4×3 zachowuje rozmiar48×26 i równomierne większe odstępy. Nie ma rezerwy na scrollbar całej kolumny.

Minimum wylicza `ConfigureLeftMinimum`: maksymalny przybornik192, presety114, tereny126 (nagłówek, akcje, przewijanie i pełna pozycja), dwa pełne zestawy po120, rzeczywisty szybki pasek, dół27 i konwersja klient/ramka przez SizeFromClientSize. Wynik na96DPI: **1180×806**. Jest stały, nie zależy od figury ani zasobów, mieści się w FullHD100%. Zmniejszanie wysokości redukuje tylko resztę terenów; nie obcina dołu.

Parametry mają etykietę i pole w jednej linii, od lewej. Pola mierzone na `-360` z oddzielnym miejscem sufiksu/paddingu, równe także z rysikiem/gumką. Dla najbogatszego trójkąta przy tej szerokości stosowane ∠ i ↻ z pełnymi tooltipami i nazwami dostępności. Punkt/Koło/wypełnienie zachowują właściwe disabled; swobodne obiekty nie pokazują parametrów stempla. Zakresów, geometrii, normalizacji, historii i pierwszego gestu nie przebudowano.

Presety: Nowy/Edytuj/Zapisz/Usuń/Przywróć w pierwszym rzędzie, lista+Odśwież w drugim. Status ma30px na dwie linie i istniejący pełny tooltip; niezapisany pozostaje czerwony. Natywny flat ComboBox nie udostępnia prostego koloru obrysu; pozostawiono jego ramkę, zamiast tworzyć nową kontrolkę.

Zestawy ikon/obiektów:0 pozycji nie ma rzędu,1–6 ma jeden,7–12 dwa. Brak pustych przycisków/ramek. Uwolniona wysokość trafia do terenów, minimum pozostaje stałe. Limit24/12 i semantyka użycia zasobu zachowane.

## Aktualna biblioteka, fallback i draft

Reprodukcja na bazowym0.9.0: po zapisie mapy ze starym osadzonym presetem i nowszego pliku presetu katalog zawierał nowe tereny/0ikon, a panel stare tereny/12ikon. To był wybór źródła panelu, nie utrata pliku mapy.

`AdoptLibraryPanel` działa przy przyjęciu dokumentu: poprawny katalogowy preset **po ID** dostarcza trzy sekcje; nie dopasowujemy po nazwie. Otwórz i MRU używają wspólnego TryLoadProject z odświeżeniem katalogu. Konstruktor mapy z argumentu ma tę samą regułę. Brak pliku zostawia dane osadzone. Uszkodzony preset jest zgłaszany przez istniejący loader i pomijany; fallback pozostaje, bez tworzenia pliku biblioteki.

Pomalowane heksy zachowują własne wyglądy, ikony/obiekty własne źródła. Przyjęcie panelu nie oznacza dirty mapy. Rozszerzony test zapisu/odczytu obejmuje faktycznie pomalowany heks, ikonę i instancję obiektu, a lokalny preset ma inny kolor i puste zestawy. Po przyjęciu panelu i ponownym round-tripie wszystkie wystąpienia i wymagane PNG pozostały. To nie jest eksport danych mapy do Library.

Zwykłe przełączanie w sesji nie wykonuje ponownego przyjęcia katalogu: zachowuje roboczy preset i jego quick. Ochrona draftów przed Nowa/Otwórz/MRU pozostaje. Zapis mapy nie zapisuje presetu bibliotecznego; zapis presetu nie zastępuje zapisu mapy.

## Częściowy zapis przy zamykaniu

Systemowe pytanie wymienia nazwę presetu i nie obiecuje już wszystkich sekcji. Tak otwiera istniejący dialog z **aktywnymi** switchami Tereny/Ikony/Obiekty, początkowo On. AllOff blokuje zapis z dotychczasową pomocą. W zamykaniu dodatkowy tekst wyjaśnia odrzucenie pominiętych zmian dopiero po ostatecznym zamknięciu.

Nadpisanie zachowuje pominięte sekcje z aktualnego poprawnego pliku, nowy zapis pozostawia je puste; dotychczasowy writer, walidacja i konflikty pozostają. Po udanym zapisie procedura przechodzi do następnego ID, bez ponownego pytania o ten sam preset. Pominięte sekcje nie są oznaczane jako zapisane. Późniejsze Anuluj zachowuje drafty, wcześniejsze udane zapisy zostają, kolejne zamykanie pyta ponownie. Błąd/anulowanie zapisu natychmiast zatrzymuje procedurę.

Pytanie mapy brzmi teraz: „Mapa „Nazwa” zawiera niezapisane zmiany. Czy chcesz je zapisać przed kontynuowaniem?”. Decyzje, needsFirstSave i ochrona przed porzuceniem bez zmian. Nie dodano własnego systemu MessageBox.

## Quick, kosz i ograniczenie przebudów

W0.9.0 zmiana listy usuwała i tworzyła kontrolki całej sekcji. Teraz `UpdateQuickTiles` porównuje ID, zachowuje istniejące przyciski i miniatury, usuwa tylko brakujące, tworzy tylko nowe, zmienia kolejność tylko w razie potrzeby. Przy zmianie źródła aktualizuje miniaturę z zachowaniem proporcji, także dla starych niekwadratowych zasobów. Przywracanie wyłącznie terenów nie wywołuje RefreshIconPreview/RefreshObjectPreview.

Licznik kontrolek: przy niezmienionej liście **0 usunięć /0 tworzeń**, przy przywróceniu samych terenów również **0/0**, przy usunięciu jednej pozycji **1/0**. To dowód ograniczenia przebudowy, nie pomiar płynności ani dowód braku pojedynczej migającej klatki. Sekcje używają buforowanych paneli, bez globalnej kompozycji/DoEvents.

Lewy kosz ikon/obiektów otwiera małe okno z dwiema operacjami i Anuluj. Quick jest bezpiecznym wyborem początkowym; opcja biblioteki niedostępna dla kopii bez rzeczywistego pliku. Dopiero wybór biblioteki wywołuje właściwe ostrzeżenie: brak Kosza, sesyjne Undo do300wpisów, utrata możliwości cofnięcia po Nowa/Otwórz/zamknięciu lub wypadnięciu wpisu.

Najpierw konflikt-safe przejście pliku przez istniejący LibraryFiles, dopiero po sukcesie usunięcie ID ze wszystkich zainicjalizowanych roboczych list i rezerw. Błąd pliku nie czyści sesji. Presety nie stają się dirty, zapisanych presetów nie przepisujemy, wystąpienia i źródła mapy pozostają. Przywrócenie osadzonej kopii z presetu może pokazać kafelek bez odtworzenia samodzielnego pliku.

Podczas rzeczywistej próby wykryto zależność Undo: poprzednia wersja kandydata przywracała plik, ale nie listę. W tym samym istniejącym LibraryChange dodano migawkę zainicjalizowanych list sesji; Undo/Redo odtwarza również quick, bez nowego kroku historii i bez serializacji tych list do formatu. Zależny test i zwykły końcowy EXE potwierdziły poprawkę. PPM kafelka nadal zawiera wyłącznie „Usuń z szybkiego dostępu”. Kosz presetów bez dwóch operacji. Brak usuwania zbiorczego/kaskadowego/zakazanych ID.

## Formularze, styl i symbole

| Miejsce | Wykonana lokalna zmiana |
|---|---|
| Tworzenie i nazwa roboczego presetu, zapis jako nowy | Wspólny EditorDialog.Row osadza natywny jednoliniowy TextBox o PreferredHeight i centruje go w wierszu |
| Dodawanie/edycja terenu | AppearancePoolDialog centruje naturalną wysokość pola nazwy względem etykiety; pełna szerokość dwóch etykiet switchy |
| Edycja ikony/obiektu, nazwa grafiki terenowej | Ten sam wspólny wiersz tekstowy; etykieta aktualizacji ma własny pełny jednoliniowy wiersz |
| Przywracanie, wariant bez źródła, Kadruj, biblioteka grafik, lista obiektów | Wspólny EditorButton rysuje disabled jasnym przygaszonym tekstem i neutralną ramką, bez aktywnej czerwieni |
| Edytor koloru | Aktywne Usuń punkt czerwone; disabled Usuń punkt/Losuj czytelne;25px dodatkowej wysokości i większy odstęp pod opisem kierunku |
| Rysik/wskaźnik | Właściwe osobne tytuły „Kolor rysika”/„Kolor wskaźnika” |
| Wariant terenu/masek | Kolor/Gradient/Grafika jako RadioButton o wyglądzie switcha; dokładnie jeden On, aktywna opcja nie wyłącza wszystkich |
| Grafika wariantu | Importuj/Biblioteka/Kadruj we właściwej kolejności; wspólny wiersz zmienia zawartość bez pustych pozostałości |
| Lista obiektów | DarkForm używa istniejącego wspieranego ciemnego natywnego tytułu i fallback; bez hybrydy/nowej ramki |
| Przybornik | Lokalnie czytelniejsza dłoń/pędzel, gumka z wypełnioną dolną częścią, skorygowane położenie gumki/oczu; pozostałe symbole zachowane |
| Kafelek terenu | Usunięto natywny dotted focus po kliknięciu; Tab i focus klawiatury pozostają z własnym niebieskim obrysem |

Zachowano zaznaczanie tekstu, schowek, natywne tekstowe Undo, Tab i walidację. Nie dodano globalnie dwukropków, nie zmieniano zaakceptowanego rename, ScrubNumber ani RichNoteEditor/WPF. Radio-switche mają semantykę i klawiaturę RadioButton; nie są trzema niezależnymi checkboxami. Jednolinijkowe malowanie wspólnego switcha nie zmienia zależności On+Disabled zachowania rozmieszczenia wariantów.

Świadomie pozostawiono natywne Open/Save/MessageBox, obrys ComboBox, kolory treści/próbek i obrazów. Disabled nie jest utożsamiony z Off. Zmiana nie jest kampanią przemalowywania wszystkich kontrolek aplikacji.

## Import i kwadratowy kadr

Przepływ plik→SquareImportDialog→istniejący zapis biblioteczny. Wspólne ReadImport zachowuje walidację wejścia **8MiB,2048×2048**, PNG/JPG/BMP/GIF bez animacji. Draft używa istniejącego CropTransform/CropMath.Draw, początkowo **contain całego obrazu**, nie cover. Przeciąganie LPM/MMB przesuwa, kółko skaluje, suwak obraca; capture kończy gest poprawnie.

W100% widoczny właściwy kwadrat560×560 na większej powierzchni728×640; otoczenie obrazu poza kwadratem jest widoczne i przyciemnione. Wynik to wyłącznie **512×512 ARGB PNG**. Zachowuje przezroczystość; nie maskuje heksagonalnie. Pełny bitmap źródła istnieje tylko podczas draftu i jest zwalniany z dialogiem. Zapis używa istniejącego IconAsset i budżetu PNG/zasobów; nie podnoszono limitów, nie dodawano źródła na każdy kafelek.

Anuluj nie tworzy Result ani wpisu; oryginalny plik jest tylko odczytywany. Kwadratowe kadrowanie obejmuje import ikony/obiektu i wybór nowego pliku w ich edycji. Nie jest funkcją ponownego kadrowania wszystkich już zapisanych zasobów; po zatwierdzeniu źródłem biblioteki jest wynikowy PNG. Heksagonalny TerrainCropDialog i CropMath nie zostały zmienione, więc nie ponawiano starej kampanii kadrowania terenów.

## Celowane testy metod, kontrolek i renderowania

Runner: `tests/Run-Patch091Tests.ps1 -Case <jeden jawny Case>`. Źródła testów wersjonowane, każde wyjście pod własnym `tests/_work`. Nie uruchamiano Run-AllTests ani historycznych runnerów hurtowo.

| Case | Wynik i dowód |
|---|---|
| baseline |73c8c46f9a4643699f61f785a866d544: odtworzony wybór osadzonego panelu mimo nowszego pliku biblioteki na bazie0.9.0; test metod, nie kliknięcie EXE |
| open |016c8f4fe23e4b6abcc99d30e4f0d649 PASS: obie kolejności zapisu, aktualny katalog, wspólny Open/MRU, draft, missing/corrupt fallback, brak automatycznego importu i dirty. Końcowe cc169e02862d4d35805e0f1f87cd6620 PASS rozszerza fixture o rzeczywisty pomalowany heks/ikonę/obiekt i round-trip; powtórzenie potrzebne do weryfikacji zachowania historycznych wyglądów i PNG, bez zmiany produkcji |
| quick |da8167f3f0c54b5282fa0d6cd93467ba PASS:0/1/6/7/12, brak placeholderów, zachowanie kontrolek, terrain-only0/0, usunięcie1/0, błąd konfliktu bez czyszczenia, listy wszystkich draftów, kopie presetów/mapy, restore bez odtworzenia pliku, Undo/Redo bibliotecznego usunięcia ikon i Undo obiektu |
| close |57ca5ed352704d02975734165c68e8bb PASS: aktywne sekcje/allOn/allOff, częściowe nadpisanieA→CancelB, pominięte nadal dirty, kolejna procedura pytaA, cancel/błąd zapisu zatrzymuje |
| controls |525a88c9f55d4a558d448e6ead3ad458 PASS: minimum/maksymalny układ, marginesy, krawędzie, -360°, pętla wysokości bez zewnętrznego scrollbara, centrowanie wspólnego pola, radio dokładnie jeden, Kadruj/disabled render |
| crop |a2af8e0336964257815d3620a0cef4e6 PASS: contain640×320, alpha, pan/zoom/37°, deterministyczny wynik, większy podgląd, Cancel bez Result, niezmienione bajty źródła |

Powody wcześniejszych powtórzeń: controls wykrył zbyt mały obszar liczby po paddingu oraz różnicę2px skrajnych krawędzi; poprawiono lokalny pomiar i odstępy, zależny Case PASS. Quick rozszerzono po rzeczywistym wykryciu braku przywracania list Undo. Jedna wcześniejsza porażka quick wynikała z testowego przełączenia presetu między delete i Undo (co dodawało własny krok historii); poprawiono kolejność testu, nie semantykę produkcji. Pierwsze kompilowanie testu baseline poprawiono z powodu błędnej nazwy metody inicjalizacji. Bez ponawiania niezależnych PASS po małej poprawce Undo.

## Zwykły EXE — oddzielna sesja

Własna kopia/dane: `tests/_work/patch-091-fixture-abd2542d2fcf4968b09056b3588cd0c7`. Obsługa rzeczywistych okien przez Computer Use, bez prywatnych map/Library/MRU. Użytkownik ręcznie zamknął pierwszą kopię w trakcie niezakończonego importu i to potwierdził; import nie utworzył pozycji. Wznowiono niedokończony fragment, nie uznano tego za crash.

Sprawdzono: otwarcie własnej mapy przez MRU z aktualnym presetem bibliotecznym i cleanmap; układ/marginesy, parametry Punkt/Trójkąt, rzeczywiste zmniejszenie do minimum przez systemowe Rozmiar, dostęp wszystkich sekcji bez scrollbara kolumny; pusty preset bez rzędów; import ikony z przesunięciem, skalą i obrotem133° oraz import obiektu z contain; kosz quick/biblioteka; wygląd formularza terenu, pełne etykiety switchy/disabled, trzy wykluczające radio-switche, brak źródła i disabled Kadruj/OK; gradientowe czerwone Usuń punkt i disabled Losuj; częściowe zamykanie.

W rzeczywistym zamykaniu A zapisano **tylko Ikony**, otrzymano pytanie B, wybrano Anuluj. B zachował Teren roboczyB i niezapisany status; rzeczywisty wybór A z listy pokazał Roboczy terenA i niezapisany status. Następnie oba własne drafty zapisano przed bezpiecznym zamknięciem kopii. Nie usuwano ich odpowiedzią Nie.

Po poprawce Undo zbudowano kandydata. Zależna krótka próba tej wersji: start/MRU, import własnego obiektu przez kwadratowy kadr, biblioteczne usunięcie z ostrzeżeniem, Ctrl+Z przywróciło kafelek na poprzedniej pozycji, Ctrl+Y usunęło, kolejne Ctrl+Z przywróciło. Status presetu pozostał zgodny. Własny proces zamknięty bez pozostawionych modalnych okien. Sam plik po Undo potwierdzony odczytem katalogu testowego i niezależnymi asercjami metod.

Końcowy przegląd ścieżki różnicowej wykrył rozciąganie niekwadratowego obrazu przy podmianie źródła na istniejącym przycisku. Zachowano proporcje i przezroczyste marginesy. Zależny Case quick da8167… dodał asercję obrazu16×32 i zachowania tej samej kontrolki; PASS. Po tym jedynym wpływającym szczególe ponowiono build i krótki zwykły start/MRU/panel/zamknięcie na nowych bajtach, bez ponawiania całej sesji i bez nowych zmian produkcji.

Kopia końcowa i główny EXE mają identyczny SHA-256:
`A1FF9F96D0CAE6EA0D64430B1CFB353B940F5387BC68A7C39396515DC030A61D`.
Wersje obu0.9.1.0. Główny EXE opublikowano atomowo z przetestowanych bajtów, bez ponownej kompilacji zmieniającej kopię. Pierwsze wywołanie publikacji miało błędnie przekazany pusty parametr backupPath i zachowało0.9.0; ponowiono z poprawnym NullString, porównanie hashów PASS. Brak pozostawionego niepewnego podmieniania.

## Ograniczenia i niewykonane próby

- Stanowisko rzeczywiste:2560×1440, GraphicsDpiX96 (**100%**). Sprawdzono geometrię okna/minimum mieszczącą się na1920×1080 oraz reprezentatywną obsługę przy minimum. **Nie było fizycznego testu monitora FullHD** ani pełnej macierzy DPI; do ręcznego odbioru na docelowym stanowisku pozostaje wrażenie wizualne FullHD100%.
- Brak nagrania klatek: nie deklarujemy braku pojedynczego flasha ani płynności ze screenshotu. Potwierdzono ograniczenie usuwania/tworzenia kontrolek i oddzielono je od oceny wizualnej. Nie mierzono FPS.
- Nie klikano każdej instancji wspólnego wiersza nazwy/disabled ani każdej kombinacji dialogów. Ich wspólną regułę sprawdzono kontrolkami i reprezentatywnie w EXE. Nie odtwarzano modalnego ostrzeżenia uszkodzonego presetu w zwykłym EXE; loader/warning/fallback pokrywa Case open. Złożone błędy pliku i kilka draftów pokrywają metody/hooki, a częściowe nadpisanie+Cancel również rzeczywiste okno.
- Nie wykonywano osobnej zwykłej sesji częściowego nowego zapisu przy zamykaniu: ten sam istniejący writer i aktywny dialog zachowują zasadę pustych pominiętych sekcji. Nie powtarzano wcześniejszego pełnego zestawu zapisów presetów.
- Natywny obrys ComboBox pozostał; brak nowej kontrolki tylko dla koloru ramki. Wspierane ciemne obramowanie listy obiektów ma dotychczasowy fallback, bez fizycznego sprawdzania niewspieranego Windows.
- Bez kampanii geometrii figur, ramki hybrydowej,5-min autosave, rename-conflict, WPF/gumki, masek i przenośności. Bez prywatnych danych. To celowana weryfikacja0.9.1, **nie pełna regresja aplikacji**.

## Zakres i krótka checklista odbiorcza

| Punkty zlecenia | Stan |
|---|---|
|1–3: zasady, odebrana baza,100% | Zachowano bazę i ograniczenia; fizyczne FullHD ma powyższe ograniczenie stanowiska |
|4–8: szerokość, wysokość, parametry, presety, puste rzędy | Wykonane i sprawdzone metodami/kontrolkami oraz reprezentatywnie w EXE |
|9: ograniczenie flasha | Różnicowe kontrolki/miniatury i selektywne restore; brak dowodu wideo pojedynczej klatki |
|10: źródło panelu | Biblioteka poID / osadzony fallback / odrębny draft; test treści i round-trip |
|11: zamykanie | Aktywne sekcje, częściowy sukces, pominięte drafty, późniejsze Cancel, nazwane pytanie mapy |
|12: kosz | Lewy wybór dwóch operacji, PPM tylkoquick, konflikt-safe, zachowane kopie i Undo |
|13–17: symbole, focus, formularze, disabled, tytuły i wariant | Lokalne poprawki, bez zmiany semantyki koloru/historii |
|18: kadr importu | Kwadratowy wspólny draft ikon/obiektów, alpha/contain/pan/skala/obrót/Cancel |
|19: lista obiektów | Istniejący ciemny natywny tytuł/fallback i czytelny disabled, bez reworku listy |
|20: odłożone | Brak0.9.2, instalatora/aktualizatora, nowych starterów, zakładek/ustawień, migracji Library; kolejność planu zaktualizowana |
|21–22: testy i wydanie | Jawne Case, zwykły EXE, porównanie bajtów, dokumentacja i jeden lokalny commit |

Do ręcznego odbioru użytkownika:

- [ ] FullHD100%: równe krawędzie/marginesy, najbogatszy układ przy minimum, resize wysokości bez scrollbara kolumny.
- [ ] Zestawy0/1/6/7/12, brak pustych ramek, względne ograniczenie flasha podczas pojedynczych zmian i restore tylko terenów.
- [ ] Biblioteczny preset zasila panel po otwarciu; mapowe wystąpienia zachowują wygląd; brak pliku daje fallback.
- [ ] Częściowe zamykanieA→B→Anuluj zachowuje pominięte drafty, kolejne zamykanie pyta ponownie.
- [ ] Kosz quick/biblioteka, właściwe ostrzeżenie i Undo; PPM wyłączniequick.
- [ ] Czytelna dłoń/pędzel/gumka/oczy, focus klawiatury terenu, nazwy i pełne etykiety; disabled i czerwone usuwanie.
- [ ] Radio-wariant dokładnie jeden; import ikony/obiektu z kwadratowym kadrem, alpha i Anuluj.

Plan/backlog zachowuje dalsze0.9.x; zbiorcze usuwanie0.9.2, prawy panel0.10, zakładki0.11, instalacja/aktualizator0.12, źródła/kolejka0.13. Pozostałe wcześniejsze pomysły zachowane, bez wdrożenia.

## Commit i Git

Jeden lokalny commit wydania z angielskim tytułem; jego identyfikator można odczytać przez `git log -1 -- docs/raport-0.9.1.md` i podano go w końcowej odpowiedzi. Przed commitem sprawdzono diff i brak danych `_work`/Library/MRU w indeksie; po commicie sprawdzany jest czysty Git. Wyniki robocze nie są częścią wydania. Nie zamykano okien użytkownika; własne procesy testowe zakończone.
