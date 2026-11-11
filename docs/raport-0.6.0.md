# HexMapEditor 0.6.0

Wykonano zakres funkcjonalny na bazie ukończonej0.5.4, bez rozpoczynania poprzednich etapów od nowa. Baza `1d6cc6a`; commit implementacji **`3e3fb2c`**, uzupełnienie podglądu grubości **`90f61ed`**. Źródła pozostały w src, Library obok EXE. Bez nowych zależności/usług, migracji prywatnych danych, sprzątania testów, push, tagu i Release'a.

Główny `HexMapEditor.exe`: aplikacja0.6.0, FileVersion **0.6.0.0**, SHA-256 **6E08AF16D789C9E4CD7EADF5470A596C3F0B2A119F5E55220FDD73AC218559EB**. Własna zwykła kopia w `tests/_work/patch-060-fixture-153d28f2917c4b158947eed60d07eef8/HexMapEditor.exe` ma identyczne bajty. Kompilacja zakończyła się sukcesem, po wznowieniu nie powtarzano jej bez zmiany kodu.

## Poprawki odbiorcze

- Dokładne Ctrl+Z/Ctrl+Y; Ctrl+Shift+Z konsumowane bez Undo/Redo również w roboczej puli i natywnych polach/dialogach WinForms. Zachowano dokładny routing WPF i granice historii tekstu. Usunięto obietnicę ponawiania tą kombinacją z bieżącej dokumentacji/tooltipu.
- Krzyż wariantu rysowany z osią `(rozmiar klienta−1)/2`, bez przesunięcia wynikającego z całkowitoliczbowej połowy; czerwony/kontrastowy, ostatnia pozycja nadal chroniona.
- Mozaika bez obrysów miniheksów. Rozdział slotów modulo liczba reprezentowanych pozycji i stabilne tasowanie daje różnicę liczebności najwyżej1. Identyczne grafiki w różnych pozycjach mają osobne udziały, bez wyrównywania losowania mapy. Dwa rzędy i pojemność12 zachowane, bez Random malowania.
- Źródłem drobnego przeskoku tła były dwie geometrie: cover liczony z zaokrąglonego rozmiaru bufora i prezentacja zależna od jego proporcji. Cover liczymy z rzeczywistego viewportu, a stały prostokąt prezentacji z tych samych proporcji. Pozostała optymalizacja160px podczas gestu i dokładniejszy wynik po nim. Nie deklarujemy oceny płynności ze zrzutów.
- Bezpośrednia **Biblioteka grafik** obok Dodaj teren: działa na Domyślnym, ciemny tryb zarządzania tego samego selektora nie przypisuje grafiki do terenu. Import/edycja/usuwanie nadal używają dotychczasowej historii plików. Kontekst wyboru z wariantu pozostał.
- Przybornik ma trzeci rząd. Minimalna wysokość okna748 zamiast720 zapewnia dodatkowe28px; nie zwężono treści. W próbie zauważono ucięty podpis ustawienia wskaźnika; przyciski wyrównano do dostępnej szerokości i skrócono podpis do Wskaźnik z pełną nazwą dostępności/podpowiedzią.

## Rysik, gumka i wskaźnik

Rysunki to polilinie świata: ID, RGB, grubość i punkty. Obrys pod kursorem pokazuje aktualną grubość/średnicę przeliczoną przez zoom, bez zmiany dokumentu. Nie ma bitmapy całego świata, przyklejenia do heksa lub statycznego tła. Pan/zoom skaluje rysunek; resize/orientacja nie przypisuje punktów do nowych heksów. Kropka jest dozwolona. Otrzymane próbki są łączone segmentami z okrągłymi łączeniami. Wyjście poza płótno przerywa fragment; utrata capture/fokusu kończy gest bez łącznika przez nieznaną drogę.

Gumka wycina części segmentów kapsułą między próbkami, uwzględniając grubość kreski. Pozostałe fragmenty stanowią rzeczywistą geometrię zapisu/renderowania/Fit, a nie farbę tła. Nietknięte fragmenty i poprzednie wersje są współdzielone. Jeden gest rysika/gumki ma jedną migawkę przed/po we wspólnej historii300. PPM gumki czyści warstwę jednym wpisem bez pytania; pusta warstwa niczego nie dopisuje. Undo/Redo odtwarza dokładne fragmenty i ID, nie wykonuje ponownie algorytmu gumki. Obwiednie są buforowane, a rysowanie ograniczone do przecinających clip fragmentów.

