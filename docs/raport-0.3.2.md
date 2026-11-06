# Raport HexMapEditor 0.3.2

## Stan przygotowania

Implementacja zakończona, trzy celowane scenariusze metod/kontrolek zaliczone. Wykonano reprezentatywne próby zwykłego EXE i końcowy build 0.3.2.0. Nie jest to pełny audyt aplikacji.

Baza: czysty master `d28c7173d8b53abaa4b72e504e8e2f86926487a6`. Origin: https://github.com/amdesar/HexMapEditor.git; na początku HEAD i lokalne origin/master zgodne. Nie wykonywano fetch ani synchronizacji, których nie żądano w tej łatce. Przeczytano README, raport 0.3.1, odpowiednie wymagania i kod; nie znaleziono dodatkowych AGENTS.md w sprawdzonym repozytorium/ścieżce nadrzędnej. Nie było zastanych zmian plików. Nie rozpoczynano ponownie prac 0.3.1 ani diagnostyki potwierdzonych napraw. Bez push/tagu/Release'a.

## Zmiany

- Strzałki terenów przewijają o wysokość jednego wiersza wraz z marginesem (36 px), zamiast poprzednich 72 px. Na końcu zakresu krok jest ograniczany. Obie kontrolki mają jednakowe wymiary, marginesy i wyrównanie; wspólny lokalny bitmapowy symbol jest dokładnie odwrócony w pionie. Kółko, stałe miejsce przycisków i pozostałych sekcji bez zmian.
- Świadomy wybór terenu lub ikony po Mgle/Wiadrze mgły wybiera zwykły Pędzel. Zwykłe Wiadro oraz inne narzędzia zachowują wybór. Ustawienia pędzla zostają; sam wybór nie zmienia mapy/historii/gwiazdki. Synchronizacja i odświeżanie nie uruchamiają reguły. Dotyczy także głównego pełnego selektora; przyszłe obiekty opisano bez wdrażania.
- Edytuj ikonę ma Wybierz z biblioteki zamiast listy samych nazw. Wydzielony IconPickerDialog jest wspólny dla pełnego wyboru głównego i edycji. W kontekście edycji zwraca grafikę tylko do podglądu, zachowując logiczną tożsamość i nazwę edytowanego wpisu. Anuluj zachowuje dotychczasowy podgląd, a anulowanie całej edycji nie zapisuje zmian. Pozostają import pliku, nazwa, checkbox aktualizacji mapy domyślnie wyłączony oraz jedna operacja zatwierdzonej edycji. Poprawiono kontrast przycisku Anuluj selektora.
- Pliki presetów uczestniczą we wspólnej chronologicznej historii mapy/ikon/palety: nowy, kopia, nadpisanie, zapis nazwy z przemianowaniem pliku i usunięcie. Robocza zmiana nazwy nadal jest osobną operacją bez zapisu dyskowego. Domyślny pozostaje całkowicie chroniony, jego zapisaną własną kopię można cofnąć.
- LibraryFiles jest wyodrębnionym bezpiecznym mechanizmem plików ikon, współdzielonym z presetami. PresetFileChange przechowuje bajtowe stany/ścieżki i pojedynczą pozycję biblioteki, łącząc zmianę pliku z opcjonalną paletą projektu w jednym EditAction. Nie dodano osobnego stosu, zależności ani trwałego archiwum.

## Usuwanie i bezpieczeństwo historii presetów

Usuwanie **nie korzysta z Kosza**. Bajty i metadane poprzedniego pliku pozostają w pamięci sesyjnego Undo. Potwierdzenie wyjaśnia brak Kosza i utratę cofania po zakończeniu programu, udanym Nowa/Otwórz lub wypadnięciu wpisu z limitu 300. Undo usunięcia odtwarza rzeczywisty zapisany plik i właściwą paletę, bez dodatkowego Zapisz. Jeśli przed usunięciem paleta miała robocze różnice, one również zostają odtworzone. Limit 300 jest zaakceptowanym ograniczeniem.

Odczyt/zapis zapamiętuje dokładne bajty i ścieżkę; operacja i Undo/Redo wykrywają zmieniony/zniknięty plik, zajęty cel oraz kolizje ID/nazw innych presetów. Źródło jest etapowane pod unikalną nazwą i sprawdzane ponownie. Zapis używa CreateNew, Flush i atomowego Move bez nadpisania obcego celu. Przy błędzie odtwarzana jest poprzednia ścieżka. Jeżeli zewnętrzna kolizja uniemożliwi sam rollback, komunikat wskazuje zachowaną kopię `.library-session-*.tmp`; program nie nadpisuje zajętej ścieżki. Nowe pliki robocze mechanizmu mają nazwę `.library-write-*.tmp`.

