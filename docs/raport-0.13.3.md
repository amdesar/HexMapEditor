# HexMapEditor 0.13.3

## 1. Metadane grafik

Nowe wpisy zapisują JSON-y w `Terrains/Graphics/Metadata`, `Icons/Metadata`, `Objects/Metadata` i `Backgrounds/Metadata` aktywnej Library. Oryginały `*-original.*` pozostają przy tych folderach. Przykładowo `Icons/Metadata/icon-Osada.json` odwołuje się do `Icons/Osada-original.png`. Odwołanie jest nadal interpretowane względem katalogu kategorii. Kadry, parametry i ID pozostają własnością wpisu, a kilka wpisów może używać wspólnego źródła.

Przy pierwszym wczytaniu kategorii w sesji pełny czytnik rozpoznaje prawidłowe wpisy z płaskiego układu. Program przenosi ich JSON-y bajt w bajt. Kolizja lub odmowa zapisu pozostawia plik pod dotychczasową ścieżką i pojawia się w ostrzeżeniu. Dwa poprawne wpisy o różnych ID i tej samej nazwie pliku są dostępne z obu ścieżek. Wpisy, których czytnik nie może zweryfikować, pozostają na dysku pod pierwotną ścieżką. Obce pliki, obrazy, presety, stany, mapy i `Library/Sources` pozostają na miejscu.

Czytniki, kolejka i historia obsługują płaski, nowy oraz mieszany układ. Wpisy ze starszym źródłem w `Library/Sources` nadal działają. Reguły kolejki, format Map12 i format presetów pozostają zgodne z 0.13.2. Kopia starsza od 0.13.3 widzi wpisy z płaskiego układu. Odczyt katalogów `Metadata` wymaga 0.13.3 lub nowszej.

## 2. Blokada podczas aktualizacji

W izolowanej próbie 0.13.2 Setup zwrócił kod 7 w `PrepareToInstall`. Docelowy `Program/HexMapEditor.exe` był zamknięty. Działał `OtherProgram/HexMapEditor.exe` (PID 19968), czyli osobny edytor z inną Library. Obie odizolowane kopie używały wspólnego testowego odpowiednika muteksu instalacji. To potwierdziło fałszywą blokadę przez kopię spoza celu. Przyczyny pozostawania obu procesów ze zgłoszenia publicznego nie dało się przypisać bez ich ścieżek i PID-ów. W kontrolowanej próbie proces instalatora i uruchomiony edytor miały różne ścieżki i role.

Setup nadal czeka na PID przekazany przez edytor inicjujący aktualizację. W etapie zapisu testuje wyłącznie współdzielenie EXE docelowej instalacji. Działający edytor trzyma otwarty własny EXE, dlatego test chroni także działającą kopię 0.13.2. Przy blokadzie Setup wyświetla konkretną ścieżkę i przycisk **Ponów**, który sprawdza ją ponownie w tym samym przebiegu. Sprawdzony plik Setup pozostaje zablokowany przed podmianą podczas aktualizacji. Przy odmowie albo błędzie Setup przerywa zapis. Kod klienta pobierającego, zapis map i presetów, SHA-256, uprawnienia i przydział ról przy uruchamianiu Setup pozostają zgodne z 0.13.2.

## 3. Próby

| Próba | Wynik |
| --- | --- |
| Metody `metadata` | PASS. Cztery kategorie, świeży zapis z kolejki, przeniesienie płaskiego układu bez zmiany bajtów, kolizja dwóch wpisów, obcy JSON, błąd tworzenia `Metadata`, odczyt starego wpisu, ponowny odczyt, edycja, usuwanie i Undo oraz ponowny skan kolejki. |
| Metody `library`, `queue`, `map` | PASS. Dwa kadry na jednym oryginale, starsze `Sources`, przeniesienie Library, odrzucenie niebezpiecznych odwołań, błąd zapisu w kolejce z pozostawieniem wejścia i samodzielny Map12. |
| Zwykły EXE z Portable | Otworzył kopię próbnej Library użytej w 0.13.2. Sprawdzono zgodność SHA-256 pliku `icon-b.json` przed i po przeniesieniu. **Biblioteka → Ikony** otworzyła katalog z `b-original.png` i podfolderem `Metadata`. |
| Kontrolowany Setup `scope` | PASS. Rzeczywisty próbny edytor otwarty w docelowym katalogu zablokował zapis z kodem Windows 32. Setup pozostawił jego plik i proces. Drugie wywołanie tej samej zweryfikowanej paczki zaczekało na PID inicjatora 32192. Skrypt zamknął jego okno zwykłym żądaniem. Setup zakończył aktualizację kodem 0 i zgłosił wersję 0.13.3. Osobna kopia pozostała uruchomiona. |

Test aktualizacji używał izolowanej instalacji osobistej, lokalnych paczek i rzeczywistych procesów. Ponowienie wykonano przez drugie wywołanie tej samej paczki. Przycisk **Ponów** w otwartym oknie oraz aktualizacja maszynowa z UAC pozostają do ręcznego odbioru. Nie wykonano publicznego przejścia z wydanej 0.13.2. Gdy odbędzie się po publikacji, sprawdzi nowy Setup uruchomiony przez dotychczasowego klienta. W 0.13.3 poprawka znajduje się w Setup, nie w kodzie pobierania klienta.

## 4. Artefakty i Git

Aplikacja 0.13.3. FileVersion i ProductVersion obu EXE to `0.13.3.0`. Setup i Portable korzystają z jednej kompilacji. ZIP zawiera EXE, znacznik Portable, licencję i instrukcję. Końcowy EXE jest bajtowo identyczny ze sprawdzoną kopią z ZIP. Paczki bez podpisu znajdują się w ignorowanym `tests/_work/patch-0133-release/packages`.

| Plik | SHA-256 |
| --- | --- |
| `HexMapEditor.exe` | `6A57BACA8A32AC31AA7635C185F59531971364705D5E36979A5A52EC7B4EF3A5` |
| `HexMapEditor-0.13.3-Setup.exe` | `4A921AA90805254AF1E3F7D9322AA33D2055DA0E35B662ADD9EA96857D76DAC5` |
| `HexMapEditor-0.13.3-Portable.zip` | `A737C585A4ABFC2B3ABC8F22166BD48C96CB50269984B08AC621DD45609890DA` |

Na początku `HEAD`, lokalne `origin/master` i tag `v0.13.2` wskazywały `90b72fa`. Katalog roboczy był czysty. Wydanie powstało jako jeden lokalny commit ponad bazą, z README i bez danych prób.

## 5. Pre-release

### HexMapEditor 0.13.3 — porządek w Library i węższa blokada aktualizacji

- Metadane grafik trafiają do podfolderów `Metadata`. Oryginały zostają widoczne bezpośrednio w folderach kategorii, a istniejące wpisy są porządkowane przy odczycie.
- Setup sprawdza, czy jest zajęty plik aktualizowanej kopii. Gdy działa ta kopia, po zapisaniu i zamknięciu jej okna można ponowić próbę z tego samego instalatora.

Dołącz oba pliki do pre-release’u `v0.13.3`. Po publikacji sprawdź anonimowe pobranie obu odznak z README i SHA-256. Na kopii 0.13.2 uruchom aktualizację przy otwartej innej instalacji i sprawdź, czy pozostaje otwarta. Zajęcie pliku docelowej instalacji powinno zatrzymać Setup i pozwolić ponowić próbę po zwykłym zamknięciu edytora. Po otwarciu Library sprawdź oryginał i odpowiadający mu JSON w `Metadata`.
