# HexMapEditor 0.13.2

## 1. Oryginały w Library

Nowe grafiki są zapisywane jako oryginalne bajty w folderze danej kategorii w aktywnej Library. Przykłady ścieżek względnych:

| Kategoria | Przykładowe źródło |
| --- | --- |
| Ikony | `Icons/Osada-original.png` |
| Obiekty | `Objects/Wóz-original.jpg` |
| Grafiki terenów | `Terrains/Graphics/Trawa-original.jpg` |
| Tła | `Backgrounds/Las-original.png` |

Podstawą nazwy jest nazwa zaakceptowana przy pierwszym zapisie. Domyślna nazwa pochodzi z pliku wejściowego albo z opisu importu ze schowka. Typ danych decyduje o rozszerzeniu. Polskie znaki są zachowywane, niedozwolone znaki są zastępowane, nazwy urządzeń chronione, a długie nazwy skracane z rezerwą na sufiks. Kolizje bez względu na wielkość liter otrzymują kolejny numer, np. `Trawa-original-2.jpg`.

JSON wpisu zapisuje `SourceId` (SHA-256) i względną nazwę `SourceFile` obok metadanych zasobu. Czytnik sprawdza nazwę, miejsce, typ i integralność pliku. W jednym katalogu identyczne bajty korzystają z jednego oryginału, a kadry i identyfikatory wpisów są niezależne. Kategoria może mieć własną kopię. Zmiana nazwy wpisu, zmiana kadru, usunięcie wpisu i Undo nie przemianowują ani nie usuwają oryginału. Gotowy oryginał może pozostać po nieudanym zapisie metadanych i zostać użyty przy ponowieniu.

## 2. Istniejące dane i import

- Istniejące wpisy nadal odczytują `Library/Sources`, wcześniejsze źródła JSON i osadzone grafiki. Sam odczyt, start i zmiana nazwy nie przenoszą starych plików. Nadpisany kadr o tych samych bajtach zachowuje dotychczasowe odwołanie. Przy zapisie drugiego wpisu z tego obrazu nowy wpis dostaje źródło w swojej kategorii.
- Świeża Library dostaje fabryczne ikony i obiekty jako nazwane PNG z zachowaniem dotychczasowego obrazu i ID. Inicjalizacja respektuje dotychczasowe znaczniki i wpisy.
- Import z programu, schowka i kolejki używa tego samego magazynu. Kolejka nadal skanuje tylko `Import`, rozpoznaje zawartość w obrębie kategorii i sprawdza ją przed usunięciem pliku po udanym zapisie. Plik wybrany spoza kolejki pozostaje na miejscu.
- Map12, preset 5 i przenośność pojedynczego pliku mapy pozostają zgodne z poprzednim wydaniem. Nowe wpisy z `SourceFile` wymagają wersji 0.13.2 lub nowszej. Setup i aktualizator używają dotychczasowego kodu.

## 3. Próby celowane

| Przypadek | Wynik |
| --- | --- |
| `library` | PASS. Próby nazwy domyślnej i zaakceptowanej, schowka PNG, polskich znaków, nazwy zastrzeżonej, długości, kolizji i zachowania oryginalnych bajtów. Drugi kadr i odrębna kategoria, usunięcie i Undo, ponowna inicjalizacja starterów oraz odrzucenie fałszywego odwołania i podmienionych bajtów. Odczyt wpisów 0.13.1 z `Library/Sources`, zachowanie źródła po zmianie kadru i nazwy, starszy JSON i odczyt przeniesionej Library. |
| `queue` | PASS. Sprawdzenie oryginału w każdej kategorii, ponowny skan, deduplikacja, zmiana zawartości wejścia, anulowanie, błąd zapisu z pozostawieniem wejścia, grupowy import i porównanie bajtowe mapy oraz presetu. |
| `map` | PASS. Zapis po wczytaniu nazwanego źródła z Library, następnie odłączenie próbnego katalogu, osobny odczyt, porównanie wyglądu oraz ponowny zapis zmienionego kadru. |
| Zwykły EXE | Polecenia Grafiki terenów, Ikony, Obiekty i Tła otworzyły właściwe katalogi w osobnej aktywnej Library. Eksplorator pokazał w każdym nowy plik `-original`. Druga, rozpakowana kopia Portable startowała bez Library, wyświetliła samodzielną mapę i zapisała ją przez Ctrl+S. Zapis potwierdził Map12, wersję 0.13.2, dwa kadry terenu, jedno osadzone źródło, obiekt i krycie tła 65%. |

