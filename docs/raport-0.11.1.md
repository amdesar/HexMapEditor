# HexMapEditor 0.11.1

## 1. Wykonanie

- Każda karta ma jedno tło i obrys. Nazwa zaczyna się od lewej, kończy wielokropkiem w razie potrzeby, a gwiazdka ma własną rezerwę miejsca przed akcjami. Ołówek i czerwony × są niezależnymi przyciskami z fokusem. Plus jest rysowany centralnie. Prawa strzałka stoi przy końcu pełnych kart, a oba dostępne kierunki mają niebieski kolor. Pod obiektami jest cienka linia z odstępem, bez osobnej sekcji „Zakładki”. Zachowano karty 207 px i pasek 36 px przy skali użytej do testu.
- Autozapis startuje przy pierwszej mapie. Wspólny termin przesuwa się po okresach 5 minut, niezależnie od otwierania kart, przełączania, ręcznego zapisu i zamknięcia najstarszej mapy. Zablokowane terminy łączą się w jeden cykl po ustąpieniu blokady. Kolejne dokumenty przechodzą przez istniejący zapis migawki z kontrolą ścieżki, rewizji i odcisku. Konflikt pozostawia gwiazdkę i wstrzymuje autozapis danej mapy. Podsumowanie liczy sukcesy i błędy całego cyklu. Pusty cykl nie pokazuje komunikatu.
- Zmieniona karta jest widoczna za pytaniem o zapis. Anulowanie pozostawia ją aktywną. Przy wyjściu zapis lub odrzucenie natychmiast zamyka rozpatrzoną kartę. Późniejsze anulowanie pozostawia jeszcze otwarte karty i wykonane zapisy. Stan roboczych presetów przeżywa zamknięcie dokumentu. Podgląd w lewym panelu pokazuje nazwę, tereny, ikony i obiekty, również przy pustym obszarze mapy. Wspólny draft ma jedno pytanie w danym wyjściu. Pominięte sekcje częściowego zapisu pozostają robocze po anulowaniu. Zamknięcie ostatniej karty przez × prowadzi do ekranu powitalnego po rozpatrzeniu zmian.
- Dopasuj mapę i Dopasuj wszystko wykorzystują całe płótno przy obecnym lewym panelu i paskach. Prawy panel wraz z uchwytem jest nakładką. Dopasuj wszystko nadal uwzględnia dotychczasowe elementy widocznych warstw.

## 2. Testy metod i kontrolek

Każdy przypadek uruchomiono jawnie przez `tests/Run-Patch0111Tests.ps1 -Case <przypadek>`.

| Przypadek | Wynik |
|---|---|
| `tabs-fit` | PASS. Jedna karta i przepełnienie, pełne karty i stany strzałek, rezerwa gwiazdki, kolejność akcji, pełny status i minimum 1180 px. Oba dopasowania dały identyczne parametry dla trzech stanów prawego panelu. Skala mapy 1,887305, położenie (105,7647; 267,4664). Skala całości 0,7486178, położenie (78,15302; 363,1161). |
| `autosave` | PASS. A/B/C otwarte w minutach 0/1/3, cykl w minucie 5, konflikt A, zapis B/C w tle, mapa bez ścieżki pominięta. Jeden bilans 2/1. Zamknięcie A pozostawiło termin 10, pusty cykl bez komunikatu. Wymuszona nowsza rewizja zachowała gwiazdkę i wcześniejszy plik. |
| `close` | PASS. Zmieniona nieaktywna A przed pytaniem, Anuluj z A na wierzchu. Wyjście z odrzuceniem albo zapisem A i anulowaniem na B. Błąd zapisu na ścieżkę innej karty. Wspólny preset sprawdzony raz. Częściowy zapis ikon w rzeczywistym oknie zapisu i anulowanie przy kolejnym presecie zachowały niezamknięte drafty i pozwoliły ponownie otworzyć mapę bez wskrzeszania kart. |

Ponowiono `close` po dodaniu rzeczywistego zapisu częściowego odłączonego presetu, a `autosave` po dodaniu próby nieaktualnej migawki. Standardowe polecenie edycji było blokowane podczas zapisu. Zmieniono samą próbę, aby zasymulować nową rewizję w złączu testowym przed autoryzacją publikacji.

## 3. Zwykły EXE i ograniczenia

Sprawdzona kopia w `tests/_work` pracowała na sześciu lekkich mapach i osobnej bibliotece. Wizualnie sprawdzono jedną i kilka kart, wielokropek z gwiazdką na aktywnej i nieaktywnej karcie, trzy położenia przewijania, prawą strzałkę, stały plus z dwiema akcjami, status i linię pod obiektami. Tab i spacja uruchomiły ołówek oraz zamknięcie zapisanej karty. Mysz otworzyła pytanie dla zmienionej karty w tle. Za dialogiem widoczna była przywrócona mapa A z właściwym heksem. Anulowanie zostawiło A aktywną. Przed pytaniem o roboczy preset pokazano jego elementy w lewym panelu. Anulowanie przywróciło normalny panel z zachowanym draftem.

W zwykłym EXE panel pozostawał widoczny podczas dopasowania. Dopasuj wszystko pokazało 100% i to samo położenie przy uchwycie i rozwiniętym panelu. Pełne porównanie obu poleceń w trzech stanach oraz minimalna szerokość pochodzą z testu kontrolek. Harmonogram sprawdzono zegarem kontrolowanym. Próba nie obejmowała czekania na rzeczywiste pięć minut, dwukliku w Eksploratorze ani macierzy DPI. Odłożone artefakty prawego panelu pozostają poza zakresem.

## 4. Checklista odbiorcza

- Zmień mapę o długiej nazwie → wielokropek i osobna gwiazdka przed ołówkiem i ×.
- Przewiń pełny rząd → prawa strzałka przy jego końcu, kolor niebieski w dostępnych kierunkach.
- Zamknij zmienioną kartę w tle, anuluj → właściwa mapa widoczna za pytaniem i aktywna po anulowaniu.
- Zapisz A i anuluj wyjście na B → A znika, B pozostaje aktywna i zmieniona.
- Zmień kilka zapisanych map → wspólny cykl próbuje zapisać je po kolei i pokazuje jeden wynik.
- Dopasuj widok z otwartym, zwiniętym i zamkniętym prawym panelem → identyczna kamera.

## 5. Build i Git

Wersja aplikacji 0.11.1, FileVersion/ProductVersion 0.11.1.0. Map12, preset 5 oraz schematy bibliotek zachowane. Główny plik jest identyczny ze sprawdzoną kopią według `fc.exe /b`.

SHA-256 `C03629CF282023822356524395F0AD3CE1800A37EBC22D299C978029433F75DC`.

Baza `b441ab1`, początkowo bez zmian lokalnych. Raport należy do jednego lokalnego commita wydania. Dane prób i kopia EXE pozostały w ignorowanym `tests/_work`. Nie wykonano push, tagu ani publikacji wydania.
