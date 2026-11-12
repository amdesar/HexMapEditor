# Raport HexMapEditor 0.6.4

12 listopada 2026. Baza lokalna potwierdzona: `0cf9c24` (dokumentacja ukończonej 0.6.3), kod `1955be9`. Początkowy Git czysty. Prace tylko w zadanym repozytorium, dane prób i kopie tylko w tests/_work. Bez prywatnej Library/map użytkownika, sprzątania, migracji, pełnych zestawów, push, tagu i Release'a.

Aplikacja **0.6.4**, FileVersion **0.6.4.0**. Główny HexMapEditor.exe i zwykła kopia użyta w końcowej próbie:
`tests/_work/patch-064-fixture-4cbe2dd88a48477c804b9c7dbf2238ac/HexMapEditor.exe`.
Oba SHA-256: **3B40353AC41D4CC09E46C6569823EB0AABFEE94983BC9DFC083E07542E1ACFF3**.

## 1. Zoom i duże kreski

W 0.6.3 ograniczenie cache do 32 kafli ograniczało RAM, ale nie pracę. Kafel należał do jednej kreski i skali; renderer mógł w jednym Paint zbudować więcej kafli niż mieścił cache, wyrzucając i odtwarzając potrzebne wyniki. Każdy wykonywał rasteryzację segmentów i ponownie rysował pełną ścieżkę. Zmiana zoomu zmieniała zestaw wyników; przy dopisywaniu nowej kreski Paint nadal przechodził przez stare. Walidacja każdej nowej próbki przeglądała również wszystkie stare drogi gradientów. To odrębne od naprawionej w 0.6.3 walidacji tekstów powierzchni.

Zastąpiono arbitraż koloru piksel po pikselu natywnym liniowym brushem RGB. Segment poza widocznym kaflem odpada przed utworzeniem brush/pen. Jest jeden ponownie używany scratch 256×256 w dokładnej skali urządzenia, bez ograniczania skali do potęg dwójki i bez cache kafli per kreska. Kolejne przejścia przez scratch nie są nowymi alokacjami/wyparciami bitmap.

Cache ukończonej warstwy dotyczy tylko bieżącego viewportu, kamery i niezmiennych kresek. Aktywna kreska jest rysowana osobno. Mała invalidacja kursora i klip podglądu gumki nie odbudowują starych rysunków. Pełna walidacja pozostaje przy początku/końcu gestu oraz odczycie; w ruchu sprawdzana jest aktywna kreska i suma limitu punktów. Nie osłabiono walidacji nowych danych.

Budżet obrazu cache: **5,8 mln pikseli**, około **22,13 MiB** ARGB; scratch **0,25 MiB**. Razem poniżej poprzedniego 24 MiB rastra. Większy viewport renderuje bez cache, w pełnej jakości. Pusta warstwa nie tworzy obrazu cache. Nie ma bitmapy świata, kolejki starych klatek, nowych workerów, DoEvents ani globalnego timera. Dispose/Clear/zmiana dokumentu zwalniają obrazy. Nie podniesiono limitów dokumentu jako obejścia.

## 2. Gradient rysika i wskaźnika — osobne ustalenia

Główny wykazany koszt gradientowego rysika to programowa rasteryzacja: odległość osi, wybór zwycięzcy i sampler palety dla pikseli kolejnych segmentów, powtarzane dla kafli. Natywny brush używa tej samej liniowej interpolacji RGB i pełnego cyklu 80% paleta / 20% powrót. Distance, CycleWorld, 300 logicznych px, faza od drogi, Tęcza i brak animacji istniejących danych pozostają.

