# HexMapEditor 0.13.0

## 1. Library i kolejka

Oryginały nowych grafik są zwykłymi plikami w `Library/Sources`. Nazwa łączy `source-`, SHA-256 i rozszerzenie ustalone z danych pliku. Jeden oryginał może zasilać wiele wpisów w różnych kategoriach. JSON wpisu pozostaje w `Terrains/Graphics`, `Backgrounds`, `Icons` albo `Objects` i odwołuje się do źródła identyfikatorem. Kadry heksagonalne, wymiary i efekty tła oraz kadry kwadratowe są zapisane niezależnie w metadanych odpowiednich wpisów. Kadr kwadratowy 512×512 jest odtwarzany przy odczycie. PNG startowe nie mają kadru i zachowują dotychczasowe identyfikatory. Źródła potrzebne wspólnym wpisom i Undo pozostają w magazynie po usunięciu wpisu. Nie ma automatycznego czyszczenia samych źródeł.

Dotychczasowy import pliku zapisuje jego oryginalne bajty. Import ze schowka używa reprezentacji obrazu dostępnej w schowku. Zmiana kadru pozostawia oryginał bez zmian. Czytelne stare wpisy ikon, obiektów, terenów i teł są nadal obsługiwane, łącznie ze starym źródłem Base64 przy odczycie. Nie ma konwersji przy otwieraniu katalogu. Przy świadomym zapisie edycji wpisu program może go zapisać w nowej postaci. Wpisy nieczytelne są pomijane z ostrzeżeniem w bibliotece. Fabryczne wpisy powstają tylko przy inicjalizacji, z zachowaniem istniejących plików i identyfikatorów. Nowy zapis metadanych ikon ma wersję 3, obiektów 2, metadane terenów i teł zachowują postać referencyjną. Stary edytor nie otworzy nowych wpisów, gdy potrzebuje odczytać nowy magazyn źródeł. Nie wdrożono migratora biblioteki.

Folder `Import` w aktywnej Library ma cztery podfoldery `Backgrounds`, `Terrains`, `Icons`, `Objects`. Skan powitania biegnie w tle, a kolejkę otwiera się na żądanie. W edytorze do folderu i kolejki można wejść z menu **Biblioteka**. Tożsamość kandydata to SHA-256 zawartości w obrębie kategorii. Nieodczytywalne obrazy nie zatrzymują skanu. Zmiana nazwy pliku nie przywraca pominiętej pozycji w tej samej sesji. Świadomy import przez wybór pliku nadal pozwala utworzyć inny kadr z tego samego źródła.

Kolejka zapisuje wyłącznie katalog Library. Zatwierdzony oryginał i wpis są warunkiem automatycznego usunięcia pliku wejściowego. Po zapisie program otwiera i sprawdza zawartość tego samego pliku przed usunięciem. Plik zmieniony, zablokowany albo niemożliwy do usunięcia zostaje na miejscu. Zapisany wpis wyklucza tę samą zawartość z następnego skanu kategorii. „Usuń plik…” obsługuje pojedynczy plik, z potwierdzeniem i użyciem Kosza. Funkcja grupowa używa opisanych kadrów domyślnych dla nieprzejrzanych pozycji i pokazuje błędy według pliku.

## 2. Format mapy i pomiary

Map12 pozostaje samodzielnym plikiem JSON `.hexmap`. Już przed zmianą kilka kadrów terenu i tło mogły odwoływać się do jednego osadzonego oryginału po SHA-256. Ikony i obiekty używające identycznego wyrenderowanego PNG współdzielą pozycję w tabeli zasobów. Oryginały kwadratowych grafik pozostają w Library, ponieważ wcześniejsza edycja mapy operuje na gotowym obrazie. Oryginały potrzebne do edycji terenu i tła nadal są w mapie.

W badanej mapie dalsze zmniejszenie tych tabel wymagałoby zmiany reprezentacji albo rezygnacji z danych edycyjnych. Został dotychczasowy zapis. Pliki z obu wersji są bajtowo identyczne po ujednoliceniu wyłącznie pola `LastSavedAppVersion`. Presety zachowują dotychczasowy przenośny format 5.

Mapa z 2 400 heksami, dwoma kadrami terenu i tłem używającymi jednego JPEG oraz ikoną i obiektem współdzielącymi PNG. Ten sam plik, stanowisko i ten sam mechanizm produkcyjnego zapisu. Poniżej mediana siedmiu odczytów i zapisów po rozgrzaniu procesu.

| Wersja | Bajty | Odczyt | Zapis |
|---|---:|---:|---:|
| 0.12.4 | 1 625 467 | 88,67 ms | 92,31 ms |
| 0.13.0 | 1 625 467 | 91,28 ms | 96,58 ms |

To próbka porównawcza na tym stanowisku. Nie dowodzi przyspieszenia ani stałej różnicy wydajności.

## 3. Próby i odbiór