Próby `library` i `queue` powtórzono po dostosowaniu skracania długich nazw przed wyświetleniem formularza. W próbie EXE użyto wyłącznie map i bibliotek w `tests/_work`. Końcowy Setup skompilowano bez instalowania go. Publiczna aktualizacja online pozostaje do próby po publikacji. Nie wykonywano pełnej kampanii testowej.

## 4. Artefakty i Git

Aplikacja 0.13.2. FileVersion i ProductVersion EXE oraz Setup to `0.13.2.0`. Jedna kompilacja zasila Setup, Portable i główny EXE. Obie paczki są w ignorowanym katalogu `tests/_work/patch-0132-release/packages`. ZIP zawiera EXE, znacznik Portable, instrukcję i licencję. Pakiety przygotowano bez podpisu.

| Plik | SHA-256 |
| --- | --- |
| `HexMapEditor.exe` | `7252A671DE9D968DC58CE01684DEBDFD6F52F8762694DD074F624ACD5E69BD13` |
| `HexMapEditor-0.13.2-Setup.exe` | `EDE253AF7910AC50A7DA552A9FAB11D2EC53B9784848E36FE301D8B9AFBEADC4` |
| `HexMapEditor-0.13.2-Portable.zip` | `167903642D7D5CF9AE691E0F26F8DD78EFA1DCF83D01FDDA5667EDE9B6D64E64` |

Baza aplikacji, lokalne `master` i zdalne `origin/master` przed zmianą wskazywały `9d28871`. README zawiera dokładne nazwy obu paczek pod tagiem `v0.13.2`. Stan końcowy to jeden lokalny commit ponad bazą. Paczki i dane prób nie należą do commita.

## 5. Pre-release

### HexMapEditor 0.13.2 — oryginały grafik w folderach Library

- Nowe oryginały trafiają bezpośrednio do folderów ikon, obiektów, grafik terenów i teł pod czytelnymi nazwami, np. `Trawa-original.jpg`.
- Kolejne kadry mogą współdzielić oryginał, a dotychczasowe wpisy i pliki w `Library/Sources` nadal działają.
- Zapis Map12 pozostaje samodzielny i zachowuje osadzone grafiki.

### Checklista pierwszej próby online z 0.13.1

1. Opublikuj pre-release `v0.13.2` z dokładnymi nazwami Setup i Portable z tabeli. Pobierz oba załączniki bez logowania z odznak README i porównaj SHA-256.
2. W zainstalowanej 0.13.1 wybierz **Sprawdź aktualizacje**. Oczekiwany jest wybór 0.13.2 i przycisk **Zainstaluj aktualizację**. W Portable ta oferta udostępnia ręczne pobranie ZIP.
3. Z własną próbną mapą anuluj zapis przy pierwszej próbie. Setup nie powinien ruszyć. W kolejnej próbie zapisz pracę i sprawdź ukończenie instalacji, wznowienie edytora i numer 0.13.2. Jeśli masz osobną kopię maszynową, powtórz po zgodzie UAC.
4. Otwórz próbne istniejące wpisy z `Library/Sources`. Zaimportuj nowy plik w wybranej kategorii i otwórz jej folder z menu Biblioteka. Oczekiwane są działające stare wpisy oraz nowy plik `-original` w folderze kategorii.
