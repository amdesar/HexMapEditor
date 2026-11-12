# Plan HexMapEditor 0.6.4

## Baza i granice

HEAD potwierdzony: `0cf9c24`, kod 0.6.3 `1955be9`. Początkowy Git czysty. Brak pliku AGENTS.md w repozytorium. Odczytano raport 0.6.3 i sekcję 38 specyfikacji. Bez migracji, porządków, dawnych kampanii i backlogów. Dane prób wyłącznie tests/_work. Nie zamykać okien użytkownika.

## Etapy

1. Osobno zmierzyć na stałej próbce: duże kreski i zoom, gradient rysika/wskaźnika, faktyczny gest gumki. Zachować pomiary bazowego 0.6.3 przed zmianami funkcjonalnymi. Sprawdzić ograniczenie viewportu, liczbę kafli/przebudów, ścieżki i boolean.
2. Lokalnie naprawić wykazane koszty. Późniejszy fragment gradientowej kreski przykrywa wcześniejszy, bez zmiany fazy/cyklu i powierzchni gumki. Sprawdzić okręgi i szwy, deterministyczność, historię i krótki round-trip. Preferowany format map11/preset4 bez zmiany danych autorytatywnych.
3. Picker: pionowe RGB, bezpośrednie Jednolity/Gradient, Losuj wyłącznie robocze kolory istniejących punktów. Krótki pomiar cold/warm otwarcia własnej biblioteki i tylko uzasadniona lokalna poprawka.
4. Wersja 0.6.4 dopiero po ukończeniu kodu, build finalnego EXE, identyczna kopia i reprezentatywna próba zwykłego okna. Raport, README/specyfikacja, checklista i lokalne commity.

## Decyzje i ustalenia początkowe

Obecny renderer gradientu ma 32 kafle na wszystkie kreski, a każdy Paint odwiedza pełny prostokąt przecięcia obwiedni z Graphics.VisibleClipBounds. Każdy kafel rysuje ponownie całą ścieżkę. Nie traktować tych obserwacji jako pomiaru. Obecne kolorowanie wybiera najmniejszą odległość osi; wymaganie 0.6.4 zmienia je na kolejność malowania. Gumka teraz odejmuje i serializuje wynik dla każdej próbki. Najpierw pomiary.

## Punkt wznowienia

Wykonane: odczyt bazy i odpowiednich źródeł. Pozostały wszystkie etapy implementacji i prób. Zaliczone próby 0.6.4: brak. Ostatnia niepewna operacja: brak. Własnych procesów/dialogów nie uruchomiono; przed UI sprawdzić rzeczywisty stan. Główny EXE nadal ukończone 0.6.3.

## Etap 1 — pomiary i pierwsze lokalne zmiany

Bazowy profil 0.6.3 `patch-064-profile-0199d150561e4f6dbc2f18006a1f6fbc`: viewport 1000×640, 6×160 punktów, grubość 80 świata, zoom .25/.5/1/1.46/2. Jednolity zoom 6,91 ms, gradient 234,06 ms; gradient samoprzecięcia 224,98 ms, droga bez przecięć 55,87 ms. W samoprzecięciach 52 kafle przy limicie 32 — ponowne rasteryzacje i arbitraż najmniejszej odległości piksel po pikselu. Gumka 70 próbek na 10×180 punktów (mieszane kolory) 518,69 ms, 9643 punkty pozostałości. Kometa 20 renderów: jednolita 33,57 ms, gradient 31,20 ms; w tej próbie nie odtworzono osobnej regresji wskaźnika. To pomiary rendererów, nie FPS okna.

Wprowadzono natywne liniowe brushe RGB z cyklem 80/20, w zapisanej kolejności odcinków; bez wybierania najbliższej osi i per-pixel samplera. Jeden roboczy kafel 256×256 w dokładnej skali urządzenia; brak LRU kafli per kreska i thrash. Cache całej ukończonej warstwy w bieżącym widoku do 5,8 mln pikseli (~22,2 MiB), aktywna kreska osobno; duży viewport działa bez cache. Nie zwiększono dawnego 24 MiB budżetu rastra. Kontury gumki nadal autorytatywne.

Gumka zbiera wszystkie kapsuły gestu, podgląd wyklucza ich geometryczną sumę, finalny boolean/serializacja raz na przeciętą kreskę. Bez usuwania zakrętów, max 10000 próbek gestu. Podgląd krawędzi klipu może różnić się AA do jednego piksela; dokładny wynik na zakończenie. Pierwszy pomiar po zmianach `5d5293ae65004cd0bc39a643ad21663a`: gradient zoom 98,49 ms; samoprzecięcia 85,48 ms; gumka 179,85 ms / 9831 punktów (inna segmentacja konturów, powierzchnia do porównania w etapie obrazu). Ten profil jest wstępny — próby obrazu jeszcze niezaliczone.

