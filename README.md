# HexMapEditor 0.10.0

Samodzielny edytor map heksagonalnych Windows: C# / Windows Forms / GDI+ i osadzony edytor opisu WPF z lokalnego .NET Framework. Offline, bez usług i pobierania zależności/zasobów przy uruchamianiu. Rozwijamy istniejący projekt.

## Uruchamianie

Uruchom HexMapEditor.exe albo Uruchom HexMapEditor.bat. Wymagany pełny .NET Framework 4.x z WPF (zalecany 4.8). Aplikacja **0.10.0** zapisuje mapy w **formacie 12**, presety w formacie **5**, ikony biblioteczne w **2**, a wzorce obiektów w **1**.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Build.ps1
```

Build.ps1 kompiluje src/*.cs lokalnym kompilatorem. Library pozostaje obok EXE, niezależnie od katalogu roboczego. Główny EXE jest śledzony. Grafiki startowe i symbole interfejsu są rysowane lokalnie, bez zewnętrznych zestawów.

Ta wersja odczytuje mapy 12 i presety 4/5. Starszą mapę odrzuca z komunikatem o wersji formatu, pozostawiając plik na dysku. Brak sekcji ikon/obiektów w presecie 4 oznacza puste zestawy. Biblioteczne grafiki terenu mają format 2.

## Prawy panel w 0.10.0

Trzy widoki pokazują heks, pojedynczy obiekt albo grupę. Pionowy uchwyt zwija szczegóły z zachowaniem zaznaczenia. „Dostosuj” na górnym pasku ustawia domyślną szerokość, a „Zamknij” czyści zaznaczenie. Ołówek otwiera edycję nazwy w miejscu.

Opis heksa można powiększyć w panelu. Obrazy wstawione z pliku lub osadzone we wklejonej treści zapisują się razem z mapą. Obsługiwane reprezentacje schowka obejmują sam obraz, RTF z PNG/JPEG i HTML z danymi obrazu. Osobne polecenie wkleja sam tekst.

W macierzy grupy każdy członek osobno wybiera wartość indywidualną albo grupową dla stanu, rozmiaru, mgły, widzenia i blokady. Przy odłączeniu lub rozwiązaniu grupy można zachować wybrane wartości wynikowe. [Raport 0.10.0](docs/raport-0.10.0.md).

## Szybkie zestawy presetów w 0.9.3

Import lub jawne usunięcie z szybkiego dostępu sygnalizuje niezapisane zmiany, gdy zmienia zawartość widocznej sekcji Ikony lub Obiekty. Status, przycisk zapisu i pytanie przed zamknięciem wskazują ten sam roboczy preset. Zapis częściowy rozlicza tylko zapisane sekcje. Sama kolejność, użycie zasobu oraz automatyczne czyszczenie po usunięciu z biblioteki zachowują dotychczasowe zachowanie. [Raport 0.9.3](docs/raport-0.9.3.md).

## Poprawki bibliotek i kadru 0.9.2

Biblioteki ikon i obiektów mają Importuj / Edytuj / Usuń / Zamknij. Klik zaznacza do zarządzania, dwuklik lub Enter używa zasobu; Zamknij nie zatwierdza przypadkowego wyboru. Edycja zachowuje ID, pozwala zmienić nazwę i podmienić plik przez istniejące kadrowanie. Usuwanie korzysta ze wspólnej, konflikt-safe historii sesji, bez usuwania wystąpień mapy lub osadzonych kopii presetów.

Ponowny import z przeciwnej biblioteki używa trwałego odpowiednika według ID źródła, również po restarcie. Nie nadpisuje ręcznej edycji odpowiednika; dawne duplikaty nie są scalane. Wiadro ikon zatrzymuje się na granicy terenu i innego stanu ikony: puste pola, obszar tej samej innej ikony albo usuwanie wybranej ikony. Przycisk zapisu presetu świeci wspólnym niebieskim stylem tylko dla niezapisanego bieżącego presetu, niezależnie od zapisu mapy.

Kwadratowy import ikon/obiektów ma kompaktowy dialog i „Obrót 0°” zerujący wyłącznie kąt. Kadr terenu pokazuje szachownicę i pełne przyciemnione otoczenie z niebieskim obrysem heksa. Zmianę istniejącej grafiki terenu można nadpisać, zapisać jako nową z własną nazwą albo anulować; wynik pozostaje wybrany. Osobna operacja biblioteczna nie przemalowuje mapy. Edycja terenu nadal stosuje dotychczasowy switch aktualizacji mapy. Okno wariantów domyślnie mieści pełne5×2 kafelki. Scrollbary pozostają natywne.

Wyniki i ograniczenia celowanych prób: [raport0.9.2](docs/raport-0.9.2.md). Bez multizaznaczania, zbiorczego usuwania, migracji ani zmiany struktury Library.

## Lewy panel i presety 0.9.1

Przybornik ma układ 4×3. Szybkie zestawy ikon i obiektów mają do dwóch rzędów po sześć kafelków: puste rzędy znikają, bez ramek placeholderów. Stały panel 276 px plus uchwyt 18 px nie ma scrollbara całej kolumny; przewija się tylko lista terenów. Parametry figur oraz rozmiar rysika/gumki są w pojedynczych rzędach. Narzędzia, materiały i zaznaczenia używają wspólnego niebieskiego ChoiceStyle. Lista terenów pozostaje pełna; dodatkowa wysokość okna powiększa przede wszystkim ją. Panel można schować, nie ma użytkowego splittera.

Figury: Punkt, Linia, Koło, Trójkąt, Kwadrat, Prostokąt, Pięciokąt, Heksagon i Oktagon. Rozmiar 1–100; prostokąt ma A/B, trójkąt kąt 1–179° (domyślnie 60°), a obrót całkowite stopnie w prawo, wokół środka. Wpis 400° daje 40°, -390° daje -30°, a niezerowe pełne obroty zachowują znak ±360°. Ustawienia są sesyjne, z jednym krokiem Undo na wpis/drag i bez dirty mapy. Rysik/gumka pozostają 1–200, domyślnie 5.

Preset obejmuje tereny i do 12 startowych ikon oraz 12 obiektów. Nowy preset jest pusty; chroniony Domyślny dostarcza podstawowe tereny i zestawy 12+12. Ręczny zapis pozwala wybrać sekcje: nowy plik ma pozostałe puste, a nadpisanie zachowuje pozostałe z aktualnego pliku. Przywracanie odczytuje wybrane sekcje z dysku i nie przemalowuje heksów. Robocze wersje presetów są niezależne w sesji, ich zmiany nie są dirty mapy. Zamykanie pyta najpierw o mapę, potem o każdy niezapisany preset; zapis przy zamykaniu pozwala wybrać sekcje, a Anuluj przerywa całą procedurę. Pominięte zmiany pozostają w pamięci po późniejszym anulowaniu zamykania.

Każda lista szybkiego dostępu ma do 24 unikalnych pozycji, z których pierwsze 12 jest widoczne i trafia do presetu. Sam wybór nie przestawia listy; udane użycie pozycji z rezerwy lub dodanie/import nowej przenosi ją na początek. Użycie widocznej pozycji zachowuje kolejność. PPM → „Usuń z szybkiego dostępu” usuwa tylko pozycję listy; pliki i wystąpienia na mapie pozostają. Historia szybkiego dostępu sama nie oznacza niezapisanej zmiany presetu.

Przy otwieraniu mapy panel korzysta z aktualnego poprawnego presetu bibliotecznego o tym samym ID. W razie braku/uszkodzenia pliku pozostają dane osadzone, bez automatycznego importu do Library. Wyglądy pomalowanych heksów i istniejących instancji nie są zmieniane. Przełączanie presetów w sesji zachowuje robocze drafty.

Lewy klik kosza ikon/obiektów daje wybór: usuń z szybkiego dostępu albo z biblioteki. Dopiero druga opcja ma ostrzeżenie o pliku i granicach sesyjnego Undo. Plik biblioteki jest zmieniany z ochroną konfliktów; wystąpienia mapy i osadzone kopie presetów pozostają. PPM kafelka nadal usuwa wyłącznie z szybkiego dostępu. Niezmienione kafelki są ponownie wykorzystywane, a przywrócenie samych terenów nie przebudowuje zestawów ikon/obiektów.

Import ikony/obiektu z pliku otwiera kwadratowe kadrowanie: początkowo cały obraz, przeciąganie, kółko skali, obrót, przezroczystość i większy podgląd z przyciemnionym otoczeniem. Zapisuje tylko kwadrat 512×512; Anuluj nie tworzy wpisu i nie zmienia oryginału. Wariant terenu wybiera się grupą radio-switchy Kolor / Gradient / Grafika. Dotykane formularze mają wyśrodkowane pionowo pola nazw, pełne etykiety switchy i czytelne disabled.
Zoom 12800% i długa statyczna nazwa w rename mają jawny jednoliniowy render z pionowym centrowaniem; reset i dropdown pozostają osobnymi obszarami.

## Start i jakość podglądu — zachowane zasady

Edytor ma dwa stałe wiersze nad całym obszarem pracy: **Plik / Edycja / Mapa / Widok / Biblioteka / Pomoc** oraz kompaktowe ikony Nowa/Otwórz/Zapisz, Cofnij/Ponów, zoom, dwa dopasowania i High/Low. Menu jest zintegrowane z hybrydowym obszarem tytułu: ikona i nazwa aplikacji, sześć menu, nazwa mapy oraz natywne przyciski Windows. Nazwa mapy jest centrowana względem całego okna, ograniczana bez kolizji i skracana; rzeczywisty systemowy tytuł zachowuje nazwę i gwiazdkę. Wolny obszar po menu i nazwa mapy służą do przeciągania. Drugi wiersz to dotychczasowy szybki pasek po lewej. Zapis z niezapisanymi zmianami i aktywny Low mają wspólny zaakceptowany niebieski styl Jednolity/Gradient. Przy braku miejsca szybki pasek ma overflow, bez trzeciego wiersza. Stałe minimum dla pełnego lewego panelu wynosi 1180×806 przy 100%; nie zależy od wybranej figury ani liczby kafelków.

**Edycja → Zmień nazwę pliku…** przenosi aktualny plik w tym samym folderze, bez kopii i zapisywania zmian z pamięci. Kolizja nie nadpisuje danych. Po sukcesie tytuł/MRU i przyszły autozapis używają nowej ścieżki; dirty pozostaje. Dokument bez pliku musi najpierw zostać zapisany. **Biblioteka** otwiera wyłącznie rzeczywiste foldery w Eksploratorze; selektory zasobów pozostały w odpowiednich edytorach.

Pasek zoomu ma dwa obszary: procent ustawia 100% ze środkiem widoku, a strzałka otwiera i zamyka listę wartości. Wybranie terenu, ikony lub obiektu przełącza na pędzel tylko z narzędzi mgły i ograniczenia widoczności; pozostałe narzędzie pozostaje aktywne. Jasne strzałki podmenu poprawiają czytelność na ciemnym tle.

Własne pola liczbowe: klik rozpoczyna wpis, poziomy drag po przekroczeniu progu zmienia wartość; Enter zatwierdza liczbę, Escape przywraca początek edycji/gestu, Tab waliduje i przechodzi dalej. Bez spinnerów. Narzędzia zachowują 8 logicznych px na krok, rysik/gumka 1–200, domyślnie5. Przecinek, kropka i Decimal numpada są separatorami dziesiętnymi. Wpis stosuje się po zatwierdzeniu lub kliknięciu poza polem: zaokrąglenie połówek od zera do precyzji pola, potem min–max. Pusty/niekompletny wpis przywraca poprzednią wartość; niedozwolona wklejka jest odrzucana w całości. Liczby pozostają po prawej. Jeden drag/wpis ustawienia rozmiaru narzędzia to jeden krok wspólnej historii, bez dirty ani rewizji autozapisu. Dialog koloru ma własne sesyjne Undo RGB/HEX/HSV, także po zakończeniu wpisu. Cancel/no-op bez wpisu. Suwaki graficzne kadru/efektów i WPF nie są zastępowane.

Zwykły start pokazuje istniejącą ikonę, wersję, Otwórz projekt, Stwórz nowy projekt oraz do5 dostępnych ostatnich map. Okno ma stały rozmiar, a wycentrowana lista ostatnich map około połowy szerokości. Anulowanie lub błąd pozostawia ekran startowy. Można przekazać ścieżkę `.hexmap` jako argument EXE — walidowana mapa omija start, bez chwilowego pustego projektu. Library jest odczytywana dopiero po wyborze projektu. Jedynie host opisu WPF przygotowuje się raz przed pętlą okien; nie ładuje bibliotek ani obrazów na zapas. Start zamyka się dopiero po skutecznym pokazaniu edytora.

**Nowy projekt ze startu**: po nazwie i parametrach program tworzy `Library/Hexmaps/<nazwa>.hexmap` obok EXE, bez dialogu wyboru ścieżki. Kolizja nazwy (również różniącej się tylko wielkością liter) wymaga poprawienia nazwy; parametry pozostają, bez nadpisania i automatycznych sufiksów. Błąd pozostawia działający start. Po sukcesie projekt ma plik, czysty stan, wpis MRU i gotowe Ctrl+S.

**Nowa z edytora**: dokument pozostaje w pamięci, bez pliku, ścieżki i nowego MRU. Gwiazdka i pytanie przy porzuceniu dotyczą także nietkniętej nowej mapy. Pierwsze Zapisz/Ctrl+S otwiera dotychczasowy dialog; Anuluj zachowuje mapę. Udany zapis przypisuje ścieżkę, a kolejne Ctrl+S zapisuje bez dialogu. Zapisz jako nadal działa, z potwierdzeniem nadpisania.

Start pokazuje `HexMapEditor by amdesar` oraz drugi wiersz `Wersja 0.10.0 • 20.11.2026`. Duża nazwa zachowuje styl, autor ma styl napisu wersji. Data wydania pochodzi z jawnej metadanej w `src/ReleaseMetadata.cs`.

`recent-projects.json` **obok EXE**, poza Library, przechowuje do30 pełnych ścieżek (do256KiB). Zapis atomowy po udanym otwarciu/zapisie; missing nie jest usuwany z historii. Dostępność jest sprawdzana w tle; niedostępny nośnik/sieć nie blokuje ekranu. Uszkodzony/niedostępny plik historii nie blokuje pracy. Nie zawiera map ani zasobów.

**Autozapis co 5 minut** dotyczy tylko zmienionej mapy z przypisaną ścieżką. Zapisuje bieżący plik, bez backupów i dialogu sukcesu. Udany zapis ręczny/automatyczny oraz otwarcie/utworzenie pliku zaczyna nowy okres; samo malowanie nie resetuje licznika. Gest, modalny edytor, gumka lub aktywna sesja edycji odracza jedno żądanie do bezpiecznego stanu. Mapa bez ścieżki nigdy nie dostaje automatycznego pliku.

Wynik przygotowuje jeden worker na odłączonej migawce. Podczas zapisu konfliktujące polecenia dostają informację w statusie; Escape lub próba zamknięcia anuluje oczekujące przygotowanie (po zakończeniu ponów zamknięcie). Nie przerywa się połowy publikacji pliku. Sukces pokazuje „Zapisano automatycznie”, zachowuje Undo i nie przebudowuje paneli. Błąd pozostawia niezapisane dane i kolejną próbę najwcześniej po 5 minutach. Obca zmiana lub zniknięcie pliku wstrzymuje autozapis — rozwiąż konflikt ręcznie albo użyj Zapisz jako. Ochrona porównuje bajty pliku przez SHA-256; nie jest systemem odzyskiwania po awarii dysku lub zasilania.

Przycisk **High / Low** (także Widok → Jakość) dotyczy tylko sesji. High jest domyślny i wraca po udanym Nowa/Otwórz. Low pomija tło, pokazuje średni kolor właściwego kadru/gradientu heksa z zachowaniem pokrycia alpha oraz pierwszy kolor gradientowej kreski/wskaźnika. Zachowuje dane, fazę, geometrię, maski i flagi Widoku. Powrót do High przywraca pełny wygląd, także kreski narysowanej w Low. Nie zapisuje jakości ani dirty/historii; edytory nadal pokazują pełne źródła.

Pickery współdzielą **12 kolorów sesji** w dwóch rzędach po6. Zatwierdzenie jednolitego koloru dodaje go na początek bez duplikatów; gradient i Anuluj niczego nie dopisują. Próbka może zmienić aktywny punkt gradientu. Restart zeruje pulę; brak pliku i wpisów głównej historii.

Gumka przygotowuje dokładny wynik poza UI na odłączonej migawce. Puste trafienia, także wewnątrz otworu, odrzuca test powierzchni bez uruchamiania przetwarzania. Podczas pracy konfliktujące polecenia są blokowane logicznie z informacją, bez wygaszania całych drzew kontrolek. Pasek **Przetwarzanie…** pojawia się dopiero po180ms, bez przesuwania layoutu; można anulować lub zamknąć program. Podgląd wymazania pozostaje do przejęcia przez dokładny wynik. Wynik jest stosowany atomowo do zgodnego dokumentu jako jeden krok historii. Anulowanie/błąd przywraca oryginał. Pan mapy wykorzystuje poprzedni cache i dorysowuje odsłonięte pasy. Przy serii zoomu oddzielny ograniczony cache przejściowej klatki pomija powtarzane przeliczanie identycznego podglądu; po120ms od końca serii powstaje dokładny obraz. Nie zmienia zapisanych kolorów i powierzchni.

[Plan wydań](docs/plan-wersji.md), [raport 0.9.1](docs/raport-0.9.1.md). Testy były celowane; raport oddziela metody od zwykłego EXE i wskazuje niewykonane próby.

Binarne opcje używają wspólnego kompaktowego switcha z czytelnym On/Off/Disabled. Stempel terenów, ikon i masek może mieć środek poza siatką; wynik jest przecięciem kształtu z istniejącymi heksami. Wiadro nadal wymaga heksa początkowego.

## Podmiana ikony programu

Zamknij HexMapEditor, podmień plik `assets/app/icon.ico`, uruchom `Rebuild-Program.bat` z katalogu głównego, a następnie uruchom program. BAT korzysta z `Build.ps1`, źródeł `src` i lokalnego kompilatora .NET Framework. Parametr `/nopause` wyłącza oczekiwanie na klawisz przy uruchomieniu z konsoli. Nowy EXE zastępuje stary po udanej kompilacji.

Okno odczytuje ICO z katalogu programu. Przy braku lub błędzie odczytu używa ikony osadzonej w EXE. Ikona samego pliku w Eksploratorze wymaga przebudowy. Windows może przez pewien czas pokazywać ikonę z pamięci podręcznej.

## Widok warstw

Menu **Widok → Warstwy** przełącza kolejno Tło, Tereny, Ikony, Obiekty, Ograniczenie widoczności, Mgłę i Rysik. Można zmienić kilka pozycji przed zamknięciem menu. Ukryty teren przyjmuje neutralny kolor. Wyłączenie obiektów usuwa je z rysunku i hit-testu. Wyłączenie mgły pokazuje dane pod spodem. Flagi są chwilowe, a Nowa/Otwórz przywracają pełną widoczność. Próba edycji ukrytej warstwy pozwala ją pokazać. Po zgodzie ponów operację.

## Rysik, gumka, ograniczenie widoczności i pole widzenia

Rysik i Gumka mają **jeden wspólny rozmiar ekranowy 1–200 logicznych pikseli (wartości całkowite)**, domyślnie **5**. Nowy gest przelicza wartość przez zoom i DPI na geometrię świata. Gotowa kreska skaluje się potem z mapą; kolejny gest przy innym zoomie znów ma ustawioną szerokość ekranową. Obrys odpowiada narzędziu. Zmiana kamery kończy rozpoczęty fragment. Mały uchwyt przy liczbie zmienia wartość poziomym przeciąganiem, bez samoczynnego przyspieszania: 8 px = 1; przy wspólnym rozmiarze pędzla/mgły/ograniczenia widoczności 8 px = 1.

Rysik LPM rysuje kreskę świata również poza siatką. Kolor wybiera wspólny picker. PPM to osobny chwilowy wskaźnik: pełne kolorowe koło bez obwódki (20 px przy DPI96), zwężająca się kometa do **750 ms** i najwyżej128 próbek. Kursor ukrywa się tylko na płótnie podczas PPM i wraca po zakończeniu/utracie kontekstu. Żywe kliknięcia mogą się łączyć, wygasła smuga nie daje starego łącznika. Brak zapisu, dirty, historii i wpływu na Dopasuj. Timer działa tylko przy aktywnym wskaźniku lub smudze. Kółko zoomuje, MMB przesuwa widok.

Gumka odejmuje faktyczną powierzchnię koła (klik) lub ciągłego pasa kół (drag) od wszystkich przecinanych rysunków. Nie usuwa osi ani całych warstw kreski poza tym obszarem. Powstają wektorowe kontury z otworami, przez które widać rzeczywiste warstwy. Położenie, zapis i dokładne Undo/Redo zachowują powierzchnię; Dopasuj uwzględnia pozostały rysunek. PPM czyści wszystkie rysunki z jednym Undo; pusta warstwa niczego nie dopisuje. Jeden lewy gest rysika/gumki to jeden wpis. Limity nadal5000 fragmentów,200000 punktów dokumentu,10000 punktów fragmentu i2 mln współdzielonych punktów historii; stos300. Grubość zapisanej kreski to dodatnia skończona wartość świata, może przekraczać200 po przeliczeniu oddalenia; walidacja dopuszcza do2 mld jednostek (200 px przy minimalnym zoomie i DPI do960), bez zwiększania liczby punktów.


Ograniczenie widoczności i Wiadro ograniczenia widoczności zapisują **bazową** maskę na heksach. Nowa mapa jest jasna. Kształty i tryb nakładania/usuwania działają jak przy mgle: tryb z pierwszego pola, odwiedzone pola nie przełączają się ponownie. Wiadro wymaga spójnego rodzaju/wersji terenu oraz początkowego stanu bazowego ograniczenia widoczności. PPM przycisku narzędzia → Wygląd ograniczenia widoczności: wspólna pula kolor/grafika, lokalne Undo100 i jawna aktualizacja z zachowaniem rozmieszczenia. Domyślna czerń #000000. Wybrany wariant jest utrwalony; grafika zachowuje alpha, co nie ujawnia danych logicznie.

Instancja ma niezależne **Pole widzenia** i promień 0–20. Działa dla każdego stanu, niezależnie od odkrywania Fog. Promień0 to heks środka,1 to sześciu sąsiadów; źródło poza mapą może sięgać do istniejących heksów. Wzorzec przechowuje ustawienia początkowe nowych instancji (domyślnie wyłączone); jego checkbox aktualizacji mapy nadal dotyczy grafiki. Grupa ma opcjonalne nadpisania pola widzenia i promienia; Indywidualnie zachowuje bazę członków, źródła nadal znajdują się na członkach. Odłączanie pozwala zachować/przywrócić również te dwie właściwości.

Efektywne ograniczenie widoczności to baza bez sumy aktualnych zasięgów pola widzenia. Ruch/wyłączenie/usunięcie źródła przywraca wynik **aktualnej bazy**, bez odtwarzania dawnych snapshotów lub losowania. Ręczna zmiana bazy podczas oświetlenia pozostaje. Pole widzenia nie odkrywa Fog. Wyłączone w Widoku Obiekty nie uczestniczą chwilowo w odsłanianiu maski; ponowne pokazanie przywraca zasięgi z aktualnych ustawień, bez zmiany bazy lub odkrytego Fog. Wynik jest obliczany po zmianach i odczycie, nie w pętli co klatkę.

Widok pozwala niezależnie wyłączyć logiczną maskę Fog i Ograniczenia widoczności. Gdy obie są widoczne, maskowanie używa geometrycznej sumy ich heksów bez podwójnego liczenia. Przy najwyżej50% widocznego prostokąta obiektu lista/szczegóły maskują dane. Kolumny Pole widzenia i Widoczność pokazują efektywne ustawienia oraz procent ciemnej powierzchni; maskowany wpis nie zdradza parametrów źródła. Przezroczystość grafik nie zmienia tej geometrii. Kreski i wskaźnik są nad obiema maskami, wewnątrz płótna. Próba edycji ukrytej warstwy wymaga pokazania jej i ponowienia gestu.

We wszystkich oknach Ctrl+Z cofa, Ctrl+Y ponawia. Ctrl+Shift+Z nie wykonuje operacji. Historie tekstowe i lokalne historie puli zachowują swoje granice.

Przycisk **Biblioteka grafik** obok Dodaj teren otwiera zarządzanie istniejącymi grafikami, także na Domyślnym. Nie przypisuje wybranego zasobu do terenu. Wybór z edytora wariantu pozostaje osobnym kontekstem tego samego okna.

## Pule wyglądu, źródła i kadrowanie

Nowa mapa i rozszerzane brzegi mają stan **Niepomalowany**, niezależny od neutralnego terenu presetu, choć oba mogą wyglądać identycznie. Aktualizacja neutralnego zmienia tylko faktycznie pomalowane nim pola. Wiadro rozróżnia te regiony. Odczyt nie zgaduje po kolorze, które dawne pola były puste; ich zapisane ID pozostają.

W edycji własnego terenu kliknij kwadratowy kafelek wariantu albo taki sam plus. Pula ma 1–32 kolory, grafiki lub ich mieszankę. Widoczny czerwony × usuwa pozycję bez pytania, poza ostatnią; prawy klik korzysta z tej samej operacji. Lokalne przyciski Cofnij/Ponów oraz Ctrl+Z i Ctrl+Y obejmują 100 roboczych operacji tego edytora, także kadrowanie i usunięte ID. Pole nazwy zachowuje tekstowe Undo, a aktywny dialog potomny własne skróty. Ta historia nie cofa plików biblioteki. Zatwierdzenie przekazuje jedną zmianę dokumentu; Anuluj odrzuca pulę, ponowne otwarcie ma pustą historię. Ołówek Domyślnego nadal wyjaśnia ochronę wzorca.

Zmiany puli dotyczą przyszłego malowania; „Zastosuj do bieżącej mapy” świadomie aktualizuje pola edytowanej wersji. Domyślnie zaznaczone „Zachowaj rozmieszczenie wariantów” przypisuje zmiany po stabilnych ID, również po zmianie typu lub kolejności. Dodany wariant nie zajmuje starych pól; usunięty zachowuje na nich historyczną migawkę i zasoby. Wyłączenie zachowania układu pozwala jawnie przelosować pasujące pola; Redo odtwarza wynik.

Pędzel i Wiadro niezależnie losują pozycję z równym prawdopodobieństwem dla każdego malowanego heksa. Powtórzone identyczne grafiki pozostają osobnymi losami z własnymi ID. Wynik zostaje w heksie; odrysowanie i Redo nie losują go ponownie, a ponowna wizyta w jednym geście nie zmienia wyboru. Region Wiadra wymaga tego samego TerrainId i zastosowanej wersji VariantId. Losowane wyglądy jednej wersji nie rozbijają regionu, ale nowsza wersja może tworzyć przegrodę. Wiadro mgły dodatkowo respektuje początkowy Fog. Sama nazwa i kolejność puli nie tworzą nowej wersji powierzchni; zmiana jej zawartości tak.

Import zachowuje pełne, oryginalne bajty nieruchomego PNG/JPEG/BMP/GIF do 32 MiB i 4096 × 4096. Jeden większy podgląd przełączasz między Pointy Top i Flat Top; startuje w orientacji mapy, której ten przełącznik nie zmienia. Przeciągaj obraz, używaj kółka i suwaka 0–360°/resetu. W nowym imporcie zmiany aktualizują oba zestawy parametrów do pierwszego przełączenia orientacji. To przełączenie rozłącza je bez resetu; później pozostają niezależne. Ponowna edycja zapisanej grafiki jest niezależna od początku, także przy dawnym Linked. Nie ma ponownego łączenia ani automatycznego obrotu. „Kadruj…” otwiera pełne osadzone źródło i obie transformacje. Renderuje się wyłącznie wybrany podgląd z bufora do 1024 px; źródło i wynik zapisu zachowują pełne dane. Anulowanie nie zmienia wariantu, a OK bez zmiany parametrów nie tworzy kroku.

Przycisk terenu pokazuje stabilną mozaikę regularnych miniheksów z dodatkowymi przyciętymi rzędami poza krawędzią, pokrywającą cały prostokąt, do 12 różnych reprezentantów puli. Małe pule pokazują każdą pozycję możliwie równo (różnica najwyżej jednego miniheksa na pozycję), ze stabilnym przemieszaniem. Większe mają stabilny podzbiór. Obrysów miniheksów nie rysujemy; identyczne grafiki w osobnych pozycjach nadal liczą się oddzielnie. Nazwa, hover, zaznaczenie i repaint nie losują układu ani nie zużywają generatora malowania. Orientacja wybiera właściwy kadr. Tooltip podaje pełną liczbę wariantów.

Biblioteka: `Library/Terrains/Graphics/terrain-Nazwa.json` zawiera logiczne ID, nazwę, kolejność i oba kadry; `Sources/source-SHA256.json` współdzieli pełne źródło. Mapa i preset zawierają własne deduplikowane tabele potrzebnych źródeł/definicji, także dla palet projektu. Cache nie jest zapisywany. Edycja biblioteki nie zmienia historycznie namalowanego wariantu. SHA-256 wskazuje kandydatów, potem sprawdzane są bajty i parametry. Identyczna pozycja daje wybór Użyj istniejącej / Importuj jako nową / Anuluj. Nowa pozycja ma niezależne ID, współdzieląc źródło. Zatwierdzony import pozostaje operacją biblioteczną nawet po późniejszym anulowaniu terenu.

Limity źródeł: 64 obrazy i 96 MiB bajtów łącznie na dokument/preset, 64 obrazy i 192 MiB na magazyn biblioteki (także nieaktywne); do 256 definicji kadru, 128 aktywnych wpisów biblioteki. Katalog biblioteki zwalnia pełne bajty po walidacji; wybór przypina niezależne dane do dokumentu. Magazyn Sources zachowuje niezmienne źródła również po Undo/usunięciu pozycji; nie czyścimy danych automatycznie. Po osiągnięciu limitu nowy import daje błąd. Mapa do 256 MiB, preset do 160 MiB, z zapasem na Base64 i metadane. Wyniki kadrów to odbudowywalne 256 × 256; cache LRU do 256 kafelków (64 MiB) łącznie dla kluczy obu orientacji. Limity bajtów źródła dotyczą plików skompresowanych, nie pamięci bitmap: obraz 4096² RGBA to około 64 MiB. Grafiki nie są dekodowane dla każdego heksa w każdej klatce. Pełna tabela limitów i wyniki prób: `docs/raport-0.5.3.md`.

Picker terenu, stanu i ramki pozostaje wspólnym HSV/HEX: `#RRGGBB`, sześć cyfr, stare/nowe, Zatwierdź/Anuluj.

