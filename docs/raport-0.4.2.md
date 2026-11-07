# Raport HexMapEditor 0.4.2

Baza `4c0a7ff` zawiera zakończoną próbę rozwiązania grupy z 0.4.1. Zastane `icon.ico` i `icon.png` były materiałami do wdrożenia. Sprawdzono odczyt ICO 256×256 i PNG 1254×1254. Zachowano dostarczony ICO jako `assets/app/icon.ico`. Usunięto drugi wariant `icon.png` z katalogu głównego.

## Program i przebudowa

Zamknij program, podmień `assets/app/icon.ico`, uruchom `Rebuild-Program.bat` z katalogu głównego i otwórz program. BAT uruchamia `Build.ps1` względem własnego katalogu. Wymaga źródeł `src` i lokalnego kompilatora .NET Framework 4.x. Parametr `/nopause` służy do uruchamiania z konsoli bez oczekiwania na klawisz. Build kompiluje do pliku tymczasowego i publikuje wynik dopiero po sukcesie. W razie blokady EXE zgłasza problem.

Okno odczytuje ICO i zwalnia plik. Brak lub błąd odczytu uruchamia zasób z EXE. Eksplorator korzysta z ikony osadzonej podczas kompilacji. Windows może przejściowo pokazywać poprzedni wygląd z pamięci podręcznej.

## Stany, grupy i zapis

- `Library/Objects/States/state-Nazwa.json` przechowuje ID, nazwę, kolor i prawo odkrywania. Nazwy plików mają sufiksy przy kolizji. ID Ally, Neutral i Enemy chronią wzorce przed usunięciem i pozwalają je edytować. W czystej bibliotece wzorce pochodzą z kodu. Plus zapisuje własny stan w bibliotece i udostępnia go mapie.
- Nowa mapa kopiuje aktualne definicje biblioteki. W otwartej mapie pierwszeństwo ma definicja dokumentowa. Dodatkowy stan biblioteki trafia do dokumentu po użyciu. Stan wyłącznie dokumentowy można świadomie zapisać z menu kafelka. Edycja pliku zmienia mapę po włączeniu checkboxa. Usunięcie własnego stanu po potwierdzeniu usuwa plik bez Kosza i zachowuje definicję mapy.
- Zmiana uprawnienia jest oddzielna od checkboxa odkrywania i nakładki. Uzgodnienie aktywnych zasięgów zapisuje wynikową deltę. Historia 300 obejmuje stan pliku przez LibraryFiles, definicję i mgłę. Konflikt pliku zatrzymuje przejście. Operacja dotycząca wyłącznie biblioteki nie oznacza zapisanej mapy gwiazdką.
- Dodanie do grupy zachowuje jej ID, nazwę, ramkę i nakładki. Łączenie wybiera jedną z grup lub tworzy nazwaną grupę ze świeżymi ustawieniami. Domyślna opcja to pierwsza świadomie zaznaczona grupa. Przy pojedynczym geście obszarowym rozstrzyga porządek grup w dokumencie. Baza wszystkich członków jest zachowana. Członkostwo, nakładki i potrzebne delty mgły mają jeden wpis historii.
- Ramka ma wybrany kolor, jest kreskowana i odsunięta od złotego zaznaczenia. Otacza osobno grafikę każdego członka. Mgła przycina ją wspólnie z grafiką. Rozłączenie usuwa ramkę grupową członka.
- Zapis używa formatu 6, aby starszy program nie zgubił ramek lub zmienionych podstawowych definicji. Odczyt formatu 5 przyjmuje wyłączoną ramkę, gdy jej pola nie istnieją. Plik osadza użyte grafiki i podstawowe definicje oraz własne stany wskazane przez instancje lub grupy. Zachowuje także stany bazowe przykryte nakładką i pełną paletę terenów. Filtr dotyczy pliku.

## Lista i panel

Lista pokazuje ciemne wiersze, nagłówki i zaznaczenie. Suma geometrycznych przecięć zasłoniętych heksów z prostokątem grafiki wyznacza procent ukrycia. Heksy mają rozłączne wnętrza, a powierzchnia poza mapą jest widoczna. Przy widoczności od 0 do 50% dane i miniatura są maskowane. Powyżej 50% dane są jawne. Częściowe ukrycie pokazuje procent ukrytej powierzchni zaokrąglony do najbliższej liczby, z połówką w górę. Podgląd ujawnia dane i zachowuje rzeczywisty opis mgły na liście. Szczegóły instancji zachowują dotychczasową ochronę dla częściowego ukrycia.

W 0.4.1 zmiana listy stanów niszczyła wszystkie kafelki i dodawała je ponownie. Każde dodanie mogło uruchomić layout z zawijaniem. Zmiana obiekt/grupa dodatkowo przełączała widoczność przed wypełnieniem docelowych kontrolek. Teraz kafelki są aktualizowane po ID, layout jest grupowany, a widoczny panel ma granicę synchronicznego odrysowania przy zmianach struktury. Pierwsze pokazanie następuje po wypełnieniu. Zrzuty po akcji potwierdziły układ po dodaniu i zmianie wyboru. Zrzuty nie mierzą klatek przejściowych ani pełnej płynności.

