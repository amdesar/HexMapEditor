# HexMapEditor 0.8.3

Data wydania: 14.11.2026. Baza `a31405b` / `511cdda`, zastany Git czysty. Zakres: cztery poprawki odbiorcze; map11/preset4, struktura Library i zaakceptowana hybryda bez zmian.

## Zmiany

- Zoom ma dwa przyciski we wspólnym hoście. Lewy pokazuje wartość i ustawia100% dotychczasową operacją z zachowaniem środka; prawy przełącza istniejącą listę. Osobne tooltipy i focus; zamknięcie kliknięciem strzałki zachowuje ochronę przed ponownym otwarciem i Dispose popupu. Zakres i presety bez zmian.
- Jednoliniowy TextBox był osadzany w większym polu: w szczegółach wymuszano co najmniej26px, w rename używano Dock Fill. W tych dwóch miejscach kontrolka używa naturalnej wysokości i pionowego wyśrodkowania w wierszu. Nie zmieniono pól liczbowych, walidacji i WPF.
- Wybór obiektu używa istniejącego `MaterialChosen`, tak jak teren i ikona. Pędzel włącza się tylko dla FogBrush/FogBucket/DarkBrush/DarkBucket; inne narzędzia pozostają aktywne. Wybór nie dodaje historii.
- `FitMenuRenderer` ustawia jasną strzałkę dla dostępnego i przygaszoną dla wyłączonego podmenu. Istniejąca paleta i geometria pozostają, menu systemowe i ComboBoxy nie są zmieniane.

## Testy metod i kontrolek

`Run-Patch083Tests.ps1 -Case controls` PASS, ostatni log `tests/_work/patch-083-controls-c07aaedf54b8449386351cabd2c89614/results.log`. Sprawdzono wszystkie wartości enum we wspólnej regule oraz rzeczywiste handlery wyboru trzech rodzajów zasobów dla czterech narzędzi masek, Rączki i Rysika. Kontrolki: reset100%, 12800%, otwarcie/zamknięcie listy, preset50%, obecność Niestandardowe, naturalna wysokość pola rename, pole obiektu i renderer. Powtórzenia wynikały z rozszerzenia asercji o integracje i preset; produkcyjnej logiki po próbie zwykłego EXE nie zmieniono. Brak Run-AllTests.

## Zwykły EXE

Izolowana kopia `tests/_work/patch-083-final/HexMapEditor.exe` i własna mapa: rzeczywisty klik resetuje146%→100%; strzałka otwiera i drugim kliknięciem zamyka listę. Jasne strzałki Widok→Warstwy/Jakość widoczne na ciemnym tle. Otworzono rename, sprawdzono jego pole, anulowano dialog. Wybór obiektu przy Kursorze zachował narzędzie; na własnej mapie umieszczono instancję i otworzono jej szczegóły, sprawdzając pozycję tekstu. Mapę testową zapisano, własne okno zamknięto. To próba okna, oddzielna od testów metod.

Główny EXE to te same bajty co kopia, SHA-256 `8FFAEF057BD0638580AB7F3DE845D31A8D3A315B71CC2C44436E215CAA9F622B`; FileVersion/ProductVersion **0.8.3.0**. Prywatnych danych nie używano.

Ograniczenia: nie wykonano osobnej interaktywnej edycji tekstu i tekstowego Undo, wpisania wartości w Niestandardowe, wizualnej próby disabled strzałki ani pełnej sesji każdej kombinacji zasobu. Nie powtarzano testów ramki, autozapisu, WPF, masek, bibliotek i przenośności. Wynik kontrolki nie jest dowodem pełnej regresji aplikacji. Obserwację nieustalonego błędu bardzo długiej nazwy zapisano w backlogu bez zmiany walidacji.

## Checklista odbiorcza

- [ ] Klik procentu resetuje do100%, a strzałka otwiera i zamyka listę.
- [ ] Pola nazw w rename i szczegółach obiektu są czytelne, wpis i zaznaczanie działają.
- [ ] Z czterech narzędzi masek wybór zasobu włącza pędzel; Rączka i Rysik pozostają aktywne.
- [ ] Strzałki podmenu są widoczne również po zaznaczeniu pozycji.

## Git

Zmiany kodu, EXE, testów i dokumentacji pozostają lokalnie niezacommitowane; bazowy HEAD `a31405b`. Brak push, tagu i Release'a. Pełnej regresji nie deklarowano.