## Wygląd mgły i statyczne tło

Prawy klik narzędzia mgły → **Wygląd mgły…** otwiera ten sam edytor puli i lokalną historię. Domyślnie jest dotychczasowa szarość. Nałożenie wybiera i zapamiętuje wariant. Grafika zachowuje alpha **bez nieprzezroczystej bazy**: warstwy pod nią mogą prześwitywać. Pole nadal ma logiczny Fog, więc alpha nie ujawnia danych w szczegółach/listach ani nie zmienia hit-testu i reguł odkrywania. Nie analizujemy pikseli do maskowania. Domyślnie wyłączone „Zastosuj wygląd do obecnie zasłoniętych heksów” zmienia wyłącznie istniejący wygląd w jednym Undo. Zależna opcja zachowania rozmieszczenia działa po ID jak dla terenu, a usunięta pozycja pozostawia stare migawki. Aktywni odkrywcy nadal chronią swój zasięg.

**Mapa → Tło mapy…**: import/zmiana, prostokątne przycięcie myszą (narożniki, środki czterech krawędzi albo nowy prostokąt), krycie 0–100%, nasycenie 0–200% z resetem 100% i rozmycie 0–24 px. „Podgląd na płótnie” zachowuje proporcje aktualnego obszaru mapy i tę samą logikę cover/centrowania. Zachowujemy pełne źródło i parametry. Tło proporcjonalnie wypełnia płótno, niezależnie od kamery; pan i zoom mapy go nie ruszają. Dopasowania je pomijają. Efekty są buforowane do rozmiaru płótna (największy wymiar bufora do 2048). Zatwierdzenie/usunięcie ma jedno Undo; Anuluj nie zmienia mapy. Widok → Tło jest wyłącznie flagą widoku. MMB w lewym podglądzie przesuwa cały prostokąt kadru, bez zmiany jego wymiarów; LPM zachowuje zaznaczanie i uchwyty.