Stan programu i stos przesuwają się dopiero po udanym przejściu dyskowym. Konflikt można rozwiązać i ponowić Undo/Redo. Nadpisanie samego presetu na dysku nie cofa wcześniejszej roboczej palety ani nie oznacza fałszywie mapy gwiazdką. Nowy/kopia/usunięcie rzeczywiście zmieniające paletę projektu zachowują odpowiedni stan niezapisanych zmian. Nie są cofane niezwiązane dane heksów, tekst, mgła ani ikony. Reset historii nie usuwa aktywnych plików biblioteki; błąd/anulowanie otwarcia jej nie zeruje.

## Nazwy ikon i formaty

Generator startowych ikon był już poprawny w bazowej 0.3.1. Krótka kontrola pustego własnego katalogu dała od razu `icon-Czaszka.json`, `icon-Gwiazda.json`, `icon-Krzyż.json`, `icon-Namiot.json`, `icon-Wieża.json`. Ponowne wczytanie po usunięciu własnej pozycji nie odtworzyło jej i nie zmieniło znacznika inicjalizacji. Nie zmieniano generatora, nie resetowano znacznika ani nie przemianowywano prywatnych plików. Długie stare nazwy pochodzą z mechanizmu 0.3.0; dotychczasowa świadoma edycja z OK porządkuje właściwy plik.

Format mapy **3**, presetu **1**, bibliotecznej ikony **2** bez zmian. Nadal osadzane są użyte deduplikowane zasoby i logiczne metadane wystąpień; nie ma zależności mapy od ścieżek biblioteki. Nie dodano konwerterów lub migracji map. Źródła pozostają w src, Library obok EXE.

## Wykonane testy metod i kontrolek

Każdy przypadek uruchomiono raz przez Run-Patch032Tests.ps1 z jednym Case, bez Run-AllTests i bez dawnych zestawów. Dane tylko we własnych podkatalogach tests/_work, po przebiegach pozostawiono results.log:

- **controls — PASS**, `tests/_work/patch-032-229bbde633a548e38ca0174468f7cb7d/results.log`: krok w obie strony, clamp, nieruchome inne sekcje; równe kontrolki i pikselowe pionowe odbicie symbolu. Wybór obu materiałów po dwóch narzędziach mgły, zachowanie Pędzla/Wiadra/Zaznaczania/Rączki, ustawień i danych/historii/gwiazdki, brak przełączenia przy refresh/synchronizacji. Lokalny wybór/anulowanie grafiki w edycji i zwracanie wpisu przez wspólny selektor.
- **library — PASS**, `tests/_work/patch-032-e5cb5a8a83d9448a9c9e22ab540f2fae/results.log`: pięć czytelnych nazw w pustym katalogu i trwały znacznik bez odtwarzania usuniętej ikony; jedno dodanie ikony i dokładne plikowe Undo/Redo jako bezpośrednia zależność wydzielenia LibraryFiles.
- **presets — PASS**, `tests/_work/patch-032-58db688a50bd4e0a8339ba0353c9352c/results.log`: kopia Domyślnego i neutralny nowy preset, nadpisanie, robocza nazwa oraz zapis przemianowania, usunięcie; dokładne bajty/ścieżki/stan programu przez Undo/Redo. Brak fałszywej gwiazdki po nadpisaniu zapisanego projektu i zachowanie niezwiązanej nazwy/opisu/mgły heksa. Zajęta odtwarzana ścieżka bez nadpisania/stosów, ponowienie po usunięciu własnego konfliktu, zewnętrzna zmiana oraz blokada pliku bez zmiany pamięci/historii. Błędne otwarcie zachowuje historię; udane Otwórz/Nowa zerują stosy bez usuwania aktywnych plików. Blokada usunięcia Domyślnego.

Ostatnia zmiana po tych wynikach dotyczy wyłącznie kolorów przycisku Anuluj selektora. Nie ponawiano z tego powodu zaliczonych testów logiki. Historyczne testy dawnego callbacku Kosza i oczekiwania braku plikowego Undo presetów pozostają w repozytorium jako historyczne; ich stare założenia nie są wymaganiami 0.3.2.

## Faktyczne próby zwykłego EXE

Własny zwykły EXE (bez diagnostycznych haków) zbudowano przez Build.ps1 w `tests/_work/ui-032-15b00899b16f4ff4b94a1cb708f035af`. Własna mapa 8×5, preset z 18 terenami i świeża biblioteka, bez prywatnej Library:

