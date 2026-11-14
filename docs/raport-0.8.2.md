# HexMapEditor 0.8.2 — hybrydowy tytuł

Data przygotowania: 14.11.2026. Wdrożona wyłącznie hybryda z **natywnymi przyciskami Windows**. Nie powstał własny pasek ani wariant awaryjny. Baza sprawdzona lokalnie: d3b7435 (dokumentacja), 0c52b28 (kod); początkowy Git czysty. Zakres obejmuje wyłącznie główne okno edytora i metadane wydania.

## Implementacja i układ

`EditorCaption.cs` rozszerza istniejący HWND przez DWM. Zachowane WS_CAPTION, WS_THICKFRAME, systemowe polecenia i przyciski. `CommandBars.cs` przekazuje tylko host/layout do nowej klasy; sześć MenuStrip oraz szybki ToolStrip zachowują dotychczasowe handlery, skróty i stany. Nie kopiowano logiki poleceń. Dialogi i start zachowują dotychczasową ramkę.

Pierwszy wiersz: ikona, HexMapEditor, Plik/Edycja/Mapa/Widok/Biblioteka/Pomoc, nazwa mapy, natywne przyciski. Drugi: szybki pasek od lewej, bez trzeciego rzędu. W testowanym środowisku wysokości32+36px. Wysokość tytułu wynika z systemowej metryki i fontu, nie z szerokości przycisków.

Nazwa mapy celuje w środek całej szerokości, jest minimalnie przesuwana do dostępnego obszaru, skracana wielokropkiem, a przy mniej niż80px miejsca ukrywana. Nie nachodzi na menu/przyciski. Rzeczywisty tytuł okna i gwiazdka pozostają dla systemu/Alt+Tab, podobnie niezależny niebieski stan Zapisz. Wolny obszar za Pomoc oraz nazwa mapy dają HTCAPTION; rzeczywisty MenuStrip pozostaje kontrolką. Zmiany dokumentu, formatów, Library i historii nie były potrzebne.

## Dwie rozpoznane regresje i lokalne poprawki

1. **Wzrost wysokości po SC_RESTORE.** Nowa próba odtworzyła1440×940→1440×971. Windows przywracał właściwy prostokąt zewnętrzny, a WinForms następnie przeliczał zapamiętany rozmiar klienta z nadal istniejącym WS_CAPTION. Trace SetBoundsCore pokazał dodatkowe przeliczenie. Obserwacja natywnego stanu IsZoomed/IsIconic podczas WM_WINDOWPOSCHANGED wykrywa powrót do normalnego stanu; tylko w tym przejściu SetBoundsCore zachowuje aktualny zewnętrzny RECT Windows. Nie odejmujemy31px, nie modyfikujemy prywatnych pól Frameworka ani położenia na podstawie starej kopii RestoreBounds.
2. **Przywróć po maksymalizacji.** Rzeczywiste kliknięcie ujawniło HTCLIENT zamiast HTMAXBUTTON. Celowany test potwierdził wynik1 zamiast9. Hit-test czyta teraz rzeczywiste prostokąty ekranowe przycisków przez WM_GETTITLEBARINFOEX i zwraca HTMINBUTTON/HTMAXBUTTON/HTCLOSE. Windows nadal rysuje i wykonuje przyciski; nie dodano ich imitacji ani własnych handlerów maximize/close. Powtórzona próba i rzeczywiste kliknięcie Przywróć zakończyły się sukcesem.

