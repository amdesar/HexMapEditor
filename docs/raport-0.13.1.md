# HexMapEditor 0.13.1

## 1. Setup i aktualizacja

- Jeden widoczny kreator Inno zbiera katalog programu, osobną lokalizację Library oraz zadania i wyświetla podsumowanie. Świeży Setup proponuje systemowy Program Files. Jeśli zwykły użytkownik ma dostęp do zapisu, Setup działa w jego kontekście. Dla chronionego celu pomocnik w tym samym kontekście uruchamia etap maszynowy tego samego pakietu i systemową zgodę UAC. Zmiana ścieżki przed instalacją nie wymaga ponownego wyboru ustawień.
- Etap podniesiony sprawdza ochronę katalogu i plików programu oraz przekierowania ścieżek. Uprawnienia zapisu przydziela wyłącznie nowym, wydzielonym katalogom danych. Wznowienie edytora, gdy Setup potwierdzi wynik, odbywa się ze zwykłego procesu.
- Istniejącą instalację rozpoznaje wpis HKCU albo HKLM. Aktualizacja zachowuje zakres, folder programu, Library i zadania. Konflikt dwóch rejestracji zatrzymuje operację.
- Oba tryby instalowane oferują „Zainstaluj aktualizację” i pokazują postęp pobierania. Po sprawdzeniu rozmiaru, typu pliku i SHA-256 edytor daje możliwość zapisania wszystkich zmienionych map i presetów albo przerwania aktualizacji. „Zapisz jako”, błąd zapisu i anulowanie mają tę samą ochronę co zwykły zapis. Przy przerwaniu karty pozostają otwarte, a udane wcześniejsze zapisy pozostają skuteczne.
- Setup czeka na zamknięcie edytora, potwierdza wersję, ścieżkę i zakres oraz ponownie sprawdza sumę z blokadą zapisu i usunięcia pliku. Sukces wymaga zakończenia właściwego etapu, oczekiwanej wersji EXE i zachowanego wpisu rejestracji. Portable nadal kieruje do ręcznego ZIP. Kanał testowy, HTTPS, reguły hosta i ścieżki przekierowania, limity oraz oznaczenie Windows pochodzenia pliku pozostały w działającej ścieżce.
- Wcześniejsza 0.13.0 przekazywała pobranie dla instalacji osobistej domyślnej przeglądarce. Anonimowy odczyt kanału zwrócił HTTP 200 i najnowsze wydanie `v0.13.0`, więc stara aplikacja nie miała nowszej oferty. Znikającej karty nie odtworzono. Kontrolowana oferta 0.13.1 dla instalacji osobistej wyświetliła przycisk instalacji bez otwierania przeglądarki.

## 2. Kolejka i README

Kolejka ma ciemne nagłówki i dwa słupki informacji, kategorię oraz nazwę pliku. Kategoria korzysta z pomiaru czcionki, a nazwa wypełnia pozostałą szerokość i w razie potrzeby jest skracana wielokropkiem. Pełną nazwę można odczytać w podpowiedzi. Zasady importu, kadrowania, pomijania i usuwania oraz Map12 i magazyny Library pozostają zgodne z 0.13.0.

README jest przygotowane na wydanie `v0.13.1`. Obie odznaki wskazują dokładne nazwy paczek i raport bieżącej wersji. Po opublikowaniu pozostaje sprawdzić oba pobrania bez logowania.

## 3. Próby metod i kontrolek

| Przypadek | Wynik |
| --- | --- |
| `channel` | PASS. Kontrolowane wydania, źródło i przekierowania, wariant Setup i Portable, odrzucenie błędnej sumy. Powtórzono dla nowo zbudowanego Setup. |
| `personal-update` | PASS. Argumenty obu trybów instalowanych, zachowanie Portable i ponowna weryfikacja pliku przed startem. Powtórzono po dodaniu trybu postępu Setup. |
| `close` | PASS. Zapis mapy i wszystkich sekcji presetu, anulowanie, próba odrzucenia oraz błąd zapisu zatrzymujący aktualizację. |
| `queue` z prób 0.13.0 | PASS. Zachowanie importu w modelu. |
| Sonda Inno | Bezpośrednie uruchomienie drugiego Setup zwracało kod 5 również bez blokady pliku. Uruchomienie przez zwykły pomocnik z blokadą dotarło do skryptu drugiego Setup. Powtórnie sprawdzono przekazanie po zmianie mechanizmu. |

## 4. Rzeczywiste próby na odizolowanych danych