Dla samego wskaźnika wybrana próba **nie potwierdziła większego kosztu gradientu**: 20 renderów 80 próbek po rzadkiej falistej trasie, viewport 1000×640, głowa 20 px: 0.6.3 jednolity/gradient **33,57/31,20 ms**, 0.6.4 **33,53/31,52 ms**. Nie przypisujemy mu diagnozy rysika. Cache ukończonej warstwy zmniejsza jednak koszt odrysowywania rysunków pod kometą. Timer zatrzymuje się też po zgaśnięciu smugi nieruchomej aktywnej głowy; ruch wznowi timer bez zmiany fazy. Nie zmieniono półprzezroczystego składania komety, max pokrycia, 750 ms, rozmiaru, kursora ani zasad żywych kliknięć.

## 3. Faktyczny gest gumki

0.6.3 wykonywało dla każdej próbki boolean, serializację nowego konturu i walidację wyniku, po czym następna próbka obrabiała już tę powierzchnię ponownie. Nie jest to dawny problem późniejszego rysowania po gumce — tę poprawkę zachowano.

Nowy InkEraseGesture zbiera **wszystkie kapsuły** kolejnych próbek. Geometryczna suma nie pomija zakrętów ani nie zależy od czasu. Podczas ruchu klip warstwy pokazuje wycięty pas bez destrukcyjnych zmian danych. Przy zakończeniu jedno Exclude na każdą rzeczywiście przeciętą kreskę, jedna serializacja wyniku i końcowa walidacja. Odrzucenie/błąd przywraca cały stan sprzed gestu. Jeden gest to jeden wspólny Undo/Redo, bez losowania przy Redo.

Limit gestu: 10000 próbek, bez zwiększania limitów konturów, punktów dokumentu i historii. Nadal wektorowe powierzchnie, przezroczyste otwory, prawdziwe warstwy poniżej, poprawny Fit. Nic nie wymazuje heksów, obiektów lub masek. Podgląd używa klipu GDI+, więc krawędź może różnić się od końcowej antyaliasingiem około jednego piksela; dokładny wynik powstaje przy końcu gestu. Nie ma pozornej gumki malującej tłem.

Porównanie 45-próbkowej trasy z wcześniejszym sekwencyjnym odejmowaniem dało **246 różnych pikseli krawędzi AA, żadnego o błędzie kanału >32** w obrazie 1000×640. Kontury mają inną segmentację (nie identyczny tekst), dlatego kontrola dotyczy powierzchni/renderowania. Zachowanie kolorów wnętrza poza otworem, prawdziwa dziura, brak odsłonięcia wcześniejszego fragmentu, Undo/Redo, map11 i Fit zaliczone. Dokładny boolean nadal ma koszt końcowy — nie ogłaszamy całkowitego usunięcia wszelkich przycięć.

## Pomiary przed/po

Ta sama reprezentatywna próbka i sekwencja wejściowa: viewport **1000×640**, 96 DPI bitmapy, **6 kresek ×160 punktów**, grubość **80 jednostek świata** (20 px przy .25), zoom kolejno **.25, .5, 1, 1.46, 2**. Następnie trzy odrysowania na 2, dopisanie 12 punktów nowej kreski szerokości 20 świata z renderem po każdym. Czas renderera/zmiany danych, bez pomiaru całego MouseMove, UI, historii czy gwarancji FPS. Pierwsza seria obejmuje także zimne cache/JIT.

| Operacja, suma lokalna | 0.6.3 | Końcowe 0.6.4 |
| --- | ---: | ---: |
| 5 zoomów, jednolity | 6,91 ms | 21,71 ms |
| 5 zoomów, gradient | 234,06 ms | 115,37 ms |
| 3 kolejne rendery na 2, jednolity | 1,47 ms | 3,76 ms |
| 3 kolejne rendery na 2, gradient | 22,96 ms | 3,29 ms |
| 12 dopisań i renderów, jednolity | 6,67 ms | 14,50 ms |
| 12 dopisań i renderów, gradient | 129,26 ms | 65,09 ms |
| Pojedynczy render falistej trasy gradientu na .25 | 55,87 ms | 34,91 ms |
| Pojedynczy render Lissajous gradientu na .25 | 224,98 ms | 93,23 ms |
| 70 próbek gumki, 10 kresek ×180 punktów, szerokość 28, gumka 36 | 518,69 ms | 178,44 ms |
| 12 renderów pustej warstwy | 2,70 ms | 2,81 ms |