| Ustawienie/budżet | Wartość |
| --- | --- |
| Grubość rysika w świecie | 0,5–40, domyślnie3 |
| Średnica gumki w świecie | 1–160, domyślnie28; obrys na płótnie |
| Fragmenty dokumentu | do5000 |
| Punkty dokumentu / fragmentu | do200000 /10000 |
| Współrzędne świata | skończone, ±10000000 |
| Punkty zachowane w historii rysika | do2 mln różnych współdzielonych punktów |
| Wspólna historia | najwyżej300 wpisów |

Przekroczenie limitu odrzuca gest w całości i pokazuje komunikat w statusie. Budżet historii nie wymaga kopiowania obrazów; zapis i świadome ponowne otwarcie zeruje sesyjną historię. Dotychczasowe budżety grafik i pliku mapy nie zostały zwiększone.

PPM Rysika to niezależny chwilowy wskaźnik: osobny kolor, kółko18 logicznych px ze stałym rozmiarem ekranowym, kontrastowy brzeg, smuga4px do1000ms, najwyżej128 próbek. Timer33ms działa tylko przy aktywnym wskaźniku lub niewygasłej smudze i invaliduje jej obszar płótna. Po zakończeniu wygasania zatrzymuje się. Brak dirty/historii/zapisu/Fit i zmian masek. Kółko/środkowy przycisk zachowują kamerę. Warstwa Rysik ukrywa także wskaźnik.

## Bazowa ciemność i dynamiczne światło

Nowa mapa jest jasna. CellData.Darkness/DarkFill zachowują bazową maskę i wylosowany wygląd niezależnie od Fog. DarkPalette dokumentu używa wspólnego edytora1–32 pozycji kolor/grafika, obu kadrów, pełnych źródeł i lokalnej historii100. Domyślna czerń #000000. PPM przycisku narzędzia otwiera Wygląd ciemności. Aktualizacja istniejących bazowo ciemnych pól jest jawna, domyślnie wyłączona; zachowanie rozmieszczenia domyślnie włączone, po stabilnych ID. Nie zmienia zakresu maski. Usunięte warianty zachowują historyczny wygląd pól.

Malowanie/wiadro wykorzystują gesty mgły, wspólne odciski i regiony. Tryb nakładania/usuwania ustala pierwsze pole całego gestu, a już odwiedzone pola nie są ponownie przełączane/losowane. Wiadro zamraża TerrainId, zastosowaną wersję i bazowy Darkness; spójność sześciu sąsiadów, bez zależności od chwilowego światła lub wariantu wyglądu. Jeden gest ma jeden zapisany wynik Undo/Redo.

Instancja i wzorzec przechowują LightEnabled/LightRadius. Domyślnie wyłączone, promień1; zakres0–20. Wzorzec określa początek nowych instancji, a aktualizacja grafiki nie nadpisuje istniejących ustawień światła. Grupa ma niezależne opcjonalne nadpisania włączenia i promienia; Indywidualnie zachowuje bazę. Źródło jest na każdym członku, bez dodatkowej lampy środka grupy. Odłączanie pozwala zachować/przywrócić również światło i promień (siedem kategorii).

Źródło działa dla każdego stanu, bez powiązania z odkrywaniem mgły. Jeden heks rozszerzonej geometrii pod środkiem, metryka axial:0 własny,1 pierwszy pierścień. Także źródło poza mapą może sięgać do istniejących pól; nie tworzy nowych heksów ani ciemności poza mapą. Ukrycie Obiektów i Fog nie wyłącza lamp.

Efektywna ciemność = baza pomniejszona o sumę aktualnych zasięgów. Nie kasujemy bazy ani jej wyglądu, nie losujemy po odejściu światła i nie odtwarzamy dawnych snapshotów na nowszą edycję. Ręczne usunięcie/przemalowanie bazy podczas oświetlenia pozostaje. Kilka źródeł sumuje się. Światło nie odkrywa Fog, trwałe odkrywanie nie włącza światła. Cache wyliczany po ruchu/ustawieniach/dodaniu/usunięciu/geometrii/historii/odczycie, bez pętli co klatkę i bez osobnego kroku historii wyniku.

