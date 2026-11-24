# HexMapEditor 0.12.2

## 1. Wykonanie

- Powitanie ma zaokrąglony przycisk GitHub po lewej, z widocznym fokusem i stanem najechania. Otwiera stały adres projektu w przeglądarce. Sprawdzanie aktualizacji jest po prawej. Pod pełną listą pięciu projektów zachowano odstęp.
- Nowa mapa z powitania trafia raz do `<aktywna Library>\Hexmaps` w każdym trybie. Katalog Data nadal służy MRU i ustawieniom. Dotychczasowy `Data\Hexmaps` zachowuje swoją zawartość i pozostaje możliwy do odczytu. `Zapisz jako` nadal pozwala wskazać inny katalog. Tworzenie nowej karty wewnątrz edytora zachowuje dotychczasową ścieżkę.
- Kanał testowy `amdesar/HexMapEditor` czyta listę wydań bez poświadczeń. Uwzględnia opublikowane pre-release’y i wydania stabilne z tagiem liczbowym `vX.Y.Z` (lub `X.Y.Z`). Szkice i wersje równe lub starsze są pomijane. Do 20 stron po 30 wyników, z limitem 2 MiB na stronę. Zwracana jest najwyższa parsowalna wersja. Brak właściwej paczki w tym wydaniu daje komunikat z jej nazwą. Niedostępne repozytorium daje błąd dostępu. Testowe oznaczenie GitHuba jest niezależne od liczbowego tagu. Przy 1.0.0 planowany jest domyślny kanał stabilny. [GitHub REST API – lista wydań](https://docs.github.com/en/rest/releases/releases).
- Setup zachowuje sprawdzanie typu, rozmiaru, źródła i przekierowania, SHA-256, weryfikację tuż przed uruchomieniem i oznaczenie pochodzenia internetowego. Decyzja o instalacji korzysta ze zwykłego zamykania map i presetów. Portable prowadzi do właściwego ZIP do ręcznej aktualizacji.
- Setup zachowuje identyfikator instalacji, katalog, Library i zadania 0.12.1. Rozpoznaje starszy skrót po opisie i celu. Po utworzeniu własnego `HexMapEditor.lnk` usuwa swój skrót o starej nazwie. Przy kolizji respektuje zarówno skrót wspólny, jak i ręczny skrót na pulpicie pierwotnego użytkownika. Informuje o menu Start i pozostawia istniejące skróty. Odznaczenie lub odinstalowanie usuwa wyłącznie rozpoznane własne skróty.
- Nagłówek Zakładki jest pod Obiektami. Rozpatrywany preset zasila zwykłe kontrolki lewego panelu z osobnej migawki podglądu, także po zamknięciu ostatniej mapy. Anulowanie zachowuje nierozstrzygnięte drafty i wcześniejsze skutki zamknięcia. Kursor ma osobną kolumnę o szerokości wynikającej z etykiet siatki 1–200 i używanej czcionki. Zmiana współrzędnych nie przesuwa pól mapy ani kart.
- Komórka Mgła w macierzy respektuje efektywny stan członka, także przy grupowej zmianie kolumny. Rozbrojenie przejęcia stanu może zablokować komórkę bez wyczyszczenia zapisanej wartości. Ponowne uzyskanie uprawnienia odsłania ten sam stan. Widzenie i format Map12 zachowują dotychczasowe zasady.

## 2. Próby metod i kontrolek

- `channel` PASS. Stabilne wydanie, liczbowo nowszy pre-release na drugiej stronie, pominięcie szkicu, zła lub brakująca paczka, niedostępny kanał, nieprawidłowe źródło i typ, brak lub zła suma oraz zmieniony plik po pobraniu. Próbne dane korzystały z bajtów Setup. Osobno potwierdzono wybór właściwego linku Portable i odmowę uruchomienia Setup w tej ścieżce.
- `controls` PASS. Nagłówek w minimalnym oknie, współrzędne A1, GR200 oraz brak współrzędnych przy stałych prostokątach statusu i kart. W macierzy sprawdzono przywróconą z Map12 zapamiętaną wartość mgły przy niedostępnej komórce, przejęcie Sojusznika z wartością On i Off, ponowne przejście do Neutralnego, pole widzenia i masową zmianę kolumny.
- Zależna próba `close` z 0.11.1 potwierdziła normalne kontrolki w pustym edytorze, jeden podgląd współdzielonego presetu, częściowy zapis i ponowne otwarcie z zachowanym draftem. Próba zamknięcia z 0.12.0 potwierdziła anulowanie i dalsze działanie edytora oraz przekazanie sprawdzonego instalatora po udanym zapisie. Wykonano ją ze względu na zmianę podglądu presetów w tym samym przebiegu zamykania.

## 3. Rzeczywisty Setup i zwykły EXE

- Ręczny upgrade 0.12.1 → 0.12.2 wykonano rzeczywistym Inno Setup w izolowanych katalogach i kluczach. Po instalacji bazy 0.12.1 z własną Library, skrótem, mapą oraz MRU uruchomiono Setup 0.12.2 bez deinstalacji. Porównano pliki danych, zawartość Library i MRU przed i po. Próba potwierdziła zachowanie ręcznego skrótu i komunikat o kolizji, następnie zastąpienie starego własnego skrótu po zwolnieniu nazwy. Wyłączenie zadania usunęło własny skrót. Zainstalowane EXE miało bajty końcowego kandydata.
- Zwykły zainstalowany EXE pokazał pięć pozycji MRU oraz osobno GitHub i aktualizacje. Shift+Tab pokazał niebieski obrys fokusu przycisku GitHub. W edytorze obserwowano nagłówek Zakładki, położenie pól statusu oraz odblokowanie i ponowne zablokowanie mgły po zmianie przejęcia stanu. `Zapisz jako` utworzyło Map12 poza Library. Nowy projekt z powitania utworzył pojedynczy Map12 w wybranej Library i żadnej kopii w Data. Mapa zapisana przez 0.12.1 w `Data\Hexmaps` otworzyła się na drugiej karcie z zachowaną wersją ostatniego zapisu.
- Zamknięcie zmienionej mapy z zapisem odsłoniło pusty edytor z pełnym podglądem właściwego draftu, w tym listą presetów, terenami, ikonami i obiektami. Anulowanie pozostawiło pusty edytor. Ponowna próba wyjścia pokazała ten sam draft, który następnie zapisano jako własną kopię.
- Izolowana deinstalacja zachowała Library, Data, mapy i ręczny skrót. Uruchomiono też zwykły EXE z końcowego ZIP. Pokazał wersję 0.12.2, oba przyciski i pustą listę ostatnich projektów.

Stanowisko to jeden Windows z izolowanym Setup. Rzeczywista instalacja w Program Files z UAC i zmiana skojarzeń systemowych były poza tą próbą. Kanał 0.12.1 był pusty, stąd ręczne przejście. Kontrolowane metadane potwierdzają logikę wyboru i weryfikacji. Rzeczywista aktualizacja online z 0.12.2 wymaga następnego opublikowanego wydania z poprawnymi metadanymi obu paczek. Nie sprawdzano publicznego pobierania, załączników ani logów GitHuba.

## 4. Gotowość do upublicznienia

README jest stroną projektu z linkami prosto do paczek tagu `v0.12.2` i wyraźnym statusem przed publikacją. Opisy starszych wersji pozostają w dokumentacji. Licencja MIT dotyczy kodu. Jej kopia jest w Setup i ZIP. Aplikacja używa systemowych składników .NET Framework oraz generuje grafiki starterów w kodzie. Śledzony plik ikony powstał z pomocą ChatGPT. Przegląd podpiera się źródłami danego elementu i nie nadaje uprawnień do grafik osób trzecich.

Obejrzano SS1–SS4. Widoczne są przykładowe nazwy map i elementów, bez prywatnych ścieżek, danych konta i innych okien. PNG mają techniczne pola obrazu bez bloków tekstowych i EXIF. **Przed upublicznieniem trzeba potwierdzić możliwość pokazania importowanych grafik w SS2 i SS3.** Dotyczy to m.in. ciemnej ikony i tekstur mapy demonstracyjnej oraz obrazu wody w kadrze. Zrzuty zachowano zgodnie z dostarczoną wersją. Sama MIT kodu nie rozstrzyga tej kwestii.

Sprawdzono pliki śledzone i ścieżki w historii osiągalnej z lokalnych referencji, w tym lokalne tagi i lokalny obraz `origin`. Historyczna Library zawiera jedynie placeholder `.gitkeep`. Brak śledzonych plików map, MRU i zawartości `tests/_work`. Wyszukiwanie typowych formatów tokenów, kluczy, poświadczeń i danych dostępu w treści zmian oraz wiadomościach commitów nie dało dopasowań. To przegląd wzorców, z ograniczeniem do dostępnej historii lokalnej. Największe wcześniejsze obiekty w historii to wydaniowe EXE do ok. 1,1 MB. Metadane autorów historii obejmują też adresy spoza domeny GitHub noreply. Właściciel powinien ocenić ich przeznaczenie przed zmianą widoczności. Wynik nie obejmuje zdalnych logów, załączników ani referencji niedostępnych lokalnie.

## 5. Checklista i kolejność publikacji

1. Z powitania wybierz Nową mapę → jeden plik w `Hexmaps` aktywnej Library. Użyj `Zapisz jako` → projekt może trafić do innego katalogu.
2. Uruchom Setup nad 0.12.1 → ustawione wcześniej Library, zadania, MRU i mapy pozostają. Przy obcym skrócie można uruchomić edytor z menu Start.
3. Otwórz grupę i przejmij stan Sojusznika przez neutralnego członka → przełącznik Mgła staje się dostępny z zapamiętaną wartością.
4. Zmień draft presetu i zamknij zmienioną mapę → przed decyzją o presecie jego zwykły lewy panel jest widoczny również z pustym płótnem. Anulowanie zachowuje draft.
5. Po wyjaśnieniu pochodzenia grafik i ocenie metadanych Git właściciel może zatwierdzić publiczną widoczność. Właściciel wskazuje zweryfikowany commit, tworzy tag `v0.12.2` i przygotowuje na nim szkic wydania oznaczonego jako pre-release.
6. Do szkicu dołącz dokładnie `HexMapEditor-0.12.2-Setup.exe` i `HexMapEditor-0.12.2-Portable.zip` z tabeli poniżej. Porównaj sumy SHA-256 załączników, wpisz krótki opis i dopiero wtedy opublikuj. Po publikacji sprawdź obie odznaki, pobieranie bez logowania i pole `digest` w metadanych paczek. Zaktualizuj w README zdanie o dostępności.
7. Pierwszą rzeczywistą próbę aktualizacji online wykonaj z 0.12.2, kiedy pojawi się nowsze opublikowane wydanie z odpowiednimi paczkami. W zainstalowanej wersji użytkownik odrębnie podejmuje decyzję o uruchomieniu Setup, a Portable prowadzi do pobrania ZIP.

## 6. Wersja, pakiety i Git

Wersja aplikacji 0.12.2, FileVersion i ProductVersion 0.12.2.0. Map12 i dotychczasowe formaty presetów oraz bibliotek. Paczki zbudowano z jednej kompilacji w `tests/_work/patch-0122-candidate`. ZIP zawiera `HexMapEditor.exe`, `deployment.mode`, `README-Portable.txt` i `LICENSE`. Główny EXE ma te same bajty co sprawdzone kopie ZIP i izolowanej instalacji. EXE i Setup są niepodpisane.

| Plik | SHA-256 |
|---|---|
| `HexMapEditor.exe` | `472A6C78F2787B3615DA735089886DEBD1FAE8CB3584F8B81986259B33785D46` |
| `HexMapEditor-0.12.2-Setup.exe` | `2E2990464539C181BEE0A28EF89ED9AE073117354A866150CC0B933C1C8BE53C` |
| `HexMapEditor-0.12.2-Portable.zip` | `32F625449BEF3FF113A69EC13DDB4259284B6AE88A54474DB86A77C71AC2959C` |

Baza `df48ca4`. Zastane zrzuty dołączono bez zmiany ich bajtów. Zmiany projektu i główny EXE obejmuje jeden lokalny commit. Paczki, wyniki i dane prób pozostają w ignorowanym `tests/_work`. Czynności publikacji należą do właściciela.
