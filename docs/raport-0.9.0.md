# HexMapEditor 0.9.0 — raport wykonania i prób celowanych

Data przygotowania: 14.11.2026. Baza sprawdzona wcześniej: `041215a`, początkowo czysta. Wersja aplikacji 0.9.0, FileVersion/ProductVersion 0.9.0.0. Jeden lokalny commit obejmuje kod, główny EXE, źródła testów i dokumentację; jego identyfikator: `git log -1 -- docs/raport-0.9.0.md`. Nie wykonano push, tagu ani Release'a. Dane robocze wyłącznie w ignorowanym `tests/_work`; prywatne mapy, Library i MRU nietknięte.

## Zakres i dwie poprawki po 0.8.3

Wykonano przebudowę lewego panelu, figury/parametry, trzy sekcje presetów, częściowy zapis/przywrócenie, drafty, zamykanie, szybki dostęp i startery. Geometria hybrydowego tytułu, natywne przyciski, hit-testy, restore oraz odstępy menu pozostały bez zmian. Nie realizowano 0.10–1.0 ani dodatkowych zmian bibliotek.

Zoom: problem wynikał z domyślnego renderowania tekstu przycisku, które mogło zawijać zbyt długą wartość. `SingleLineButton` rysuje napis z SingleLine/VerticalCenter i osobno zmierzonym miejscem na `12800%` plus marginesy; szerokość strzałki nie jest wliczana w dostępny obszar tekstu. Font stały. Reset 100% i toggle listy nadal korzystają z istniejących handlerów. Faktyczne okno: wprowadzono 12800% przez Niestandardowe, zaobserwowano cały znak %, środek pionowy i osobną strzałkę; reset działał.

Rename: statyczny `Label.AutoEllipsis` nie gwarantował jednoliniowego pionowego centrowania przy długim tekście. `EllipsisNameLabel` używa jawnych SingleLine/VerticalCenter/EndEllipsis. W oknie sprawdzono długą obecną nazwę: jeden wiersz z wielokropkiem, pionowo przy etykiecie. Pole nowej nazwy, brak prezentowanego rozszerzenia, dwukropki i mechanika rename bez zmian. Nie powtarzano kampanii konfliktów plików.

## Panel, symbole i wybór

Stała szerokość 298 px + 18 px uchwytu (przy bazowej skali), bez splittera; poszerzenie potrzebne na sześć kafelków z marginesami i awaryjny scrollbar. Jedna kolumna sekcji, ukrywanie/pokazywanie zachowane. Przybornik 4×3, ikony/obiekty po 2×6. Operacyjne przyciski 48×26, kafelki zasobów 40×28 z obrazem 24×24. Przy normalnej wysokości i Kursorze widać około sześciu pełnych terenów Domyślnego; dodatkowa wysokość trafia do terenów. Rozwinięte parametry zużywają część miejsca. Przy minimum okna cała kolumna może przewijać się pionowo, więc dolne sekcje nie są odcinane. Minimum okna nie zwiększone.

Presety: Nowy/Edycja/Zapisz/Usuń, poniżej wybór/Odśwież/Przywróć; status ma do trzech linii i pełny tooltip, niezapisana różnica czerwona. W próbie wykryto zasłanianie akcji przez DockTop wyboru; naprawiono lokalny layout przed końcowym buildem. Lista terenów pełna, z edycją/usuwaniem przy danym terenie, „Dodaj teren” i „Biblioteka terenów” (dotychczasowe zarządzanie grafikami, bez nowej biblioteki definicji).

ChoiceStyle jest wspólnym źródłem niebieskiego wyboru narzędzi, terenów, ikon/obiektów, kafelków stanów oraz zaznaczeń heksów/obiektów/obszaru. Nie zamieniano globalnie żółtych kolorów grafik i ostrzeżeń. Kosze pozostają czerwone. Nie przemalowywano niezwiązanych dialogów gradientu/kadru.

Symbole narzędzi są lokalnymi rysunkami GDI: jasna dłoń/rysik/gumka/pędzel/lasso, przechylone wiadro z wypełnionym dołem i kroplą, konturowa/pełna chmura, konturowe/pełne oko z otworem. Plusy zarządzania to sam +. Przywracanie ma osobny symbol od odświeżania. Tooltipy i dotychczasowa klawiatura pozostały. Natywny scrollbar może mieć systemowy jasny tor; systemowe MessageBox zachowano zgodnie z zakresem.

## Figury, stopnie i historia

