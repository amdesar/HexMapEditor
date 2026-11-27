# HexMapEditor 0.13.5 — raport wydania

## 1. Zmiany

- Akcje edytora tła są ułożone w kolejności Schowek, Plik, Biblioteka, Usuń tło. Pod przyciskami jest więcej miejsca na opisy. W bibliotece można rozpocząć import z obu źródeł, wybrać istniejące tło albo usunąć zaznaczony wpis. Zachowano obsługę kadrowania i czerwony styl operacji usuwania.
- „O programie” jest dostępne z Pomocy i z powitania. Wyśrodkowanie wykorzystuje obszar roboczy monitora okna wywołującego i aktualny rozmiar okna. Dane wydania pozostały, a akcja „Repozytorium projektu” otwiera domyślną przeglądarkę. Usunięto wzmianki o narzędziach użytych przy tworzeniu.
- Przycisk warstw ma wysokość sąsiednich przycisków. Kwadratowa część z ikoną ma osobny wąski obszar strzałki. Separatory mają symetryczne odstępy. Kontrolowana oferta aktualizacji przeszła sprawdzenie układu i kolorów. Kod aktualizatora i instalatora pozostał zgodny z 0.13.4.
- AltGr w opisie i polach WinForms jest przepuszczany do wpisywania. W 0.13.4 WPF przyjmował do skrótów każdy stan z Ctrl, a WinForms potrafił zatrzymać Ctrl+Alt+Z, zanim znak dotarł do edytora. W 0.13.5 skróty opisu dopuszczają Ctrl lub Ctrl+Shift, a obsługa historii WinForms przepuszcza kombinacje z Alt.
- Cztery biblioteki grafik korzystają ze wspólnej wyszukiwarki, która porównuje nazwy wyświetlane bez względu na wielkość liter i znaki diakrytyczne. Własna kolejność pochodzi bezpośrednio z biblioteki. „Najnowsze” i „Najstarsze” używają `LibraryOrder`, pola utrwalanego już przy zapisie wpisu. Starsze wpisy bez kolejności mają wartość 0 i rozstrzygnięcie po ID. Zamykanie okna czyści frazę. Sortowanie jest niezależne dla każdej biblioteki i pamiętane tylko w sesji. Zaznaczenie i operacje nadal używają ID.

## 2. Wagi i zgodność plików

Warianty koloru, gradientu i grafiki mają wagę 1–200. Kafelek pokazuje wagę oraz iloraz wagi i sumy wag. Edytor używa lokalnej historii pola. Rozkład terenu, mgły i ograniczenia widoczności powstaje podczas malowania albo jawnego przelosowania. Rysowanie podglądu nie zużywa generatora. Undo/Redo używa zapisanych rezultatów. Zmiana wyłącznie wagi zachowuje ID i rewizję powierzchni, więc nie rozdziela regionu wiadra. Zachowanie rozmieszczenia nie przelosowuje istniejących pól.

Mozaika przydziela miejsca proporcjonalnie do wag z rozdzielaniem reszty. Gdy miejsc wystarcza, każdy z maksymalnie 12 reprezentowanych wariantów otrzymuje co najmniej jedno. Układ ma deterministyczną kolejność.

Map12 i format presetu 5 zyskały opcjonalne dla czytnika pole `Weight`, domyślnie 1. Wagi terenu są w definicji puli w presecie bibliotecznym i w jego osadzonej kopii w mapie. Wagi mgły i ograniczenia widoczności są w ich dotychczasowych pulach mapy. Sprawdzono, że czytnik 0.13.4 odczytuje oba pliki próbne z nowym polem. Taka wersja używa wyboru równomiernego, a zapis ze starej wersji nie zachowuje nowych wag. Korzystanie z wag wymaga 0.13.5. Nie ma migracji Library ani zmiany formatu jej grafik.

## 3. Selektywna gumka