**Z biblioteki…** odtwarza pełne źródło, kadr, krycie, nasycenie i rozmycie. Biblioteka ma ciemny selektor, rename, usuwanie i jawną edycję/zastąpienie. Po Zatwierdź zmienionego tła wybierasz: Zapisz jako nowe tło (nazwa), Nadpisz wybrane tło (jednoznaczny cel), Tylko zastosuj do mapy albo Wróć do edycji. Sam import/suwaki nie zapisują plików; niezmienione zatwierdzenie nie mnoży wpisów. Błąd zapisu pozostawia edytor otwarty. Usunięcie tła z mapy nie usuwa biblioteki.

Pliki `Library/Backgrounds/background-Nazwa.json` (format1) przechowują ID, nazwę, kolejność i parametry. `Library/Backgrounds/Sources/source-SHA256.json` współdzielą pełne źródła; nie zapisujemy gotowej bitmapy okna. Do128 aktywnych teł,64 źródeł i192 MiB skompresowanych źródeł magazynu, także zachowanych dla historii. Pojedyncze źródło nadal do32 MiB/4096×4096; metadane do1 MiB. Katalog odczytuje metadane i nie zatrzymuje całej biblioteki rozkodowanej w RAM. Mapa zachowuje własną migawkę i potrzebne źródła w dotychczasowym budżecie96 MiB. Zmiana/usunięcie wpisu nie zmienia istniejących map.

