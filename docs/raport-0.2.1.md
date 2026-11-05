# HexMapEditor 0.2.1 — raport wykonania

Data zakończenia: 6 listopada 2026. Aplikacja **0.2.1**, EXE **0.2.1.0**, format mapy nadal **2**, odczyt formatu **1**.

## Baza i zakres

Praca wyłącznie w `C:\ALTERNATYWNA PARTYCJA\HexMapEditor-Codex`. Baza: czysty `master`, HEAD i `origin/master` **5a88faeffbe84ec869f77b4d55c691f662338cb0**. Sprawdzono origin (`https://github.com/amdesar/HexMapEditor.git`) i pobrano referencje; aktualizacja fast-forward nie była potrzebna. Po wznowieniu zachowano rozpoczęte zmiany, sprawdzono status, diff i działające okna; nie przyjęto, że niedokończony zapis lub kopiowanie EXE powiodły się.

Ukończono kod uzgodnionego zakresu 0.2.1:

- Jeden zwarty rząd paska. Nazwa z pliku jest nieedytowalna, wyśrodkowana między grupami, w jednej linii z wielokropkiem i pełną nazwą w konfiguracji podpowiedzi. Przyciski mają wspólną wysokość 32 px, ikony 18 px i wyrównane odstępy.
- Dopasuj liczy skalę z prostokąta mapy i dostępnego miejsca, bez ograniczenia 170%; uwzględnia lewy panel, pomija prawą nakładkę i zachowuje proporcje. Pierwszy ruch kółka wychodzi od rzeczywistej skali dopasowania.
- Prawy panel ma uchwyt, minimum/maksimum, zapamiętaną szerokość w sesji i reset do 440 px. Przelicza marginesy, kontrolki, przewijanie i szerokości tabel. Nie zmienia kamery ani danych gestem uchwytu.
- Paleta ma dwie kolumny kafelków 52 zamiast 65 px. Dodaj teren pozostaje nad nimi. Akcje ołówka/kosza ujawnia hover lub fokus; są też Tab, F2 i Delete. Nie zmieniają rozmiaru kafelka i nie wybierają przy okazji innego terenu.
- Domyślny jest chronionym wzorcem przez ID, także po zmianie roboczej nazwy. Zapis zestawu jawnie wybiera nadpisanie własnego zapisanego presetu lub nową kopię z nowym ID/nazwą. Chroniony i niezapisany zestaw wymagają nowej kopii. Kolizja, anulowanie i błąd nie oznaczają danych jako zapisanych.
- Dodanie, edycja i usunięcie terenu należą do wspólnej chronologii palety/mapy. Edycja z aktualizacją heksów jest jednym atomowym Undo. Migawki odtwarzają warianty, kolejność i wybór; nie cofają notatek ani plików biblioteki. Wspólna historia obejmuje też zmiany roboczych zestawów i ich wybór. Historie tekstu pozostają oddzielne.
- Rozmiar ma cztery podpisane pola Góra/Dół/Lewo/Prawo, domyślnie 0, i wspólny podgląd wyniku. Cały plan jest walidowany przed zmianą. Wycinanie dotyczy starego prostokąta; nie można wyciąć całej starej osi, maskując to dodaniem po drugiej stronie. Jeden krok Undo/Redo zachowuje ID, dane i parzystości. Ostrzeżenie liczy narożniki raz i udostępnia pełną przewijaną listę współrzędnych sprzed operacji; domyślne Nie.

Nie zmieniono technologii, schematu mapy, migracji, lokalizacji Library ani zachowania bezpiecznego zapisu map. Nie dodano zależności ani pobierania przy uruchamianiu. Nie wykonano push ani Release'a.

## Ustalone przyczyny

**Limit dopasowania:** `MainWindow.RecalculateFitTransform` zawierał `Math.Min(1.7f, ...)` oraz dolny limit `.08f`. To potwierdzona przyczyna zatrzymania na 170%, niezależna od wolnego miejsca. Obecnie skalę wyznacza minimum ilorazów szerokości i wysokości dostępnego prostokąta do granic mapy. Margines wynosi 32 px oraz dodatkowe 28 px z lewej / 24 px u góry przy rysowanych osiach. Pozostaje jedynie techniczne zabezpieczenie przed skalą zerową. Dawny ręczny limit 400% też powodowałby skok po większym Dopasuj; koło ma teraz czynnik 1,12 i zakres obejmujący bieżącą skalę dopasowania (zwykły zakres ręczny 1%–12800%).