Jawny kompromis: przy tak małym prostym **jednolitym** rysunku bitmapowy compositor dodaje narzut względem natychmiastowego GDI+. Nie przedstawiamy poprawy gradientu jako przyspieszenia każdej operacji. Cache służy ponownym odrysowaniom dużej ukończonej warstwy, z kosztem pierwszej budowy po zmianie kamery. Pusta warstwa cache pomija; samo 12-krotne wywołanie pustego compositora zmierzono na 0,22 ms (bez czyszczenia bitmapy tła).

Bazowy Paint falistej trasy zbudował 24 kafle; Lissajous 52 przy cache mieszczącym 32. Końcowy renderer wykonał odpowiednio 24/48 przejść przez **ten sam scratch**, bez alokacji/eviction bitmap dla każdego przejścia i bez programu odwiedzającego piksele. Nie są to identyczne liczniki cache. Przyrost geometrii po gumce: 9643 → 9831 punktów kontrolnych pozostałości, w istniejących limitach.

Pierwsze dwie trasy miały różne długości, więc nie izolują wpływu samoprzecięć. Dodatkowy kontrolowany Case `comparison` odtwarza renderer z **0cf9c24** w tests/_work i podaje mu ten sam JSON co 0.6.4. Nie wymaga pozostawionego starego EXE ani starego runnera. Po 145 punktów, szerokość 30, zoom1, viewport1000×640, trzy rendery z zimnym pierwszym:

| Droga | Długość świata | 0.6.3 | 0.6.4 |
| --- | ---: | ---: | ---: |
| Bez samoprzecięć | 7214,0 | 75,13 ms | 29,14 ms |
| Z samoprzecięciami | 7802,6 | 46,77 ms | 15,06 ms |

Nie potwierdza to hipotezy, że sama liczba przecięć zawsze zwiększa koszt. Znaczenie mają też pokryty obszar, przestrzenne nakładanie i przejścia rastra. To krótka lokalna kontrola, nie uniwersalny model wydajności.

## 4. Kolejność kolorów na przecięciach

Późniejszy odcinek zapisanej drogi jest na wierzchu, również w jednym nieprzerwanym geście. Usunięto wybór według najbliższego odcinka, bez średniej kolorów. Korpusy mają płynny liniowy przebieg, okrągłe łączenia i końce; nie stemplujemy pełnych płaskokolorowych dysków próbek. Kolorowe pole jest nieprzezroczyste, jedna ostateczna geometria nadaje AA. Wstępny obraz ujawnił delikatne dyski na gęstych łączeniach; skorygowano sektory łączeń przed końcową próbą.

Cyfra podobna do 4 ma kolor późniejszej pionowej części na przecięciu. Cache/rebuild/odczyt zachowuje tę samą kolejność. Gumka odejmuje całą powierzchnię, więc nie odkrywa wcześniejszej poziomej części w otworze. Oryginalne Points/Distance pozostają do kolorowania pozostałości, bez restartu lub przeskalowania fazy. Samplowanie na 146%: maksymalny błąd kanału względem analitycznego samplera **8**, bez szwów kafli.

## 5. Małe okręgi

Porównano dokładnie te same przebiegi: **8 i 64 próbek**, promień 32 świata, szerokość 8, jednolity/gradient, skale **.5/1.46/2**. Sylwetki są zgodne w obu trybach. Próba wykryła obcięcie piksela AA przez zaokrąglenie obwiedni gradientu; naprawiono floor/ceil z marginesem urządzenia. Nie potwierdzono odrębnego zamieniania gęstego okręgu w kwadrat. Rzadkie ośmiopunktowe wejście pozostaje widocznym wielokątem — nie dodano globalnego wygładzania ani nowego narzędzia krzywych. Finalne okno pokazało obydwa przygotowane przebiegi; nie udajemy ręcznego narysowania okręgu narzędziem obsługującym tylko prosty drag.