Pliki teł korzystają z sesyjnego Undo/Redo300 i ochrony konfliktów LibraryFiles, bez Kosza. Nadpisanie/rename przywraca bajty i ścieżkę; konflikt nie przesuwa historii. Sama biblioteka nie dodaje gwiazdki mapy. Wspólne zastosowanie do mapy i zapis to jedna transakcja; historia nie przetrwa resetu/końca programu. Niezmienne źródła nie są automatycznie usuwane przy Undo.

Obie zewnętrzne powierzchnie edytora tła mają równe wymiary. W czasie gestu suwaka stosowany jest lżejszy podgląd najnowszej wartości, po puszczeniu dokładny renderer podglądu; wynik dokumentu nie jest pomniejszanym obrazem podglądu.

## Narzędzia, kamera i materiał

- Przybornik3×4: **Kursor | Rączka | Rysik | Gumka**; **Pędzel | Wiadro | Zaznaczenie prostokątne | Lasso**; **Mgła | Wiadro mgły | Ograniczenie widoczności | Wiadro ograniczenia widoczności**. Podpowiedzi, nazwy dostępności i aktywne ramki. Prawy przycisk nie jest potrzebny do wyboru lassa.
- Rączka nad dostępną instancją przesuwa ją lub jej trwałą grupę, bez otwierania prawego panelu. Nad tłem przesuwa kamerę. Respektuje efektywne blokady i mgłę. Środkowy przycisk nadal przesuwa kamerę. Pusty klik poza mapą odznacza heks/zamyka jego panel, ale rozpoczęcie panowania tego nie robi.
- Jeden materiał: teren, ikona albo wzorzec obiektu z niebieską ramką lub brak materiału. Wybór terenu/ikony po narzędziu mgły wybiera Pędzel; zwykłe Wiadro pozostaje Wiadrem. Wybór wzorca przełącza na Pędzel wyłącznie z narzędzi masek; inne narzędzie pozostaje aktywne. Sam wybór nie edytuje mapy.
- Po udanym OK edycji ikony brak wybranego materiału, także bez zmiany wartości. Narzędzie pozostaje; Pędzel/Wiadro bez materiału nic nie edytują, mgła działa niezależnie. Anuluj zachowuje wybór.
- Lista terenów ma własne kółko/strzałki o jeden wiersz. Panel ma stałe 298 px plus uchwyt 18 px; przy minimalnej wysokości i rozwiniętych parametrach przewija się również cała kolumna. Ikony i obiekty mają po 2×6 kafelków, pełna biblioteka pod … . Pasek zwijania ma własne miejsce.
- Prawy panel jest nakładką z uchwytem i resetem szerokości; samo otwarcie nie rusza kamery. Menu Dopasuj ma styl aplikacji i polecenia Dopasuj mapę / Dopasuj wszystko. Oba pomijają prawą nakładkę. Używają tego samego algorytmu i marginesu: jeśli grafiki mieszczą się w obwiedni mapy, zoom/pan są dokładnie takie same. Dopasuj wszystko uwzględnia efektywne prostokąty widocznych Obiektów i pozostałe kreski widocznego Rysika, także poza mapą. Ukryte warstwy nie rozszerzają kadru; samo przełączenie flag nie przesuwa kamery. Otwórz nadal dopasowuje samą mapę.

