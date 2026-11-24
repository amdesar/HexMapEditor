# Raport HexMapEditor 0.10.1

## 1. Wykonanie

- **Ostatni zapis.** Wspólny writer zapisuje opcjonalne pole `LastSavedAppVersion` wraz z mapą. Ctrl+S, Zapisz jako i autozapis publikują je w tej samej operacji co dane projektu. Otwarcie odczytuje informację. Anulowanie i błąd nie zmieniają pliku. Ostatnie projekty odczytują do 512 znaków początku pliku w istniejącym sprawdzaniu dostępności w tle. Osobny wiersz etykiety jest widoczny tylko po odczycie poprawnej wersji. Komunikat dla niezgodnego formatu podaje wersję zapisującą lub informuje, że jest nieznana. Map12 pozwala na pole opcjonalne, więc nie wymaga zmiany numeru. Formaty presetów i bibliotek pozostają dotychczasowe.
- **Prawy panel.** Windows może kopiować poprzednie bity przy zmianie położenia okna potomnego. Panel jawnie wyłącza to kopiowanie dla swoich zmian geometrii. Unieważnia własne kontrolki i odsłonięty pas płótna. Przy przejściu grupa–obiekt układ jest wstrzymany do przygotowania docelowego widoku. Zamiast zagnieżdżonego automatycznego dopasowania wysokość zawartości jest mierzona dla dostępnej szerokości. Miejsce na scrollbar jest uwzględniane wtedy, gdy pełna szerokość daje treść wyższą od widoku. Przycisk usunięcia ma od razu 30 px. Wiersze właściwości utrzymują wysokość przy przełączaniu promieni i koloru ramki.
- **Trzy widoki.** Nagłówki nazw urosły z 15 do 29 pt, metadane mają 15 pt, nagłówki sekcji 12 pt. W heksie teren i ikona są w dwóch wierszach obok podglądu. Współrzędne są przy linii bazowej nazwy. Ustawienie nazwy ikony zależy od przypisania do heksa, ponieważ wcześniejsze sprawdzanie `Visible` zwracało również stan ukrytego rodzica. Usunięto separator nad obiektami. Obiekt zachowuje miniaturę, kwadratowy plus ma większy glif, a przyciski rozmiaru wysokość pola. Powtórzona nazwa stanu wymaga potwierdzenia. Grupa ma kolorowe pozycje stanu, ikonowe tryby rozmiaru i osobny reset oraz bursztynową ramkę rozwiązania. Minimalna szerokość wynika z pomiaru pięciu pełnych nagłówków, osobnej kolumny nazw, uchwytów, marginesów i rezerwy przewijania. W próbie wyniosła 456 px.
- **Rozwiązanie grupy.** Kolumna właściwości, „Wszystko” w wierszu i „Wszystko” dla całej tabeli operują na kwalifikujących się komórkach. Zmiana pojedynczego switcha przelicza stan zbiorczy. Wartość grupowa Off jest zachowywalna. Anulowanie zostawia grupę bez zmian, a zatwierdzone rozwiązanie używa istniejącej transakcji Undo.
- **Opis.** Powiększenie przeniesiono do wiersza rozmiaru czcionki. Większe ikony zawijają się, podświetlenia odpowiadają zaznaczeniu, a Undo/Redo sygnalizują stan lokalnej historii. Kliknięcie przycisku formatowania nie przejmuje fokusu z opisu. Zwykłe wklejanie, tabele i wklejanie tekstowe mają dotychczasowe wejścia.
- **Import schowka.** Ikony i obiekty mają „Ze schowka” przed plikiem i przeciwną biblioteką. Wariant grafiki terenu ma „Schowek / Plik / Biblioteka / Kadruj”. Odczytany raster trafia do istniejącej walidacji i kwadratowego lub heksagonalnego kadru. Po akceptacji nadal działają istniejące zapisy biblioteczne z historią i szybkim zestawem. Anulowanie kadru nie tworzy wpisu.

## 2. Odstępstwa i ograniczenia

