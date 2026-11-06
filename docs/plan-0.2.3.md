# Plan 0.2.3

Baza: czysty master b0fd2b6, zgodny z origin/master po fetch. Zachowujemy mapę 2 (odczyt 1), preset 1, offline i istniejącą aplikację. Usuniętej przez użytkownika Library nie odzyskujemy; próby wyłącznie w tests/_work.

1. Czytelne, bezpieczne nazwy plików: polskie znaki/spacje, normalizacja Windows, ograniczenie długości, sufiksy kolizji. ID pozostaje niezależne. Stare pliki czytane bez masowego przemianowania. Przy jawnym zapisie jeden aktywny plik; zmiana nazwy z bezpiecznym zapisem i rollbackiem przy błędzie.
2. Odczyt/odświeżanie istniejącym walidatorem: aktualizacja listy biblioteki bez edycji projektu, historii, wyboru i roboczej palety. Komunikaty o błędach, konflikcie, zmianie/zniknięciu aktywnego pliku. Bez obserwatora.
3. Nazewnictwo presetów, 2×2 przyciski i kompaktowy refresh przy selektorze. Responsywna zawartość lewego panelu z rezerwą na scroll, niższe wiersze, stałe akcje, czerwony kosz/krótkie tips. Zachować kontrolki i funkcje przybornika.
4. Ctrl+Shift+V tylko przy fokusie opisu: istniejące Paste(true), zatrzymane zdarzenie, jedna historia, etykieta skrótu w menu. Krótki przegląd pozycjonowania prawego nagłówka; wyłącznie mała poprawka jeśli uzasadniona.
5. Celowane testy etapów, zwykły końcowy EXE w izolowanej bibliotece, raz końcowe wszystkie regresje. README/specyfikacja/raport i lokalny commit, bez push/tagu/Release'a.

Odłożone: schemat rozmiaru z siedmioma heksami, tymczasowe wyróżnienie zagrożonych pól, zakładki projektów, stylistyka grup, przebudowa opisów. Przycisk Rozmiar bez zmian.

## Zakończenie etapów

1–4 wdrożone i sprawdzone celowanymi 7 testami. Prawy nagłówek: brak potwierdzonej prostej zbędnej pętli pozycjonowania; pozostawiony bez zmiany, przeskok nie jest naprawiony. Etap 5: jeden zbiorczy przebieg 111/111, exit 0; rzeczywiste próby zwykłego EXE (minimum/duże okno, presety/refresh/rename, scroll/tips, Ctrl+Shift+V i Undo/Redo) opisane osobno w raporcie. Końcowy build po ujednoliceniu pojedynczego komunikatu migracji uruchomiony bez ostrzeżeń. README/specyfikacja/raport gotowe. Własny przebieg UI sprzątnięty po zamknięciu procesów; pozostał ignorowany log. Końcowa kontrola przygotowanych plików i lokalny commit zamykają etap; bez push/tagu/Release'a. Usuniętej Library nie odzyskiwano.