Picker: kod pionowego RGB, dwa radio-przełączniki i Losuj własnym RNG. Nie przetestowano jeszcze. Biblioteka: baseline bez rysunków `patch-064-library-e25ea1706d5e4ffa852fac8a8299cb65`: 20 wpisów / wspólne źródło 2048×1024, ctor+Show cold 512,22 ms, warm 485,57 ms. Każda pozycja ponownie dekodowała źródło i tworzyła wynik 1024×1024. Zastąpiono to bezpośrednią małą miniaturą i LRU 32 miniatur (około 0,78 MiB, bez źródeł). Po zmianie `0ad6c6f2e2e64e8bad73f0b724acf39d` cold 73,54 ms / warm 32,92 ms. Układ/resize bez zmian. Case library PASS; nie ponawiać bez zmiany tej ścieżki.

Pozostałe: testy obrazu/semantyki, picker, dalsza kontrola produkcyjnego cache i podglądu gumki, ewentualne lokalne korekty, finalny pomiar, wersja/build, zwykły identyczny EXE, raport/docs/commit. Własnych zwykłych okien nie uruchomiono; formularze testowe zakończone. Ostatnia niepewna operacja: brak. Główny EXE nadal 0.6.3.

## Etap 2 — obraz, historia i picker

Natywny renderer koloruje kapsuły w kolejności drogi (późniejszy fragment przykrywa), a AA pochodzi raz z końcowej powierzchni. Cykliczny brush GDI+ zachowuje paletę 80/20 i zapisane Distance/CycleWorld. Raster pracuje w dokładnej skali urządzenia, bez obniżania jakości gotowej kreski; jeden scratch, bez budowy/wyrzucania kafli LRU. Bufor ukończonej warstwy ma stały viewport, nie obszar chwilowej invalidacji. Stare ukończone kreski nie są przebudowywane przy dopisywaniu nowej ani podglądzie gumki.

Weryfikacja obrazu wykryła obcięcie pojedynczego piksela AA na obwiedni gradientu; naprawiono floor/ceil + 1 px margines. Końcowy `image` PASS `patch-064-image-c65a5c26f0364f12909fa9cdd9309540`: późniejsza pionowa część cyfry4, rebuild/cache eviction, niezmienne kolory poza otworem, prawdziwy otwór, map11/read/history/Fit, wykluczenie podglądu gumki, brak przebudowy cache na dirty region. Różnice batch/sequential tylko 250 pikseli krawędzi AA, żadnego o błędzie kanału >32. Porównanie kształtu kół 8/64 próbek przy .5/1.46/2 zgodne jednolity/gradient; rzadkie wejście pozostaje wielokątem, nie wygładzano świadomych narożników. Raster 146% maksymalny błąd kanału 8 wobec analitycznego samplera, bez szwów.

`picker` PASS `patch-064-picker-b194cbc6eb824bbca6f4cf0b8bef1013`: pionowe RGB/HEX/HSV i niepoprawny HEX, dwa wykluczające przełączniki, Losuj własnym RNG przy zachowaniu pozycji/kąta/liczby, brak losowania podczas redraw, Anuluj i no-op. Stany/ramki nadal wyłącznie jednolite. Nie zmieniono cyklu/Tęczy/kątów heksów.

Przy dopisywaniu rysika pełna walidacja dokumentu raz na początek/koniec, a w ruchu tylko aktywna kreska + łączny budżet. Niezmienne dane starych kresek nie są ponownie przeglądane przy każdym punkcie; odczyt nadal pełny. Timer wskaźnika zatrzymuje się też po wygaśnięciu smugi stojącej aktywnej głowy; ruch go wznowi, bez zmiany fazy i koloru.

Pozostałe: końcowy wynik profilu (polecenie właśnie uruchomione — odczytać wynik przed ewentualnym ponowieniem), fixture, wersja/build, finalny identyczny EXE/UI, raport/docs/Git. Własnych zwykłych okien brak; metody formularzy zakończone. Bez ponownego library/picker/image, jeśli kod tych ścieżek nie ulegnie zmianie.

## Etap 3 — gotowy kod i build do próby UI

Końcowy profil `patch-064-profile-66ed04512df44f039f28142930c74f6f` PASS. Te same wejścia: gradient zoom 100,39 ms, warm3 3,21 ms, append12 59,25 ms, bez przecięć 27,90 ms / samoprzecięcia 89,07 ms; gumka 175,35 ms. Solid zoom 19,61 ms (narzut kompozycji vs baseline 6,91 ms), warm3 3,47 ms; nie ukrywać tego kosztu w raporcie. Kometa 20 renderów 33,57/31,08 ms — brak osobnej reprodukcji gradientowego kosztu w tej próbie. Natywny renderer nie zwiększa pamięci rastra; licznik tiles oznacza teraz przejścia przez ten sam scratch, nie alokacje LRU.

