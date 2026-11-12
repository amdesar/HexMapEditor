# Raport HexMapEditor 0.6.3

12 listopada 2026. Baza sprawdzona jako master `2bfb6dc`, ukończona 0.6.2. Początkowy Git czysty. Zachowano historię, stare testy i prywatne dane. Bez migracji, sprzątania, Run-AllTests, dawnych kampanii, push/tagu/Release'a.

Aplikacja **0.6.3**, FileVersion **0.6.3.0**. Główny `HexMapEditor.exe` i zwykła kopia w `tests/_work/patch-063-fixture-aa29ceac858d47dc924eecfdfe33a674/HexMapEditor.exe` mają identyczny SHA-256:
**C5BDCC6490458FDD86FDF89E12573F5C7760D64B0DBBF723254748BF057BFE39**.

## Regresja gumki — faktyczna przyczyna i pomiary

Najpierw wykonano porównanie na kodzie 0.6.2, przed dodaniem gradientów: 12 większych falistych kresek, 2880 punktów wejściowych; 90 próbek gumki przecinających rysunek, potem nowa kreska/odrysowanie i pusta warstwa. Po gumce powierzchnie miały 12908 punktów kontrolnych. WPF już buforował parsowanie tekstu Surface. Istotny koszt pochodził z ponownego pełnego sprawdzania niezmiennych konturów, liczenia ich punktów i konwersji do GDI+ przy każdej walidacji całej warstwy; nowa próbka rysika również wywoływała tę walidację. Paint odtwarzał wszystkie widoczne ścieżki GDI+.

Dodano cache wyniku walidacji dla niezmiennego tekstu powierzchni/szerokości i liczby punktów oraz ograniczony LRU ścieżek GDI+: 256 wpisów / 200000 punktów. Wyparcie, Clear, zmiana dokumentu i Dispose zwalniają native paths. Walidacja nowych/odczytanych/zmienionych powierzchni nadal pełna. Broadphase gumki po obwiedni pozostał. Nie zmieniono boolean, tolerancji, powierzchni, budżetów kresek ani geometrii poza obszarem gumki. Nie dodano kolejki klatek/bitmapy świata.

| Ta sama próbka, sumy lokalne | Baseline 0.6.2 | Końcowy kod 0.6.3 |
| --- | ---: | ---: |
| Validate ×25 przed gumką | 6,52 ms | 2,72 ms |
| Długi gest (90 próbek) | 1424,28 ms | 1334,06 ms |
| Validate ×25 po gumce | 88,93 ms | 0,05 ms |
| Walidacje 90 próbek nowej kreski po gumce | 318,85 ms | 0,48 ms |
| Paint ×25 przed gumką | 91,77 ms | 92,28 ms |
| Paint ×25 po gumce | 74,86 ms | 59,80 ms |
| Paint ×25 pustej warstwy | 3,28 ms | 2,91 ms |

Pierwszy pomiar może obejmować JIT; to krótka lokalna seria, nie benchmark wszystkich rysunków. Pusta warstwa w profilu to renderer pustej listy, nie osobna interaktywna próba przycisku Clear. **Koszt samego dokładnego odejmowania wciąż istnieje**; nie przedstawiamy różnicy 1,42 → 1,33 s jako gwarantowanego przyspieszenia każdego gestu. Największa potwierdzona poprawa dotyczy następujących po nim nowych kresek/walidacji. Brak deklaracji FPS/płynności na podstawie screenshotów.

PNG wynikowego rysunku baseline i obu pomiarów po optymalizacji ma ten sam SHA-256: `5CAEE16D837D436EDE090E1C072856AC035FAFD3F627A25C46A33DF69D1A30BC`. Round-trip konturów zachował dane. Dokładna gumka powierzchniowa, przezroczyste otwory, jedna historia gestu i obwiednia pozostałości nie zostały zastąpione cięciem osi.

## Jeden model gradientu i picker

`GradientDefinition` zawiera 2–16 uporządkowanych punktów `(Position,Color)` i `Angle`. Końce dokładnie 0/1; odstęp minimum 0,01, walidowany z tolerancją 1e-9. Kolory #RRGGBB, bez alpha; interpolacja liniowa RGB. Stopy i zastosowane dane są głęboko kopiowane przy edycji. Sampler/cache nie są autorytatywnym zapisem i nie mają live-powiązania z późniejszą edycją ustawień narzędzia/palety.