UI: Punkt, Linia, Koło, Trójkąt, Kwadrat, Prostokąt, Pięciokąt, Heksagon, Oktagon. Stare wewnętrzne Horizontal/Vertical pozostają wyłącznie jako kompatybilne wejścia istniejącej geometrii; nie są dodatkowymi pozycjami UI. Wspólny StampAt obsługuje teren, ikony, Fog i ograniczenie widoczności, obie orientacje, fill/outline i środek poza mapą. Zwracane są tylko istniejące heksy. Podgląd i malowanie korzystają z tego samego zestawu. Wiadra i pojedyncze swobodne obiekty nie zostały przekształcone w figury.

Rozmiar 1–100; koło/kwadrat/prostokąt zachowują skalę połowy wymiaru = rozmiar × 24,24871 świata. Prostokąt A/B to lokalna szerokość/wysokość przy 0°. Pięciokąt/oktagon mają taki promień opisany. Linia przy 0° pozioma, długość (rozmiar−1) × 48,49742 z wyborem najbliższych heksów. Heksagon zachowuje przy 0° dawny zasięg axial. Trójkąt: równe ramiona rozmiar × 48,49742; kąt 1–179°, 60° równoboczny. Środek to centroid powierzchni, czubek przy 0° do góry; wysokość h daje czubek −2h/3 i podstawę h/3. Kąt zmienia proporcje przy stałej skali ramion, rozmiar skaluje całość.

Obrót całkowity, dodatni w prawo, wokół środka figury. Normalizacja przed clamp: 361→1, −361→−1, 400→40, −390→−30, 720→360, −720→−360, 0→0. Bardzo długi poprawny zapis liczony modulo cyframi, bez overflow. Scrub rusza od widocznej wartości; stopnie to nieedytowalny sufiks. Punkt ma rozmiar/obrót disabled, koło obrót disabled, Punkt/Linia wypełnienie disabled bez zmiany miejsca. Równe pola mieszczą minus i °. Nowe parametry używają istniejącej globalnej historii ustawień: jeden wpis/drag, no-op/cancel bez kroku, bez dirty i bez serializacji. Rysik/gumka: jedna wartość 1–200, default 5, bez obrotu; osobne kolory i jednolite tło sekcji.

## Presety i formaty

Nowy zapis presetu **5** jest uzasadniony nowymi sekcjami Icons/Objects i tabelą IconAssets. Do 12 ikon i 12 obiektów; definicje mają stabilne ID/nazwy i referencje źródeł, obiekt także bazowe pole widzenia. Pełne PNG współdzielone po identyfikatorze treści z dotychczasową deduplikacją. Kopie definicji są odrębne, niezmieniane źródła obrazu współdzielone. Przy odczycie walidowane listy, unikalność ID, nazwy, referencje, promienie i źródła przed zmianą dokumentu.

Preset **4** nadal odczytywany; brak nowych sekcji oznacza puste listy, nie startery ani błąd. Nowy pusty preset nie tworzy terenu neutralnego. Domyślny pozostaje chroniony przed edycją/nadpisaniem, można zapisać kopię. Pusta paleta nie usuwa danych istniejących pól.

Mapa pozostaje **11**: rozszerzone dane są wewnątrz osadzonych presetów i istniejącej tabeli IconAssets. Zapis mapy zawiera potrzebne źródła zestawów niezależnie od lokalnej biblioteki. Starszy program 0.x może odrzucić osadzony preset5; nie deklarujemy zgodności starszego EXE z nowymi danymi. Nie dodano migracji ani zmian struktury Library. Budżety nie zwiększone: 128 zasobów ikon/obiektów, do2 MiB PNG jednego, około32 MiB PNG wspólnie, preset160 MiB, mapa128 MiB, do100 roboczych presetów. Do24 pozycji listy i12 zapisywanych nie znoszą tych limitów.

Ręczny zapis ma trzy switche On. Nowy plik: zaznaczone z bieżących danych, pozostałe puste. Nadpisanie: pominięte z aktualnego odczytu pliku; kontrola oczekiwanych bajtów i LibraryFiles nadal odrzuca obcy konflikt. Wszystkie Off: disabled zapis i wymagany tekst pomocniczy, bez popupu. Częściowy zapis nie uznaje pominiętych draftów za zapisane. Częściowa pierwsza kopia presetu bez lokalnego pliku zachowuje także jego źródłowy draft.

Przywracanie zaczyna od trzech Off, czyta cały aktualny plik przed zmianą. Tereny zmieniają tylko paletę, bez heksów i bez apply. Ikony/obiekty zastępują wybrane listy, czyszcząc ich rezerwę; nie kasują biblioteki ani mapy. Błędny odczyt nie stosuje części wyniku. Undo/Redo przywrócenia obejmuje listy, kolejność i potrzebne definicje.

