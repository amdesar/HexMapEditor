# Raport HexMapEditor 0.11.0

## 1. Wykonanie

- Jedno okno i jeden zestaw kontrolek obsługują sesje dokumentów. Każda ma własną mapę, ścieżkę, odcisk pliku, stan zapisu, historię Undo/Redo, historię opisu i stan widoku. Po powrocie kamera i nadal istniejące zaznaczenie wracają, także przy zwiniętych szczegółach. Rzeczywiste przełączenie kończy poprzedni gest oraz anuluje niezatwierdzoną nazwę i odznacza materiał. Narzędzie przybornika i szerokości paneli są wspólne.
- Pasek dzieli wiersz ze statusem mapy. Plus i strzałki są na stałych pozycjach. Karty mają po 207 px, w stałym wierszu 36 px. Aktywowana karta jest odsuwana na widoczną część. Własna nazwa pliku i gwiazdka są widoczne na karcie, pełna nazwa i ścieżka są w podpowiedzi. Karta bez ścieżki otrzymuje aktywny ołówek po pierwszym zapisie.
- Otwarcie tej samej znormalizowanej ścieżki wybiera istniejący dokument. Ścieżki odróżniają także identyczne nazwy plików w różnych folderach. Następne uruchomienie tej samej kopii programu przekazuje argument lokalnym potokiem w obrębie katalogu instalacji, sesji Windows i konta. Oczekujące otwarcie zaczeka na koniec dialogu, gestu lub operacji gumki. Skojarzenia plików pozostały po stronie systemu.
- Zamknięcie nieaktywnej karty zachowuje bieżący wybór. Po zamknięciu aktywnej wybierana jest prawa karta, a gdy jej nie ma, lewa. Ostatnia prowadzi do ekranu powitalnego w tym samym procesie. Zamknięcie programu pyta o mapy, a następnie o wymagające zapisu presety. Wspólny preset jest rozpatrywany raz. Przy usuwaniu jednej karty wspólny roboczy preset pozostaje dostępny, jeżeli korzysta z niego jeszcze inna karta. Anulowanie zachowuje karty i skuteczne wcześniejsze zapisy.
- W dialogach źródła ikony i obiektu fokus początkowo trafia na kontener. Pierwszy Tab przechodzi na „Ze schowka” i wyświetla ramkę. Przycisk importu grafiki terenu pozostaje dotychczasowy.

## 2. Zapis i presety

`MapData` przechowuje heksy, osadzone zasoby, obiekty oraz wybór palety. Robocze definicje palet są w sesji. Są osadzane przy zapisie danej mapy. Otwierana mapa dostaje paletę katalogu powiązaną przez istniejący identyfikator. Gdy pliku zabraknie, używa palety osadzonej. Powiązane sesje odczytują wspólny draft po znormalizowanej ścieżce pliku, a różne pliki o tej samej nazwie pozostają oddzielne. Sesyjne, chwilowe użycie zasobu wpływa na kolejność szybkiego dostępu w jednej karcie. Jawne zmiany sekcji ikon lub obiektów trafiają również do wspólnego draftu przy tym samym pliku. Nie powoduje to zapisu, zmiany historii ani podmiany pomalowanych pól innego dokumentu.

Odświeżenie czyta katalog i zachowuje robocze dane. Zapis presetu sprawdza tożsamość pliku i odcisk danych. Przy cofnięciu starszego wpisu, który nadpisałby późniejszą zmianę wspólnej palety lub szybkiego zestawu z innej karty, program zachowuje obie historie i zgłasza konflikt. Częściowy zapis pozostawia pominięte sekcje do dalszej edycji. Ścieżka pliku i stan widoku nie trafiają do modelu Map12.

Ctrl+S i Zapisz jako zapisują aktywną mapę. Zapis jako i zmiana nazwy odrzucają ścieżkę zajętą przez inną kartę. Jeden zegar odmierza gotowość osobno dla dokumentów, a jeden cykl kolejkuje kwalifikujące się zmienione mapy z plikiem. Zapisy wykonywane są pojedynczo, na migawkach z rewizją i odciskiem pliku. Wynik aktualizuje właściwą kartę również po zmianie aktywnej. Konflikt nie usuwa gwiazdki i nie zatrzymuje pozostałych zapisów. Jest jeden wynik cyklu w pasku statusu z informacją o błędach w podpowiedzi. Zachowano format Map12, pole wersji ostatniego zapisu oraz wersje formatów presetów i bibliotek.

## 3. Testy metod i kontrolek

