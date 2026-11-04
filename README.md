# HexMapEditor

Pierwszy przyrost edytora map heksagonalnych dla Windows. Program korzysta z natywnego Windows Forms i .NET Framework 4.x oraz rysuje mapę przez GDI+. Nie pobiera zależności podczas uruchamiania.

## Uruchamianie

Uruchom dwuklikiem `HexMapEditor.exe` albo `Uruchom HexMapEditor.bat`. Projekty są zapisywane jako UTF-8 JSON (`.hexmap.json`).

## Budowanie na tym komputerze

W folderze projektu uruchom z PowerShell:

```powershell
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /optimize+ /out:HexMapEditor.exe /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll HexMapEditor.cs
```

## Format projektu, wersja 1

Projekt jest obiektem JSON z `formatVersion`, nazwą, wymiarami, orientacją i tablicą `cells`. Pola zapisano w kolejności wierszami: indeks pola to `wiersz * columns + kolumna`. Każde pole zawiera typ terenu, kolor HTML `#RRGGBB`, nazwę własną, zwykły opis i `notesFormat: "plainText"`. Wartości logicznych współrzędnych są indeksowane od zera w danych, a interfejs wyświetla kolumny A, B, …, Z, AA, … oraz wiersze od 1.

Zwykły opis jest zapisany wprost w polu `description`. `notesFormat` pozwala później rozszerzyć notatki formatowane bez utraty tekstu.

## Biblioteka i dalsze przyrosty

Folder `Library` jest obok programu; w wersji developerskiej jest obok plików projektu. Biblioteka pozostaje lokalna i nie jest zapisywana w Dokumentach ani w przeglądarce.

Później można dodać przycinanie i rozszerzanie mapy, Wiadro, Mgłę i Reset, zestawy terenów, import i kadrowanie grafik, ikony i obiekty ruchome, formatowane notatki, eksport obrazu i tekstu, pakiet przenośny z grafikami oraz usuwanie tła. Nie są częścią pierwszej wersji.