PPM na przycisku otwiera wielokrotny wybór bez tła. Domyślny zakres to Rysik. Inny zakres ma niebieski znacznik i własny tooltip. Heksy wybierane są według warunku okrągłego pędzla i z promieniem wynikającym z rozmiaru gumki. Środek instancji musi zmieścić się w obszarze gestu. Dodatkowo instancja musi być widoczna w więcej niż połowie według tego samego kryterium co lista obiektów, z tymi samymi flagami Widoku. Blokada ruchu nie blokuje usunięcia. Puste grupy są usuwane, a grupy z jednym członkiem zachowują ustawienia.

Teren wraca do „Niepomalowany”. Nazwa heksa i opis to oddzielne pozycje. Wyczyszczenie opisu obejmuje jego osadzone obrazy, które można przywrócić przez Undo gestu. Ikony czyści się zgodnie z dotychczasowym usuwaniem ikony, czyli razem z ID obrazu, ID wpisu i nazwą ikony. Odkrywanie przez gumkę zdejmuje mgłę, a wymazywanie widoczności usuwa jej bazowe ograniczenie. Obliczanie skutecznych pól widzenia i ochrona odkrywców podczas nakładania mgły pozostają bez zmian. Przy ukrytej warstwie działa dotychczasowy dialog i gest trzeba powtórzyć. Nazwa i opis są powiązane z warstwą Terenu na potrzeby tej kontroli.

Zmiany warstw są przygotowane do wspólnego zatwierdzenia. Jeśli gest wymaga geometrycznego wymazania rysunku, zatwierdzenie czeka na wynik obliczeń Rysika. Błąd i anulowanie odrzucają cały gest. Globalne Undo/Redo przywraca łącznie jego skutki. Wyczyszczenie opisu rozpoczyna nową lokalną historię edycji tego pola, aby miejscowe cofnięcie nie odwracało tylko części gestu.

## 4. Próby

| Próba celowana | Wynik |
| --- | --- |
| `Run-Patch0135Tests.ps1 -Case controls` | PASS. Systemowe tłumaczenie wszystkich 18 znaków w układzie Polski (programisty), routing Ctrl+Alt w WinForms i WPF, geometria dla monitora o ujemnej współrzędnej, wysokość Warstw, symetria separatorów przy minimum, wymuszony overflow, przyciski tła. |
| `Run-Patch0135Tests.ps1 -Case weights` | PASS. Waga domyślna, ważone losowanie, deterministyczny przydział mozaiki, zgodność ID powierzchni, lokalne Undo/Redo, zapis i odczyt Map12 oraz bibliotecznego presetu. Zmiana wag mgły i widoczności z zachowaniem pól. Pędzel terenu i oba wiadra zachowały wylosowany wariant po Undo/Redo bez pobierania kolejnych liczb losowych. |
| `Run-Patch0135Tests.ps1 -Case libraries` | PASS. Wyszukiwanie w obu kierunkach, sort po trwałej kolejności, niezależność kategorii, minimalna szerokość, usuwanie według ID pod filtrem i czyszczenie frazy przy ponownym otwarciu. |
| `Run-Patch0135Tests.ps1 -Case eraser` | PASS. Każda pozycja osobno, maskowanie przez Widok, zablokowany obiekt, grupa jednoosobowa i pusta, niezmieniona mgła przy samym usuwaniu instancji, wspólne Undo/Redo, anulowanie gestu i wymuszony błąd obliczeń Rysika. |
| `Run-Patch0134Tests.ps1 -Case flow` | PASS. Kontrolowana oferta i anulowane pobieranie. Ten sam rząd, niebieski styl instalacji i czerwone Pomiń/Anuluj. Próbny obraz ze schowka przeszedł dotychczasową ścieżkę zapisu tła. |

Powtórzono `controls` po zmianie odczytu Unicode w teście, `weights` po poprawieniu odczytu instancji mapy i uruchamiania gestu pędzla w runnerze, `libraries` po dodaniu rzeczywistych miniaturek w pozostałych kategoriach oraz `eraser` po dopasowaniu reguły zdejmowania mgły do istniejącego narzędzia.

### Próba zwykłego EXE z Portable

