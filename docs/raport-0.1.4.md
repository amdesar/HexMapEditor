# HexMapEditor 0.1.4 — mała łatka

Data: 5 listopada 2026. Format mapy: **1**.

## Baza i zakres

Praca wyłącznie w głównym `HexMapEditor-Codex`. Początkowo czysty `master`, commit `1e8dd212b935b79ee1e93f1c28460cb6083ddbe7`, zgodny z `origin/master` po pobraniu referencji. Fast-forward nie był potrzebny. Zachowano C# / Windows Forms / GDI+, tryb offline i brak pobierania zależności przy uruchamianiu. Nie realizowano funkcji 0.2.0.

## Rzeczywista przyczyna dwukliku

Odtworzono błąd w diagnostycznej kompilacji kodu 0.1.3 z aktywnym Pędzlem i rzeczywistym dwuklikiem w natywnym dialogu Otwórz. Powstał pomalowany heks, gwiazdka i wpis Undo. Log wykazał następującą kolejność:

```text
OPEN RETURN OK buttons=Left ... dirty=False stroke=0 undo=0 capture=False
LOAD MODEL buttons=Left ... dirty=False stroke=0 undo=0 capture=False
MOVE buttons=Left event=Left/0@{X=279,Y=100} ... stroke=0 undo=0
PAINT 0 buttons=Left ...
UP buttons=None event=Left/1@{X=279,Y=100} ... dirty=True stroke=1 undo=0
FINISH ...
```

Pomiędzy załadowaniem mapy i malowaniem **nie było CanvasMouseDown**. Dialog zakończył się, gdy drugi przycisk dwukliku był jeszcze wciśnięty. Płótno otrzymało ruch z przyciskiem Left i późniejsze Up. `CanvasMouseMove` malował na podstawie samego stanu przycisku, bez wymagania początku gestu na płótnie. `CanvasMouseUp` kończył takie obce pociągnięcie i zapisywał je w historii. To rozpoznana przyczyna, a nie założenie o dodatkowym kliknięciu przechodzącym przez okno.

Naprawa przechowuje przycisk i narzędzie gestu rozpoczętego na płótnie. Ruch i Up bez tego początku nie malują, nie zaznaczają i nie przesuwają. Wspólna obsługa modalna kończy istniejący prawidłowy gest przed dialogiem, czyści stan i przechwycenie przed i po nim, a następnie wymaga zwolnienia przycisków i świeżego Down. Odrzuca też końcówkę dwukliku jako pierwszy gest po dialogu. Utrata przechwycenia kończy własny gest. Narzędzie pozostaje wybrane. Nie zastosowano arbitralnego opóźnienia ani wyłączenia otwierania dwuklikiem.

Diagnostyczny EXE **nie zastępował głównego EXE**. Zgodnie z późniejszym poleceniem użytkownika pozostawiono lokalnie tylko `HexMapEditor.Trace.exe` i `input.log` w `tests/_work/repro-b83327d9a45649a1b7e5304eb94ef5cb/`. Są ignorowane i nie należą do commita. Roboczy kod, mapę kontrolną i puste katalogi tej próby usunięto po zakończeniu procesu.

## Testy metod i kontrolek

- Historia: **11/11** — zachowane regresje 0.1.2, w tym tekst, NotesFormat, puste pociągnięcia i stan zapisu.
- Techniczne: **12/12** — zapis 0.1.3, ścieżki, odmowy/błędy, biblioteka, wymiary i orientacje. Test layoutu sprawdza teraz osobne zakresy w trzeciej kolumnie, kursywę, odstęp i miejsce na tekst.
- Gesty: **10/10** — obce Move/Up dla wszystkich narzędzi, zarejestrowana sekwencja błędu, świeże malowanie i grupowanie pól, wymóg zwolnienia przycisku, końcówka dwukliku, zaznaczanie, przesuwanie także środkiem myszy, reset przed dialogiem, zagnieżdżone/wyjątkowe wyjście z dialogów i utrata przechwycenia.
- Build głównego `HexMapEditor.exe` w wersji 0.1.4: poprawny.

To testy metod i kontrolek, nie zastępstwo prób natywnego dialogu.

## Próby rzeczywistego okna końcowego EXE 0.1.4