- Jedno kliknięcie dolnej strzałki przesunęło widoczny początek z Teren 01 na Teren 02, nie o dwa wiersze; otaczające sekcje pozostały nieruchome.
- Mgła → kliknięcie terenu oraz Wiadro mgły → kliknięcie ikony dały Pędzel i właściwy materiał, bez gwiazdki mapy.
- Edycja ikony przy wybranej Mgle: wspólny graficzny selektor pokazał miniatury/nazwy; Anuluj pozostawił podgląd, wybór Namiotu zmienił tylko podgląd grafiki, nazwa Gwiazda i checkbox wyłączony pozostały. Po OK zmieniła się grafika biblioteki, główne narzędzie pozostało Mgłą, materiał pozostał tą samą logiczną ikoną, mapa czysta.
- Po zgodzie użytkownika zatwierdzono usunięcie wyłącznie własnego presetu testowego. Sprawdzono brak pliku, następnie Ctrl+Z odtworzył plik, aktywną paletę i jej stan zgodny z zapisem. Gwiazdka zniknęła; wcześniejsza edycja grafiki ikony pozostała. SHA256 odtworzonego pliku był identyczny z poprzednim: `64B0C215F577C2745612C63C3A6A76ABA4F39096A6C989AF5E50F4128956BF2D`. Nie wykonywano drugiego usunięcia przez Redo w oknie; Redo sprawdzono metodą.

Pierwsza próba głównego builda zakończyła się CS0016 (plik używany przez proces). Computer Use pokazał istniejące okno użytkownika HexMapEditor 0.3.1 z gwiazdką; nie zamykano go i nie odrzucano danych. Użytkownik zamknął swoje okno i potwierdził możliwość kontynuowania. Po sprawdzeniu listy okien główny build zakończył się powodzeniem: FileVersion/ProductVersion **0.3.2.0**. Początkowa lista procesów z sandboxa nie pokazała starego okna, więc nie stanowiła dowodu, że nie działało.

Końcowy główny EXE skopiowano do własnego przebiegu UI; oba pliki miały SHA256 `5F335F178CB6E07C6E086FED89D692F9CF41936869B5B934A3921B1F609342CD`. Uruchomiono tę identyczną zwykłą kopię i obejrzano zmieniony kontrast przycisku Anuluj. Selektor zamknięto Escape, program zamknięto czysto. Nie powtarzano wcześniej zaliczonych prób z powodu wyłącznie kosmetycznej zmiany. Nie uruchamiano głównego pliku przy prywatnej Library jako materiału testowego.

Po potwierdzeniu zakończenia własnych procesów usunięto wyłącznie własną mapę, EXE i Library z przebiegu UI. Pozostawiono `tests/_work/ui-032-15b00899b16f4ff4b94a1cb708f035af/results.log`. W trzech przebiegach metod również pozostały tylko logi. Źródła i skrypty są śledzone, artefakty ignorowane. git ls-files nie wykazał śledzonych Library/tests/_work/bin; git check-ignore potwierdził wszystkie trzy reguły.

## Niewykonane próby i ograniczenia

Nie uruchamiano pełnego zestawu, starych kampanii mgły/interpolacji/geometrii/przeskoku, Worda/formatowania opisów, DPI ani wydajności dużych map. Nie deklarujemy pełnego przetestowania aplikacji ani historycznych wyników jako nowych. Testy metod nie są pełnym testem dialogu Windows. Nie symulowano każdej możliwej awarii systemu plików ani wyścigu z innym procesem; sprawdzono wymienione konflikty i odmowę dostępu. Prywatna biblioteka, Kosz Windows i mapy użytkownika nie były materiałem testowym ani przedmiotem sprzątania.

## Checklista użytkownika

- Kliknij obie strzałki terenów: jeden wiersz, równe symbole, poprawne granice; kółko nadal działa.
- Po Mgle/Wiadrze mgły wybierz teren lub ikonę (także z pełnej biblioteki): Pędzel; po zwykłym Wiadrze wybór materiału zachowuje Wiadro i ustawienia pędzla.
- Edytuj ikonę → Wybierz z biblioteki: miniatury, Anuluj bez zmiany podglądu, wybór bez zmiany głównego materiału/narzędzia przed OK; sprawdź checkbox domyślnie wyłączony.
- Na własnym presecie sprawdź nowy/kopię, nadpisanie, roboczą nazwę i zapis zmienionej nazwy oraz usunięcie: Undo/Redo odtwarza odpowiednie pliki i stan, bez cofania niezwiązanych heksów.
- Przy zajętej ścieżce odtworzenia oczekuj odmowy bez nadpisania; po rozwiązaniu konfliktu ponów. Nowa/Otwórz po sukcesie zeruje historię bez kasowania aktywnych presetów. Cofanie jest tylko sesyjne, do 300 wpisów.

## Commit i uruchomienie

Uruchamianie: główny HexMapEditor.exe lub Uruchom HexMapEditor.bat. Budowanie: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Build.ps1`. Końcowy build i sprzątanie własnych danych wykonano. Commit obejmujący ten raport i zmiany 0.3.2 podano w końcowej odpowiedzi; można go odczytać przez `git log -1`. Nie wykonano push/tagu/Release'a.
