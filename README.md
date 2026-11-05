# HexMapEditor 0.2.0

Edytor map heksagonalnych dla Windows, rozwijany z wersji 0.1.4. C# / Windows Forms / GDI+, lokalne pliki i praca offline. Formatowany edytor jest osadzonym składnikiem WPF z zainstalowanego .NET Framework; aplikacja nie pobiera bibliotek, ikon ani zasobów przy uruchamianiu. Nie wymaga konta ani usługi internetowej.

## Uruchamianie i budowanie

Uruchom `HexMapEditor.exe` albo `Uruchom HexMapEditor.bat`. Wymagany pełny .NET Framework 4.x z WPF (zalecany 4.8), dostępny lokalnie w Windows. Numer aplikacji: **0.2.0**. Numer zapisywanego formatu mapy: **2**.

Build z głównego folderu, bez pobierania zależności:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Build.ps1
```

Kompilator i zestawy pochodzą z lokalnego .NET Framework. Główny EXE pozostaje śledzony w Git. Własne ikony są rysowane w GDI+; nie dodano zewnętrznego zestawu ani nowej płatnej biblioteki. Edytor WPF nie zastępuje aplikacji Windows Forms.

## Podstawowa obsługa

- Pierwszy rząd paska: pliki i historia mapy po lewej, widok i rozmiar po prawej. Drugi rząd pokazuje nieedytowalną nazwę projektu. Pełna nazwa jest w podpowiedzi.
- Nowa mapa: nazwa początkowa (domyślnie „Nowa mapa”), zawsze 15×10, zakresy 1–200, orientacja i działający zestaw terenów. Etykiety startują jako Axes / Osie.
- Przed pierwszym zapisem nazwa początkowa jest proponowaną nazwą pliku. Po zapisie lub otwarciu nazwa pochodzi z pliku: `mapa.txt.hexmap` → `mapa.txt`. Stare wewnętrzne `Name` nie ma pierwszeństwa. Anulowanie lub błąd zachowują nazwę i ścieżkę.
- Zaznaczanie otwiera szczegóły; ponowny klik zaznaczonego heksu lub Zamknij chowa nakładkę. Pędzel maluje, Wiadro wypełnia spójny obszar jednej utrwalonej wersji terenu. Rączka i środkowy przycisk przesuwają mapę, kółko zmienia zoom względem kursora.
- Zwinięty lewy panel ma wąski pasek rozwijania na całej wysokości mapy. Panele nie zmieniają kamery. Dopasuj uwzględnia obecną szerokość lewego panelu, a celowo pomija prawą nakładkę.
- Pointy Top ma wierzchołek na górze, Flat Top poziomą krawędź. Identyfikatory plików nadal brzmią `PointyTop` / `FlatTop`. Zmiana orientacji jest zapisywana i oznacza zmianę, ale nie należy do historii mapy. Przyjęta tolerancja trafiania przy zewnętrznym brzegu pozostaje.
- Każdy przycisk ikonowy ma polską podpowiedź i nazwę dostępną; aktywne narzędzie wyróżnia tło i grubsza ramka.

## Opisy oraz historia tekstu

Opis obsługuje rozmiar czcionki, pogrubienie, kursywę, podkreślenie, przekreślenie, akapity, wyrównanie do lewej/środka/prawej i justowanie, listy punktowane i numerowane. Tabela jest obiektem dokumentu z edytowalnymi komórkami: wybierz wymiary przy wstawianiu, ustaw kursor w komórce i użyj przycisków dodawania/usuwania wierszy lub kolumn oraz usuwania tabeli. Dane tabeli nie są zastępowane tabulatorami; zwykły `Description` jest dodatkową czytelną reprezentacją.

Wklej / Ctrl+V używa RTF i zachowuje obsługiwane formatowanie; Sam tekst wkleja zwykły tekst. Przed konwersją usuwane są obiekty, obrazy, metadane motywu i instrukcje pól. Wyniki tekstowe pól pozostają tekstem. Następnie dokument jest upraszczany do dozwolonych elementów. Nie ładujemy HTML, XAML, pakietów schowka ani zasobów sieciowych. Jeśli konwersja się nie powiedzie, używamy tekstu Unicode ze schowka; przy jego braku pokazujemy komunikat i zachowujemy opis. Nie jest to pełna zgodność układu Worda: krój i kolor tekstu, obrazy, obiekty, scalania tabel i zaawansowany układ nie należą do obsługiwanego modelu. Nie wykonano próby z Microsoft Wordem; sprawdzono syntetyczny RTF i rzeczywisty schowek między opisami tej aplikacji.

Nazwa heksu i opis mają osobne, wielopoziomowe historie dla każdego heksu. Ctrl+Z/Ctrl+Y w aktywnym edytorze dotyczą wyłącznie jego pola; poza edytorami sterują mapą. Górne strzałki zawsze dotyczą mapy, a panel ma własne przyciski tekstowe. Powrót do heksu zachowuje historię w sesji. Nowa/Otwórz i ponowne uruchomienie ją zerują; zapis jej nie czyści i nie umieszcza w pliku. Nowa edycja po Undo usuwa tylko właściwe Redo.

Limit: 100 kroków na pole i 32 MiB dodatkowych migawek Undo/Redo łącznie. Bieżąca treść dokumentów nie wchodzi do tego budżetu. Po przekroczeniu limitu usuwane są najstarsze dostępne kroki, a w razie potrzeby pozostałe migawki Redo. Pisanie jest grupowane przy przerwach poniżej 750 ms, najwyżej przez 5 sekund; nawigacja i zmiana pola przerywają grupę. Formatowanie, tabele i wklejanie opisu stanowią osobne jednostki. Limit opisu: 2 Mi znaków modelu JSON, 20 000 elementów, głębokość 32. Tabele mają granicę 100 wierszy/kolumn, z uwzględnieniem wspólnego limitu dokumentu. Operacje przekraczające limit są odrzucane z komunikatem, bez utraty poprzedniej treści.

## Zestawy terenów i Wiadro

Selektor wybiera roboczy zestaw, a kafelki teren. Nowy zestaw / Nazwa / Zapisz zestaw oraz Dodaj / Edytuj / Usuń pozwalają go utrzymywać. **Zapisz zestaw** zapisuje go lokalnie w `Library/Terrains/*.terrainset.json`; zapis projektu zachowuje też samowystarczalną kopię roboczych palet. Niezapisane do biblioteki zmiany zestawu można więc zachować w mapie. Zestaw domyślny jest odtwarzany z kodu, również bez lokalnego Library.

`TerrainId` identyfikuje teren, niezmienny `VariantId` konkretną wersję, a `Revision` jest rosnącym numerem informacyjnym. Edycja tworzy nowy wariant i domyślnie dotyczy przyszłego malowania. Przełącznik **Zastosuj do bieżącej mapy** jest wyłączony przy każdym otwarciu edycji. Po włączeniu zmienia tylko heksy używające poprzedniego dokładnego wariantu i tworzy jeden wpis Undo mapy; nie rusza nazw ani opisów. Cofnięcie przywraca teren heksów, a nie paletę. Zmiana zestawu i usunięcie terenu z palety nie przemalowują mapy. Każdy heks przechowuje własne właściwości, więc mapa nie zależy od istnienia pliku presetu.

Wiadro porównuje `VariantId`, odwiedza sześciu rzeczywistych sąsiadów i używa kolejki zamiast rekurencji, także na 200×200. Oddzielone obszary pozostają bez zmian. Całość jest jednym Undo mapy; pusta operacja zachowuje Redo i stan zapisu. Zabezpieczenie gestów po dialogach obejmuje także Wiadro.

## Zmiana rozmiaru i historia mapy

Rozmiar pozwala dodać lub usunąć wybraną liczbę wierszy/kolumn z góry, dołu, lewej lub prawej. Wynikowe osie: 1–200. Nowe pola są neutralne i mają nowe identyfikatory. Przed usunięciem pól z terenem innym niż stan neutralny, nazwą lub opisem pojawia się ostrzeżenie z liczbą wszystkich usuwanych pól i pól z danymi, domyślnie z odmową. Podgląd i anulowanie nie zmieniają mapy.

Cała zmiana rozmiaru to jeden krok Undo/Redo odtwarzający także usunięte dane. Zachowane heksy mają stabilne ID, więc ich tekst i historia nie przechodzą do innego pola. Zapisane parzystości początku siatki zachowują względne przesunięcia i sąsiedztwo po zmianie od góry/lewej. Historia malowania odwołuje się do ID, a nie przesuniętych indeksów. Limit historii mapy: 300 operacji; małe zmiany zachowują tylko właściwości terenu, zmiana rozmiaru listy odniesień do heksów. Nie kopiujemy całej mapy po każdym znaku tekstu. Historia mapy nie obejmuje edycji palet, nazw projektu ani zmiany orientacji.

## Format 2 i bezpieczny zapis

Plik `.hexmap` to JSON UTF-8. **0.2.0 odczytuje format 1 i zapisuje format 2; wersje 0.1.x nie odczytają formatu 2.** Po otwarciu starej mapy komunikat wyjaśnia zmianę; Zapisz jako pozwala zachować oryginał. Zwykłe opisy pozostają niezmienione podczas migracji. Starsze tereny dostają jawne warianty migracyjne: znane domyślne pary nazwa/kolor odpowiadają domyślnym ID, pozostałe mają deterministyczny identyfikator z tej pary. W formacie 1 nie było danych pozwalających odtworzyć pierwotne pochodzenie lub wersję terenu.

Format 2 zapisuje wymiary, orientację, etykiety, parzystości początku, ID heksów, kopie właściwości i wariantów terenów, robocze palety oraz opisy. `NotesFormat` to `plainText` lub `richDocument1`; `RichDescription` zawiera ograniczony model JSON akapitów, tekstu, list i tabel. Nie serializujemy wykonywalnego dokumentu WPF. Nieprawidłowe/nieobsługiwane dane, sprzeczne właściwości jednego wariantu i przyszłe numery formatu są odrzucane. Limit całego pliku: 128 MiB dla zapisu i odczytu.

Zapisz respektuje bieżącą ścieżkę. Zapisz jako potwierdza **raz końcową ścieżkę**, po normalizacji rozszerzenia, także `mapa.txt` → `mapa.txt.hexmap`; `.HEXMAP` jest rozpoznawane. Odmowa, anulowanie i błąd zachowują plik, nazwę, ścieżkę i oznaczenie zmian. Zapis wykorzystuje plik tymczasowy obok celu, Flush(true) i atomowe zastąpienie; niepotwierdzony plik pojawiający się w międzyczasie nie jest nadpisywany. Zapis i Undo/Redo prawidłowo aktualizują gwiazdkę, również przy późniejszej edycji tekstu.

## Lokalna biblioteka

Library zawsze znajduje się obok uruchomionego EXE, niezależnie od katalogu roboczego. Nowa, niezapisana mapa proponuje `Library/Hexmaps`; Zapisz jako otwartej mapy proponuje katalog bieżącego pliku. Można wybrać inne miejsce. Brakujące katalogi są tworzone bez usuwania istniejącej zawartości:

```text
Library/Backgrounds
Library/Terrains
Library/Icons
Library/Objects
Library/Hexmaps
Library/Exports/Images
Library/Exports/Text
```

Błąd utworzenia biblioteki jest zgłaszany; nie przenosimy danych do innej lokalizacji. Główne `/Library/` jest lokalne i ignorowane w Git. Nie wdrożono importu grafik, ikon/obiektów mapy, eksportu, mgły, Resetu, kadrowania ani własnego paska tytułu/ostrzeżeń. Przyciski narzędzi mają ikony, ale nie oznacza to obsługi ikon umieszczanych na mapie.

`bin` był dawnym miejscem kompilacji testów 0.1.2, a nie biblioteką. Ówczesny dialog mógł proponować go z zapamiętanej lokalizacji Windows; od 0.1.3 katalogi początkowe są jawne. Sprawdzone zbędne artefakty usunięto w 0.1.4. Obecne skrypty nie tworzą głównego `bin` i nie migrują map użytkownika.

## Testy i paczka programu

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tests\Run-AllTests.ps1
```

Można też uruchomić osobno `Run-HistoryTests.ps1`, `Run-TechnicalTests.ps1`, `Run-GestureTests.ps1` i `Run-FeatureTests.ps1` z `tests/`. Źródła i skrypty są śledzone w Git. Każdy przebieg kompiluje EXE i tworzy własne pliki tylko w podkatalogu `tests/_work/`, po czym sprząta swoje standardowe wyniki. Zbiorczy skrypt zostawia log w osobnym podkatalogu `_work/all-*`. Całe `/tests/_work/` i główne `/bin/` są ignorowane. `_work` można usunąć po zamknięciu procesów; skrypty go odtwarzają. Nie zmieniamy globalnych TEMP/TMP ani nie tworzymy map testowych w użytkowym Library. Zachowane ślady diagnostyczne 0.1.4 pozostają lokalne w `_work`.

Testy metod/kontrolek nie zastępują prób okna Windows, Worda ani różnych DPI. Rzeczywiste wyniki i ograniczenia: `docs/raport-0.2.0.md`. Specyfikacja i plan: `docs/specyfikacja.md`, `docs/plan-0.2.0.md`.

Paczka dla użytkownika powinna zawierać główny `HexMapEditor.exe`, plik uruchomieniowy i dokumentację. Bez tests, testowych EXE, logów i cudzej biblioteki. Program sam tworzy Library; nie uruchamia sprzątania testów przy starcie.

## Historia zmian

- **0.2.0** — dwa wiersze paska, nazwa projektu z pliku, własne ikony, dwukolumnowa paleta i poprawione panele. Formatowany opis z rzeczywistymi tabelami, listami i osobną wielopoziomową historią tekstu. Lokalne zestawy terenów z wersjonowaniem, Wiadro oraz dodawanie/usuwanie wierszy i kolumn z Undo/Redo. Odczyt formatu 1, zapis formatu **2**. Zachowano zabezpieczenia 0.1.2–0.1.4.

- **0.1.4** — gest płótna musi rozpocząć się na płótnie; powrót z dialogów nie maluje, nie zaznacza i nie przesuwa mapy zdarzeniami zamykającego je gestu. Zakresy wymiarów są osobnymi tekstami kursywą po prawej stronie pól. Artefakty testów trafiają wyłącznie do ignorowanego `tests/_work/`; usunięto potwierdzone stare artefakty z głównego `bin`. Całe główne `Library/` pozostaje lokalne. Format mapy: **1**.
- **0.1.3** — pojedyncze potwierdzenie nadpisania końcowej ścieżki po dopisaniu `.hexmap`; jawne katalogi początkowe zapisu; przygotowanie struktury lokalnej biblioteki; nowa mapa zawsze proponuje 15×10 z widocznym zakresem 1–200; nazwy orientacji Pointy Top / Flat Top. Geometria, Dopasuj i historia malowania pozostają bez zmian. Format projektu: **1**.
- **0.1.2** — historia malowania przywraca wyłącznie teren i kolor, zachowując późniejsze nazwy, opisy i `NotesFormat`; Ctrl+Z/Ctrl+Y w polach tekstowych nie uruchamiają historii mapy; puste pociągnięcia nie zmieniają historii ani stanu zapisu. Format projektu pozostaje w wersji **1**.
- **0.1.1** — poprawiona prostokątna geometria obu orientacji, wybór i dopasowanie heksów, przyciski orientacji i etykiet, zapamiętywanie etykiet, przełączanie panelu szczegółów, skróty zapisu, rozszerzenie `.hexmap` oraz uczciwy licznik łącznej liczby heksów.
- **0.1.0** — pierwszy prototyp edytora.