**Przeskakiwanie palety:** stary `RefreshPresetPicker` usuwał i tworzył wszystkie kafelki także przy niezmienionej zawartości. Pomiar dla sześciu terenów wykazał `removed=6, added=6, firstDisposed=True`, a wysokości przepływowego panelu przechodziły przez `142,71,71,0,71,71,142,213`. AutoSize przesuwał zależne kontrolki. Po poprawce ta sama próba daje `removed=0, added=0, firstDisposed=False`, bez zdarzeń zmiany wysokości. Porównujemy zawartość, zachowujemy istniejące kontrolki i grupujemy rzeczywiste zmiany układu. Nie użyto opóźnienia. Przy wyborze zmienia się wyróżnienie; ruch kursora i pisanie opisu nie przebudowują palety.

Próba okna ujawniła również pozostawanie poziomego paska po zwężeniu prawego panelu: obsługa Resize zmieniała zawartość po obliczeniu wirtualnego obszaru przewijania przez WinForms. `DetailsScrollPanel.OnLayout` robi to przed pomiarem. Kotwice kontrolek są ustalane przy szerokości projektowej, przed uruchomieniem responsywnego układu. Ponowna próba zwykłego EXE potwierdziła poprawny reset, marginesy i brak zbędnego poziomego paska.

## Testy metod i kontrolek

Końcowe `tests/Run-AllTests.ps1`: **90/90**, kod zakończenia **0**.

| Zestaw | Wynik | Zakres |
|---|---:|---|
| Historia | 11/11 | Późniejsze nazwy/opisy/NotesFormat, puste i rzeczywiste malowanie, Redo, zapis i stan zmian, oddzielenie skrótów tekstu |
| Techniczne | 12/12 | Końcowa ścieżka, pojedyncze potwierdzenie, odmowa/anulowanie/błędy/wyścig zapisu, ścieżki i zachowanie Library, dialog Nowa mapa i orientacje |
| Gesty | 11/11 | Obce Move/Up, modalne powroty i końcówka dwukliku, świeży gest, capture, pan/select/brush/bucket, wyjątki i zagnieżdżone ostrzeżenia |
| Funkcje 0.2.0 | 35/35 | Formatowany zapis/odczyt, tabele/listy/RTF, odrębne historie, presety/wersje, Wiadro i rozmiar obu orientacji, format 1, limity i stan zapisu |
| Poprawka 0.2.1 | 21/21 | Pasek minimum/duże okno, Dopasuj >170% i >400%, pierwsze koło, małe/duże/wąskie mapy obu orientacji i oba stany lewego panelu; prawa nakładka; uchwyt, marginesy/przewijanie/tabele; stabilność kontrolek i akcje kafelków; chroniony preset, kopia, nadpisanie, kolizja, anulowanie i błędy; wspólna chronologia i atomowość; mieszane cztery strony, stare wpisy historii, ID/parzystość/opisy, brak operacji, ochrona całej osi i pełna lista 39 800 koordynatów |

Nowy test układu tworzy rzeczywiste uchwyty formularza i pompuje komunikaty przy zmianach szerokości, ale nadal jest testem kontrolowanym z kodu, nie próbą użytkownika w Computer Use. Sprawdza także, że zmiana układu nie zmienia modelu, historii i gwiazdki. Pierwszy przebieg tego testu przeszedł, lecz runner nie usunął automatycznie utworzonej przy Shown biblioteki obok testowego EXE. Runner poprawiono, aby sprzątał wyłącznie zweryfikowany katalog własnego przebiegu w `_work`; ponowione przebiegi zakończyły się poprawnie.

Końcowy lokalny log: `tests/_work/all-3d1bb0bfab9c472cbfa2d09b9e960830/results.log`. Pomiary palety: `tests/_work/repro-021/before.log` i `after.log`. Logi są ignorowane, nie są częścią commita.

## Faktyczne próby okna