## Instancje i biblioteki

Wzorzec biblioteki, grafika i instancja na planszy są osobnymi rzeczami. Wybierz wzorzec i kliknij płótno: jeden klik tworzy jedną instancję, także poza mapą, bez snapowania. Drag nie stempluje, Wiadro nie tworzy obiektów, figury pędzla ich nie dotyczą. Nowa instancja: Neutralny, ruch odblokowany, odkrywanie wyłączone, rozmiar **30**, promień 1. Istniejące rozmiary nie są zmniejszane. Wiele obiektów może dzielić heks lub nakładać się, bez zmiany jego terenu/ikony/tekstu.

Prawy panel pojedynczej instancji działa **na bieżąco**, bez Zastosuj: nazwa, kolorowe kafelki stanów, rozmiar 8–1024 jednostek świata (najdłuższy bok z proporcjami), blokada, odkrywanie i promień 0–20. Czerwony Usuń instancję usuwa obiekt z mapy, nie wzorzec. Checkbox/stan to pojedynczy wpis; nazwa i liczby są scalane do sesji pola, kończonej przez utratę fokusu, inną operację, zapis, Undo/Redo lub zamknięcie. Poprawny tekst liczbowy działa bez wychodzenia z pola; przejściowe puste/- i wartości poza zakresem nie trafiają do modelu. Ctrl+Z/Y w tych polach obsługuje wspólną historię właściwości; heksowy opis/nazwa zachowują własne historie.

Nazwa wzorca jest wyłącznie początkową nazwą nowych instancji. Goblin/Ork nie zmieniają się po przemianowaniu wzorca Przeciwnik na Wróg. Checkbox edycji wzorca aktualizuje **grafikę** pasujących instancji bieżącej mapy, bez zmiany ich nazw, położeń, stron lub ustawień. Nazwy ikon heksowych zachowują dotychczasowe reguły.

Biblioteki ikon/obiektów mają dodawanie z pliku lub grafiki drugiej biblioteki, edycję nazwy/grafiki i wspólne wizualne selektory/podgląd. Kopiowana jest grafika do niezależnego ID; identyczne PNG nie tworzą powiązania edycyjnego. Wybór grafiki nie zmienia nazwy wpisu. Anulowanie niczego nie zapisuje. Kosz wzorca jest nieaktywny bez wyboru. Usunięcie wzorca nie usuwa instancji. Pliki: Library/Objects/object-Namiot.json, object-Drużyna (2).json i Library/Icons/icon-Czaszka.json — bezpieczne nazwy Windows, zachowane litery, mały prefiks, sufiksy kolizji. Import nieruchomych PNG/JPG/BMP/GIF do 8 MiB/2048×2048, PNG proporcjonalnie do 512×512, bez blokowania źródła. Czysta biblioteka startowa jest poprawnym scenariuszem, bez masowej migracji prywatnych plików.

## Stany i trwałe grupy

Plus w szczegółach instancji tworzy stan w `Library/Objects/States` i udostępnia go bieżącej mapie. Prawy klik plusa udostępnia wykluczony stan, jeśli jego definicja nadal jest w bibliotece. Przywrócenie nie przypisuje stanu obiektom i można je cofnąć. Prawy przycisk na kafelku otwiera edycję bibliotecznej nazwy, koloru i uprawnienia do odkrywania, usuwanie albo zapis stanu dostępnego tylko w dokumencie. Można edytować trzy podstawowe wzorce. Ich stabilne ID chronią je przed usunięciem.

Nowa mapa przyjmuje aktualne definicje biblioteki. Otwarta mapa daje pierwszeństwo własnej definicji przy tym samym ID. Edycja biblioteki aktualizuje otwartą mapę po zaznaczeniu opcji „Zaktualizuj definicję na bieżącej mapie”. Wyłączenie uprawnienia wstrzymuje odkrywanie. Checkbox i nakładka zachowują wartości. **Usuń stan z mapy** pokazuje referencje obiektów i grup oraz wybór zamiennika z domyślnym ID Neutral. Własny stan znika z tej mapy także po ponownym otwarciu. Opcjonalne usunięcie jego pliku i wymiana referencji podlegają jednemu Undo. Pliki `state-Nazwa.json` mają bezpieczne sufiksy przy kolizji i sesyjne Undo/Redo.

Prostokąt/lasso wybierają centra. Zwykły klik członka wybiera całą trwałą grupę; Ctrl dodaje/odejmuje grupę lub pojedynczy niezgrupowany obiekt. **Alt+klik** wybiera pojedynczego członka również przed wcześniejszym wyborem grupy; nie rozwiązuje jej. Obszarowy wybór rozszerza trafionego członka do grupy. Mgła nadal przycina obrysy ukrytych członków. Ruch wielu obiektów ma jeden wpis historii i zachowuje ich rozstawienie; zablokowani są pomijani. Delete na wyborze grupy usuwa instancje razem, Alt-wybór pozwala usunąć jednego. Puste grupy są usuwane, grupa z jednym członkiem zachowuje ustawienia.

Grupuj wybrane tworzy grupę z niezgrupowanych instancji. Zaznacz całą grupę i dodatkowe obiekty, aby użyć „Dodaj do grupy”. Przy kilku pełnych grupach użyj „Połącz grupy”. Pierwsza świadomie zaznaczona grupa jest pierwszą i domyślną opcją. Przy jednoczesnym zaznaczeniu obszarem obowiązuje kolejność grup w dokumencie. Wybrana grupa zachowuje ID, nazwę, ramkę i nakładki. „Świeże ustawienia” tworzą nową nazwaną grupę z ustawieniami indywidualnymi. Baza członków i przydział mgły podlegają wspólnej historii.

Panel grupy zawiera opcjonalną kolorową ramkę każdego członka. Jest kreskowana i odsunięta od zaznaczenia, a mgła przycina ją tak samo jak grafikę. Po odłączeniu ramka grupowa znika. Panel członka pokazuje grupę, przejście do niej i Odłącz.

**Nakładki nie zmieniają bazy członków.** Każda może być Indywidualnie: stan, blokada, odkrywanie, promień i rozmiar. Nowa grupa ma wszystko wyłączone. Rozmiar może być przesunięciem od bazowego (np. +10) z clamp 8–1024 albo wspólnym (np. 40); nie kumuluje się i nie przesuwa środków. Grupowa blokada true blokuje wszystkich, jawne false może odblokować indywidualnie zablokowanego. Alt-edycja zmienia bazę, nie wyłącza nakładek. Panel członka wyraźnie pokazuje wartości efektywne, które mogą przykrywać edytowaną bazę.

Odłącz/Rozwiąż pyta o Zachowaj wynikowe / Przywróć indywidualne / Wybierz dla każdej właściwości / Anuluj. Kategorie: stan, rozmiar, blokada, odkrywanie, promień; przy rozwiązaniu decyzja dotyczy całej grupy. Zachowaj zapisuje efektywne wartości jako bazę, z kontrolą uprawnień; Przywróć odsłania zachowane bazowe. Pozycje/nazwy i dawne odkrycia zostają. Cała zmiana członkostwa/właściwości/nowej mgły jest jednym Undo. Anuluj niczego nie zmienia.

## Odkrywanie i podgląd mgły

Najpierw efektywny stan określa uprawnienie, potem działają efektywny checkbox i promień. Stan bez uprawnienia zawsze uniemożliwia odkrywanie. Indywidualne przejście do niego wyłącza bazowy checkbox; powrót nie włącza go sam. Ograniczenie grupowe nie niszczy bazowego checkboxa członka. Panel pojedynczego członka edytuje jego wartości indywidualne i wskazuje przykrywającą je nakładkę.

