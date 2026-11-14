# Plan 0.8.3

- Potwierdzono czysty Git, HEAD a31405b i kod 511cdda; przeczytano raport i plan 0.8.2. Nie znaleziono AGENTS.md w repozytorium.
- Plan: split zoomu; wyrównanie pól nazw; warunkowy wybór pędzla; wspólny renderer strzałek. Następnie celowane testy i izolowana próba zwykłego EXE, dokumentacja i finalny build.
- Zachować zaakceptowaną ramkę i wszystkie dane użytkownika. Testy i kopie tylko w tests/_work.

## Punkt wznowienia

Wykonane: odczyt bazy, stanu Git i dokumentacji. Znaleziono wspólną paletę FitMenuColors oraz miejsca tworzenia rendererów w CommandBars.cs i ViewLayers.cs.
Pozostałe: cała implementacja, testy, dokumentacja końcowa i EXE. Nie uruchomiono testów ani procesów testowych. Ostatnia operacja: odczyt kodu, wynik pewny. Główny EXE pozostaje 0.8.2. Nie utworzono commita.

## Etap implementacji — w toku

Wprowadzono prototyp czterech lokalnych zmian: osobne przyciski zoomu we wspólnym hoście, wspólny jasny renderer strzałek, zastosowanie istniejącego MaterialChosen do wyboru obiektu i naturalną wysokość jednoliniowych pól nazw wyśrodkowanych w wierszu. Rozpoznano wymuszanie wysokości pola obiektu do 26 px i Dock Fill w rename; do sprawdzenia kontrolkami. Ramki nie zmieniono.

Pozostałe: kompilacja prototypu, testy metodyczne i rzeczywistej sesji EXE, poprawki z testów, wersja i dokumentacja. Próby: brak. Ostatnia operacja: zapis zmian źródeł, wynik pewny. Brak własnych procesów/okien. Główny EXE nadal 0.8.2; prototyp nie jest wydaniem.

## Testy w toku

Build izolowany przeszedł; `tests/_work/patch-083-controls-6fe9ce3a20544d6eb0ce81b5cabe9dd7/results.log`: PASS wspólnej reguły narzędzi dla wszystkich enum, przyciski zoomu, maksymalny tekst, wysokość pól i renderer. Zbudowano kandydata 0.8.3.0 w `tests/_work/patch-083-final` na potrzeby próby. Zwykły EXE: reset 146→100, otwarcie i drugi klik zamykający listę, jasne strzałki Widoku, wygląd rename oraz pola nazwy instancji; wybór obiektu przy Kursorze nie przełącza. Własną instancję zapisano we własnej mapie, zamknięto własny proces. Test nie potwierdza pozostałych kombinacji ani edycji tekstu. Nie użyto prywatnych danych.

Pozostałe: uzupełnić celowane próby brakujących integracji, zdecydować czy potrzebna korekta po wynikach; tylko po ich ukończeniu główny EXE i raport. Ostatnia operacja: zapis planu; wynik pewny. Brak własnych otwartych okien. Główny EXE 0.8.2, kandydat w _work nie jest jeszcze wydaniem.

## Końcowy etap

Rozszerzony Case controls `patch-083-controls-19ac49e8483743d99e1208f3b5f9f76d` PASS: rzeczywiste SelectTerrain/SelectIcon/SelectObjectTemplate × cztery narzędzia masek i Rączka/Rysik. Powtórzono wyłącznie ze względu na nowe asercje integracji. Główny EXE atomowo otrzymał przetestowany kandydat 0.8.3.0; SHA-256 obu `8FFAEF057BD0638580AB7F3DE845D31A8D3A315B71CC2C44436E215CAA9F622B`. Wynik sprawdzony; brak własnych okien. Pozostałe: raport, przegląd Git i ewentualne lokalne commity. Nie testowano osobno każdego presetu, custom zoomu ani wpisywania w obu polach; ujęte jako ograniczenia raportu. Ostatnia operacja: dokumentacja, bez niepewnej operacji dyskowej.

Zakończono dokumentację i raport. Dodatkowy Case controls `patch-083-controls-c07aaedf54b8449386351cabd2c89614` PASS po dodaniu asercji presetu50% i obecności pozycji Niestandardowe; powtórzenie z powodu rozszerzenia testu. Wersja głównego EXE i zgodność SHA-256 potwierdzone. Brak własnych procesów, brak niepewnej operacji; nie wykonywano push/tagu/Release'a. Zmiany pozostają lokalnie niezacommitowane.