Użyto Computer Use i zwykłych kompilacji `HexMapEditor.exe` 0.2.1, bez Trace. Dla ochrony prywatnej biblioteki uruchamiano kopię w `tests/_work/ui-021`, z biblioteką obok tej kopii. Po końcowym buildzie potwierdzono identyczny SHA-256 głównego EXE i kopii: **67942CD56D30E647F992955478E5A18C2D109BD8A6BB4B0129E9098E5B1647CC**. Próby były prowadzone podczas kolejnych iteracji; po ostatniej korekcie nazwy ponownie sprawdzono zwykły końcowy EXE, otwarcie mapy, skalę i wielokropek przy minimalnej szerokości.

- Duże okno 1440×940 oraz minimum 1180×680: jeden rząd, rozdzielone grupy, poprawne wyrównanie i miejsce nazwy. Próba długiej nazwy ujawniła zawijanie jej na dwie linie; ograniczono wysokość etykiety do jednej linii i potwierdzono końcowy wielokropek bez nakładania grup.
- Mapa 4×4: Pointy Top po otwarciu 409%, pierwszy ruch kółka 458%, Dopasuj wróciło do 409%. Przy minimum Pointy Top 266%, Flat Top 222%. Mapa pozostawała proporcjonalna i mieściła się po Dopasuj. Samo zmniejszenie okna nie uruchamiało ponownego Dopasuj.
- Uchwyt rozszerzył panel z 440 do około 668 px; zamknięcie/otwarcie zachowało szerokość, reset przywrócił 440. Kamera i wybrany heks pozostały na miejscu, bez Undo i gwiazdki. Po poprawce układu ponowiono rozszerzenie/reset w zwykłym EXE: kontrolki i pasek opisu zawijały się wewnątrz marginesów, bez poziomego przewijania. Minimum okna zapewniało przewijanie pionowe.
- Obowiązkowa sekwencja: A1 pomalowane jasną wodą, F2 otworzyło edycję wody; przełącznik aktualizacji mapy był wyłączony. Po zmianie na RGB 0,0,128 A1 pozostało jasne, B1 pomalowano ciemnym kolorem. Pierwsze Ctrl+Z cofnęło B1, drugie przywróciło jasną wodę w palecie. Dwa Ctrl+Y odtworzyły ciemną paletę i B1. To obserwacja rzeczywistego okna, niezależna od regresji metod.
- Zapis zestawu Domyślnego pokazał wyłączone nadpisanie i wyjaśnienie. Zapis jako nowy utworzył „Próba 0.2.1” z własnym ID. Dodaj teren pozostało nad kafelkami, pojawił się siódmy teren i wskaźnik różnicy. Zapis własnego presetu udostępnił świadome nadpisanie, zapis zachował ID i siedem terenów; status wrócił do zgodności z plikiem, a projekt nadal miał gwiazdkę. Plik kontrolny sprawdzono także przez odczyt JSON. Wszystko w izolowanym Library wewnątrz `_work`.
- Kafelki pokazywały akcje na fokusie/hover, F2 otwierało jeden dialog edycji. Dodanie nie przemieszczało przycisku Dodaj teren. Pełne nazwy domyślnych terenów są czytelne w dwóch liniach przy wysokości 52 px.
- Dialog rozmiaru zaczął od czterech zer. Dla starej 4×4 ustawiono Góra −1, Dół +2, Lewo −1, Prawo +1: podgląd 4×5. Ostrzeżenie podało **7** unikalnych usuwanych heksów, **1** z danymi i współrzędną **A1** (neutralny teren z nazwą/opisem). Fokus i domyślne Enter wskazywały Nie. Enter anulowało usunięcie: nadal 4×4, brak gwiazdki i historii.
- Otwieranie mapy dwuklikiem i powroty z opisanych dialogów nie utworzyły niezamierzonej edycji. W próbie zapisu własnej mapy przez systemowy dialog zapis usunął gwiazdkę i ustawił nazwę z pliku. Okna testowe zamknięto przed sprzątaniem.

## Ograniczenia i rzeczy odłożone