Promień 0 = rzeczywisty heks pod środkiem grafiki, 1 = pierwszy pierścień sześciu sąsiadów itd. Rozmiar jest niezależny. Poza mapą nie używa się najbliższego pola. Ruch odkrywa drogę między zdarzeniami i zasięg każdego uprawnionego członka; brak łącznika po utracie capture/nieznanej drodze. Odkryte pola zostają po odejściu/usunięciu/wyłączeniu. Undo ruchu cofa jego nową mgłę, nie wcześniejsze odkrycia.

**Aktywny zasięg jest chroniony przed ręczną mgłą.** Po właściwych operacjach (mgła, ruch/umieszczenie, ustawienia wpływające na odkrywanie, odłącz/rozwiąż) wynik jest uzgadniany z aktualnymi efektywnymi zasięgami. Malowanie i wiadro nie zasłaniają ich, także w trakcie gestu. Bez pętli co klatkę. Końcowa delta jest częścią tej samej operacji; Undo/Redo odtwarza ją bez nowego przeliczania, więc nie niszczy sensu cofania. Odczyt nie odkrywa ponownie.

**Podgląd bez mgły** to wyraźnie oznaczona flaga widoku, bez historii/gwiazdki/zmiany Fog i bez kopii całej mapy. Pokazuje rzeczywiste grafiki i szczegóły oraz pozwala edytować normalnie ukryte dane; ruch nadal może odkrywać rzeczywiste Fog. Zapis zachowuje mgłę. Udane Nowa/Otwórz wyłącza podgląd. Ręczna próba mgły pyta o przywrócenie widoczności: Nie nic nie zmienia, Tak przywraca widoczność i **wymaga ponownego gestu**.

Lista instancji: Nazwa z miniaturą / Stan efektywny / Pozycja (Poza mapą lub pusto) / Mgła (Widoczny, Częściowo ukryty, Ukryty). Gdy widoczne jest najwyżej 50% prostokąta grafiki, lista maskuje nazwę, stan i pozycję przez ??? oraz używa neutralnej miniatury. Próg używa powierzchni przed zaokrągleniem. Kolumna Mgła podaje zaokrąglony procent ukrycia przy częściowym zasłonięciu. Poza mapą nie ma mgły, wystająca grafika może być częściowo ukryta. Podgląd pokazuje realne dane, a kolumna Mgła nadal stan rzeczywisty, zgodnie z tytułem listy. Świadomy wybór/Pokaż nadal znajduje ukrytą instancję bez zmiany Fog. Nie jest to widok zabezpieczony dla gracza; mgła nie szyfruje pliku.

## Zapis, geometria i historia

Format **8** zachowuje Objects z bazowymi właściwościami, ObjectStates oraz Groups z ID, członkami, nakładkami i ramką. Zapis zawiera trzy podstawowe definicje oraz własne stany użyte przez instancje lub grupy, w tym stany bazowe przykryte nakładką. Side instancji jest ID definicji stanu. IconAssets nadal deduplikuje użyte PNG ikon/obiektów po SHA-256 bez łączenia ich edycji; nie zapisuje całej biblioteki. Sam plik mapy wystarcza, również dla obiektów poza mapą. Walidacja sprawdza ID, referencje, pojedyncze członkostwo, brak zagnieżdżeń, skończone pozycje/rozmiary/promienie przed podmianą. Podgląd nie jest danymi mapy. Limity: 5000 instancji, 2500 grup, 100 stanów, świat ±10 000 000, 128 wspólnych zasobów, 2 MiB PNG/zasób i około 32 MiB PNG łącznie; plik do 128 MiB. Bezpieczny zapis, pojedyncze potwierdzenie końcowej ścieżki i ochrona gestów dialogów pozostają.

Resize/orientacja zachowują zaakceptowane zasady obiektów i lokalnych osi offsetu; crop ich nie usuwa. Techniczne przeliczenie nie odkrywa nowej drogi. Wspólna historia 300 obejmuje mapę, instancje, grupy/stany, końcowe delty mgły i pliki bibliotek. Nazwa/pole liczbowe ma logiczną sesję; pusty/niezmieniony zapis nie tworzy operacji. Sam wybór/nawigacja/podgląd bez historii. Pliki bibliotek są cofane z bajtów sesji przez LibraryFiles, bez Kosza i cichego nadpisania obcego celu; konflikt nie przesuwa stosu, można ponowić. Reset/limit/zamknięcie usuwa możliwość cofania, nie aktywne pliki. Udane Nowa/Otwórz zeruje historie/wybór, błąd/anulowanie zachowuje dokument.

Zachowano chroniony Domyślny, czytelne presety i odświeżanie, dziewięć geometrii figur i ciągłość pędzla, regiony wiader terenów/ikon/mgły, osobne historie tekstu, formatowane opisy i Ctrl+Shift+V. Nazwa projektu nadal pochodzi z pliku; nowy projekt proponuje 15×10, orientację/preset i etykiety Osie. Nie diagnozowano ponownie zaakceptowanych obszarów 0.4.0.

## Biblioteka, testy i dalsza praca

Celowane scenariusze 0.4.2 uruchamia `tests/Run-Patch042Tests.ps1 -Case states|groups|controls|render` z pojedynczym wybranym przypadkiem. `fixture` przygotowuje mapę do prób okna. Szczegóły wykonanych prób zawiera `docs/raport-0.4.2.md`.

Library jest lokalne i ignorowane przez Git. Program tworzy brakujące Backgrounds, Terrains/Presets, Terrains/Graphics, Icons, Objects/States, Hexmaps, Exports/Images i Exports/Text. Nowy projekt proponuje Hexmaps, a otwarty zachowuje swoją ścieżkę.

