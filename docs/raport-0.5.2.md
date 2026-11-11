# Raport HexMapEditor 0.5.2

## Wynik i przyczyna

Łatka na bazie `67120d1`; repozytorium było czyste. Nie odczytywano ani nie zmieniano prywatnej Library, presetów i map użytkownika. Nowe formaty bez zmian: mapa 8, preset 3, grafika terenu 2. Aplikacja 0.5.2, FileVersion głównego EXE 0.5.2.0.

W czystej, odizolowanej kopii gotowego 0.5.1 zarówno Nowy preset, jak i Domyślny jako kopia działały. W tej samej kopii dodano własny syntetyczny wpis z czytelnymi ID/nazwą i nieobsługiwaną definicją terenów. Obie operacje zaczęły kończyć się dokładnym zgłoszonym komunikatem: „Niepełna lub nieobsługiwana definicja terenu; plik pozostawiono bez zmian”.

Przyczyna jest w ścieżce ZAPISU nowych danych: `PresetCatalog.PrepareWrite` w celu wykrycia kolizji wywoływał pełny `ReadFile` dla każdego niezwiązanego pliku katalogu. Błąd wpisu pominiętego przez listę blokował poprawnego kandydata jeszcze przed jego serializacją i zmianą projektu. Ta sama zależność była w `PresetFileChange.Write`, także dla Undo/Redo. Nowy preset i kopia Domyślnego korzystają z obu tych wspólnych metod. Konstrukcja neutralnego i pustki, pule, kopiowanie oraz zapis aktualnego formatu nie wymagały obejścia ani migracji.

Nie ustalano nazwy prywatnego pliku użytkownika i nie twierdzimy, że ją sprawdzono. Mechanizm i dokładny błąd odtworzono na własnych danych; brak konwersji starego pliku nie jest rozwiązaniem ani wyjaśnieniem, dlaczego aplikacja powinna blokować nowe dane. Nie trzeba kasować Library.

## Lokalna naprawa

Dodano oddzielny od importu `ReadIdentity`: rezerwuje jawne ID i nazwę oraz odczytuje kolejność, nie uznając zawartości pomijanego pliku za zaimportowany preset. Użyto go w dwóch miejscach kontroli kolizji. Kandydat nadal przechodzi pełne Validate, a rzeczywiście nadpisywany plik pełne ReadFile i kontrolę bajtów expected. Load/ReadFile, LibraryFiles, budowanie neutralnego/Domyślnego, klonowanie i formaty nie zostały rozluźnione ani przebudowane.

Nieznane ID/nazwa (np. plik `{}` lub uszkodzony JSON) nadal mogą bezpiecznie zablokować zapis, bo nie można wykluczyć ukrytej kolizji; komunikat wskazuje problem. Czytelna tożsamość pominiętego wpisu nadal blokuje zbieżne ID/nazwę. Zajęta nazwa pliku otrzymuje istniejący czytelny sufiks. Obce bajty, niejednoznaczny cel i zmiana zewnętrzna nie są cicho nadpisywane. Nie zmieniono żadnego prywatnego ani syntetycznego starego pliku w ramach naprawy.

## Testy metod — wykonane

Runner `tests/Run-Patch052Tests.ps1` wymaga jednego jawnego Case: regression / lifecycle / safety; nie ma domyślnego uruchomienia wszystkich. Nie uruchamiano Run-AllTests, siedmiu przypadków 0.5.1 ani dawnych kampanii.

- **regression — FAIL przed / PASS po poprawce.** Zasadnicza próba wywołuje rzeczywiste `TryCreatePreset` i `TrySaveWorkingPreset` używane przez przyciski, z syntetycznym pomijanym wpisem. Nowy ma neutralny teren i format 3; kopia Domyślnego ma niezależne ID i edytowalność, zachowuje sześć terenów, neutralny i pustkę. Mutacja puli kopii nie zmienia wzorca. Natychmiastowy odczyt działa, Load nadal ostrzega o pominiętym wpisie, jego bajty pozostają niezmienione. FAIL: `patch-052-regression-da66a58f6b184dd49f92e3721abbae74`; PASS: `patch-052-regression-fb771bba5dd54f6fbac295cb56b5bb4e`.
- **lifecycle — PASS.** Utworzenie rzeczywistą ścieżką, AddTerrain i UpdateTerrain, nazwa/kolor/mieszana pula z własnym obrazem, zapis i odczyt pełnego źródła, referencji i obu różnych kadrów. Kopia własnego presetu, niezależna mutowalna lista wariantów i robocza edycja kadru, wybór innego/powrót, przemianowanie pliku bez duplikatu, usunięcie terenu i ponowny zapis. Nowy MainWindow i Load z dysku bez oryginalnego PNG; usunięcie presetu oraz Undo/Redo pliku. Odrzucone usunięcie/nadpisanie Domyślnego. `patch-052-lifecycle-ad42f100fc6848ee915c059dc524720f`.
- **safety — PASS.** Niepoprawna pusta pula odrzucona bez zmiany aktywnej mapy/pliku. Pominięte ID/nazwa nadal rezerwowane. Zajęty plik Nowy.json zachował obce bajty, zapis użył Nowy (2).json. Konflikt duplikatu ID podczas Undo nie przesunął historii ani danych. Konflikt ścieżki Redo nie nadpisał obcych bajtów; po usunięciu własnego konfliktu ponowienie zadziałało. Nieczytelna tożsamość `{}` odrzucona bez połowicznej zmiany. `patch-052-safety-d851499750f24e25b42e2184785060d7`.

