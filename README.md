# HexMapEditor 0.1.4

Prototyp edytora map heksagonalnych dla Windows. Program korzysta z C#, natywnego Windows Forms, .NET Framework 4.x i GDI+. Nie pobiera zależności podczas uruchamiania. Numer wersji aplikacji jest niezależny od numeru formatu mapy.

## Uruchamianie

Uruchom dwuklikiem `Uruchom HexMapEditor.bat` albo `HexMapEditor.exe`. Projekty mają rozszerzenie `.hexmap`; ich zawartość to UTF-8 JSON.

## Budowanie na tym komputerze

W folderze projektu uruchom z PowerShell:

```powershell
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /optimize+ /out:HexMapEditor.exe /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll HexMapEditor.cs
```

## Format projektu, wersja 1

Projekt jest obiektem JSON z `FormatVersion`, nazwą, wymiarami, orientacją, opcjonalnym `LabelMode` i tablicą `Cells`. Pola zapisano w kolejności wierszami: indeks pola to `wiersz * Columns + kolumna`. Każde pole zawiera typ terenu, kolor HTML `#RRGGBB`, nazwę własną, zwykły opis i `NotesFormat: "plainText"`. Wartości logicznych współrzędnych są indeksowane od zera w danych, a interfejs wyświetla kolumny A, B, …, Z, AA, … oraz wiersze od 1.

Brak `LabelMode` w starszym pliku oznacza `Axes` (Osie). Dozwolone wartości to `Axes`, `InCells` i `None`. To ustawienie prezentacji i nie zmienia logicznych współrzędnych. Zwykły opis jest zapisany wprost w polu `Description`. `NotesFormat` pozwala później rozszerzyć notatki formatowane bez utraty tekstu.

## Historia zmian

- **0.1.4** — gest płótna musi rozpocząć się na płótnie; powrót z dialogów nie maluje, nie zaznacza i nie przesuwa mapy zdarzeniami zamykającego je gestu. Zakresy wymiarów są osobnymi tekstami kursywą po prawej stronie pól. Artefakty testów trafiają wyłącznie do ignorowanego `tests/_work/`; usunięto potwierdzone stare artefakty z głównego `bin`. Całe główne `Library/` pozostaje lokalne. Format mapy: **1**.
- **0.1.3** — pojedyncze potwierdzenie nadpisania końcowej ścieżki po dopisaniu `.hexmap`; jawne katalogi początkowe zapisu; przygotowanie struktury lokalnej biblioteki; nowa mapa zawsze proponuje 15×10 z widocznym zakresem 1–200; nazwy orientacji Pointy Top / Flat Top. Geometria, Dopasuj i historia malowania pozostają bez zmian. Format projektu: **1**.
- **0.1.2** — historia malowania przywraca wyłącznie teren i kolor, zachowując późniejsze nazwy, opisy i `NotesFormat`; Ctrl+Z/Ctrl+Y w polach tekstowych nie uruchamiają historii mapy; puste pociągnięcia nie zmieniają historii ani stanu zapisu. Format projektu pozostaje w wersji **1**.
- **0.1.1** — poprawiona prostokątna geometria obu orientacji, wybór i dopasowanie heksów, przyciski orientacji i etykiet, zapamiętywanie etykiet, przełączanie panelu szczegółów, skróty zapisu, rozszerzenie `.hexmap` oraz uczciwy licznik łącznej liczby heksów.
- **0.1.0** — pierwszy prototyp edytora.

## Cofanie i lokalne testy regresyjne

Przyciski Cofnij/Ponów dotyczą malowania. Edycja nazwy mapy, nazwy heksu i opisu korzysta z natywnego cofania kontrolki Windows Forms `TextBox` (Ctrl+Z, ograniczony bufor). Nie zapewniamy tekstowego Redo przez Ctrl+Y; ten skrót w polu tekstowym nie ponawia też malowania. Po przeniesieniu fokusu na mapę Ctrl+Z/Ctrl+Y sterują historią malowania.

