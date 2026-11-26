# HexMapEditor 0.13.4 — raport wydania

## 1. Wykonanie

- Przycisk warstw znajduje się bezpośrednio przed High/Low. Lista pozwala zmienić kilka warstw kolejno i działa także z menu nadmiarowego paska. Ctrl+Shift+L otwiera listę z fokusem na pierwszej pozycji. Menu Widok → Warstwy i szybki przycisk odczytują te same flagi sesji. Przełączenie warstwy nie zmienia pliku, stanu zapisu ani reguł edycji ukrytej warstwy.
- Każde pole liczbowe przybornika ma własną historię wpisu i zatwierdzonych wartości, w tym przeciągania. Nieprawidłowy wpis i anulowanie nie dopisują zmiany. Ctrl+Z i Ctrl+Y w polu działają lokalnie, a w kontekście mapy używają jej historii.
- Okno oferty aktualizacji mieści opis, niebieską akcję instalacji i czerwone „Pomiń” w jednym rzędzie. Podczas pobierania to samo okno pokazuje postęp, a czerwony przycisk zmienia nazwę na „Anuluj”. Anulowanie jest sprawdzane między odczytami i po weryfikacji. Czas reakcji na anulowanie może zależeć od limitu czasu trwającego odczytu. Pominięcie zamyka bieżącą ofertę. Zachowano weryfikację pakietu, przygotowanie zapisu pracy, uruchamianie Setup i ręczną aktualizację Portable.
- Edytor i biblioteka teł używają wspólnego odczytu rastra ze schowka. Import używa limitów tła do 32 MiB zakodowanych danych i 4096 px na wymiar oraz istniejącego kadrowania. Zatwierdzony obraz i metadane trafiają do istniejących katalogów Library. Zamknięcie lub anulowanie szkicu przed zatwierdzeniem niczego nie zapisuje.
- Poprawiono kontrast własnych przycisków, pól liczbowych, przełączników i menu. Zmiany objęły ustawienia i bibliotekę teł, kolejkę importu, wybór źródła i komunikat o duplikacie grafiki, kadrowanie terenu oraz komunikat w oknie zmiany rozmiaru mapy. Ciemna kolejka ma czytelny nagłówek, zaznaczenie i ramkę fokusu. Natywne komunikaty zapisu, systemowy wybór plików i powierzchnie WPF zachowują własny styl.
- Pomoc → O programie otwiera jedno niemodalne okno z numerem i datą wydania, autorem, licencją i informacją o pomocy ChatGPT oraz Codexa. Link prowadzi do repozytorium w domyślnej przeglądarce. Esc uruchamia akcję zamknięcia okna.
- Powitanie i edytor przyjmują do ośmiu plików `.hexmap` naraz. Otwarcie używa dotychczasowego czytnika, MRU i wyboru istniejącej karty. Katalogi, inne rozszerzenia, niedostępne pliki i ścieżki w `Library/Import` są odrzucane. Błędy odczytu map pokazuje istniejąca obsługa formatu. Upuszczenie podczas gestu, zapisu w tle lub dialogu modalnego jest ignorowane. Windows może blokować przeciąganie pomiędzy procesami na różnych poziomach uprawnień.

Zachowano Map12, format presetów i układ Library. Kod Setup i wydajność mapy 60×60 pozostają poza zakresem tej wersji.

## 2. Próby i ograniczenia

### Metody i kontrolki

| Próba | Wynik |
| --- | --- |
| `Run-Patch0134Tests.ps1 -Case tools` | PASS. Spójność flag przycisku i menu, zwykła i minimalna szerokość okna, wymuszony overflow, skrót klawiaturowy w obu położeniach, lokalna historia pola po wpisie i przeciąganiu, walidacja i anulowanie, osobne Undo mapy, niemodalność i zamknięcie okna informacji. |
| `Run-Patch0134Tests.ps1 -Case flow` | PASS. Jeden rząd akcji, zmiana etykiety i anulowanie pobierania bez pozostałego częściowego pliku. Próbny raster PNG w obiekcie danych schowka obsłużony przez edytor tła, odrzucenie tekstu, brak zapisu przed zatwierdzeniem, zapis oryginału i `Metadata` z porównaniem bajtów. |
| `Run-Patch0134Tests.ps1 -Case drop` | PASS. Rzeczywiste kontrolki WinForms z próbnym zdarzeniem `FileDrop`. Dwie mapy, wybór istniejącej karty, MRU, limit, odrzucenie ścieżek spoza zakresu oraz zachowanie kolejności na powitaniu i blokada podczas przejścia. |
| `Run-Patch0120Tests.ps1 -Case channel` | PASS. Kontrolowane odpowiedzi kanału i walidacja pobierania, w tym błędna suma. Setup nie był uruchamiany przez aktualizator. |
| `Run-Patch0130Tests.ps1 -Case metadata` oraz `-Case map` | PASS. Bieżący układ źródeł i metadanych w Library oraz samodzielny odczyt i zapis mapy Map12. |