Prawa krawędź została poprawiona bez wariantu zapasowego. W próbie bazowej wykonano oba kierunki ruchu panelu. Stan końcowy był poprawny. Nie udało się zarejestrować zgłaszanych pojedynczych klatek smużenia, przycinania ani mignięcia grupa–obiekt. Narzędzie przy równoległym żądaniu zrzutu zwróciło obraz po zakończeniu gestu. W próbie metod sprawdzono geometrię w ośmiu pozycjach aktywnego gestu, a w GUI odsłonięcie płótna i krawędzie w obu kierunkach. Wynik nie wyklucza pojedynczych klatek na innym stanowisku.

Pierwsze otwarcie heksa zadziałało w bazie i kandydacie, bez wykazanego powodu do przenoszenia kosztu na start. Wpis rozmiaru czcionki 20 i przeciągnięcie do 24 zastosowały się do zaznaczonego tekstu. Sporadyczny błąd nie wystąpił w tej próbie. Start z argumentem własnej mapy zadziałał w bazie i w końcowym EXE. Zachowano jego dotychczasową obsługę.

Nie potwierdzano fizycznego Full HD/100%. Faktyczne okno edytora w sesji miało zrzut 1426×934 px. Bez pełnej regresji, kampanii historycznych formatów i testu długiego oczekiwania na autozapis. Uwzględniono tylko zakres 0.10.1.

## 3. Faktyczne źródła schowka i limity

Word w trybie zgodności otworzył własną próbkę RTF. Po zaznaczeniu tekstu z obrazem schowek udostępnił m.in. RTF, HTML i UnicodeText. RTF zawierał osadzony PNG pod `shppict`, z zagnieżdżonymi właściwościami, i alternatywny metaplik pod `nonshppict`. HTML odwoływał się do pliku tymczasowego Worda. Stary parser odrzucał grupę właściwości i obraz znikał, mimo że osadzony PNG był dostępny. Nowy parser omija całe grupy właściwości i zachowuje raster w miejscu znacznika między fragmentami tekstu. Kopia samego obrazu udostępniała też PNG, JFIF, Bitmap i DIB. Jeśli reprezentacja dokumentu nie zawiera treści, program próbuje PNG/JFIF i bitmapy zamiast wstawiać pusty akapit.

Zachowano lokalną interpretację RTF/HTML i brak pobierania z odwołań zewnętrznych. Format metapliku samodzielnie, prywatne formaty Office i binarny RTF nie służą do odtwarzania obrazów. Bezpośrednie próby przeglądarki i OneNote pozostają poza wykonaną sesją.

| Budżet | Limit |
|---|---|
| Zakodowane źródło opisu | 8 MiB PNG/JPEG |
| Rozmiar dekodowanego źródła opisu | Bok do 8192 px i do 24 Mi pikseli, także bitmapa ze schowka |
| Osadzone PNG | Dłuższy bok do 2048 px, do 17 MiB na obraz, mniejsze źródło bez powiększania |
| Opis / mapa | 12 obrazów i 24 Mi znaków na opis, 64 Mi znaków łącznie na opisy mapy, cały plik do 256 MiB |
| Bogata reprezentacja schowka | 36 Mi znaków RTF lub HTML |
| Import grafik | Teren 32 MiB i 4096 px na bok, ikona/obiekt 8 MiB i 2048 px na bok, końcowy kwadrat 512×512 |

Próba z drobnym tekstem użyła PNG 2800×5400 o rozmiarze 2 689 310 B. Po wstawieniu wynik miał 1062×2048 i zachował przezroczysty margines. Po przeniesieniu mapy bez źródłowego pliku zapisane PNG wróciło z opisu.

## 4. Testy i powody powtórzeń