## 6. Picker i Losuj

R/G/B 0–255 w zwartej pionowej grupie, zachowane HEX/HSV, synchronizacja, walidacja niepełnego HEX, kolejność Tab i obecny/nowy wygląd. Wspólny picker narzędzi ma dwa wykluczające się przełączniki Jednolity/Gradient. Edytor wariantu nadal dopuszcza Kolor/Gradient/Grafika. Jednolite konteksty stanów i ramek nie pokazują gradientu.

Losuj znajduje się między Tęcza i Usuń punkt. Lokalny generator zmienia tylko RGB istniejących punktów roboczych; pozycje, liczba i kąt bez zmian, bez alpha, bez generatora heksów i bez ponownego losowania na Paint/OK/odczycie. Anuluj/no-op zachowane; zatwierdzony wynik jest zwykłą definicją gradientu z istniejącą historią i zapisem. Brak nowych presetów lub bibliotek gradientów.

Podczas UI wykryto zależny problem wejścia po ręcznym wpisaniu rozmiaru: Focus na płótnie zatwierdzał NumericUpDown, którego ValueChanged wykonywało EndCanvasGesture już po ustawieniu nowego gestureButton. Pozostawała kropka zamiast pierwszego drag. Przeniesiono canvas.Focus przed inicjalizację gestu. Regresja odtworzona rzeczywistym wewnętrznym TextBoxem, po poprawce zaliczona metodą i pierwszym rzeczywistym drag gumki po wpisaniu 40. Nie dodano opóźnienia ani DoEvents.

## 7. Czas otwierania biblioteki

Na własnych 20 wpisach z jednym źródłem 2048×1024, **bez rysunków**, ctor+Show: cold **512,22 ms**, ponowne otwarcie **485,57 ms**. Każda miniatura ponownie dekodowała źródło i tworzyła gotowy wynik 1024×1024, mimo wyświetlania 88×72. To odrębny wykazany koszt, nie wyłącznie obciążenie rysunkami.

Miniatura powstaje teraz bezpośrednio ze źródła i kadru w 88×72. LRU do 32 małych podglądów (~0,78 MiB), bez zatrzymywania pełnych źródeł. Klucz identyfikatora obejmuje źródło i transformacje, a rename go nie zmienia; wyparcie/Dispose zwalnia bitmapy. Cold/warm po poprawce **73,54/32,92 ms**. Układ, stały spacing, responsywne kolumny i historia nie przebudowywane. Finalne okno z rysunkami również otworzyło własną bibliotekę poprawnie, ale nie podajemy z tego arbitralnego czasu ani gwarancji dla dowolnej biblioteki.

## Formaty, zasoby i granice

**Map11/preset4 bez zmiany.** Autorytatywne pola wystarczają; nowy wynik przecięć wynika z istniejącej kolejności. Cache i gest podglądu nie są zapisywane. Brak migracji, odzyskiwania lub regenerowania danych. Nie zmieniano budżetów źródeł, konturów/punktów ani 300 wpisów historii. Grafiki alpha, heksy, grupy, WPF, panel i resize zachowane. Backlogi pozostają backlogami, a zgłoszenie źródeł grup pozostaje zamknięte.

## Testy metod, renderowania i kontrolek

Runner wymaga jednego jawnego Case, brak/niewłaściwy kończy się błędem przed próbami. Bez Run-AllTests, poprzednich runnerów i pełnej kampanii. Wszystkie poniższe katalogi w tests/_work:

- `profile`: baseline `patch-064-profile-0199d150561e4f6dbc2f18006a1f6fbc`, finalny po zmianie łączeń `patch-064-profile-29868737b5d94516999ae58297d59f67`: PASS. Czasy/zakres powyżej, bez wyników całego UI. Pośrednie profile służyły bezpośrednim zmianom renderera/gestu, nie są osobnymi kampaniami.
- `comparison`: `patch-064-comparison-76d7643c4a664bbba5edc08ca3599145`: PASS. Baseline odtworzona z Git przez własny generator, te same dane dla obu rendererów. Pierwsza wersja używała zachowanego bazowego EXE; runner poprawiono, aby nie wymagał odtwarzania dawnych artefaktów.
- `image`: `patch-064-image-1eb4e3acaf8b4fa2bc6d7b8b450de57e`: PASS. Późniejszy kolor, rebuild, brak przemapowania poza otworem (z tolerancją AA), rzeczywisty otwór, batch/sequential, gęsty/rzadki okrąg,146%, produkcyjny compositor/dirty region/preview, rzeczywiste Begin/Move/Finish/Undo/Redo, map11 i Fit. Powtórzony po wykrytym przycięciu AA i korekcie łączeń; wcześniejsze niezaliczone asercje nie są sukcesem.
- `picker`: `patch-064-picker-b194cbc6eb824bbca6f4cf0b8bef1013`: PASS. Pionowe RGB i synchronizacja/walidacja, dwa przełączniki, kontrolowany Random zachowuje parametry, Paint nie losuje, Anuluj/no-op. To kontrolki/metody, nie ręczne okno EXE.
- `library`: baseline `patch-064-library-e25ea1706d5e4ffa852fac8a8299cb65`, po zmianie `patch-064-library-0ad6c6f2e2e64e8bad73f0b724acf39d`: PASS. Bez rysunków, 20 własnych pozycji, cold/warm, zachowane wpisy.
- `input`: błąd odtworzony `patch-064-input-612a4d298ab745f386d41116d3257a7f`, po poprawce `patch-064-input-4672596ec53a4202af9f6d75204b6537`: PASS. Wpis w wewnętrzny TextBox, blur, nowy drag i co najmniej 2 punkty. Wstępna błędna nazwa metody i próba NumericUpDown.Text nie były dowodem tego scenariusza.
- `fixture`: generator własnej mapy i 20 grafik, nie test funkcji. Pierwszy brakujący katalog poprawiono w generatorze; udany `patch-064-fixture-4cbe2dd88a48477c804b9c7dbf2238ac`. Dane użytkownika nie służyły do przygotowania.

## Rzeczywisty zwykły finalny EXE

Próby na końcowym **3B40353…**, hash identyczny z głównym artefaktem. Wcześniejsze kandydaty służyły wykryciu artefaktu łączeń i problemu fokusu; nie są przedstawiane jako zaliczony finalny scenariusz.

1. Uruchomiono własną kopię i wczytano własną mapę 15×10 z plamą, przygotowaną jedną gradientową cyfrą4 i dwoma okręgami. Obejrzano zachowanie na przecięciu i różnicę gęstego/rzadkiego przebiegu.
2. Gumka 40 wpisana w polu. Po zgodzie użytkownika w chwili usuwania: długi drag `(440,285)→(1190,580)` przez plamę — ciągły pas i siatka poniżej. Pierwszy drag po wpisie działa, bez zastępczej kropki.
3. Drugi drag `(742,690)→(805,785)` wyciął środek gradientu. Ctrl+Z przywróciło, Ctrl+Y wykonało ten sam otwór; boki zachowały kolory, wcześniejsza część nie pojawiła się w dziurze.
4. Rysik, finalny picker: pionowe RGB, dwa tryby, Gradient→Losuj (kolory dwóch punktów/podgląd), Zatwierdź. Zoom 146%→100%→89%; nowa kreska 40 `(350,800)→(1250,800)` po długim wymazywaniu. Przybliżenie 100%→112%, kolejna `(340,785)→(730,785)`. Właściwe końcowe obrazy i przebieg.
5. Otwarto własną bibliotekę z rysunkami: 20 miniatur, 4 kolumny, ciemny widok. Zamknięto właściwy modal. Zapisano wyłącznie własną mapę (gwiazdka zniknęła), zamknięto własne okno. Kolejne inventory potwierdziło brak własnych okien/dialogów. Hash root/kopii ponownie zgodny.