- Zwykły kreator pozwolił przejść od domyślnej ścieżki do własnej zapisywalnej ścieżki, oddzielnej Library i podsumowania w tym samym oknie. Świeża instalacja osobista na fizycznym D: i odinstalowanie kopii próbnej przeszły. Odrębna Library została na miejscu.
- Rzeczywisty upgrade osobisty 0.13.0 → 0.13.1 zakończył się kodem 0, potwierdzeniem wersji przez Setup i wznowieniem edytora. Porównano bajtowo próbną mapę, Library, MRU, ustawienia oraz oba skróty. Wywołanie przekazało parametry i sumę stosowane przez aplikację.
- Zwykły EXE w Portable wyświetlił cztery kategorie i długą zaznaczoną nazwę na minimalnej szerokości z czytelnym kontrastem. Po poprawce przeliczenia szerokości powtórzono tylko tę próbę. Poziomy scrollbar nie pojawił się w sprawdzonym układzie.
- Świeża izolowana instalacja w Program Files przeszła z podniesionym etapem, wpisem HKLM i EXE identycznym z kompilacją. Pierwsze podejście poprawnie zatrzymała blokada działającego próbnego edytora. Próbę powtórzono po jego zamknięciu.
- Rzeczywisty upgrade maszynowy 0.13.0 → 0.13.1 przez parametry aktualizatora przeszedł w trybie 64-bitowym z zachowaniem zakresu, ścieżek, próbnej mapy, Library, MRU, ustawień i skrótów. Setup potwierdził wersję oraz wznowił edytor z procesu koordynatora. Niemy etap Inno sam potwierdził powodzenie. Koordynator po zakończeniu tego etapu zamyka własną ścieżkę Inno, co w trybie cichym daje zewnętrzny kod 1 lub 2. Komunikat o wyniku i wznowienie zależą od wyniku etapu zapisującego oraz sprawdzenia EXE i rejestracji. Bazę maszynową powtórzono po dostosowaniu opcji testowego kompilatora do zakresu i architektury wydanego Setup.
- Osobna próba kontrolowanego pobierania uruchomiła przepływ do natywnego ostrzeżenia o pochodzeniu pliku. Pełnego przebiegu od kliknięcia w edytorze do wyniku Setup nie potwierdzono w jednej sesji. Nie przeprowadzono odmowy UAC ani prób na świeżym Windows. Stanowisko wykonało rzeczywiste operacje z podniesionym tokenem, a Hyper-V odmówił dostępu do CIM.
- Publiczna aktualizacja z 0.13.1 do nowszego wydania pozostaje do odbioru po publikacji takiego wydania. Paczki są niepodpisane.

## 5. Checklista odbioru

1. Świeży Setup → zmiana Program Files na zapisywalny katalog zachowuje jeden kreator, a podsumowanie pokazuje właściwy folder programu i oddzielną Library.
2. Instalacja w Program Files → zgoda UAC uruchamia etap instalacji i zachowuje wybory. Odmowa wraca z komunikatem, bez wykonania instalacji.
3. Kontrolowana oferta w obu trybach zainstalowanych → postęp pobierania, zapis pracy i potwierdzenie wyniku Setup. Anulowanie zapisu lub odrzucenie wadliwej paczki nie rozpoczyna instalacji.
4. Portable → ręczny ZIP, zachowane pliki danych i brak uruchamiania Setup.
5. Cztery kategorie i długa nazwa w kolejce → jasny tekst na ciemnym tle, pełna nazwa w podpowiedzi i czytelne zaznaczenie.

## 6. Wersja, artefakty i Git

Aplikacja 0.13.1. FileVersion oraz ProductVersion aplikacji i Setup to `0.13.1.0`. Paczki pochodzą z jednej kompilacji i znajdują się w ignorowanym `tests/_work/patch-0131-release-final/packages`.

| Artefakt | SHA-256 |
| --- | --- |
| `HexMapEditor.exe` | `3AF55C7311F83A4EC77F27EE970F2B83BCC25C54813B646EB464AC7A5C4566C4` |
| `HexMapEditor-0.13.1-Setup.exe` | `B3EC7BD6AFB035F1879489C3E80B45530273886AAE4957E43B7BDFA658BEEA77` |
| `HexMapEditor-0.13.1-Portable.zip` | `D3F4F217FB26C4F4DFA43252414B7B2FD2C2E69C57DDCD041681D634C5C88629` |

ZIP zawiera wyłącznie EXE, znacznik Portable, instrukcję i licencję. Porównanie `fc /b` potwierdziło identyczność głównego EXE z kompilacją i rozpakowanym EXE. Bazą jest `dfbea07`, zawierająca kod `96c1e9c` oraz aktualizację README z origin/master. Wydanie ma jeden lokalny commit na `master` ponad bazą. Katalog prób jest ignorowany.

## 7. Opis Release’a

### HexMapEditor 0.13.1 — wydanie testowe

- Jeden kreator instalacji obsługuje Program Files oraz własne zapisywalne katalogi.
- Aktualizacje kopii zainstalowanych można pobrać i uruchomić z edytora po zapisaniu pracy. Kopię Portable aktualizuje się ręcznie przez ZIP.
- Kolejka importu ma ciemną tabelę, skalujące się kolumny i czytelne zaznaczenie.
- Mapy pozostają samodzielnymi plikami Map12. Istniejąca Library i dane użytkownika pozostają na miejscu podczas aktualizacji.

Utwórz pre-release pod tagiem `v0.13.1` i dołącz obie wymienione paczki. Po publikacji sprawdź anonimowe pobranie obu adresów z README i zgodność sum.