## Maski, lista i dopasowanie

Kolejność: Tło → Tereny → Ikony → Obiekty → Ciemność → Mgła → Rysik. Alpha grafik obu masek działa wizualnie, lecz nie ujawnia logicznie danych. Zsumowane heksy aktywnego Fog lub efektywnej Ciemności trafiają do wspólnej geometrii przecięcia prostokąta obiektu; brak podwójnego naliczania nakładania. Dane listy/szczegółów są maskowane przy widoczności<=50%, niezależnie od alpha. Hit-test ukrytych obszarów i clipping ramek/zaznaczeń uwzględniają obie maski. Szczegóły heksa blokowane pod aktywną maską; grupa całkowicie maskowanych członków nie ujawnia właściwości.

Lista ma Światło (efektywne włączenie/promień lub ???) oraz Oświetlenie (jasny/częściowo/w ciemności, procent ciemnej powierzchni). Jasne pole nigdy niepomalowane ciemnością jest oświetlone bez wymagania lampy. Kolumny procentowe opisują rzeczywiste maski, a ich flags Widoku wyłączają odpowiednie maskowanie danych. Fog i Ciemność można oglądać niezależnie w czterech kombinacjach. Flagi nie tworzą dirty/historii ani zmiany w zapisie; udane Nowa/Otwórz przywraca widoczność. Ochrona edycji ukrytej warstwy pyta o pokazanie i wymaga ponowienia gestu.

Dopasuj mapę nadal obejmuje samą geometrię. Dopasuj wszystko dodaje efektywne prostokąty widocznych Obiektów i pozostałe fragmenty widocznego Rysika wraz z grubością. Nie dodaje ukrytych warstw, wymazanych fragmentów, wskaźnika/smugi ani tła. Zmiana flag sama nie uruchamia dopasowania. Prawa nakładka nadal celowo pomijana, Otwórz nadal Dopasuj mapę.

## Format i zasoby

**Mapa9**: nowe Ink, DarkPalette, Darkness/DarkFill i ustawienia świateł instancji/grup uzasadniają jawny nowy format. Wymagane pola/liczby/referencje i limity są walidowane przed podmianą dokumentu. Zachowano bezpieczny zapis. Bez migracji/konwerterów lub kasowania map8. Nie zapisujemy chwilowych flag, wskaźnika ani autorytatywnego cache światła; odczyt nie wykonuje ponownie odkrywania Fog.

Źródła i kadry ciemności są zbierane razem z terenem/mgłą/tłem do istniejących tabel deduplikacji. Plik zawiera potrzebne palety i historyczne wyglądy, nie całą bibliotekę. Kreski nie mają bitmapy per punkt/heks. Presety3, grafiki2, biblioteczne ikony2 i wzorce obiektów1 zachowują numery. Stary wzorzec bez nowych pól ma bezpiecznie wyłączone światło i promień1; nie przepisujemy bibliotek przy starcie. LibraryFiles i ochrona konfliktów pozostały dotychczasowe.

## Weryfikacja metod i kontrolek

Bez Run-AllTests, dawnych patch-runnerów, audytu WPF/DPI i benchmarków. Własne dane wyłącznie tests/_work. Poniższe jawne Case uruchomiono do uzyskania poprawnego wyniku. Po wznowieniu nie powtarzano zaliczonych; nowy ink-preview dodano wyłącznie dla uzupełnionego obrysu:

| Case | Faktyczne sprawdzenia | Wynik |
| --- | --- | --- |
| acceptance | zbalansowane stabilne pozycje mozaiki, niezależny Random mapy, Ctrl+Shift+Z puli/głównego handlera, ciemny tryb zarządzania, geometria lekkiego/dokładnego cover | PASS |
| ink | rozcięcie kapsułą, nietknięte snapshoty, gest/Undo/Redo, wyczyść/no-op, współrzędne świata | PASS |
| pointer |128 próbek, kółko po wygaśnięciu starej smugi podczas przytrzymania, timer po puszczeniu, brak zmian modelu/dirty/stosu | PASS |
| darkness | baza i granice wiadra/Redo, promień0/1, suma źródeł, ręczna baza podczas światła, nakładka grupy bez utraty bazy, źródło poza mapą w obu orientacjach | PASS |
| visibility | geometryczna suma masek, niezależne flags/dirty, brak wycieku kolumny źródła, Fit po flags, round-trip mapy9 z kreską/światłem/pełnym źródłem ciemności | PASS |
| gestures | ruch lampy jeden Undo/Redo i cache, brak zmian bazy, Random odwiedzonego pola, ochrona Fog, maskowany panel grupy, cache obwiedni rysika, koniec timera i wybrane limity | PASS |
| ink-preview | hover, światowa grubość przeliczona na ekran, brak zmiany modelu/historii, brak obrysu przy ukrytej warstwie | PASS |

Case acceptance początkowo nie skompilował helpera przez błędną nazwę typu selektora, a potem wywołał helper z błędną liczbą argumentów. Poprawiono tylko test i uzyskano PASS; nie były to pominięte niepowodzenia produktu. Osłona runnera bez Case raz odrzuciła wywołanie bez uruchomienia prób. `fixture` jedynie utworzył własną mapę wejściową; nie liczymy go jako osobnej próby okna. Logi w `tests/_work/patch-060-CASE-GUID/results.log` pozostają ignorowane.

## Rzeczywisty zwykły EXE

Własna mapa: `tests/_work/patch-060-fixture-153d28f2917c4b158947eed60d07eef8/Próba 0.6.0.hexmap`, czysta lokalna biblioteka tej kopii. Nie użyto prywatnej Library/map, nie zamknięto okien użytkownika.

- Otwarto bibliotekę grafik na Domyślnym: ciemne okno zarządzania, bez przypisania lub dirty.
- Przeciągnięto lampę Rączką: nowe otoczenie jasne, dawne ciemne, Ctrl+Z przywrócił wynik. Nie otwierał się panel właściwości.
- Przełączono kolejno cztery kombinacje Fog/Ciemność. Menu pozostało otwarte, maski niezależne; flaga nie dawała gwiazdki.
- Rysikiem narysowano żółtą kreskę nad maskami. Po zgodzie użytkownika gumka rozcięła tylko jej środek, pozostawiając dwa boki i odsłaniając rzeczywistą warstwę; natychmiastowe Ctrl+Z przywróciło kreskę.
- Po wyłącznie kosmetycznej korekcie szerokości/podpisu ustawień uruchomiono kopię o SHA `366A89AA311CC7A23AE0993A252B23F0433BF500AD571EAA923FA743150BEA7B`: ponownie otwarto zapisaną mapę z kreską i lampą, potwierdzono nowe napisy. Gestów gumki nie ponawiano z powodu zmiany samej etykiety. Pierwszy scenariusz miał SHA `34A5D51F2E476F4900EFA16FEBF1A2A8937DB20E4FE59D722FCA6194C4F7A47A`, identyczny z głównym artefaktem w chwili tamtej próby; logika gestów nie zmieniła się potem.
- W tej kopii klik PPM Rysika pozostawił krótki zanikający kolorowy punkt bez dirty. Ciągłego przytrzymania i smugi nie oceniono wizualnie: API drag nie udostępnia wyboru prawego przycisku. Model/timer sprawdzono w Case pointer; nie przedstawiamy tego jako fizycznego gestu PPM.
- Dokończono przerwaną próbę Dopasuj wszystko: kreska poza mapą rozszerza kadr; po wyłączeniu Rysika flaga sama nie porusza kamery, kolejne dopasowanie pomija kreskę. Przeniesiono lampę poza mapę, dopasowanie objęło cały prostokąt grafiki; po wyłączeniu Obiektów kolejne dopasowanie wróciło do kadru siatki. Źródło nadal oświetlało G5 mimo ukrytej grafiki. Polecenia wybrano klawiaturą End/Enter po otwarciu menu; sam wcześniejszy klik zamykający menu nie został uznany za zaliczone dopasowanie.
- Po uzupełnieniu wyłącznie obrysu średnicy uruchomiono kopię o finalnym SHA z nagłówka: wybrano Rysik, wpisano grubość20, przesunięto kursor na płótno środkowym kliknięciem bez panowania. Widoczny czarno-biały obrys odpowiadał średnicy około29px przy zoom146%; brak dirty. Case ink-preview potwierdził skalowanie i brak edycji danych. Nowa próba nie ponawiała gestów gumki, światła i dopasowania, których logika nie uległa zmianie.
- Zapisano własną mapę i zamknięto własne okno. Sprawdzono listę okien po zamknięciu: brak procesu/okna testowej kopii. Brak oczekującej zgody/dialogu.

