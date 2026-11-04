# HexMapEditor 0.1.1

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

- **0.1.1** — poprawiona prostokątna geometria obu orientacji, wybór i dopasowanie heksów, przyciski orientacji i etykiet, zapamiętywanie etykiet, przełączanie panelu szczegółów, skróty zapisu, rozszerzenie `.hexmap` oraz uczciwy licznik łącznej liczby heksów.
- **0.1.0** — pierwszy prototyp edytora.

## Biblioteka i dalsze przyrosty

Folder `Library` jest obok programu; w wersji developerskiej jest obok plików projektu. Biblioteka pozostaje lokalna i nie jest zapisywana w Dokumentach ani w przeglądarce.

Później można dodać przycinanie i rozszerzanie mapy, Wiadro, Mgłę i Reset, zestawy terenów, import i kadrowanie grafik, ikony i obiekty ruchome, formatowane notatki, eksport obrazu i tekstu, pakiet przenośny z grafikami oraz usuwanie tła. Nie są częścią pierwszej wersji.