| Przypadek | Wynik |
|---|---|
| `Run-Patch0110Tests.ps1 -Case documents` | PASS. Zapisy A/B z ponownym odczytem, osobne cofanie i ponawianie, odrzucenie zapisu do ścieżki innej karty. Otwarcie tej samej ścieżki ze zmianą wielkości liter, otwarcie identycznej nazwy z innego katalogu i nieudane otwarcie. Odtworzenie heksa, obiektu, grupy, kamery i zwinięcia, anulowanie draftu nazwy, wspólne narzędzie z odznaczeniem materiału. Wspólna paleta i jawna zmiana szybkiego zestawu wyłącznie dla wspólnego pliku, odświeżenie bez utraty draftu. Anulowanie zamknięcia, zamknięcie nieaktywnej karty i anulowanie wyjścia po zapisie A. Przy minimum 1180 px obszar kart miał 705 px na trzy pełne karty, pasek 36 px. Dodatkowe lekkie karty wymusiły przewijanie. Trzy dopasowania przy różnych liczbach kart nie zmieniły wysokości płótna. |
| `Run-Patch0110Tests.ps1 -Case autosave` | PASS. Kontrolowany termin cyklu, zmiana aktywnej karty w trakcie przygotowania, konflikt odcisku A, dalszy zapis nieaktywnej B i pominięcie mapy bez ścieżki. Ponowny odczyt potwierdził treść B i wersję 0.11.0. |
| `Run-Patch0110Tests.ps1 -Case fixture` | PASS. Lekka biblioteka i trzy odizolowane pliki map, w tym dwie mapy z nazwą pliku identyczną w różnych folderach. |

Próba dokumentów została ponowiona po korekcie wywołania testowego Undo i po rozszerzeniu sprawdzanych ścieżek o zaznaczenie grupy i obiektu oraz jawne szybkie zestawy. Autozapis powtórzono po odseparowaniu kopii palety na potrzeby walidacji zapisu.

## 4. Próba zwykłego EXE i ograniczenia

Własna Mapa A otworzyła się bezpośrednio z argumentu. Drugie wywołanie EXE z Mapą B dodało kartę w oknie o tym samym uchwycie. Ponowne wywołanie Mapy B już po namalowaniu heksa zachowało dwie karty, gwiazdkę, heks i powiększenie. Plus pokazał dwie akcje, a anulowanie Nowej nie dodało karty. Zweryfikowano również nazwę mapy w pytaniu o zapis, Anuluj, Ctrl+S, zamknięcie nieaktywnej A oraz powrót do startu i odczyt zapisanej B. W obu dialogach źródła kontrolka schowka uzyskała widoczny fokus dopiero po pierwszym Tab.

Pierwsza wersja kandydata ujawniła przedwczesne zwolnienie uchwytu oczekiwania potoku. Po przeniesieniu zwolnienia za `EndRead` powtórzono przekazywanie i bezpośrednie otwarcie. Próby odbyły się przez wywołanie EXE z argumentami. Nie wykonano dwukliku w Eksploratorze, próby sygnału upływu pełnych pięciu minut, testu stanowiska użytkownika ani pełnej regresji. Odłożone artefakty krawędzi i edytora heksa zachowują dotychczasowy zakres.

## 5. Checklista odbiorcza

| Czynność | Oczekiwany wynik |
|---|---|
| Zmień A, otwórz B i zapisz B | Znika gwiazdka B. Zmiana i historia A pozostają. |
| Otwórz ponownie ścieżkę A | Otwiera się istniejąca karta z jej widokiem, szczegółami i niezapisanymi zmianami. |
| Zmień roboczą paletę w dwóch kartach powiązanych z tym samym plikiem presetu | Obie widzą roboczą paletę, a zapisane wystąpienia i stan zapisu drugiej mapy pozostają. |
| Zapisz jako na ścieżkę drugiej otwartej karty | Polecenie odmawia zapisu i wskazuje konflikt ścieżki. |
| Anuluj zamknięcie zmienionej karty | Karta i zmiany pozostają. Po zamknięciu ostatniej zapisanej mapy wraca ekran powitalny. |
| Otwórz Dodaj ikonę lub Dodaj wzorzec obiektu i wciśnij Tab | „Ze schowka” dostaje wyraźny fokus po Tab. |

## 6. EXE i Git

Baza `0194ce2`, początkowy status i diff czyste. Aplikacja 0.11.0, FileVersion i ProductVersion 0.11.0.0. SHA-256 sprawdzonego EXE `9810671CEA7ADBC1F182A02272DFB5EA0464B6DBE96BDD5241BD823566C81084`. Główne EXE pochodzi z tej samej kopii, porównanej bajtowo. Zakres jednego lokalnego commita obejmuje kod, EXE, źródła prób i dokumentację. Dane robocze i kopie pozostały w ignorowanym `tests/_work`. Końcowy identyfikator i stan Git podano po wykonaniu commita.