## Ograniczenia i niewykonane próby

Nie wykonano pełnej macierzy kształtów, promieni, plików/limitów i operacji biblioteki. Nie powtarzano zaakceptowanych WPF, regionów historycznych terenów, geometrii, DPI i starych historii plików. Nie oceniano klatka po klatce płynności tła, długiego rysowania ani wskaźnika; zrzut po akcji nie jest pomiarem FPS. Nie wykonano rzeczywistego środkowego drag przy aktywnym wskaźniku, ani UI wszystkich pól światła wzorca/grupy. Alpha ciemności ma wspólną ścieżkę renderowania i zapis źródła sprawdzony metodą, ale brak osobnej interaktywnej kampanii wszystkich tekstur alpha. Chroniony dokument i preset zachowano w kodzie, bez ponownego szerokiego audytu0.5.2. Przenośność do czystej kopii nadal świadomie odłożona; wykonany własny round-trip nie jest potwierdzeniem użytkownika ani pełną kampanią przenośności.

Zakres funkcji0.6.0 wykonany. Zgodnie ze zleceniem nie dodano zakładek, innych warstw, skażenia, high/low detail, widoku gracza ani dodatkowych systemów. Brak deklaracji kompleksowego przetestowania.

## Checklista odbiorcza

1. Ctrl+Z/Y w puli i tekście; Ctrl+Shift+Z nic nie robi. × wyśrodkowany, mozaika bez obrysów i z równym udziałem pozycji. Biblioteka grafik dostępna na Domyślnym bez przypisywania.
2. Rysik poza siatką, pan/zoom i kropka; gumka wycina część, jedno Undo/Redo; PPM gumki czyści i daje jedno Undo.
3. PPM Rysika: kółko stałe ekranowo, kolor, krótka smuga wygasająca po puszczeniu; bez gwiazdki/zapisu; kółko i środkowy przycisk nadal sterują kamerą.
4. Ciemność: odcisk i wiadro, kolor/grafika/pula, zachowanie rozmieszczenia, dokładne Redo. Po odejściu lampy wraca aktualna baza/wygląd; ręczna zmiana pod światłem pozostaje.
5. Światło0/1 i kilka źródeł, niezależność od stanu/Fog, ustawienia wzorca oraz grupy Indywidualnie/nadpisanie. Undo ruchu zachowuje dawną odkrytą mgłę.
6. Cztery kombinacje Widoku Fog/Ciemność, maskowanie listy/szczegółów i obrysów na granicy obu; alpha nie ujawnia danych. Ukrycie Obiektów nie wyłącza lampy.
7. Dopasuj wszystko uwzględnia tylko widoczne Obiekty/Rysik i pozostałe fragmenty; Tło/wskaźnik pomijane; sama zmiana flag nie przesuwa kamery.
8. Krótki zapis/odczyt mapy9 z kreskami/ciemnością/światłem i źródłem graficznym. Dawnych map nie kasować jako rozwiązania braku konwersji. Przenośność do czystej kopii pozostaje odłożona.

## Git i uruchomienie

Uruchom główny `HexMapEditor.exe` albo `Uruchom HexMapEditor.bat`. Kod/testy/EXE: **`3e3fb2c`** i **`90f61ed`**; raport, plan i README/specyfikacja zatwierdzane osobnym lokalnym commitem dokumentacji. Końcowy identyfikator dokumentacji podaje odpowiedź końcowa i `git log -1`. Stan po zatwierdzeniu dokumentacji: czysty master, bez plików Library i artefaktów tests/_work w commicie. Zachowano wcześniejszą historię0.5.4. Brak push/tagu/Release'a.