NCCALCSIZE zachowuje natywne boki/dół oraz systemowy margines przy maksymalizacji. Natywne metryki i prostokąty są wspólną podstawą geometrii. Dokumentacja mechanizmu: [Microsoft Custom Window Frame Using DWM](https://learn.microsoft.com/en-us/windows/win32/dwm/customframe). Ustalenie dodatkowego przeliczenia WinForms opiera się na lokalnym trace, nie tylko na dokumentacji.

Nie wyszukiwano starego prototypu w historii ani artefaktach. W normalnie czytanym kodzie nie znaleziono aktywnych pozostałości do usunięcia. Nie wykonywano sprzątania lub rollbacku. Brak globalnego WS_EX_COMPOSITED, DoEvents, dodatkowego runtime i zmian WPF.

## Celowane testy metod, kontrolek i komunikatów Windows

Runner `Run-Patch082Tests.ps1` wymaga jednego Case. Uruchomienie bez Case sprawdzono raz: błąd użycia, bez builda i uruchomienia innych prób. Nie uruchamiano Run-AllTests ani historycznych kampanii.

| Próba | Wynik |
|---|---|
| geometry: rzeczywiste SC_MAXIMIZE/SC_RESTORE, po3 cykle dla1440×940 i1220×780 | PASS, identyczny rozmiar i pozycja90,70 po każdym powrocie |
| klient przed/po dla tych dwóch rozmiarów | 1424×932 oraz1204×772, bez zmiany |
| maksymalizacja na monitorze testu Win32 | zewnętrzne(-8,-8)2576×1408, klient2560×1392 |
| SC_MINIMIZE/SC_RESTORE i WM_NCLBUTTONDBLCLK | PASS |
| Scale1.25 i dwa kolejne cykle systemowe | PASS, rozmiar1521×965, klient1505×957; **symulacja skalowania kontrolek**, nie fizyczny test DPI |
| caption: minimum1180×748 i1440×940; długa/krótka nazwa, dwa rzędy, sześć menu | PASS, brak kolizji, krótka nazwa centralnie |
| HTCAPTION, menu HTCLIENT, górna/lewa krawędź i dolny prawy narożnik | PASS |
| natywny przycisk Max/Przywróć po poprawce | PASS,9 zarówno normalnie, jak i po maksymalizacji |
| SC_CLOSE przy needsFirstSave i odpowiedzi Cancel | PASS, pytanie wywołane raz, okno pozostaje, gwiazdka zachowana |

Ostatnie zaliczone logi pod `tests/_work`: `patch-082-geometry-22386c7e55f94665945f0a072107ebcc/results.log` i `patch-082-caption-32d0e069b93d47cb968cddadeb36519c/results.log`. Wcześniejsze powtórzenia wynikały z rozpoznanego wzrostu rozmiaru, rozszerzenia o brakujące zależne kontrole i regresji HTCLIENT. Po końcowym hit-teście nie zmieniano produkcyjnej logiki.

## Zwykły EXE — rzeczywiste działania

Własna kopia `tests/_work/patch-082-final2/HexMapEditor.exe`, własna mapa „Próba ramki”, izolowane Library i MRU. Próby przez Computer Use, oddzielnie od testów powyżej:

- Natywne Max→Przywróć: rzeczywiste kliknięcia; widoczna ramka DWM1426×933 wraca do identycznych wymiarów i pozycji(-1673,202). To prostokąt widoczny w przechwyceniu, nie zewnętrzny1440×940 z Win32.
- Min przyciskiem i ponowna aktywacja: poprawny powrót. Zamknij przyciskiem: własny czysty proces zakończony; lista okien i procesów potwierdziła brak pozostawionej kopii.
- Klik Plik otwiera prawdziwe menu bez przesuwania okna; Right przechodzi do Edycja; Escape zamyka. Alt+Spacja otwiera natywne menu systemowe, Escape je zamyka.
- Resize prawym bokiem i lewym górnym narożnikiem osiąga minimum: widoczna ramka1166×741 odpowiada zewnętrznemu1180×748. Menu, nazwa i przyciski bez kolizji.
- Drag po nazwie przesuwa okno o zadaną deltę70×65px, nie zmienia rozmiaru. W poprzedniej sesji tego podejścia zaliczono też drag pustej strefy.
- Dwuklik nazwy maksymalizuje. Drag z maksymalizacji przywraca właściwy rozmiar1166×741. API wykonuje jeden odcinek: nie uznajemy tego za ocenę ciągłego podążania pod kursorem.

Pierwotna awaria przechwytywania GPU0x887A0005 nie była podstawą poprawiania aplikacji. Po zamknięciu własnej kopii przez użytkownika i odnowieniu sesji narzędzia przechwytywanie działało. Ponowne otwarcie tej samej kopii służyło wyłącznie dokończeniu brakujących prób. Chwilowy brak celu przy przejściu start→edytor rozwiązano odczytem nowego okna, bez ponawiania otwarcia mapy.

## Artefakt, formaty i ograniczenia

Główny `HexMapEditor.exe` otrzymał dokładnie bajty z przetestowanego końcowego builda, przez atomową podmianę po zamknięciu własnej kopii. FileVersion/ProductVersion **0.8.2.0**, wspólne metadane **0.8.2 / 14.11.2026**. SHA-256 głównego i kopii:

`399222EB72C6C19869700DBC4DA929059464C5F76216B6DC4C81E67C40BBC957`

Map11/preset4, struktura Library, limity i model dokumentu bez zmian. Brak drugiego wariantu paska. Nie wdrożono0.8.3, zakładek, ustawień ani aktualizatora.

Niezaliczone/niewykonane: snapping (API nie pozwala kończyć gestu poza granicami wyjściowego okna), ciągła trajektoria drag-from-maximized, fizyczne przejście między różnymi DPI, pełna macierz wersji Windows. Gesty w dolnym i prawym górnym rogu z widocznego screenshotu nie potwierdziły resize narożnika; zaliczono lewy górny gest i dolny prawy hit-test Win32. Nie wykonano osobnego Alt+F4 z niezapisanym dokumentem w zwykłym EXE — ochronę sprawdzono rzeczywistym SC_CLOSE w teście. Nie deklarujemy braku flasha ani płynności z pojedynczych zrzutów. Nie powtarzano autosave5min, rename, RGB, masek, stempla, bibliotek, gumki, WPF ani przenośności.

## Krótka checklista odbiorcza użytkownika

- [ ] Menu w pierwszym wierszu, szybkie polecenia po lewej w drugim; nazwa mapy bez kolizji.
- [ ] Kilka kliknięć Max/Przywróć nie zmienia zwykłego rozmiaru i położenia.
- [ ] Min, Zamknij, Alt+Spacja i menu/Escape działają.
- [ ] Przeciąganie pustego tytułu/nazwy, resize i minimum okna.
- [ ] Na własnym stanowisku: ciągły drag z maksymalizacji, snapping oraz używane DPI.
- [ ] Gwiazdka i sygnał Zapisz nadal widoczne dla niezapisanej mapy; zamknięcie respektuje pytanie.

## Git

Implementacja, główny EXE i testy: lokalny commit **511cdda**. Dokumentacja stanowi osobny commit zawierający ten raport (jego identyfikator: `git log -1 -- docs/raport-0.8.2.md`). Przed nim pozostały wyłącznie pięć własnych plików dokumentacji: README, specyfikacja, plan wersji, plan i raport0.8.2. Końcowe sprawdzenie Git następuje po tym commicie. Brak zastanych zmian do zachowania; nie zmieniono prywatnych danych. Własne procesy testowe zamknięte. Bez push, tagu i Release'a. Nie deklarujemy kompleksowego przetestowania aplikacji.