Jednolity picker zachował HSV i porównanie poprzedni/nowy, dodał RGB 0–255 zsynchronizowane z HEX. Niepełny HEX nie zmienia ostatniego poprawnego koloru i blokuje zatwierdzenie. Anuluj nie zmienia danych. W dozwolonych kontekstach wspólny PaintPicker ma Jednolity/Gradient, pasek punktów (klik dodaje, przeciąganie zmienia pozycję, zaznaczenie złote), usuwanie punktów wewnętrznych, Tęcza i porównanie wyglądu. Skrajnych punktów nie usuwa się. Statyczny gradient nie używa timera.

Gradient jest dostępny tylko dla Rysika, Wskaźnika i wariantów terenu/mgły/ograniczenia widoczności. Definicje stanów i kolory ramek nadal otwierają wyłącznie jednolity picker. Alpha grafik PNG pozostaje odrębna i bez zmian.

## Rysik i wskaźnik

Cykl wynosi **300 logicznych px drogi**, nie czasu. 80% cyklu przechodzi pełną paletę 0→1, 20% płynnie wraca z ostatniego do pierwszego koloru. Ostatni punkt użytkownika nie jest nadpisywany. Tęcza ma zgodne końce, więc odcinek powrotu jest wtedy wspólnym kolorem. Podgląd cykliczny i napis pokazują tę konwencję; heks ma zamiast tego pojedynczy przebieg.

Kreska zapisuje definicję gradientu, oryginalne punkty świata, utrwaloną drogę Distance i długość CycleWorld=300×DPI/96/zoom z początku gestu. Nowy gest zaczyna od 0. Postój nie przesuwa fazy, wydłużenie nie skaluje wcześniejszego gradientu. Rzadkie próbki interpolują drogę i kolor, kropka ma kolor początkowy. Zoom po gestach nie przelicza fazy względem ekranu.

Gumka zachowuje oryginalną drogę potrzebną do kolorowania i odejmuje tylko Surface. Przerwa nie restartuje przebiegu po jej drugiej stronie; Undo/Redo i odczyt odtwarzają te same dane. Przy nakładaniu się fragmentów jednej kreski kolor wyznacza najbliższy odcinek oryginalnej drogi, deterministycznie, bez edytora węzłów.

Kolorowy renderer jest kafelkowy: 32 kafle 256×256, ARGB+bufor pikseli+odległości, około 24 MiB, LRU/Dispose. Skala rastra zależy od zoomu; aktywny trace dopisuje nowe segmenty do istniejących kafli, a wymazana wersja współdzieli trace koloru. Kontur pozostaje geometrią, bez bitmapy świata. W pierwszej próbie UI znaleziono szwy przy 146%: Clamp mieszał przezroczyste brzegi kafli. Zmieniono próbkowanie brzegów na TileFlipXY; nowa asercja wykazała spadek maksymalnego błędu kanału 122→3 i ponownie zaliczyła zachowanie kolorów po gumce/odczycie. Finalny obraz UI nie miał tych szwów.

Wskaźnik przechowuje fazę i sampler przy próbkach. Smuga interpoluje barwę drogi, a czas zmienia tylko alpha. Zachowano ciągłą geometrię, max pokrycia zamiast sumowania alpha, 750 ms, obecną głowę, zwężanie, rozmiar ekranowy, kursor i budżet 128 próbek. Faza aktywnej głowy jest osobna od listy wygasających próbek, więc stojąca głowa nie resetuje koloru zegarem po 750 ms. Żywe kliknięcia kontynuują fazę, całkowicie wygasły/kontekst zakończony zaczyna od 0. Bez dirty/historii/zapisu/Fit i bez nowego stałego timera. Wizualnego ciągłego PPM nie wykonano — API drag obsługuje LPM.

## Warianty heksów i formaty

Gradient jest **jednym wariantem**, obok jednolitego koloru i grafiki. Każdy heks dostaje własny pełny przebieg. Kierunek w przestrzeni płótna: 0° lewo→prawo, 90° góra→dół; Pointy/Flat zmienia maskę, nie obraca kolorów. Projekcja wierzchołków wyznacza pełny zakres. Mozaika/miniatury pokazują gradient, a istniejąca buforowana reguła kontrastu nazwy korzysta z wynikowego obrazu.