Dodano szybkie pominięcie cache pustej warstwy. Końcowy image PASS `patch-064-image-64440510723748c3be1e62c85c9eb74d`: jak poprzednio oraz pusty compositor12 0,20 ms, brak zachowanego bufora pustej warstwy. Picker i biblioteka bez zmiany, nie ponawiano. Fixture początkowo nie utworzył katalogu grafik — błąd przygotowania, plik mapy pozostał wyłącznie w starym _work; generator naprawiony. Udany fixture `patch-064-fixture-4cbe2dd88a48477c804b9c7dbf2238ac`, mapa i20własnych grafik. Nie sprzątano i nie ponawiano częściowej operacji w tym samym katalogu.

Aktualizacja oznaczenia w jednym pliku natrafiła na błąd kodowania Pythona; stan ustalono, poprawiono jawnie UTF-8, ponowiono tylko build po brakującej zmianie etykiety. App0.6.4, FileVersion0.6.4.0. Główny EXE i kopia mają SHA256 A240BA0701D859135BA525D557ACC19C8AF89690565BA25AD269E67AFC4BA53D. Poprzedni build z FileVersion0.6.4/etykietą0.6.3 nie był użyty do UI ani uznany za finalny.

Inventory nie pokazał okna HexMapEditor. Wysłano launch własnej kopii; ustalić zwrócone okno z wyniku narzędzia przed wejściem. UI jeszcze niezaliczone; wymagana zgoda na gumkę w chwili akcji. API drag obsługuje tylko prosty odcinek LPM, więc cyfra4 jednym wieloodcinkowym gestem i ciągłe ręczne kółko są potwierdzone metodą/renderem, nie będą udawane jako ręczne gesty. Pozostałe: zwykły EXE/UI, dokumentacja/raport/commit/status. Ostatnia operacja build/copy zakończona, hash potwierdzony; nie powtarzać.

## Korekty ujawnione zwykłym oknem

Pierwszy kandydat UI pokazał delikatne dyski/łączenia gęsto próbkowanej kreski. Zamiast pełnych końców każdej kapsuły kolorowej: płaskie korpusy, okrągła zewnętrzna część łączenia z odtworzeniem poprzedniego korpusu i nowy korpus na wierzchu. Końce całej kreski pozostają okrągłe. Image powtórzony wyłącznie dla tej zmiany: PASS `1eb4e3acaf8b4fa2bc6d7b8b450de57e`; batch/sequential246pikseli AA, żadnego >32, nadal146% błąd8, wszystkie pozostałe asercje obrazu/historii/Fit/cache przechodzą. Ostateczny profil po łączeniach `29868737b5d94516999ae58297d59f67`: zoom gradient115,37ms vs234,06, samoprzecięcia93,23vs224,98, warm3 3,29vs22,96, append12 65,09vs129,26; gumka178,44vs518,69. Jednolity zoom21,71vs6,91 (koszt bufora), warm3 3,76vs1,47, append14,50vs6,67; to jawny kompromis cache, nie ogólne przyspieszenie każdego jednolitego rysunku. Pusta warstwa2,81vs2,70ms; cachepusty pomijany.

Kandydat AC9C... otworzył mapę, picker RGB/tryby/Tęcza/Losuj, zmieniał zoom146/130/100/89%. Pierwszy drag po set_value40 zostawił tylko kropkę; własny zapis potwierdził1punkt, kolejny drag dał pełną linię. Przyczyną zatwierdzenie NumericUpDown na utracie fokusu w canvas.Focus, które wykonywało EndCanvasGesture po ustawieniu nowego gestureButton. Lokalnie przeniesiono Focus przed inicjalizację gestu. Odtworzenie kontrolką wewnętrznego TextBoxa: FAIL `input-612a4d298ab745f386d41116d3257a7f`; po poprawce PASS `input-4672596ec53a4202af9f6d75204b6537`. Wcześniejszy test używał NumericUpDown.Text i zatwierdzał od razu, więc jego PASS nie dowodził tej regresji; pierwszy test miał błędną nazwę metody, poprawiono generator.

Własne okno zapisano i zamknięto, brak gwiazdki przed zamknięciem; kolejne inventory potwierdziło brak okien. Po poprawce wejścia uruchomiono tylko build/copy; wynik i hash w bieżącym tool output. Ostatnia niepewna operacja: odczytać wynik tego builda przed ponowieniem. Zaliczonych metod niezwiązanych nie powtarzać. UI końcowego hasha jeszcze do wykonania, w tym gumka ze zgodą w chwili akcji. Dane prób nadal wyłącznie w fixture4cbe...; brak użytkownika w tych operacjach.

