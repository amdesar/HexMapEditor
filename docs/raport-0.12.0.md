# HexMapEditor 0.12.0

## 1. Paczki i dane

Wymagany jest Windows 10 lub 11 z pełnym .NET Framework 4.8 albo 4.8.1 i WPF. Setup sprawdza dostępność środowiska i podaje wymaganie w razie jego braku. Edytor korzysta z bibliotek Windows i .NET Framework. Fabryczne zasoby są tworzone przez dotychczasowy kod przy pierwszym użyciu.

| Plik w `tests/_work/patch-0120-final` | Wersja | SHA-256 |
|---|---|---|
| `HexMapEditor.exe` | 0.12.0.0, plik i produkt | `905D2275289B8C1908A98EDD5071A26CD4ABB1DA84ABBD8A3EB4A296DB2473DF` |
| `HexMapEditor-0.12.0-Setup.exe` | 0.12.0.0, plik i produkt | `7CB702EB72EAAA321A1E57EDAAD378B7F2B8A69C6B9A4E4656941137DC2127C6` |
| `HexMapEditor-0.12.0-Portable.zip` | EXE 0.12.0.0 | `08E1BAD8B5A4FFB3274F1094A1C735AE313B6819575FDBF9EF04437D44A82ED0` |

- Setup wytworzono przez Inno Setup 6.7.4. Zawiera ten sam EXE co ZIP, znacznik trybu instalowanego i składniki instalatora. Skrót i wpis odinstalowania korzystają z ikony osadzonej w EXE, a Setup z tego samego `icon.ico`. Setup i aplikacja są niepodpisane. Oficjalny kompilator użyty do budowania miał zweryfikowany podpis Pyrsys B.V. [Pobieranie Inno Setup](https://jrsoftware.org/inno-setup/downloads)
- ZIP zawiera dokładnie `HexMapEditor.exe`, `deployment.mode` i `README-Portable.txt`. EXE w archiwum porównano bajtowo z głównym. Pakowanie korzysta z jawnej listy plików. Dane użytkownika i wyniki prób są poza paczkami.
- Instalacja per użytkownik trafia domyślnie do `%LOCALAPPDATA%\Programs\HexMapEditor`. Stałe ID instalacji i zapamiętany katalog umożliwiają upgrade. Plik `deployment.mode` wybiera tryb. Zainstalowany program zapisuje `Library`, `recent-projects.json` i przyszłe pliki ustawień pod `%LOCALAPPDATA%\amdesar\HexMapEditor`. Wersja nie tworzy jeszcze pliku ustawień. Portable i samodzielny EXE z repozytorium korzystają z katalogu własnej kopii.
- Setup i deinstalator zarządzają plikami programu, skrótem i własnymi kluczami. Biblioteka, MRU i mapy pozostają. Świeża instalacja nie przejmuje danych z repozytorium ani z portable. Chroniony „Domyślny” jest tworzony w pamięci z 6 terenami, 12 ikonami i 12 obiektami. Pierwsze użycie tworzy w bibliotece 20 samodzielnych ikon i 12 obiektów. Map12, preset 5 i formaty bibliotek pozostają dotychczasowe.

## 2. Skojarzenie .hexmap

Domyślnie zaznaczone zadanie Setup rejestruje dla bieżącego użytkownika własny ProgID oraz pozycję do otwierania plików. Ponowny Setup dziedziczy stan zadania. Odznaczenie przy upgrade usuwa wpisy HexMapEditora. Instalator nie zmienia wartości domyślnej rozszerzenia ani chronionego `UserChoice`. Aby zmienić domyślną aplikację, użyj „Otwórz za pomocą” lub ustawień domyślnych aplikacji Windows. [Dokumentacja rejestracji aplikacji Windows](https://learn.microsoft.com/en-us/windows/win32/shell/app-registration)

## 3. Sprawdzanie aktualizacji

Kanał produkcyjny w `src/UpdateChannelConfiguration.cs` jest pusty. Ręczna komenda pokazuje „Kanał aktualizacji nie jest skonfigurowany”. Skonfigurowana kopia sprawdza wydania asynchronicznie przy zwykłym starcie na powitaniu oraz na żądanie. Otwieranie mapy z argumentu nie wymusza powitania. Źródłem jest publiczne GitHub Releases. Sprawdzenie rozróżnia problemy dostępu od braku nowszego wydania, odrzuca drafty i prerelease i porównuje numery wersji liczbowo. Opis wydania otwiera się na żądanie. Opcjonalna etykieta pochodzi z pola `label` właściwego zasobu GitHub. [API wydań GitHub](https://docs.github.com/en/rest/releases/releases#get-the-latest-release)

W instalacji program przyjmuje tylko nazwę Setup dla oferowanej wersji i adres zasobu z tego samego repozytorium. Dozwolone przekierowanie pobrania prowadzi wyłącznie do hosta zasobów GitHub przez HTTPS. Sprawdzane są metadane, długość, SHA-256 i nagłówek PE. Metadane mają limit 2 MiB, pakiet 64 MiB. Pobrany plik zachowuje znacznik strefy Internetu Windows. Brak poprawnego znacznika albo sumy zatrzymuje instalację. [Metadane zasobów GitHub](https://docs.github.com/en/rest/releases/assets)

Po pobraniu użytkownik decyduje o zamknięciu. Istniejący cykl zapisu map i presetów może odmówić dalszego przejścia. Udane decyzje zamknięcia pozostają zgodnie z 0.11.1. Po zamknięciu edytora program ponownie weryfikuje instalator i przekazuje go do Windows. Setup czeka na zakończenie starego procesu, chroni działające okno i używa tego samego katalogu instalacji. Po błędzie uruchom ponownie edytor z menu Start lub ponownie uruchom właściwy Setup nad dotychczasową instalacją. Powtórny Setup nad tą samą instalacją sprawdzono na fixture. Wersja Portable kieruje do ręcznego pobrania właściwego ZIP i pozostawia wybór momentu aktualizacji użytkownikowi.

## 4. Weryfikacja

### Testy metod i integracja cyklu aplikacji

| Przypadek | Wynik |
|---|---|
| `paths` | PASS. Oba znaczniki i brak znacznika, odrzucenie uszkodzonego znacznika, świeża biblioteka, 20 ikon i 12 obiektów, brak pliku fabrycznego presetu, zapis i odczyt Map12 oraz MRU. |
| `close` | PASS. W edytorze z niezapisanym heksem Anuluj zatrzymało przejście do instalatora. Zapis zakończył zamykanie i pozostawił właściwą migawkę pliku. |
| `channel` | PASS. Kontrolowana odpowiedź w formacie GitHub i rzeczywiste bajty testowego Setup 0.12.1. Właściwy Setup/ZIP, etykieta zależna od metadanych, różne wersje, draft/prerelease, błąd dostępu, brak paczki, zły typ/host, nieprawidłowe przekierowanie, brakujący i błędny SHA-256 oraz modyfikacja po pobraniu. |

Próbę `channel` powtórzono po poprawce znacznika pochodzenia. Standardowa ścieżka zapisu pliku .NET Framework odrzucała nazwę strumienia NTFS. Zapis przez uchwyt Win32 przeszedł ponowną weryfikację. Próby produkcyjnego kanału nie wykonywano.

### Rzeczywisty Setup i zwykły EXE

- **Portable z finalnego ZIP.** Na Windows 11 Pro x64, build 26300, uruchomiono świeże rozpakowanie przez alias w Temp. Fizyczne pliki pozostały w `tests/_work`. Powitanie było puste, pokazało 0.12.0 i poprawny komunikat o kanale. Utworzono mapę z „Domyślnym”, namalowano heks terenem Woda, zapisano i ponownie otwarto przez MRU. Plik potwierdził Map12, teren Woda i wersję zapisu 0.12.0. Policzone zasoby odpowiadały fabrycznym zestawom.
- **Instalacja kontrolowana.** W tym samym Windows uruchomiono rzeczywisty Inno Setup z izolowanym AppId, katalogiem danych i kluczami zamiast skojarzeń bieżącego użytkownika. Setup był nieadministracyjny. Czysta instalacja 0.12.0 rozpakowała identyczny EXE, skrót i znacznik trybu. Zapisano mapę, MRU, bibliotekę i próbny plik ustawień. Upgrade do 0.12.1 dał wersję 0.12.1.0 i zachował bajty wszystkich tych danych. Zwykły zainstalowany EXE otworzył mapę. Sprawdzono domyślny On, dziedziczenie On, ręczne Off i dziedziczenie Off. Syntetyczne obce skojarzenie pozostało.
- **Ochrona i naprawa.** Setup odmówił instalacji przy działającym EXE i zachował jego proces oraz bajty. Po zamknięciu edytora ponowny Setup naprawczy zadziałał. Deinstalacja wyłącznie wariantu testowego zachowała wszystkie dane i obcy wpis w próbnych kluczach. Próby instalacji powtórzono po dodaniu blokady pliku działającego edytora i korekcie kodowania skryptu Inno, aby sprawdzić końcowy wariant.

Brak Windows Sandbox i osobnego profilu Windows na stanowisku. Nie wykonano produkcyjnego odinstalowania, rzeczywistego przypisania domyślnej aplikacji w Windows ani próby Setup na czystym Windows. Transfer i warunki podjęcia aktualizacji sprawdzono lokalnie. Nie uruchamiano automatycznego publicznego pobrania, kampanii `Run-AllTests` ani publikacji.

## 5. Checklista odbiorcza

- Instalacja z włączonym skojarzeniem w VM → skrót w Start, edytor dostępny w „Otwórz za pomocą”, poprzednia domyślna aplikacja zachowana.
- Aktualizacja instalacji z odznaczoną opcją → opcja pozostaje odznaczona, mapy i MRU pozostają.
- Start świeżego Portable → pusty MRU. Pierwsza mapa → fabryczne zasoby i zapis przy kopii.
- Zmodyfikowana mapa i Anuluj przy aktualizacji → edytor pozostaje otwarty, Setup się nie uruchamia.
- Deinstalacja w VM → znika aplikacja i jej skrót, zostały pliki Library, ustawień, MRU i mapy.

## 6. Przygotowanie publicznej próby 0.12.1

1. Wybierz publiczne repozytorium wydań. Wpisz jego właściciela i nazwę do `src/UpdateChannelConfiguration.cs`. Obecny 0.12.0 z pustą konfiguracją wymaga kontrolowanej kompilacji 0.12.0 z włączonym kanałem, aby uczestniczyć w publicznej próbie.
2. Skompiluj 0.12.1, użyj tej samej sprawdzonej kompilacji jako wejścia `packaging/Build-Packages.ps1` dla obu paczek. Sprawdź wersje, bajty w ZIP i sumy. Podpisanie Setup pozostaje osobną decyzją. Jeśli podpisujesz, sprawdź sumę po podpisaniu.
3. Po decyzji o publikacji dołącz dokładnie `HexMapEditor-0.12.1-Setup.exe` i `HexMapEditor-0.12.1-Portable.zip` do stabilnego wydania z tagiem `v0.12.1`. Sprawdź anonimowy odczyt `/releases/latest` i zgodność widocznych skrótów `digest` z lokalnym SHA-256. Etykietę można ustawić w polu zasobu jako „Poprawki błędów”, „Nowe funkcje” lub „Ważna poprawka”.
4. W odizolowanej instalacji sprawdź przejście 0.12.0→0.12.1 przez rzeczywisty kanał. Osobno potwierdź, że Portable oferuje ZIP oraz instrukcję i zachowuje swoją bibliotekę.

## 7. Git

Baza `9b9efe9`, stan początkowy czysty. Jeden lokalny commit obejmuje kod, główny EXE, skrypty, źródła testów i dokumentację. Paczki, kopie i logi pozostały poza indeksem w ignorowanym `tests/_work`. Wykonano wyłącznie operacje lokalne.
