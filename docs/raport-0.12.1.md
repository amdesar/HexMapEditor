# HexMapEditor 0.12.1

## 1. Instalacja i położenie danych

Inno Setup 6.7.4 prowadzi przez katalog programu, lokalizację Library, zadania oraz podsumowanie programu, Library i Data. Domyślnie wybiera systemowy `Program Files\HexMapEditor`. Aplikacja jest AnyCPU (PE I386, ILOnly, bez 32BITREQUIRED). Setup używa 64-bitowego trybu na zgodnym systemie x64, w tym właściwego Program Files. Można wybrać inny katalog chroniony przed modyfikacją na zwykłym koncie.

Domyślnie Library jest w `<program>\Library`. Druga opcja wskazuje `HexMapEditor\Library` w Dokumentach pierwotnego użytkownika, również przy przekierowanym folderze systemowym. Własna lokalizacja jest ścieżką docelową bez automatycznego podfolderu. Wybór obok programu podąża za katalogiem programu. Wybrana ścieżka Dokumentów lub własna zachowuje się po cofnięciu w kreatorze. Znacznik `deployment.mode` oraz chroniony `library-location.txt` pozwalają uruchomić aplikację niezależnie od katalogu roboczego.

MRU i miejsce na przyszłe ustawienia to `<program>\Data`. Nowe mapy tworzone z powitania domyślnie trafiają do `Data\Hexmaps`. Pozostałe mapy zostają na własnych ścieżkach. Portable i standalone nadal trzymają dane przy swojej kopii. Instalator nie zakłada nowego pliku ustawień ani nie wkłada przykładowych projektów. Aplikacja generuje fabryczny chroniony „Domyślny” z terenami, ikonami i obiektami.

## 2. Uprawnienia, skróty i aktualizacja

Setup maszynowy podnosi uprawnienia. Uruchamiaj go zwykłym dwuklikiem i dopiero wtedy potwierdź UAC. Pomocnik uruchamiany w kontekście sprzed UAC sprawdza Dokumenty, ochronę katalogu programu oraz możliwość utworzenia i usunięcia pliku w Data i Library. Przy ręcznie podniesionym procesie bez zwykłego kontekstu Setup zatrzymuje się z instrukcją ponownego uruchomienia. ACL nowego, wydzielonego Data i Library obok EXE dają pierwotnemu użytkownikowi prawa modyfikacji. Administratorzy i system zachowują pełne prawa. Istniejące katalogi zachowują swoje ACL. Setup nie rozszerza praw w Program Files, Dokumentach, dowolnie wybranej istniejącej Library ani w katalogu EXE i jego konfiguracji. Użytkownik otrzymuje komunikat, jeśli wybrana ścieżka nie przechodzi próby. Edytor uruchamia się później zwyczajnie ze skrótu.

Menu Start dostaje standardowy skrót. Świeże zadania mają włączone skojarzenie i wyłączony pulpit. Rejestracja maszynowa udostępnia własny ProgID w „Otwórz za pomocą” i pozostawia domyślną aplikację oraz UserChoice Windows. Zmiana zadania i deinstalacja usuwają własną rejestrację. Skrót pulpitu `HexMapEditor (instalator).lnk` ma znacznik i sprawdzany cel. Przy kolizji z obcym skrótem Setup informuje, jak kontynuować bez nadpisania.

Ponowny Setup używa istniejącego katalogu programu i zapamiętanych zadań oraz ścieżki Library. Zmiana Library pozostawia dotychczasowy katalog i jego dane. Zmiana katalogu programu istniejącej instalacji wymaga uprzedczego odinstalowania kopii. Setup czeka na edytor wskazany przez aktualizator i odmawia nadpisania działającego programu. Deinstalacja usuwa pliki programu oraz należące do niego skróty i rejestrację. Pozostawia Data, Library i mapy, również wewnątrz katalogu instalacji.

## 3. Przejście z 0.12.0 i Runtime

Osobny identyfikator Setup odróżnia instalację maszynową od wcześniejszej per użytkownik. Setup wykrywa poprzednią instalację z jej wpisu lub rozpoznawalnej kopii w domyślnym folderze. Pokazuje ścieżkę i prosi o zapisanie pracy, zamknięcie oraz zwykłe odinstalowanie przed kontynuacją. Po odinstalowaniu nadal oferuje jednorazową, domyślnie wyłączoną kopię z `%LOCALAPPDATA%\amdesar\HexMapEditor`. Dane i mapy zostają w źródle. Library i rozpoznane pliki profilu `recent-projects.json` oraz `settings.json` trafiają do nowych, oddzielnych miejsc. Przekierowania źródłowe, zajęta Library lub kolidujący plik profilu zatrzymują kopiowanie. Mapy pozostają w starym miejscu także wtedy, gdy znajdowały się w starej Library.