## UI końcowego 3B40353 — zakończone

Finalny SHA256 3B40353AC41D4CC09E46C6569823EB0AABFEE94983BC9DFC083E07542E1ACFF3, root i kopia zgodne. Uruchomiono własny zwykły EXE, otwarto zapisaną własną mapę. Użytkownik udzielił nowej zgody przy gumce (call_7b745ed083ed4d7cb911e0da2a14d709, odpowiedź tak). Po wpisaniu40 bez dodatkowego klikania płótna długi pas(440,285)→(1190,580) wyciął pełną plamę; poprawka fokusu działa. Środek gradientowej4 wycięto(742,690)→(805,785); CtrlZ/Y przywróciło właściwe obrazy, bez zmiany kolorów boków. Powrót do rysika, picker z pionowymRGB/dwoma trybami/Losuj; wybór gradientu i Losuj zmienił tylko kolory dwóch punktów roboczych. Zatwierdzono. Zoom146→100→89%, nowa kreska40(350,800)→(1250,800); przybliżenie100→112%, kolejna kreska(340,785)→(730,785). Widoczny poprawny końcowy przebieg. Biblioteka otwarta z rysunkami, własne20miniatur/4kolumny, poprawny ciemny widok. Zamknięto jej właściwy modal, zapisano własną mapę (gwiazdka zniknęła), AltF4 i kolejne inventory potwierdziło brak własnych okien. Brak otwartych dialogów i zgód.

Nie zaliczać ciągłej płynności z tych końcowych zrzutów. API drag tylko prosty odcinek LPM: cyfra4 jednym wieloodcinkowym gestem i okręgi są fixture/render/metody, nie nowy ręczny gest. W finalnym oknie obejrzano ich wynik, w tym rzadki ośmiokąt i gęstsze okrągłe wejście. Brak ciągłego PPM; model/render pozostają odrębne. Przenośność nadal odłożona.

Pozostałe wyłącznie: uzupełnienie kontrolowanego porównania dróg o zbliżonej długości (dotychczasowe Lissajous i fala miały różne długości, więc ich pomiar nie izoluje samej liczby przecięć), dokumentacja/raport/checklista, lokalne commity i Git. Implementacja ukończona; bez ponownego build/UI. Ostatnia niepewna operacja: brak.


## Zakres ukończony — dokumentacja i końcowy Git

Kod, finalny EXE 0.6.4.0 i celowane testy zapisano w **45f3481**. Ostatni wynik porównania po uniezależnieniu generatora od starych artefaktów: `patch-064-comparison-76d7643c4a664bbba5edc08ca3599145` PASS. Baseline odbudowana z lokalnego 0cf9c24 wyłącznie w tests/_work. Te same JSON,145punktów, długości7214/7802,6, szerokość30, zoom1,1000×640, render3: bez przecięć75,13→29,14ms, z przecięciami46,77→15,06ms. Nie wynika z tego uniwersalna zależność kosztu od liczby przecięć. Końcowy renderer i EXE niezmienione od próby zwykłego okna; nie było kolejnego builda ani powtórki UI.

README, sekcja39 specyfikacji i raport z siedmioma rozdzielonymi problemami, pomiarami/kompromisami, limitami, próbami metod vs EXE, ograniczeniami i checklistą uzupełnione. Bez dodatkowych funkcji/backlogu. W commicie dokumentacji usunięto też wyłącznie nadmiarowy pusty wiersz na końcu nowego testu, bez zmiany zachowania; diff --check obecnie przechodzi.

Wszystkie etapy implementacji i dostępnych prób zakończone. Niewykonane UI: wieloodcinkowa cyfra4/okrąg jednym gestem oraz ciągły PPM (ograniczenie API prostego LPM drag); są testy modelu/renderowania i obejrzane przygotowane przebiegi. Brak deklaracji FPS/płynności ze zrzutów, pełna przenośność odłożona. Własne okna/dialogi zamknięte, kolejne inventory puste; brak oczekującej zgody lub niepewnej operacji. Finalny SHA256 3B40353AC41D4CC09E46C6569823EB0AABFEE94983BC9DFC083E07542E1ACFF3 (główny i kopia zgodne).

Po tym wpisie wykonywany jest lokalny commit dokumentacji i końcowy status Git. Wynik i hashe odczytać z git log; nie ponawiać implementacji, buildów, zaliczonych prób ani diagnoz. Bez push/tagu/Release'a, bez pełnego zestawu i sprzątania.