Uruchom z głównego folderu projektu:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tests\Run-HistoryTests.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tests\Run-TechnicalTests.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tests\Run-GestureTests.ps1
```

Źródła i skrypty w `tests/` są śledzone w Git. Wszystkie własne artefakty testowe — EXE, mapy, logi, kopie robocze i inne pliki tworzone przez skrypty — należą wyłącznie do `tests/_work/`, do osobnego podkatalogu każdego przebiegu. Całe `_work` jest ignorowane przez Git, odtwarzane przez skrypty i można je usunąć po zakończeniu testów i zamknięciu procesów. Nie zmieniamy globalnych TEMP/TMP i nie używamy systemowego TEMP ani użytkowego Library do map kontrolnych. Standardowe przebiegi sprzątają swoje pliki; świadomie zachowane diagnostyczne EXE i logi również pozostają tylko w `_work`.

Zestaw historii sprawdza tekst i `NotesFormat` przy Undo/Redo, puste pociągnięcia, rozgałęzienie historii, grupowanie pól, zapis i oznaczenie zmian oraz kierowanie skrótów. Zestaw techniczny sprawdza potwierdzenia końcowej ścieżki, odmowę/anulowanie, bezpieczny zapis i błędy, ustawienia dialogu, bibliotekę, nową mapę i orientacje. Zestaw gestów odtwarza obce Move/Up, powrót z dialogu, oczekiwanie na zwolnienie przycisków, świeże gesty, przechwycenie i zagnieżdżone dialogi. To testy metod i kontrolek; próby rzeczywistych okien są raportowane osobno.

Paczka programu dla użytkownika powinna zawierać główny `HexMapEditor.exe` i potrzebne pliki uruchomieniowe/dokumentację, bez `tests/`, `_work` i testowych EXE. Nie dołączamy cudzej biblioteki ani map kontrolnych. Sama aplikacja nie uruchamia sprzątania testów.

## Zapis i katalogi początkowe

Zapisz aktualizuje bieżący plik bez ponownego pytania o nadpisanie. Dla niezapisanej mapy otwiera dialog Zapisz jako. Zapisz jako normalizuje rozszerzenie, a potem pyta raz o zastąpienie rzeczywistej końcowej ścieżki, także przy wpisaniu `mapa.txt`, gdy istnieje `mapa.txt.hexmap`. Rozszerzenie `.HEXMAP` jest rozpoznawane bez dopisywania kolejnego. Odmowa i anulowanie zachowują plik, bieżącą ścieżkę i stan niezapisanych zmian. Zapis nadal korzysta z pliku tymczasowego obok celu i bezpiecznego zastąpienia.

Nowy projekt proponuje `Library/Hexmaps` obok uruchomionego EXE. Zapisz jako dla otwartego projektu proponuje katalog bieżącego pliku. Można wybrać inny katalog.

`bin` był katalogiem budowania testów 0.1.2, a nie domyślnym miejscem map ani biblioteką. Ówczesny test interaktywny świadomie zapisał tam własną mapę. Dialog nie miał ustawionego `InitialDirectory`, więc jego późniejsza propozycja mogła wynikać z zapamiętanej przez Windows lokalizacji. Od 0.1.3 katalog początkowy jest ustawiany jawnie. W 0.1.4, po sprawdzeniu pochodzenia i zawartości, usunięto testowy `HexMapEditor.HistoryTests.exe`, znane mapy kontrolne `Nowa mapa.hexmap`, `Nowa mapa.txt.hexmap` i pusty główny `bin`. Skrypty go nie odtwarzają. Nie migrujemy map użytkownika.

## Nowa mapa i orientacje

Każde otwarcie okna Nowa mapa proponuje 15 kolumn i 10 wierszy. Zakres `(1–200)` jest osobnym tekstem kursywą po prawej stronie każdego pola liczbowego. Nowa mapa ma etykiety `Axes`. Pointy Top oznacza wierzchołek na górze, a Flat Top poziomą krawędź na górze. Identyfikatory pliku pozostają `PointyTop` i `FlatTop`. Zmiana orientacji oznacza projekt jako zmieniony i jest zapisywana; nie należy do historii malowania. Dopasuj uwzględnia lewy panel i celowo pomija prawy panel będący nakładką. Dotychczasowa tolerancja trafiania przy brzegu pozostaje bez zmian.

## Biblioteka i dalsze przyrosty

Folder `Library` jest obok uruchomionego programu, niezależnie od katalogu roboczego. Całe główne `/Library/` jest lokalne i ignorowane przez Git; nie wymaga śledzonych `.gitkeep`. Przy starcie i użyciu biblioteki program przygotowuje brakujące katalogi bez usuwania istniejącej zawartości:

```text
Library/
  Backgrounds/
  Terrains/
  Icons/
  Objects/
  Hexmaps/
  Exports/
    Images/
    Text/
```

Błąd utworzenia katalogów jest zgłaszany; program nie wybiera po cichu innej lokalizacji. Biblioteka pozostaje lokalna. Powyższe katalogi nie oznaczają wdrożonego importu, eksportu ani zarządzania zasobami.

Później można dodać przycinanie i rozszerzanie mapy, Wiadro, Mgłę i Reset, zestawy terenów, import i kadrowanie grafik, ikony i obiekty ruchome, formatowane notatki, eksport obrazu i tekstu, pakiet przenośny z grafikami oraz usuwanie tła. Nie są częścią pierwszej wersji.