Tylko minimalne, selektywne próby. **Nie uruchamiaj Run-AllTests bez osobnego polecenia.** Nowe scenariusze wybierane pojedynczo:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tests\Run-Patch090Tests.ps1 -Case presets
```

Bieżące przypadki 0.9.1: open, quick, close, controls albo crop (Run-Patch091Tests.ps1); baseline służy zapisanej reprodukcji, fixture tylko przygotowuje własne dane. Runner wymaga jednego jawnego Case, bez domyślnej kampanii. Nie wybieraj ich hurtowo ani nie ponawiaj zaliczonych prób bez wpływającej zmiany. Źródła i skrypty historyczne pozostają śledzone, a artefakty trafiają do osobnych podkatalogów ignorowanego tests/_work. Bieżące ustalenia opisuje sekcja 48 specyfikacji. Paczka programu nie zawiera tests ani prywatnej Library.

Na później pozostają widok gracza, zakładki, eksport osadzonych zasobów do biblioteki, wagi wariantów, przebudowa opisów i drżenie nagłówka. Celowane próby: `tests/Run-Patch051Tests.ps1 -Case crop` (albo pool, library, fog, background, portable, controls). Brak Case daje błąd, nigdy pełny zestaw. Wyniki i ograniczenia: `docs/raport-0.5.1.md`. Historyczne testy zachowano; przypadki starych formatów wymagają jawnego dostosowania przy osobnym audycie. Nie uruchamiaj Run-AllTests bez osobnego polecenia.

Katalogi ikon, wzorców obiektów, stanów i presetów używają trwałej kolejności dodania. Starsze wpisy bez kolejności startują według stabilnego ID. Edycja zachowuje pozycję. Kolejność terenów jest kolejnością tablicy w palecie. Lista obiektów na mapie ma odrębny cykl sortowania rosnąco, malejąco i w kolejności dokumentu. Pokazuje grupę i znacznik efektywnego stanu. Sortuje maskowane teksty według widocznego `???` i mgłę liczbowo.

Wszystkie uzgodnione funkcje, poprawki i audyty kończymy przed 1.0.0. Przyszłe przejście na 1.0.0 obejmie tylko oznaczenia wersji, bez dodatkowych zmian funkcjonalnych lub stylistycznych.


Historyczne próby 0.6.4 wybiera jawny pojedynczy Case `profile`, `comparison`, `image`, `picker`, `input` albo `library` w `tests/Run-Patch064Tests.ps1`; `fixture` tworzy tylko własne dane. `comparison` odtwarza bazowy renderer z lokalnego commita 0cf9c24 w tests/_work, bez zależności od pozostałości starych prób. Brak Case kończy się błędem. [Raport i checklista 0.6.4](docs/raport-0.6.4.md). Bez pełnego zestawu bez osobnego polecenia.

Kolejka importu z folderów to wyłącznie [szczegółowy backlog](docs/backlog-import-z-folderow.md). Nie ma skanowania wejść, okien startowych ani usuwania oryginałów.

## Historia zmian

- **0.9.1** — lewy panel4×3/2×6 i niebieskie wybory, nowe figury z obrotem i kątem trójkąta, presety trzech sekcji z częściowym zapisem/przywracaniem, niezależne drafty i sekwencja zamykania, szybkie listy24/12 oraz startery20ikon/12obiektów. Jednoliniowe12800% i długa etykieta rename. Mapa11 bez zmiany, zapis preset5 i odczyt4/5.

- **0.6.4** — renderer gradientów ograniczony do widoku, cache ukończonej warstwy i późniejszy fragment kreski na wierzchu; jedno dokładne odejmowanie całego gestu gumki; pionowe RGB, dwa przełączniki i Losuj; małe buforowane miniatury biblioteki. Mapa 11 / preset 4 bez zmiany.

- **0.6.3** — cache geometrii/walidacji usuwa koszt nowych kresek po długiej gumce; wspólne HEX/RGB/HSV i gradienty rysika, wskaźnika oraz wariantów heksów; stały spacing responsywnej biblioteki, ciemny nagłówek listy i disabled ramka usuwania tła. Mapa11/preset4.
- **0.6.2** — dokładna gumka powierzchniowa, ciągła kometa bez sumowania alpha próbek, całkowity rozmiar 1–200, odroczone otwarcie szczegółów przy pierwszym drag, lżejsze tło podczas zmiany rozmiaru okna i lokalne UI. Mapa10 zapisuje kontury wymazania; preset3 bez zmiany. Wizualna płynność komety/panelu pozostaje do odbioru.
- **0.6.1** — ekranowy wspólny rozmiar rysika/gumki, kometa750ms, biblioteka teł z parametrami, Widok Obiektów wyłącza udział źródeł, nazwy Ograniczenie widoczności/Pole widzenia i lokalne poprawki UI. Format mapy9 bez zmiany. Nieodtworzony błąd grup został odłożony za zgodą użytkownika; raport nie obiecuje jego naprawy.
- **0.6.0** — rysik, chwilowy wskaźnik, częściowa gumka, bazowa Ciemność i dynamiczne Światło instancji/wzorców/grup, łączne maskowanie oraz dopasowanie do widocznych warstw. Poprawki skrótów, mozaiki, podglądu tła i bezpośrednia biblioteka grafik. Mapa9; raport `docs/raport-0.6.0.md`. Celowane próby: `tests/Run-Patch060Tests.ps1 -Case acceptance` (albo ink, pointer, darkness, visibility, gestures, ink-preview); fixture tworzy tylko własną mapę wejściową UI.

- **0.5.4** — niepomalowane pola niezależne od neutralnego presetu; kwadraty/× i lokalne Undo/Redo100 wariantów; deterministyczna mozaika; jeden przełączany podgląd kadru, jednorazowa inicjalizacja nowego importu; symetryczne tło i lżejsze nasycenie; alpha grafiki mgły oddzielona od logicznego Fog. Odchudzono wyłącznie odtwarzalne artefakty _work, zachowując testy i logi. Formaty i limity bez zmian. Celowane scenariusze: `tests/Run-Patch054Tests.ps1 -Case empty` (albo editor, crop, presentation, background-perf). Raport: `docs/raport-0.5.4.md`.

- **0.5.3** — regiony historycznych wersji terenów, zachowanie rozmieszczenia wariantów terenu/mgły, lżejsze podglądy i walidacja współdzielonych źródeł, osłona wejścia po dialogu, kontrastowe nazwy bez podkładu, uchwyty/proporcje/efekty tła. Źródło do 32 MiB; spójne budżety dokumentu/biblioteki. Formaty bez zmian. Celowane próby: `tests/Run-Patch053Tests.ps1 -Case boundaries|updates|local|safety|perf|regions` (jeden Case na wywołanie). Wyniki i ograniczenia w `docs/raport-0.5.3.md`; bez pełnej kampanii.

- **0.5.2** — naprawa tworzenia/zapisu presetów blokowanych przez niezwiązany, pominięty wpis biblioteki. Kontrola kolizji rezerwuje jego ID/nazwę bez importowania zawartości; pełna walidacja odczytu i ochrona nadpisania pozostają. Nie trzeba kasować Library. Formaty bez zmian. Celowane próby: tests/Run-Patch052Tests.ps1 -Case regression|lifecycle|safety, wyniki w docs/raport-0.5.2.md.

- **0.5.1** — pełne współdzielone źródła, dwa niedestrukcyjne kadry i względne połączenie, mieszane pule terenu/mgły ze stabilnym losowaniem, wybór duplikatów, pełne kafelki oraz ekranowe tło z efektami. Mapa 8, preset 3, grafika biblioteki 2.

- **0.5.0** — widoczny edytor WPF, tekstury z kadrowaniem i biblioteką, mapy i presety z osadzonymi grafikami, wspólny HSV/HEX, ponowne kliknięcie odznaczające obiekt lub grupę i przywracanie stanów z biblioteki.

- **0.4.3** — warstwy Widoku, lista obiektów z sortowaniem, usuwanie i zastępowanie stanów, trwała kolejność bibliotek, obrysy grup i kompozycja prawego panelu. Komunikaty przebudowy wskazują plik ICO i wynikowy EXE.

- **0.4.2** — własna ikona EXE, przebudowa przez BAT, biblioteka stanów, dodawanie i łączenie grup, ramki, próg maskowania 50%, stabilniejszy panel oraz poprawki potwierdzeń i skrótów. Zapis formatu 6, odczyt 5 i 6.

- **0.4.1** — trwałe grupy i odwracalne nakładki, własne stany, bieżące właściwości, osobne zaznaczanie/prostokąt/lasso, kontekstowa Rączka, ochrona aktywnych zasięgów, podgląd bez mgły i maskowana lista, wspólny kadr Dopasuj; format mapy 5.

- **0.4.0** — niezależne instancje, biblioteka obiektów i kopiowanie grafik, zaznaczanie/ruch/grupy, trwałe odkrywanie mgły przez sojuszników, lista instancji, dwa dopasowania, historia plików obiektów i przenośny format mapy 4.

- **0.3.2** — strzałki o jeden wiersz i wspólny rysunek, wybór materiału po narzędziu mgły przełącza na Pędzel, wizualny selektor grafiki współdzielony z edycją ikony oraz sesyjne Undo/Redo plików presetów ze wspólnym mechanizmem ochrony plików. Generator startowych ikon potwierdzony bez zmiany i bez migracji prywatnej biblioteki. Format mapy 3, presetu 1 i ikony 2.

- **0.3.1** — region mgły według koloru/nazwy/stanu, przeciąganie wiader jako jeden gest, synchronizacja płótna/kamery, strzałki i kółko terenów, złoty wybór materiału, Domyślny całkowicie tylko do odczytu, wyłączony rozmiar Punktu, edycja ikon i sesyjna historia ich plików, czytelne icon-Nazwa.json oraz źródła w src. Lokalna interpolacja pędzla. Format mapy nadal 3, biblioteka ikon 2.

- **0.3.0** — sekcje lewego panelu z osobnym przewijaniem terenów i bocznym paskiem, wspólne osiem kształtów pędzla, biblioteka i malowanie ikon, dwa narzędzia mgły, wspólna historia warstw, samowystarczalny format mapy **3** i presety **Kosmos.json**. Odczyt wyłącznie map 3, bez migracji. Testy selektywne zamiast pełnej kampanii.

- **0.2.3** — czytelne bezpieczne nazwy plików presetów i porządkowanie po jawnym zapisie, odświeżanie bez restartu z ochroną danych roboczych, spójne napisy i przyciski 2×2, zwarty lewy panel z czerwonym koszem i krótkimi podpowiedziami oraz Ctrl+Shift+V w opisie. Format mapy **2**, odczyt **1**, preset **1**.

- **0.2.2** — bezpieczna migracja presetów i katalog Presets, tworzenie bez podwajania oraz usuwanie własnego presetu do Kosza, paleta jednokolumnowa ze stałymi akcjami, natychmiastowe odrysowanie po zwężeniu nakładki, pełne etykiety, przestrzenny dialog rozmiaru i grupy danych oraz zwykły tekst w menu opisu. Format mapy **2**, odczyt **1**, format presetu **1**.

- **0.2.1** — jeden wiersz paska, Dopasuj bez limitu 170%, regulowana prawa nakładka, stabilna kompaktowa paleta z akcjami kafelków, chroniony wzorzec i jawny zapis presetów, wspólne Undo palety/mapy oraz jednoczesny rozmiar czterech stron z pełną listą współrzędnych usuwanych danych. Format nadal **2**, odczyt **1**.

- **0.2.0** — dwa wiersze paska, nazwa projektu z pliku, własne ikony, dwukolumnowa paleta i poprawione panele. Formatowany opis z rzeczywistymi tabelami, listami i osobną wielopoziomową historią tekstu. Lokalne zestawy terenów z wersjonowaniem, Wiadro oraz dodawanie/usuwanie wierszy i kolumn z Undo/Redo. Odczyt formatu 1, zapis formatu **2**. Zachowano zabezpieczenia 0.1.2–0.1.4.

- **0.1.4** — gest płótna musi rozpocząć się na płótnie; powrót z dialogów nie maluje, nie zaznacza i nie przesuwa mapy zdarzeniami zamykającego je gestu. Zakresy wymiarów są osobnymi tekstami kursywą po prawej stronie pól. Artefakty testów trafiają wyłącznie do ignorowanego `tests/_work/`; usunięto potwierdzone stare artefakty z głównego `bin`. Całe główne `Library/` pozostaje lokalne. Format mapy: **1**.
- **0.1.3** — pojedyncze potwierdzenie nadpisania końcowej ścieżki po dopisaniu `.hexmap`; jawne katalogi początkowe zapisu; przygotowanie struktury lokalnej biblioteki; nowa mapa zawsze proponuje 15×10 z widocznym zakresem 1–200; nazwy orientacji Pointy Top / Flat Top. Geometria, Dopasuj i historia malowania pozostają bez zmian. Format projektu: **1**.
- **0.1.2** — historia malowania przywraca wyłącznie teren i kolor, zachowując późniejsze nazwy, opisy i `NotesFormat`; Ctrl+Z/Ctrl+Y w polach tekstowych nie uruchamiają historii mapy; puste pociągnięcia nie zmieniają historii ani stanu zapisu. Format projektu pozostaje w wersji **1**.
- **0.1.1** — poprawiona prostokątna geometria obu orientacji, wybór i dopasowanie heksów, przyciski orientacji i etykiet, zapamiętywanie etykiet, przełączanie panelu szczegółów, skróty zapisu, rozszerzenie `.hexmap` oraz uczciwy licznik łącznej liczby heksów.
- **0.1.0** — pierwszy prototyp edytora.


## Zmiany użytkowe 0.6.2

Górna **Biblioteka** otwiera folder Library w Eksploratorze, bez informacyjnego okienka. Biblioteka grafik nadal ma cztery kolumny w bardziej zwartym oknie; można zmienić jego rozmiar. Ograniczenie widoczności ma konturowe oczy. W grupie na dole: ramka/kolor, Rozwiąż grupę, Usuń instancje grupy. Kolumna Mgła ma stałą domyślną szerokość, bez automatycznego wypełniania nadmiaru. W tle: Z biblioteki → Z pliku → czerwone Usuń tło.

Przy zamkniętych szczegółach Kursor rozpoczyna ruch przed pokazaniem panelu; panel otwiera się po kliknięciu/końcu gestu. To lokalna poprawka pierwszego drag; pozostałe etapowe natywne pokazanie separatora nie ma deklaracji pełnej naprawy. Bez zmian kompozycji WPF. Podczas systemowej zmiany rozmiaru tło ma bufor do320px, po zakończeniu wraca dokładny wynik do2048px i dotychczasowy cover. Jedno źródło jest dekodowane i przechowywane do zmiany dokumentu/tła lub zamknięcia; brak kolejki rozmiarów i zmian danych.

Kontury gumki mają dodatkowe limity: 2 Mi znaków pojedynczej powierzchni, 16 Mi znaków dokumentu i64 Mi znaków zachowanych wersji w historii. Napisy są UTF-16 w pamięci (do dwukrotności liczby znaków w bajtach); nadal obowiązują mniejsze limity punktów kontrolnych. Przekroczenie odrzuca cały gest. Nie ma bitmapy całego świata. Nowa mapa10 wymaga jawnego pola Surface, dlatego nie odczytujemy map9 ani nie zgadujemy ich danych; stare pliki pozostają bez zmian.

[Lokalne ustawienia układu i przyszła przebudowa górnego paska](docs/backlog-ustawienia-lokalne.md) są backlogiem, podobnie jak wygląd uchwytów i drugi wymiar lewego panelu. [Kolejka importu z folderów](docs/backlog-import-z-folderow.md) pozostaje niewdrożona. Zgłoszony dawniej zanik pola widzenia grup jest obecnie nieodtwarzalny także według użytkownika; w0.6.2 nie prowadzono jego diagnozy.


## Gradienty i poprawki 0.6.3

Wspólny picker jednolitego koloru ma HEX#RRGGBB, RGB0–255 i HSV. Tylko dla Rysika, Wskaźnika oraz wariantów terenu/mgły/ograniczenia widoczności dostępny jest także Gradient:2–16 punktów, klik paska dodaje, przeciąganie przesuwa, Usuń punkt dotyczy wewnętrznych. Końce0/1 pozostają, odstęp minimum0,01. Tęcza daje gotową paletę. Poprzedni/nowy podgląd i Anuluj nie zmieniają danych. Stany obiektów i ramki grup są wyłącznie jednolite.

W heksie każdy gradient zajmuje własny pełny heks; kąt0° oznacza lewo→prawo,90° góra→dół. Orientacja mapy nie obraca kolorów. Gradient jest jednym wariantem puli; losowanie, historyczne wersje i Zachowaj rozmieszczenie pozostają. Zmiana palety bez jawnej aktualizacji nie zmienia namalowanych pól. Logiczne maskowanie nie zależy od jasności gradientu ani od alpha grafiki.

Rysik/Wskaźnik mają cykl300logicznych px drogi:80% przebieg palety,20% płynny powrót z ostatniego do pierwszego koloru. Ostatni stop nie jest nadpisywany. Postój nie zmienia koloru. Nowa kreska zaczyna od początku; istniejąca kreska nie rozciąga gradientu po wydłużeniu ani zoomie. Gumka zachowuje oryginalny zapis drogi i kolory pozostałości. Żywe kliknięcia wskaźnika zachowują fazę, wygasły/kontekst zakończony zaczyna od nowa. Timer750ms wygasza alpha smugi, nie animuje barw.

Optymalizacja gumki buforuje walidację niezmiennych konturów i ścieżki GDI+, zamiast odtwarzać je przy każdym Paint/nowej próbce. Dokładna powierzchnia i limity0.6.2 pozostają. Wyniki pomiarów nie są gwarancją FPS. Nowy cache gradientowych kolorów jest kafelkowy, ograniczony do32kafli256×256 (~24MiB z danymi roboczymi), bez bitmapy świata. Dodatkowe limity:16stopów/gradient,100000stopów/dokument i historia kresek,50000/preset; poprzednich limitów nie zwiększano.

Mapa11 i preset5 zapisują gradienty; preset5 dodatkowo zawiera zestawy ikon/obiektów. Mapa11 zachowuje przebieg kolorów kreski. Odczyt preset4 pozostaje dostępny. Brak migracji dawnych map/presetów, stare pliki pozostają nietknięte. Biblioteki grafik nadal format2; formatów źródeł/obiektów/teł nie zmieniono. Nie dołączamy nieużytej biblioteki do mapy.

Biblioteka grafik ma stały kafelek/odstęp120px, domyślnie4kolumny, więcej po poszerzeniu i mniej po zwężeniu. Lista obiektów startuje od sumy szerokości kolumn, a resztkowy nagłówek jest ciemny. Nie ma czerwonej aktywnej ramki Usuń tło, gdy brak tła. Odroczenie panelu i zaakceptowany resize0.6.2 pozostają; nie dodano ryzykownego Show w MouseDown.

Dawne zgłoszenie źródeł pola widzenia grup jest **zamknięte po ponownym potwierdzeniu użytkownika**; nie jest aktywnym zadaniem ani odbiorem. Nie ustalono historycznej przyczyny, wracamy dopiero po nowym zgłoszeniu. [Merge heksów](docs/backlog-merge-heksow.md) to osobny pomysł bez zatwierdzonego zakresu, nie funkcja0.6.3. Lokalne ustawienia/góra, panele, kolejka importu, zakładki i UX presetów nadal backlog.


## Poprawki odbiorcze 0.6.4

Gradient kreski zachowuje cykl 300 logicznych px (80% paleta, 20% powrót), fazę od drogi i Tęczę. Na samoprzecięciu wygrywa później narysowany fragment tej samej kreski. Kolor nie jest już wybierany według najbliższej osi. Gumka nie odsłania starszego fragmentu w otworze i nie zmienia fazy pozostałości. Heksy i półprzezroczysta kometa zachowują swoje zaakceptowane reguły.

Renderer używa natywnych liniowych przejść RGB w pełnej skali ekranu, jednego roboczego kafla 256×256 i cache ukończonej warstwy tylko dla bieżącego widoku (do 5,8 mln pikseli, razem poniżej dawnego budżetu 24 MiB). Aktywną kreskę rysuje osobno. Nie ma bitmapy świata ani cyklu budowania i wyrzucania kafli per kreska. Przy większym viewportcie działa rendering bez cache. Dokładne odejmowanie gumki wykonywane jest raz na przeciętą kreskę na zakończenie gestu; podgląd już podczas ruchu wyklucza wszystkie kapsuły gestu. Krawędź podglądu może różnić się antyaliasingiem około jednego piksela, końcowy wynik jest dokładny. Do 10000 próbek gestu, bez zmiany budżetów zapisu i historii.

Picker: R/G/B w pionie; Jednolity i Gradient wybierane bezpośrednio. Losuj zmienia wyłącznie RGB istniejących punktów roboczego gradientu, zachowuje pozycje/liczbę/kąt i nie korzysta z generatora malowania mapy. Anuluj odrzuca te zmiany. Stany i ramki nadal mają tylko jednolity kolor. Poprawiono też pierwszy gest po wpisaniu rozmiaru, który wcześniej mógł kończyć się kropką przy zatwierdzaniu pola na utracie fokusu.

Biblioteka grafik zachowuje responsywny układ. Małe miniatury tworzone są bez pośredniego obrazu 1024×1024, z cache do 32 podglądów (około 0,78 MiB), bez zachowywania pełnych źródeł w tym cache. Czasy zależą od danych; lokalne wyniki, narzut cache jednolitej warstwy oraz ograniczenia prób są w raporcie. Nie deklarujemy pełnej płynności ani pełnej regresji. Format mapy 11 i presetu 4 bez zmiany; bez migracji lub sprzątania bibliotek.