| Rodzaj | Wynik |
|---|---|
| `-Case metadata` | PASS. Zapis ręczny produkcyjną ścieżką, Zapisz jako, kontrolowany autozapis, MRU, błąd zapisu, anulowanie i odmowa odczytu sztucznego formatu z wersją oraz bez wersji. |
| `-Case panel` | PASS. 7, 8 i 19 stanów, wszystkie kombinacje mgły i widzenia przy 8 stanach z blokadą Off, 19 stanów w siedmiu rzędach w mniejszym oknie, stałe 30 px przycisku usunięcia, pełne pola i nagłówki. Zmierzono 802 px treści przy widoku 703 px dla 19 stanów z przewijaniem. Sprawdzono osiem położeń w aktywnym geście oraz osobno przewijanie po dodaniu stanów. Przypadek obejmuje macierz, wybory zbiorcze, brak nakładki, anulowanie, transakcję rozwiązania i Undo. |
| `-Case notes` | PASS. Lokalne dane RTF, dane RTF/HTML pobrane z własnej próby Worda, sam obraz, zastępczy PNG przy pustym wyniku HTML, plik 2800×5400, tabela, przeniesienie mapy, zaakceptowane kadry i anulowanie kwadratowego kadru. |
| Zwykłe EXE | W bazie potwierdzono brak obrazu z mieszanego schowka Worda. W 0.10.1 sprawdzono pełną etykietę MRU, uruchomienie ze ścieżką, zimny widok obiektu z ośmioma stanami, przejście grupa → członek z linku i grupa → obiekt spoza grupy przez mapę, odczyt ikony heksa pod obiektem, poszerzenie i zwężenie, oba promienie, duplikat stanu z anulowaniem i z potwierdzeniem, działanie „Wszystko” z późniejszą ręczną zmianą i anulowanie. |
| Zwykłe EXE, opis i import | Z Worda wklejono sam obraz, obszerne zaznaczenie z tekstem po obu stronach obrazu i sam tekst przez Ctrl+Shift+V. Po kliknięciu pogrubienia zaznaczenie pozostało aktywne. Ctrl+S i ponowny odczyt zachowały obraz i formatowanie. Własny obraz z Worda otworzył wszystkie trzy dialogi kadrowania. Anulowano każdy, a szybkie zestawy i lista terenów pozostały bez nowych pozycji. |

Wczesny kandydat ujawnił sklejony tekst na przycisku MRU i jasne prostokąty w miejscu wyłączonych ikon WPF. Po zmianie rysowania powtórzono tylko odpowiednie próby wizualne. Case panel powtórzono po poprawieniu nazwy ikony oraz doprecyzowaniu danych dla 7/8/19 stanów, żeby ładowanie biblioteki testowej ich nie zastępowało. Case notes powtórzono po ograniczeniu łącznego ładunku i po zmianie szablonu przycisków. Nie uruchamiano Run-AllTests.

## 5. Checklista odbiorcza

| Czynność | Oczekiwany wynik |
|---|---|
| Zapisz mapę i uruchom ekran startowy | Przy projekcie jest „Ostatni zapis: 0.10.1”. Plik bez metadanej nie ma tego wiersza. |
| Otwórz projekt o sztucznie nieobsługiwanym formacie | Komunikat podaje wersję zapisującą lub stan „nieznana”, a plik pozostaje. |
| Poszerz i zwęź heks, następnie przejdź z grupy do obiektu | Ołówek i przycisk opisu mieszczą się, odsłonięte płótno odświeża się, wybrany widok się zmienia. |
| Po starcie wejdź w obiekt z 8 stanami i przełącz oba promienie | Wysokość wierszy i przycisku usunięcia nie zmienia się. |
| Wybierz „Wszystko” przy rozwiązaniu grupy i wyłącz jedną komórkę | Wskaźniki zbiorcze aktualizują się. Anulowanie zachowuje grupę. |
| Skopiuj z Worda tekst z osadzonym obrazem i wklej do opisu | Kolejność pozostaje po ponownym odczycie. Wklej jako tekst pomija obraz. |
| Wybierz „Ze schowka” dla terenu, ikony lub obiektu | Pojawia się odpowiedni kadr. Anulowanie nie tworzy zasobu. |

## 6. Artefakt i Git

Baza `383dd55`, początkowy Git czysty. Aplikacja 0.10.1, FileVersion i ProductVersion 0.10.1.0. SHA-256 sprawdzonej kopii i głównego EXE `3AD288B4E417596925B82ADF8B1840F8CC87078D1CADB0172F5F6E56FE8B11AC`. `fc /b` potwierdził zgodność wszystkich bajtów. Zakres jednego lokalnego commita to kod, EXE, dokumentacja i źródła testów. Identyfikator commita i stan Git podano po jego wykonaniu. Dane i wyniki prób są w `tests/_work`, poza historią.