CellData zachowuje własną migawkę gradientu. Pozostały stabilne ID wariantów, zastosowane wersje terenu, niezależne losowanie pozycji puli, jawne apply i Zachowaj rozmieszczenie. Edycja bez checkboxa nie zmienia historycznych pól. Usunięty wariant nadal nie wraca do edytowalnej puli. Lokalna historia 100 operacji kopiuje gradienty; zatwierdzenie daje jedną główną transakcję. Warianty mgły/ograniczenia używają wspólnego edytora, a ich logiczne maskowanie jest niezależne od koloru i alpha grafiki.

**Mapa 11, preset 4**: wymagane jawne pola Gradient (również null), a kreski dodatkowo CycleWorld/Distance. Zmiana jest potrzebna do zapisania nowych wypełnień i niezmiennego przebiegu koloru po wymazaniu. Biblioteki grafik 2, źródła i pozostałe formaty bez zmiany. Brak migracji dawnych map/presetów, zgadywania danych lub usuwania starych plików. Zapis zawiera potrzebne dane, nie całą nieużytą bibliotekę.

Walidacja przed podmianą obejmuje stopy/kąt/liczby, zgodność drogi z oryginalną geometrią, długość cyklu i budżety. Do 16 stopów/gradient, 100000 stopów/dokument i zachowanych kresek historii, 50000/preset. CycleWorld do 3 mld, droga do 300 mld jednostek świata, zgodnie z dotychczasowym zakresem zoomu; grubości świata nadal do 2 mld. Dotychczasowych limitów punktów, konturów, źródeł, rozmiarów plików i historii 300 nie zwiększano. Native path cache ograniczony jak wyżej, nowy cache kolorów 24 MiB. Przekroczenie jest błędem, nie połowicznym sukcesem.

## Biblioteki, lista, panel i backlog

- Grafiki: stały odstępy siatki 120×114 px z obecnymi miniaturami 88×72; domyślna szerokość mieści 4 kolumny i scrollbar, resize dodaje/odejmuje kolumny bez rozciągania odstępów. Operacje i historia biblioteki bez zmian.
- Lista: domyślna szerokość od sumy kolumn, obramowania i scrollbara, ograniczona ekranem; awaryjne przewijanie pozostaje. Mgła nie wypełnia nadmiaru. Native header ma ciemne tło resztkowe przy paint/print i poszerzaniu, bez białego pasa.
- Usuń tło: bez tła tekst/ramka szare; przy aktywnym tle czerwone. Oczy narzędzi, dolne akcje grupy i geometria mozaiki nie były przebudowywane.
- Panel: jedyna oceniona możliwość BeginInvoke(Show) nadal wykonywałaby kosztowne natywne Show na tym samym wątku i mogłaby zablokować następny Move. Odrzucono ją; pozostaje działające odroczenie 0.6.2. Separator/header nadal przyszła przebudowa, bez nowej serii eksperymentów lub zmian WPF/kompozycji.
- Resize: źródło i wynik już cache'owane; mały klucz ustawień nie uzasadnia kolejnej optymalizacji. Zaakceptowane zachowanie 0.6.2 pozostało bez dalszego profilowania.
- Dawne zgłoszenie grupowych źródeł pola widzenia jest **zamknięte po ponownym potwierdzeniu użytkownika**, bez ustalonej historycznej przyczyny. Nie było ponownej diagnozy i nie ma go w aktywnej checkliście.
- [Merge heksów](backlog-merge-heksow.md): wyłącznie pomysł bez zatwierdzonego zakresu, z możliwością rozważenia wspólnego gradientu połączonego obszaru. Brak implementacji. Zachowano backlog lokalnych ustawień/góry, paneli, kolejki importu, zakładek i UX presetów.

## Testy metod, renderowania i kontrolek

Runner `tests/Run-Patch063Tests.ps1` wymaga jednego jawnego Case i odrzuca brak/niewłaściwy przed uruchomieniem. Nie uruchamiano pełnego zestawu ani poprzednich runnerów. Wyniki nie są wynikami dawnych kampanii.