Powtórzono `tools` po dodaniu jawnego skrótu i przełączeniu próby na normalną pętlę komunikatów WinForms. Poprzednia wersja runnera kończyła sprawdzone asercje, ale zwracała błąd natywnego callbacku przy zamykaniu. Próbę `drop` powtórzono po dodaniu ekranu powitalnego z działającą pętlą komunikatów. Próbę `channel` powtórzono na końcowej paczce Setup, bo zmieniły się jej bajty i suma.

### Zwykły EXE

Uruchomiono EXE rozpakowany z końcowego Portable na własnej mapie 5×4. Przycisk warstw otworzył listę myszą. Ctrl+Shift+L otworzył listę z fokusem na „Tło”, Enter przełączył pozycję bez zamykania listy, a Esc ją zamknął. W polu rysika wpis zmienił 5 na 15, a Ctrl+Z przywróciło 5 bez oznaczenia mapy jako zmienionej. Obejrzano zawartość okna „O programie”, edytor i bibliotekę teł z akcją schowka oraz pustą i wypełnioną kolejkę. Sprawdzono stan zaznaczonego wiersza i niedostępnych przycisków.

Nie wykonywano gestu z Eksploratora między oknami, próby fizycznego schowka z innej aplikacji, próby na odrębnych ustawieniach DPI ani wizualnego przeglądu każdego dialogu. Publiczna aktualizacja online i uruchomienie końcowego Setup pozostają do odbioru po publikacji. Nie uruchamiano pełnej kampanii.

## 3. Checklista odbioru

1. Otwórz warstwy z paska i zmień dwie pozycje → lista pozostaje otwarta, a menu Widok pokazuje te same znaczniki.
2. Zmień rozmiar rysika, cofnij w polu, potem cofnij edycję na płótnie → zmiany narzędzia i mapy cofają się osobno.
3. Wklej grafikę w edytorze tła lub bibliotece teł → pojawia się szkic z dotychczasowym kadrowaniem. Anulowanie szkicu nie dodaje wpisu.
4. Upuść jedną, a potem dwie własne mapy z Eksploratora na okno programu → pliki otwierają się w kartach, a powtórka wybiera istniejącą kartę.
5. Otwórz Pomoc → O programie → widoczne są wersja, autor, licencja i link. Zamknij okno klawiszem Esc.
6. Na przyszłej ofercie aktualizacji sprawdź „Pomiń”, a następnie „Anuluj” podczas pobierania → bieżąca praca zostaje otwarta.

## 4. Kompilacja i Git

Wersja aplikacji `0.13.4`, FileVersion i ProductVersion `0.13.4.0`. Paczki z jednej kompilacji znajdują się w ignorowanym `tests/_work/patch-0134-final/packages`. Portable zawiera EXE, znacznik trybu, instrukcję i licencję. `fc /b` potwierdziło zgodność głównego EXE ze sprawdzonym EXE rozpakowanym z ZIP.

| Plik | SHA-256 |
| --- | --- |
| `HexMapEditor.exe` | `5946629BEB91481755B123A54C9AEFDED4A454ADE93A0F06B0103D788B674C71` |
| `HexMapEditor-0.13.4-Setup.exe` | `EE4DC262B1E17E1A0711CDDCE2649CF56E6D6EF12391537431C1705D311E24CD` |
| `HexMapEditor-0.13.4-Portable.zip` | `373FDD8B1E5064C1E89D66E6C55C6469F9B6B28C62BCE3EBF333B4C2ABBC365A` |

Baza `master` i lokalna referencja `origin/master` wskazywały `087cb12`. Katalog był czysty przed pracą. Wydanie stanowi jeden lokalny commit przed `origin/master`, z czystym katalogiem roboczym. README wskazuje dokładne nazwy paczek pod `v0.13.4`. Tag, paczki publiczne i wydanie pozostają do publikacji przez właściciela projektu.

## 5. Opis pre-release’u

**HexMapEditor 0.13.4 — szybkie warstwy i otwieranie map przez przeciąganie**

Szybki przełącznik warstw znajduje się obok High/Low. Pola liczbowe narzędzi mają lokalne cofanie, a mapy można otwierać przez upuszczenie na edytor lub powitanie. Wydanie dodaje także import tła ze schowka, okno „O programie”, czytelniejsze okno aktualizacji i poprawki kontrastu. Format map pozostaje Map12.
