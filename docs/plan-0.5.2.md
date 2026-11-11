# Plan poprawki 0.5.2

Baza 67120d1, master i czysty stan przy rozpoczęciu. Główny EXE 0.5.1. Wszystkie próby w tests/_work; prywatna Library nieodczytywana i niemodyfikowana.

1. Odtworzenie obu działań i wspólnej przyczyny.
2. Lokalna poprawka kontroli katalogu, bez rozluźnienia walidacji ładowanych presetów i bez migracji.
3. Celowane scenariusze tworzenia/klonowania, edycji/round-trip, cyklu życia i konfliktów.
4. Build 0.5.2 i obowiązkowy przebieg zwykłego końcowego EXE, dokumentacja i lokalny commit.

## Punkt postępu
W czystej kopii 0.5.1 obie operacje zadziałały. Dodanie jednego własnego syntetycznego pliku formatu 2 do tej samej biblioteki odtworzyło dokładny błąd Nowy preset. PrepareWrite wywołuje ReadFile dla każdego pliku, więc błąd niezwiązanego wpisu blokuje zapis poprawnego kandydata przed serializacją. PresetFileChange.Write powtarza tę samą pełną kontrolę. Pozostało potwierdzić drugą operację przy syntetycznym pliku, naprawić wspólną inwentaryzację ID/nazw (bez ładowania nieobsługiwanej zawartości), testy i EXE.
Własne okno: tests/_work/hotfix052-df59c6746abd45adb373e990247f5a85/HexMapEditor.exe, mapa niezapisana po utworzeniu własnych presetów. Ostatnia operacja: zamknięcie komunikatu błędu. Użytkownik poinformował o zamknięciu głównego okna przed buildem; sprawdzić rzeczywisty stan przy budowaniu.

## Naprawa i testy metod
Obie awarie odtworzono w zwykłej kopii 0.5.1 po dodaniu własnego syntetycznego pliku pomijanego formatu. Regresja przed poprawką FAIL w patch-052-regression-da66a58f6b184dd49f92e3721abbae74. Naprawa ogranicza kontrolę kolizji w PrepareWrite i PresetFileChange.Write do jawnego ID/nazwy/kolejności; rzeczywiście nadpisywany plik nadal przechodzi pełne ReadFile. Brak czytelnego ID/nazwy nadal blokuje zapis, by nie ukryć kolizji. Load/ReadFile, LibraryFiles i formaty bez zmian.
PASS regression: patch-052-regression-fb771bba5dd54f6fbac295cb56b5bb4e.
PASS lifecycle: patch-052-lifecycle-ad42f100fc6848ee915c059dc524720f.
PASS safety: patch-052-safety-d851499750f24e25b42e2184785060d7.
Nie powtarzać zaliczonych scenariuszy. Własna diagnostyczna mapa zapisana w izolowanej Library/Hexmaps i okno zamknięte. Użytkownik zamknął główny program; inwentaryzacja nie wykazała go przed budowaniem. Ustawiono oznaczenie 0.5.2/0.5.2.0; następna operacja to końcowy build i obowiązkowy pełny przebieg UI na identycznej kopii w nowym katalogu _work. Nie uznawać samego startu za zaliczenie UI.

## Próba końcowego EXE w toku
Główny EXE i kopia ui-052-a6f8f021b9f646d08f6833586eb7f865 mają FileVersion 0.5.2.0, wspólny SHA-256 98C8D1E84B8A58D7BF1184E6C2C5A55F68D51805A2F46AD8939D9819147E07E6. W zwykłym oknie: utworzono Próba 0.5.2, dodano Nowy teren, ołówkiem zmieniono nazwę na Łąka po edycji i HEX na #24683A, zapisano Nadpisz bieżący preset, zapisano Domyślny jako Domyślny — kopia. Anulowanie kolejnego Nowy preset nie dodało pliku ani wpisu. Własna mapa została zapisana w jej Library/Hexmaps. Ostatnia akcja Alt+F4 zamykała własne okno. Przed wznowieniem sprawdzić, czy się zamknęło; pozostał restart i wybór Próba 0.5.2 z zachowaną nazwą/kolorem, potem dokumentacja i commit. Nie powtarzać kroków 1–4.

## Próby zakończone
Restart końcowej kopii zadziałał, lista zawierała Próba 0.5.2 i Domyślny — kopia. Po wybraniu Próba 0.5.2 widoczna Łąka po edycji w zapisanym kolorze #24683A, status zgodności palety z dyskiem. Obowiązkowe sześć kroków UI zaliczone na tej samej bajtowo kopii finalnego EXE. Zapisano mapę po restarcie pod odrębną nazwą. Ostatnia operacja Alt+F4 zamykała własne okno; sprawdzić zakończenie przed finalizacją. Pozostało wyłącznie raport, przegląd i lokalny commit. Bez ponawiania metod/UI.

Okno po ostatnim Alt+F4 zniknęło z inwentaryzacji; brak własnych procesów/dialogów. Raport i dokumentacja zapisane. Główny EXE i kopia nadal identyczne. Zakres ukończony; pozostał tylko przegląd i lokalne commity, bez ponawiania prób.

Zakończenie: implementacja, finalny EXE i testy w `00410ed5a3ad89d60a8f1dc63af652962551a37a`. Pozostały tylko README, specyfikacja, raport i plan do końcowego commita dokumentacji. Nie ma niewykonanych kroków zakresu ani własnych otwartych okien; ograniczenia testów są jawnie wymienione w raporcie.