Drafty są odrębne per preset; przełączanie bez pytań zachowuje je. Dirty mapy wyliczane bez palet/wyboru presetu, dirty presetu porównuje trzy zapisane sekcje do biblioteki. Szybkie używanie i promocja zasobów same nie zmieniają tego porównania; można świadomie zapisać widoczne zestawy. Zapis mapy nie zapisuje bibliotecznego presetu, a zapis presetu nie zapisuje zmian mapy. Nowa/Otwórz/Ostatnie projekty zabezpieczają drafty przed zastąpieniem dokumentu.

Zamykanie: mapa → każdy niezapisany preset. Panel aktywuje właściwy draft przed pytaniem z nazwą i informacją o wszystkich trzech sekcjach. Tak: dotychczasowy wybór nadpisania/kopii, w tej ścieżce wszystkie sekcje. Nie: następny, bez kasowania draftu. Cancel/anulowanie zapisu/błąd natychmiast zatrzymują całą procedurę. Wcześniejsze udane zapisy pozostają; późniejsze Cancel nie gubi danych pominiętych przez Nie. Systemowe komunikaty Windows, bez nowego mechanizmu dialogów.

## Szybki dostęp i startery

Dwie niezależne listy 24/12 per roboczy preset. Widoczny zasób po użyciu nie zmienia pozycji. Sam wybór z pełnej biblioteki także nie promuje. Udane umieszczenie niewidocznego lub dodanie/import nowego przenosi na początek; brak duplikatów. PPM usuwa tylko pozycję listy, rezerwa domyka dwunastkę; brakujące miejsca puste. Preset zapisuje widoczne12 bez rezerwy. Nie przebudowujemy miniaturek przy każdym UpdateUI, tylko po zmianie listy/presetu. Jawna edycja wpisu biblioteki odświeża definicję aktywnego zestawu (także przy Undo), bez podmiany pozostałych draftów. Kontrola integracji wykryła i usunęła możliwość pozostawienia starej miniatury.

StarterSets daje PNG128×128 z przezroczystym tłem, porównywalnymi marginesami i centrowaniem. Ikony: wymagane pierwsze12 (Osada…Niebezpieczeństwo) i8 dodatkowych (Skarb…Anomalia). Obiekty: trzy kolorowe Postacie, Flaga, Koń, Wilk, Smok, Powóz, Samochód, Statek wodny, Statek kosmiczny, Skrzynia. Pełne nazwy w sekcji47 specyfikacji. Nowe markery inicjalizacji v2 i stabilne ID dodają brakujące dostarczane wpisy bez nadpisania zmodyfikowanych/cudzych plików. Kolory postaci nie dodają mechaniki stanów. Uruchamiano tylko własne Library pod `_work`, nie regenerowano Library użytkownika.

## Testy metod i kontrolek

Runner `Run-Patch090Tests.ps1` wymaga dokładnie jednego jawnego Case; nie uruchamia domyślnej kampanii. Zachowano historyczne źródła/runnerów. Nie uruchamiano Run-AllTests.

| Case | Ostatni istotny wynik / log w tests/_work | Zakres |
|---|---|---|
| geometry | PASS `patch-090-geometry-8a2011c5ff64490c9174e6d100aee3a7/results.log` | figury, obroty, apex, obie orientacje, fill/outline, poza mapą, duże liczby i scrub |
| quick | PASS `patch-090-quick-2ec3ac68c5724d1f87a2b6f1f0c113a6/results.log` | unikalność,24/12, widoczne/niewidoczne użycie, usunięcie/rezerwa |
| presets | PASS `patch-090-presets-ec81bcb68b42448faa3025b02c8fd193/results.log` | preset4/5, dedup, map11 round-trip, rzeczywiste Create/Save/Restore, częściowe sekcje/drafty, konflikt, błąd odczytu, close No/Cancel/błąd/prior-save, restore Undo/Redo, częściowa kopia bez lokalnego pliku |
| controls | PASS `patch-090-controls-f2564975dc7d48c19055a1542e128594/results.log` |4×3/2×6, wnętrze zoomu,400→40 i Undo, brak zmian pól, minimum/przewijanie, wspólna szerokość, promocja po faktycznym użyciu ikony, widoczne12, symulacja skalowania125%; jawna edycja bibliotek odświeża aktywne quick i Undo |