Nie oceniano FPS lub ciągłej płynności ze screenshotów. API drag to prosty odcinek LPM: nie dało ręcznie wykonać wieloodcinkowej cyfry4 w jednym geście, okręgów ani ciągłego PPM komety. Dla tych przebiegów są metody/render i przygotowany obraz w EXE, bez udawania nowych ręcznych gestów. Nie blokowano okien użytkownika.

## Ograniczenia i niewykonane próby

Bez pełnej regresji, DPI/WPF, wielkich benchmarków, całej macierzy zoomów i źródeł. Nie obiecujemy stałego czasu ani wyeliminowania kosztu dokładnego boolean na każdym rysunku. Pierwsza budowa cache po kamerze nadal synchroniczna, a mały jednolity rysunek ma wskazany narzut. Bez ciągłego PPM i ręcznej figury4/okręgu przez ograniczenia API. Przenośność do czystej kopii nadal świadomie odłożona. Nie powtarzano zaakceptowanych heksów, grup, resize i pełnych bibliotek bez zależnej zmiany.

## Checklista użytkownika

- [ ] Wiele długich kresek, oddalanie, nowe grube kreski, stopniowe przybliżanie i następna kreska; porównaj jednolity/gradient oraz stan po Wyczyść rysunki.
- [ ] Jednym gestem gradientowa4 lub inna samoprzecinająca figura: ostatni fragment na wierzchu, bez kolorowych dysków, szwów i zmiany starych barw po redraw/zoom.
- [ ] Długa gumka przez gęstą plamę: dokładny pas, pełna przezroczysta dziura, wszystkie warstwy rysunków tylko pod gumką; następne rysowanie, Undo/Redo, Fit i krótki zapis/odczyt.
- [ ] Małe gęste/rzadkie okręgi przy kilku zoomach; zachowane narożniki, bez dodatkowego kwadratowego obcięcia gradientu.
- [ ] RGB pionowo, HEX/RGB/HSV, Jednolity/Gradient, Tęcza i Losuj: liczba/pozycje/kąt bez zmian, Anuluj/no-op, zachowanie wyniku po zatwierdzeniu i odczycie. Stany/ramki nadal jednolite.
- [ ] Wpisz rozmiar i od razu przeciągnij na płótnie — pełna kreska/pas zamiast samej kropki.
- [ ] Wskaźnik jednolity/gradient: barwa od drogi, postój bez zmiany, 750 ms i ciągła kometa, kursor wraca; po wygaśnięciu brak niepotrzebnej pracy.
- [ ] Biblioteka bez i z rysunkami: cold/warm, miniatury poprawne, stałe odstępy i responsywne kolumny. Bez ponawiania całej kampanii bibliotek.
- [ ] Przenośność do czystej kopii pozostaje odłożona; zamknięte źródła grup i pozostałe backlogi nie są aktywnym odbiorem tej wersji.

## Git

Kod, finalny EXE i celowane testy: **45f3481**. Dokumentacja w kolejnym lokalnym commicie; poprawiono w nim również wyłącznie pusty wiersz końcowy nowego źródła testów, bez zmiany zachowania. Końcowy stan: czysty po lokalnym zapisie dokumentacji; weryfikacja Git na zakończenie. Konkretne dwa commity podano także w odpowiedzi końcowej. Własne dane pozostają ignorowane w tests/_work. Bez push, tagu i Release'a. Nie deklarujemy kompleksowego przetestowania aplikacji.