Użyto zwykłego głównego `HexMapEditor.exe`, nie kompilacji diagnostycznej. Z aktywnym Pędzlem:

- Otwarcie mapy dwuklikiem w tym samym położeniu, które odtworzyło błąd 0.1.3: mapa bez dodatkowego malowania, brak gwiazdki, Cofnij/Ponów nieaktywne; Pędzel pozostał wybrany.
- Pierwszy świadomy klik na płótnie pomalował heks, włączył Cofnij i gwiazdkę.
- Otwarcie Enterem oraz przyciskiem Otwórz: bez dodatkowej edycji, brak gwiazdki i nowej historii.
- Anulowanie Otwórz myszą: bez edycji i zmian historii.
- Anulowanie Zapisz jako oraz ostrzeżenia o niezapisanych zmianach myszą: bez dodatkowego malowania; istniejący wcześniej stan zapisu i historii pozostał zachowany.
- Anulowanie Nowa mapa myszą: bez dodatkowej edycji, zaznaczenia i nowej historii.
- Okno Nowa mapa: widoczne 15×10, osobne `(1–200)` kursywą na prawo od obu pól, odstępy i brak obcinania w sprawdzonej konfiguracji.

Sesje zakończono, a własne mapy i katalog końcowej próby UI usunięto. Nie tworzono map kontrolnych w użytkowym Library ani systemowym TEMP.

## Miejsce testów, sprzątanie i Git

Wszystkie trzy skrypty tworzą własne podkatalogi przebiegów wyłącznie w `tests/_work/`; EXE i mapy również tam trafiają. Standardowe przebiegi sprzątają własne wyniki po zakończeniu procesu. Sprawdzono usunięcie i ponowne odtworzenie głównego `_work` przez skrypt. Dane diagnostyczne zachowano wtedy w pamięci i przywrócono bajt w bajt, bez kopii poza `_work`. Po usunięciu `bin` ponownie uruchomiono wszystkie trzy zestawy: przeszły i nie odtworzyły `bin`.

W głównym `bin` były wyłącznie:

- `HexMapEditor.HistoryTests.exe` — znany wygenerowany program testów 0.1.2;
- `Nowa mapa.hexmap` i `Nowa mapa.txt.hexmap` — znane mapy kontrolne wcześniejszych prób, 15×10, wszystkie pola neutralne, bez nazw/opisów. Przed usunięciem sprawdzono również zgodność SHA-256 z wcześniej odnotowaną zawartością.

Usunięto te trzy pliki i pusty główny `bin`. Nie znaleziono nieznanych danych. Nie usuwano głównego EXE, źródeł, skryptów ani danych biblioteki. Aplikacja nie wykonuje sprzątania przy starcie.

`.gitignore` wyklucza główne `/Library/`, `/bin/` i `/tests/_work/`. `Library/.gitkeep` wyłączono ze śledzenia przez `git rm --cached`, zachowując go lokalnie. Cała biblioteka pozostaje lokalna; tworzenie brakujących katalogów i zachowanie istniejącego zasobu przeszło test techniczny. Ścieżki map i zachowanie zapisu 0.1.3 nie zostały zmienione.

Źródła/skrypty `tests/`, kod, dokumentacja i główny EXE pozostają śledzone. `git check-ignore` potwierdza wykluczenie plików biblioteki i artefaktów; przegląd indeksu nie dopuszcza map, logów ani testowych EXE. Paczka użytkownika ma pomijać całe `tests/`.

## Ograniczenia i checklista użytkownika

Rzeczywiste próby dialogów wykonano z Pędzlem. Ochronę zaznaczania i przesuwania oraz utratę przechwycenia sprawdzono testami metod; nie wykonano pełnej macierzy wszystkich narzędzi i wszystkich okien Windows. Nie sprawdzano wszystkich DPI, urządzeń wejściowych, pełnego dysku, awarii zasilania ani czystego komputera offline.

Do krótkiej oceny: otworzyć mapę dwuklikiem z Pędzlem i sprawdzić brak gwiazdki/Undo, następnie świadomie pomalować pole; otworzyć Enterem i przyciskiem; anulować Otwórz i Nowa mapa; obejrzeć zakresy po prawej stronie pól. Testy można ponowić trzema skryptami z README, a `_work` usunąć po zamknięciu procesów, jeśli zachowana diagnostyka nie jest już potrzebna.