Powtórzenia wynikały z nowych asercji i wykrytych zależnych usterek. Kontrola minimum najpierw FAIL (`c398…`): teren nie miał dostępnego obszaru; po lokalnym layout/scroll PASS. Najnowszy dodatkowy test częściowej kopii najpierw wskazał celowo uszkodzony plik z poprzedniej próby w tym samym katalogu (`4cc5…`); wydzielono fixture tej niezależnej asercji, następny wynik PASS. Nie przedstawiamy błędu fixture jako błędu produkcyjnego. Geometry/quick nie ponawiano po niezależnych zmianach presetów/UI. Ostrzeżenia kompilatora dotyczą nieprzypisywanych hooków diagnostycznych/testowych; build bez błędów.

## Zwykły EXE, ograniczenia i odbiór

Rzeczywiste próby przez Computer Use, odizolowane katalogi `patch-090-ui`, `patch-090-final`, `patch-090-final2`, `patch-090-final3` i końcowy `patch-090-release`. To kolejne zależne kandydaty, nie pięć pełnych kampanii. Sprawdzono wygląd narzędzi i starterów12+12, sześć pełnych terenów, chowanie, minimum i dostęp dolnych rzędów, pusty preset, dodanie terenu, rozdzielenie dirty, figurę z obrotem400→40, częściowy zapis (allOff + komunikat, potem tylko Tereny), przywracanie Off/wybór/Cancel, przełączanie draftów i Cancel zamykania. Wybranie widocznej ikony i udane malowanie nie przestawiło jej; zapis mapy nie wymuszał zapisu presetu. Sprawdzono rzeczywiste12800% i długi rename. To obsługa okna, nie wyłącznie wywołania metod.

Ostateczna kopia `patch-090-release/HexMapEditor.exe` jest źródłem bajtów głównego EXE. SHA-256: `13DAC209E1839BD3320EF819848F5D19C9D044F3BDDF3CABC00309DBBE03AD80`. Końcowy smoke obejmuje otwarcie własnej mapy, layout i ochronę draftu w Ostatnich projektach (pytanie z nazwą, Cancel zachował mapę/draft, Undo przywróciło czysty preset). Po ostatnim odświeżaniu definicji bibliotek powtórzono zależny controls i krótki start/otwarcie, bez ponawiania całej sesji; wcześniejsze niezależne oceny wizualne nie były powtarzane bez potrzeby. Wynik końcowego smoke i stan procesów zapisano w planie.

Nie wykonano pełnej macierzy nowych figur w EXE: metody pokrywają wspólną geometrię, integracje reprezentatywne. Złożone sekwencje No/Yes/błąd z kilkoma presetami i konflikt pliku testowano metodami/hookami, a Cancel również w zwykłym oknie. PPM usunięcia quick sprawdzono modelem, bez fizycznego klikania każdej pozycji. Symulacja125% kontrolek nie jest fizycznym testem monitora/DPI. Nie deklarujemy pomiaru płynności ze zrzutów. Nie powtarzano ramki, pięciominutowego autosave, kampanii gumki/WPF/masek/bibliotek. Przenośność do czystej kopii nadal świadomie odłożona. To testy celowane, bez deklaracji kompleksowej regresji.

### Krótka checklista użytkownika

- [ ] 12800% mieści % bez skoku pionowego; reset i strzałka działają oddzielnie.
- [ ] Krótka/długa obecna nazwa rename ma tę samą linię i pionowe wyrównanie.
- [ ] Panel4×3/2×6, chowanie i dostęp przy minimum; czytelność białych symboli i niebieskich wyborów.
- [ ] Figury, A/B, apex60°/spiczasty/płaski, obrót i sufiks; podgląd=stempel, także poza siatką.
- [ ] Pusty preset; Domyślny chroniony, dostarcza12+12; startery czytelne optycznie.
- [ ] Częściowy nowy zapis/nadpisanie, Off i przywracanie wybranych sekcji bez zmiany heksów.
- [ ] Powrót do roboczego presetu zachowuje dane; dirty mapy/presetu niezależne.
- [ ] Widoczny quick nie przeskakuje, niewidoczny po użyciu promuje, PPM usuwa tylko z listy i uzupełnia rezerwą.
- [ ] Zamykanie mapa→presety, nazwa i wszystkie sekcje; Anuluj zatrzymuje całość, Nie nie gubi draftu po późniejszym Cancel.
- [ ] Znane odłożone etykiety switchy/edytor gradientu i przyszłe biblioteki pozostają backlogiem; pełna przenośność później.

Końcowy stan Git po wymaganym pojedynczym commicie ma być czysty; wyniki `_work` i prywatne dane nie należą do commita. Dokładny hash commita i końcowy status podaje odpowiedź końcowa oraz `git log -1` / `git status --short`.