Wszystkie katalogi powyżej są w `tests/_work`. Zaliczonych prób nie powtarzano; tylko regresję po naprawie jej jawnego FAIL. Testy korzystają z prywatnych metod aplikacji przez refleksję i nie są przedstawiane jako obsługa okna.

## Próby zwykłego końcowego EXE — wykonane

Kopia `tests/_work/ui-052-a6f8f021b9f646d08f6833586eb7f865/HexMapEditor.exe` była identyczna bajtowo z głównym EXE przez cały obowiązkowy przebieg, również po restarcie. SHA-256 obu: `98C8D1E84B8A58D7BF1184E6C2C5A55F68D51805A2F46AD8939D9819147E07E6`. Własna początkowo pusta Library, żadnych danych użytkownika.

1. Utworzono **Próba 0.5.2** przez plus presetów, wybrano go automatycznie, plik powstał.
2. Dodano **Nowy teren**, następnie ołówkiem zmieniono nazwę na **Łąka po edycji** i kolor pickerem HEX na **#24683A**.
3. Zapisano **Nadpisz bieżący preset**; status zmienił się na zgodność z dyskiem. Własny plik potwierdził nazwę, kolor, pulę i revision 2.
4. Wybrano Domyślny i zapisano **Domyślny — kopia** jako własny, od razu edytowalny preset. Nadpisanie wzorca pozostało nieaktywne.
5. Anulowano kolejny Nowy preset; nie powstał trzeci plik ani pozycja. Zapisano własną mapę i zamknięto własne okno.
6. Ponownie uruchomiono tę samą kopię EXE. Na liście były oba własne presety. Wybrano Próba 0.5.2, widoczna Łąka po edycji miała zielony kolor i status zgodności z zapisanym presetem. Zapisano własną mapę po restarcie pod odrębną nazwą i zamknięto proces.

Jest to pełna wymagana krótka sekwencja UI, nie sam start programu. Odtworzenie błędu 0.5.1 wykonywano w osobnym `hotfix052-df59c6746abd45adb373e990247f5a85`, którego nie użyto jako prywatnej biblioteki.

## Ograniczenia

Nie wykonano pełnego audytu, testów DPI, wydajności, wszystkich obrazów/limitów ani regresji całych opisów, obiektów, mgły i tła. Grafikę i oba kadry sprawdzono w celowanym round-trip metod, nie ponawiano całej kampanii importu UI 0.5.1. Nie testowano każdego wariantu błędu sprzętowego I/O. Nie zmieniano prywatnych danych, nie dodano zgodności ze starymi formatami ani funkcji backlogu.

Zastany tekst pomocniczy okna nadpisania presetu nadal mówi, że historia nie cofa plików biblioteki; faktyczny mechanizm jest sesyjny i test Undo/Redo pliku przeszedł. Tego napisu nie przedstawiamy jako naprawionego w tej pilnej poprawce.

## Checklista użytkownika

- Uruchom EXE 0.5.2 bez kasowania Library; utwórz własny preset.
- Dodaj teren, zmień nazwę/kolor, zapisz i sprawdź po restarcie.
- Zapisz Domyślny jako kopię; edytuj kopię, wzorzec pozostaje chroniony.
- Dla mieszanej puli sprawdź zapis/odczyt oraz własny obraz i kadry.
- Jeśli program zgłosi nieczytelne ID/nazwę konkretnego pliku, zachowaj go i podaj komunikat — nie usuwaj całej biblioteki.

## Artefakty i Git

Źródła/testy i dokumentacja są przeznaczone do commita, dane prób i kopie EXE pozostają ignorowane w tests/_work. Własne okna zakończone, nie ma oczekującego dialogu. Główny EXE został zbudowany lokalnym Build.ps1. Bez push, tagu i Release'a. Hash commita implementacji i stan po commitach zostaną dopisane przy finalizacji.

Commit implementacji/EXE i regresji: `00410ed5a3ad89d60a8f1dc63af652962551a37a`. Po nim pozostały wyłącznie cztery przewidziane pliki dokumentacji. Commit dokumentacji wskazuje końcowa odpowiedź; oczekiwany stan po nim to czysty master, dwa lokalne commity przed origin/master, bez publikacji.