Usunięcie z mapy przez przycisk lub Delete ma jedno potwierdzenie. Po zatwierdzeniu fokus wraca na płótno i historia jest dostępna bez dodatkowego kliku. Routing rozróżnia dokładne Ctrl+Z i Ctrl+Y, a Ctrl+Shift+Z nie wykonuje operacji. Menu Dopasuj korzysta z przycisku rozwijanego Windows Forms. Minimalna wysokość wynosi 720 px.

## Celowane testy metod i budowania

| Próba | Wynik |
|---|---|
| `states` | PASS. Odczyt po ponownym załadowaniu biblioteki, Nowa, chronione ID, rozdzielenie dokumentu, wspólne Undo/Redo pliku i delty, usunięcie bez utraty definicji, filtr zapisu, odczyt bez pliku biblioteki i format 5. |
| `groups` | PASS. Kolejność Ctrl, zachowanie ustawień, dołączenie, świeża grupa, Undo i spójność panelu z zaznaczeniem. Geometria widoczności 0/25/50/75/100%, dokładny próg, maska i procent listy. Próbę powtórzono po korekcie zaznaczenia po Undo. |
| `controls` | PASS. Odmowa i zgoda poprzez kontrolowany callback, jedno usunięcie, dokumentowe Ctrl+Z/Y/Shift+Z i kierowanie skrótów edytora WPF. |
| `render` | PASS. Render do bitmapy potwierdził clipping ramki, pełny obrys w podglądzie i zniknięcie po odłączeniu. Pomiar 78 px na dwa wiersze terenów w minimalnym układzie. |
| BAT i ICO | PASS. Wywołanie BAT z obcego katalogu roboczego, podmiana istniejącego EXE, odczyt ikony z zasobu i zachowanie identycznego EXE po wymuszonym błędzie kompilacji. |

Skrypt `tests/Run-Patch042Tests.ps1` uruchamia jeden wybrany przypadek. Próby i mapy robocze znajdują się w ignorowanym `tests/_work`. Nie uruchamiano pełnego zestawu, starych kampanii ani macierzy DPI.

## Zwykły EXE

- W izolowanej kopii bez sąsiedniego ICO pojawiła się własna ikona okna. Końcowa kopia z sąsiednim ICO również pokazała ikonę. Plik testowy dał się otworzyć na wyłączność podczas pracy programu. Otworzono przygotowaną mapę, pokazano pierwszy panel instancji i dodano „Próba stanu 0.4.2”. Nowy kafelek pojawił się bez przestawienia wyboru stanu i bez gwiazdki.
- Zaznaczenie obszarem objęło cztery instancje dwóch grup. Dialog proponował najpierw Zielonych. Połączenie zachowało ich nazwę i ramkę dla czterech grafik. Ctrl+Z przywróciło dwie grupy. Obserwacja panelu po cofnięciu ujawniła rozbieżność podgrupy i zaznaczenia. Poprawkę sprawdzono celowaną metodą. Sprawdzono przejście grupy do panelu pojedynczej instancji.
- Lista pokazała ciemne nagłówki i tło oraz zamaskowany wiersz przy 57% ukrycia. Wycentrowano znak zapytania. Dodatkowo poszerzono ostatnią kolumnę po obserwacji jasnego skrawka końca nagłówka.
- Pierwsza wersja przełącznika otwierała menu ponownie. Po przejściu na natywny przycisk drugi klik zamknął menu. Escape oraz klik poza menu zamknęły je w kopii końcowego EXE.
- Usunięcie z okna oczekuje na zgodę w dialogu „Usuń instancje z mapy” dla „Próba usunięcia 0.4.2”. Potwierdzenie i natychmiastowe Ctrl+Z są jeszcze przed wykonaniem. Odmowę, zatwierdzenie i skróty sprawdzono metodami.

## Krótka checklista

1. Podmień ICO, uruchom BAT i sprawdź ikonę okna oraz EXE w Eksploratorze.
2. Dodaj stan plusem, edytuj wzorzec z menu kafelka i sprawdź checkbox mapy oraz Undo pliku. Otwórz zapisaną mapę z użytym własnym stanem.
3. Dodaj obiekt do grupy, połącz dwie grupy z zachowaniem ustawień lub świeżą nazwą. Sprawdź kolor ramek, maskowanie, podgląd i Undo.
4. Otwórz listę przy częściowym zasłonięciu poniżej i powyżej progu. Sprawdź procent ukrycia oraz jawność w podglądzie.
5. Anuluj usunięcie, następnie zatwierdź na własnej kopii mapy. Sprawdź natychmiastowe Ctrl+Z, Ctrl+Y i brak operacji przy Ctrl+Shift+Z. Sprawdź też pole tekstowe.
6. Otwórz i zamknij Dopasuj tym samym przyciskiem. Sprawdź Escape, klik poza menu i dwa pełne wiersze przy minimalnej wysokości.

Końcowy build 0.4.2.0 ma SHA-256 `141DA8687F9FAD1D2A72DD4BE60529D2C50B67BEF555FB67A7627BBD89B5CCF4`. Prace wykonano lokalnie. Końcowy commit wskazuje `git log -1`.