| Case | Faktyczny wynik |
| --- | --- |
| `ink-profile` | Baseline `patch-063-ink-profile-d46f767b1cef4187894600bdd365fe18`, pierwszy cache `...06eb87fc9070409dbf1f5b04d0499211`, końcowy po bezpośredniej zmianie walidacji/formatu `...4e72e67e913845dba92cb03b065c0fa5`: PASS. Lokalna sekwencja i pomiary powyżej, identyczny PNG, brak mutacji oryginału, round-trip powierzchni. Nie jest pomiarem FPS ani interaktywnym Clear. |
| `gradient` | `patch-063-gradient-d8f588a0b6184a11adc1b915fa16a86e`: PASS. Liniowe RGB, Tęcza/płynny powrót bez nadpisania końca, dodaj/przesuń/usuń punkt i odstęp, HEX/RGB/HSV poprzez kontrolki/metody, niepełny HEX, jednolity kontekst, Anuluj/no-op i niezależność definicji. |
| `ink-gradient` | Końcowy `patch-063-ink-gradient-9d4faca7e6e04f5aa93be92e5c6837e0`: PASS. Wydłużenie bez przemapowania starego odcinka, rzadkie próbki/cykle, otwór i niezmienione kolory boków, render po odczycie, rzeczywiste Begin/Move/Finish/Undo/Redo i map 11, pointer faza/droga/postój/żywy kontekst/reset/no dirty, kolorowa kometa. Dodana po UI asercja zoomu 146%/szwów; wcześniejsze przebiegi nie są przedstawiane jako wystarczające. |
| `fills` | `patch-063-fills-14adedc3067e4400835d7b830cc6c336`: PASS. Pełny gradient heksa w obu orientacjach, lokalna historia/deep copy, rzeczywiste TryCreatePreset/AddTerrain i zapis, mieszana pula, apply/no-apply/preserve, główne Undo/Redo, PaintCell z kontrolowanym Random, mozaika bez zmian danych, preset 4/map 11, logiczne maski białego gradientu, odrzucony błędny kąt bez utraty bieżącej mapy i budżet. |
| `ui` | `patch-063-ui-e334705138294b4c89b96e04693ed279`: PASS. Kontrolki biblioteki 4→6 kolumn ze odstępem 120 px, disabled szara ramka, render ciemnego resztkowego nagłówka i stała Mgła. To próba kontrolek, nie kliknięcie finalnego EXE. |

Techniczne niezaliczone próby: pierwsze wywołanie gradient Case przed dopisaniem jego ciała sprawdziło tylko kompilację, nie funkcję. Kompilacja UI ujawniła kolizję nazwy Screen z metodą MainWindow — poprawiono. Pierwszy fills wykrył problem punktowego konstruktora LinearGradientBrush na miniaturze 90°; zastąpiono go poziomym brush z macierzą, potem fills PASS. Nowy test postoju wykrył potrzebę osobnej fazy aktywnej głowy; dodano poprawkę i powtórzono tylko dotknięty ink-gradient. Następnie UI wykrył szwy, a asercja 146% najpierw FAIL z błędem 122, po poprawce PASS z 3. Nie usuwano historycznych testów; w Patch062 dostosowano jedynie oczekiwany aktualny numer mapy, bez uruchamiania runnera.

## Zwykły finalny EXE

Inventory nie pokazał HexMapEditor; uruchomiono własną kopię i mapę `tests/_work/patch-063-fixture-aa29ceac858d47dc924eecfdfe33a674/Próba 0.6.3.hexmap`. Początkowy kandydat pokazał opisane szwy; zamknięto go bez zmian, poprawiono kod i przebudowano. **Dalsze próby dotyczą wyłącznie finalnego C5BDCC… identycznego z głównym EXE**, nie wcześniejszego 05CA….