- Nie wykonano nowych prób Microsoft Worda, większego DPI, wielu monitorów ani technologii asystujących. Nazwy dostępności i klawiaturę akcji sprawdzają regresje; nie jest to certyfikacja dostępności.
- Interaktywnie nie zatwierdzano usunięcia terenu ani heksów z danymi. Ich rzeczywiste usuwanie, pełne Undo/Redo, wszystkie mieszane strony, parzystości i długa lista ostrzeżenia są sprawdzone metodami. Nie przedstawiamy ich jako ręcznych prób okna. Przełączanie wielu presetów, błędy zapisu i wszystkie stany wyboru mają regresje metod, bez pełnej macierzy UI.
- Nie powtarzano interaktywnie całej obsługi edytora z 0.2.0, wklejania, wszystkich tabel/list i skrótów. Istniejące regresje zostały uruchomione. Zmianę szerokości tabel sprawdzono z kodu; okno sprawdzono z opisem tekstowym/pustym.
- Pomiar kontrolek potwierdza usunięcie przyczyny zbędnej przebudowy palety; nie jest pomiarem każdej klatki na dowolnym sprzęcie. Nie testowano interaktywnej płynności 200×200, długiego obciążenia pamięci ani awarii zasilania podczas zapisu.
- **Nie naprawiono i nie oznacza się jako naprawionych:** całościowego wyglądu narzędzi opisu, wskazania aktywnych stanów formatowania, zgłoszonego nieprawidłowego przełączania pogrubienia przy pustym zaznaczeniu, wysokości/przewijania pustego edytora, wariantu palety jednokolumnowej i innych nieomówionych funkcji.

## Zmienione pliki i artefakty

- `HexMapEditor.cs`, `EditorShell.cs`, `PanelSizing.cs`: pasek, skala, panel, gesty i układ.
- `TerrainTile.cs`, `PresetOperations.cs`: stabilne kafelki, akcje, robocze presety, jawny zapis i status biblioteki.
- `PaletteState.cs`, `EditorOperations.cs`: migawki palet oraz wspólna i atomowa historia.
- `MapModel.cs`, `ResizePlan.cs`, `ResizeDialogs.cs`, `EditorDialogs.cs`: chroniony zapis, walidowany rozmiar, pełna lista ostrzeżenia i wybór sposobu zapisu.
- `RichNotes.cs`: dopasowanie szerokości tabel przy zmianie obszaru i po wklejeniu; bez przebudowy narzędzi tekstowych.
- `AssemblyInfo.cs`, główny `HexMapEditor.exe`: wersja 0.2.1 i końcowy build.
- `tests/PatchTests.cs`, `tests/Run-PatchTests.ps1`, `tests/Run-AllTests.ps1`: nowe regresje i ich uruchamianie/sprzątanie.
- `README.md`, `docs/specyfikacja.md`, `docs/plan-0.2.1.md` i ten raport: aktualne zachowania, decyzje, wyniki i granice.

Własne mapy, preset, kopie EXE i robocze źródła z prób 0.2.1 usunięto po zakończeniu procesów z dokładnie sprawdzonych podkatalogów `_work`. Zachowano końcowy log regresji, pomiary palety oraz wcześniejsze uzgodnione ślady 0.1.4. Nie usuwano danych użytkownika ani nie używano głównego bin; katalog bin nie został odtworzony. Główne Library nie było używane do testów.

Źródła/skrypty testów i główny EXE są śledzone. `git ls-files Library tests/_work bin` nie wykazało śledzonych danych. `git check-ignore -v` potwierdziło `/Library/`, `/tests/_work/`, `/bin/`. Przegląd plików commita wyklucza mapy, presety, logi i testowe EXE. Końcowy commit i stan repozytorium podano w odpowiedzi po utworzeniu lokalnego commita; raport jest jego częścią.

## Uruchomienie i checklista użytkownika

Uruchom główny `HexMapEditor.exe` lub `Uruchom HexMapEditor.bat`. Lokalny build: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Build.ps1`. Regresje: analogicznie `tests\Run-AllTests.ps1`. Paczka programu bez tests i lokalnej biblioteki.

1. Przy swoim DPI oceń pasek z długą nazwą i Dopasuj małej mapy przy minimalnym i dużym oknie.
2. Przeciągnij uchwyt panelu, zamknij/otwórz i zresetuj szerokość; sprawdź własne tabele i marginesy.
3. Powtórz jasna woda → ciemna paleta → ciemne malowanie → dwa Undo i dwa Redo.
4. Zapisz roboczą kopię Domyślnego pod nową nazwą, sprawdź świadome nadpisanie własnego presetu i stan po restarcie.
5. Na kopii mapy ustaw mieszane znaki czterech stron, sprawdź koordynaty ostrzeżenia, odmowę i następnie zatwierdzone Undo/Redo.