Na danych z `tests/_work` w 0.13.4 AltGr+A wpisał „ą”, a AltGr+E wyśrodkował akapit zamiast dodać „ę”. W 0.13.5 wpisano przez AltGr wszystkie małe i wielkie polskie litery na układzie Polski (programisty) oraz potwierdzono pełny ciąg w zapisanym opisie Map12. Zwykłe Ctrl+E oraz Ctrl+Z zadziałały. Sprawdzono też AltGr+S w wyszukiwarce ikon, filtrowanie do „Świątynia” i czyszczenie Esc. Obejrzano przycisk warstw, menu gumki z domyślnym Rysikiem i niebieskim znacznikiem po zmianie zakresu, ważoną mozaikę i pole wagi wariantu. Z powitania otwarto i zamknięto wyśrodkowane okno „O programie”.

Nie sprawdzano układu Polski (214), fizycznej kombinacji różnych skal DPI, wszystkich bibliotek ręcznie ani publicznej aktualizacji. Ukryta warstwa i pytanie o jej przywrócenie pozostają na liście odbioru interfejsu. Próby usuwania danych wykonano na kontrolowanych modelach z własnymi danymi. Nie uruchamiano pełnej kampanii testowej ani pomiaru mapy 60×60.

## 5. Checklista odbioru

1. Wpisz polskie litery w opisie i wyszukiwarce → tekst pojawia się bez przypadkowego formatowania i cofania.
2. Zmień wagę wariantu na 200, zapisz preset i otwórz go ponownie → waga, udział i mozaika wracają, a zachowane pola się nie przelosowują.
3. W czterech bibliotekach wyszukaj aktywny zasób i zmień sort → kryterium wpływa na widok, a operacja dotyczy właściwego wpisu.
4. Zaznacz Rysik oraz Ikony w menu Gumki i wykonaj gest na własnej mapie → jedno Undo odtwarza obie warstwy. Ukryj Ikony i ponów → program pyta o pokazanie warstwy przed operacją.
5. Otwórz „O programie” z powitania i edytora na różnych monitorach → okno pozostaje w ich obszarze roboczym i otwiera repozytorium przez przycisk.

## 6. Paczki i Git

Program `0.13.5`, FileVersion i ProductVersion `0.13.5.0`. Setup i Portable pochodzą z jednej kompilacji. Portable zawiera EXE, znacznik trybu, instrukcję i licencję. `fc /b` potwierdziło zgodność głównego EXE ze sprawdzoną kopią z ZIP. Paczki są w ignorowanym `tests/_work/patch-0135-final/packages`.

| Plik | SHA-256 |
| --- | --- |
| `HexMapEditor.exe` | `9FBFDAFE03160B2E8709D6C5F9A0FAF1E383C36EDC70495D23D3514068B412A1` |
| `HexMapEditor-0.13.5-Setup.exe` | `44243E3BB377AC9479710C47D506B451994E89AF2586195B7231AA1F912561B0` |
| `HexMapEditor-0.13.5-Portable.zip` | `F7FCE1E1B74FF16934B789FA7FB6F9CDE1411B770737BF80E91CAB9A87A5635A` |

Punktem wyjścia był czysty `master` wskazujący `06e12c1`, zgodny z lokalnym `origin/master`. README wskazuje oba załączniki pod dokładnym tagiem `v0.13.5`. Wydanie stanowi jeden lokalny commit. Publikacja paczek i tag należą do właściciela projektu.

## 7. Opis pre-release’u

**HexMapEditor 0.13.5 — selektywna gumka, wagi wariantów i wyszukiwanie grafik**

Gumka obsługuje wybrane warstwy z jednym krokiem cofania. Wagi wariantów pozwalają sterować częstością ich wyboru podczas malowania terenu, mgły i ograniczenia widoczności. Biblioteki grafik zyskały wyszukiwanie bez względu na polskie znaki oraz sortowanie. Wydanie poprawia wpisywanie znaków AltGr w opisie i układ kilku kontrolek. Mapy pozostają w formacie Map12.