1. Wczytano własny rysunek: plama 12 falistych kresek, tęcza, 30 heksów z osobnymi gradientami i mozaika palety. Przy146% nie było poprzednich szwów. To rzeczywisty render okna, nie tylko start procesu.
2. Rysik → Kolor kreski → Tęcza; klik pustego paska dodał punkt, drag przesunął go. HEX/RGB/HSV i podgląd pokazywały wybrany punkt. Zatwierdzenie samego narzędzia nie dało gwiazdki. Narysowano cienką nową tęczę poza mapą.
3. Po nowej zgodzie użytkownika wymaganej przez Computer Use gumką 40 wykonano długi drag `(440,300)→(1140,660)` przez plamę, odsłaniając ciągły pas rzeczywistej siatki. Następnie Rysik narysował nową grubą tęczę `(450,875)→(1160,875)`. Gest zakończył się właściwym obrazem; **nie mierzymy z tych screenshotów ciągłej płynności/FPS**.
4. Gumka wycięła środek starej tęczy `(810,730)→(810,770)`; Ctrl+Z przywróciło kreskę, Ctrl+Y ten sam otwór, z barwami boków pozostającymi na pozycjach.
5. Biblioteka grafik: domyślnie 4 kolumny z 20 własnymi wpisami. Maksymalizacja dała 15 kolumn, ten sam rozmiar i odstępy; brak utrwalenia czterech kolumn. Zamknięto selektor.
6. Zapisano tylko własną mapę (gwiazdka zniknęła), zamknięto własne okno; kolejne inventory potwierdziło brak obu okien. Odczyt pliku potwierdził map 11, 15 rysunków, 3 gradientowe i 13 powierzchni. Nie wykonywano ponownego otwarcia tego zapisu w UI. Hash głównego EXE/kopii ponownie zgodny.

Nie zamykano okien użytkownika, nie używano jego Library/map i nie sprzątano artefaktów. Brak oczekujących zgód, dialogów i własnych aktywnych okien po próbie.

## Niewykonane próby i ograniczenia

Bez wizualnego ciągłego PPM — render/model potwierdzone, API drag=LPM. Bez deklaracji ciągłego FPS rysika/gumki lub pełnej płynności ze zrzutów. Dokładny boolean nadal może kosztować więcej na znacznie bardziej złożonej powierzchni; nie zwiększano limitów jako obejścia. Brak pełnego audytu map, bibliotek, DPI/WPF i wydajności. Nie powtarzano grupowych źródeł ani zaakceptowanego resize. Nie przetestowano ręcznie wszystkich typów wypełnienia/kątów/zoomów; wykonano reprezentatywne przypadki. Historyczne pliki nie są migrowane. Przenośność do czystej kopii pozostaje świadomie odłożona.

## Checklista odbiorcza

- [ ] Większa plama → długi pas gumki → nowe kreski i kolejne wymazywanie, porównaj z pustą warstwą; otwór tylko pod kołem/pasem, pełne Undo/Redo.
- [ ] HEX/RGB/HSV, dodawanie/przesuwanie/usuwanie wewnętrznych stopów, skrajne 0/1, Tęcza, no-op/Anuluj; stany i ramki nadal jednolite.
- [ ] Tęczowy Rysik: cykl od drogi, postój bez zmian, nowy gest od początku, stare kolory nierozciągane; gumka i odczyt zachowują barwy boków, Fit po wycięciu skraju.
- [ ] Wskaźnik: kolor głowy od drogi, smuga wygasza alpha przez 750 ms bez zmiany hue, stojąca głowa nie resetuje koloru; żywe kliknięcia kontynuują fazę, wygasłe nie łączą się; kursor wraca.
- [ ] Gradient każdego heksa, kąt 0/90/inny i obie orientacje; mieszana pula oraz jawna aktualizacja z zachowaniem rozmieszczenia, bez aktualizacji stare pola niezmienne.
- [ ] Gradient mgły/ograniczenia widoczności nie ujawnia logicznych danych, grafika zachowuje własne alpha.
- [ ] Biblioteka 4 kolumny domyślnie, więcej/mniej po resize, stałe odstępy; lista kompaktowa z ciemnym resztkowym nagłówkiem; brak jaskrawej ramki Usuń tło bez tła.
- [ ] Własny krótki zapis/odczyt map 11 i preset 4 z gradientami. Przenośność do czystej kopii nadal odłożona; brak zadania ponownego testowania zamkniętego zgłoszenia grup.

## Git

Kod, EXE i celowane testy: **`1955be9`**. Raport, README, specyfikacja, plan i backlog w następującym lokalnym commicie dokumentacji. Gałąź master; przed commitem dokumentacji zmienione były wyłącznie README i dokumenty tej wersji; końcowy stan Git zostanie podany po commicie. Własne artefakty pozostają w ignorowanym tests/_work, bez Library/map użytkownika w commicie. Bez push/tagu/Release'a. Nie deklarujemy kompleksowego przetestowania aplikacji.