Wymagania to Windows 10/11 i pełny .NET Framework 4.8 lub 4.8.1 z WPF. W razie braku Setup zatrzymuje instalację, oferuje kliknięcie oficjalnej strony Runtime i prosi o ponowne uruchomienie Setup po doinstalowaniu. Powitalne „Sprawdź aktualizacje” jest w prawym dolnym rogu. Mechanizm aktualizacji i pusta konfiguracja kanału pozostają dotychczasowe. [Pobieranie .NET Framework Runtime 4.8](https://dotnet.microsoft.com/download/dotnet-framework/net48).

## 4. Próby i ograniczenia

**Metody.** `paths` sprawdził tryby i ścieżki, w tym odmowę uruchomienia z brakującą konfiguracją instalacji. W `scenario` próby `legacy` i `installed` zapisały i odczytały próbne Map12 oraz presety, sprawdziły MRU, pozostawienie mapy w źródle i ponowny odczyt presetu zapisanego po przejściu. Osobno pomocnik na zwykłym koncie zaakceptował istniejące Program Files w próbie bez zapisu i odrzucił zapisywalny katalog prób.

**Rzeczywisty Setup z konfiguracją izolowaną.** `scenario` wykonał instalację i deinstalację archiwalnego 0.12.0, blokadę drugiej instalacji z podaniem ścieżki, kontrolowany brak Runtime i odnotowanie oficjalnego linku, odmowę kopiowania do zajętej Library, przejście po odinstalowaniu z zachowaniem oryginałów oraz ponowny Setup 0.12.1. Sprawdzono wszystkie trzy tryby Library, kolizję z Data, zachowanie ścieżek, włączanie i wyłączanie zadań oraz ochronę obcego skrótu. `cleanup` potwierdził zachowanie danych w Library obok EXE, położeniach alternatywnych, Data i starszym profilu oraz mapy poza programem. Rejestr testowy potwierdził dodanie i usunięcie własnych wpisów oraz zachowanie obcego wyboru. W kreatorze sprawdzono kolejność stron, cofnięcie, zmianę katalogu, domyślne checkboxy i wpisy podsumowania. Próbę kreatora anulowano przed instalacją.

**Zwykły EXE.** Kopia z izolowanej instalacji otworzyła zewnętrzną mapę 7×5 i pokazała skopiowany preset. Końcowy Portable po rozpakowaniu pokazał pustą listę projektów i pełny przycisk aktualizacji po prawej. Utworzenie projektu pokazało fabryczny „Domyślny”, sześć terenów, widoczne zestawy 12 ikon i 12 obiektów; biblioteka miała 20 plików ikon i 12 plików obiektów. Mapa i późniejsze MRU powstały obok kopii. Ponowne uruchomienie z zapisaną mapą otworzyło ją w edytorze.

**Powtórzenia.** Próbę poprzedniej instalacji powtórzono po skróceniu izolowanego AppId do rozmiaru mieszczącego się w kluczu deinstalacji Inno. Przebieg instalacji i deinstalacji wykonano po zmianie tworzenia katalogów i skrótu pulpitu. Powitanie sprawdzono ponownie po usunięciu nakładania się listy MRU na przycisk.

**Ograniczenia.** Próby odbyły się na tym samym Windows 11 z własnymi plikami i kluczami. Nie było dostępnej VM ani bezpiecznej próby produkcyjnego Setup w Program Files. Nie sprawdzono pełnego cyklu przyznania ACL z UAC, uwierzytelnienia kontem innego administratora ani działania skojarzenia w powłoce Windows. Brak Runtime był symulowany. Kanał aktualizacji pozostaje nieskonfigurowany i próba aktualizacji sieciowej jest poza zakresem.

## 5. Checklista świeżej instalacji

1. Uruchom Setup zwykłym dwuklikiem i potwierdź UAC → kreator proponuje Program Files i pokazuje folder programu, Library, zadania oraz podsumowanie.
2. Wybierz „Obok programu” → ścieżka Library kończy się na `HexMapEditor\Library`. Skojarzenie jest zaznaczone, pulpit odznaczony.
3. Uruchom edytor bez podniesienia → przycisk aktualizacji jest po prawej, a na nowej mapie jest dostępny chroniony „Domyślny”.
4. Zapisz własny preset albo zasób i otwórz go po ponownym uruchomieniu → dane są w wybranej Library, ostatnie projekty w Data. Zwykłe konto może zapisywać dane, a pliki programu pozostają chronione.
5. Uruchom Setup ponownie → zachowuje ścieżkę i zadania. Zmiana Library zostawia poprzedni katalog bez przenoszenia i scalania.

## 6. Artefakty i Git

Build 0.12.1, FileVersion i ProductVersion `0.12.1.0`. Formaty Map12, preset 5 i biblioteki pozostają bez zmian. Główny EXE i EXE rozpakowany z Portable mają te same bajty. Setup i EXE są niepodpisane. Próbne paczki są w `tests/_work/patch-0121-release`. ZIP zawiera wyłącznie `HexMapEditor.exe`, `deployment.mode` i `README-Portable.txt`. Ikona jest osadzona w EXE i używana przez Setup. Setup zawiera też własny tymczasowy pomocnik; pomocnik nie trafia do folderu programu ani do ZIP.

| Plik | SHA-256 |
|---|---|
| `HexMapEditor.exe` | `B0352801EB84E5E7C81CE19FA42514CB1E016B31DA76DDF306DEB219A0786FF6` |
| `HexMapEditor-0.12.1-Setup.exe` | `1F727485635C047690C18ED19D8F43672F28ECF86AA070747B4B276216632B4A` |
| `HexMapEditor-0.12.1-Portable.zip` | `233378EC167476B88BD042712260613D0A25E16FCA7FC5315F580AD24B1D2A69` |

Baza `2061ea2`, początkowy stan Git czysty. Jeden lokalny commit wersji obejmuje główny EXE, kod, skrypty, źródła testów i dokumentację. Wyniki robocze oraz paczki pozostają poza Git.