- **Metody `library`, `queue`, `map` — PASS.** Pokryto zapis bajt w bajt, dwa niezależne kadry na jednym źródle, odczyt ponowny, zachowane zasoby startowe i odczyt starej ikony, plik i próbkę danych schowka PNG. Kolejka sprawdziła zmianę nazwy, podmianę treści, różne kategorie, pominięcie, anulowanie kadru, błąd zapisu, wymianę pliku przed usunięciem, pojedynczy Kosz i import grupowy. Ponowiono przypadek kolejki po dodaniu porównania plików mapy i presetu. Oba pozostały bajtowo identyczne.
- **Zwykły EXE.** Kopia w trybie osobistym z osobną Library pokazała komunikat o jednej ikonie i pozwoliła ręcznie otworzyć kolejkę. Kadrowanie kwadratowe otworzyło się z kolejki. Po Anuluj plik pozostał w Import. Katalog programu nie dostał własnej Library.
- **Portable.** Druga kopia, rozpakowana z końcowego ZIP, otworzyła z argumentu pojedynczą przeniesioną mapę. Library przed startem nie istniała, a po starcie powstały tylko zwykłe dane kopii i źródła fabryczne. W zwykłym EXE zapisano mapę Ctrl+S i sprawdzono dwie definicje terenu, jedno osadzone współdzielone źródło, obiekt i metadaną 0.13.0. W przypadku metod porównano piksele terenu, tła i ikony, zmieniono kadr i odczytano mapę po kolejnym zapisie bez folderu Library.
- **Granice prób.** Nie wykonywano instalacji z finalnego Setup ani aktualizacji online z 0.12.4. Próba schowka używała lokalnego obiektu danych PNG. Nie badano każdej aplikacji źródłowej ani wydajności na wielkich zestawach plików.
- **Krótki odbiór.** Umieść własną grafikę w `Import/Icons` → powitanie pokazuje przycisk kolejki bez blokowania map. Wybierz Dodaj i anuluj kadr → plik pozostaje. Zatwierdź kadr → w bibliotece pojawia się wpis i źródło, a mapa oraz preset zostają bez zmian. Przenieś samą własną mapę do drugiego Portable → otwiera się z dotychczasowymi grafikami i pozwala zapisać zmieniony kadr terenu.

## 4. Paczki i Git

Aplikacja ma wersję 0.13.0. FileVersion i ProductVersion EXE oraz Setup to 0.13.0.0. Setup i Portable powstały z jednej kompilacji. Sprawdzono nazwy, manifest ZIP i zgodność bajtów EXE rozpakowanego z ZIP, kopii testowej i głównego pliku. Artefakty są w `tests/_work/patch-0130-release`.

| Plik | SHA-256 |
|---|---|
| `HexMapEditor.exe` | `C5EA79A658DC44EE2D2587D7EE026BEBC6C6662B67BE444EE70F94ADBEB39FCC` |
| `HexMapEditor-0.13.0-Setup.exe` | `DF60C0DC54D0C78B119082DE78E8845CBA9BF7AFEBDD6373F4E0FDACA5CE530C` |
| `HexMapEditor-0.13.0-Portable.zip` | `975BF71B193E11842107C8320765FC12FE1E440008CEAB7A415F464A0F35AE9E` |

Punkt wyjścia `58058fe`, robocze `master` zgodne z lokalnym `origin/master`, Git czysty na starcie. Podczas pracy na `origin/master` pojawił się commit `04a857d` z odnośnikami do 0.12.4. Został zachowany jako rodzic jednego lokalnego commita implementacji. Katalog roboczy czysty przy zakończeniu, paczki i dane prób poza Git. Publiczna 0.12.4 i oba jej załączniki zwróciły HTTP 200. Publiczny tag wydania 0.13.0 zwrócił HTTP 404 podczas weryfikacji.

## 5. Fragment README po publikacji

Zastąp tekst dostępności i obie odznaki poniższym fragmentem dopiero po opublikowaniu obu paczek pod tagiem `v0.13.0`. Następnie pobierz oba załączniki bez logowania i porównaj SHA-256.

```markdown
**Publiczna wersja testowa 0.13.0.** Odznaki prowadzą do obu paczek opublikowanych pod tagiem `v0.13.0`. [Raport 0.13.0](docs/raport-0.13.0.md).

[![Instalator Windows EXE](https://img.shields.io/badge/Windows-Setup%20EXE-2864a6)](https://github.com/amdesar/HexMapEditor/releases/download/v0.13.0/HexMapEditor-0.13.0-Setup.exe)
[![Portable ZIP](https://img.shields.io/badge/Portable-ZIP-3b7964)](https://github.com/amdesar/HexMapEditor/releases/download/v0.13.0/HexMapEditor-0.13.0-Portable.zip)
```

W liście funkcji po publikacji zmień „W przygotowywanej 0.13.0 dodatkowo kolejka” na „Kolejka”.
