# Raport HexMapEditor 0.7.1

## Wynik i baza

Baza potwierdzona: `03d4032` (dokumentacja 0.7.0), kod `7bf009e`. Zastany nieśledzony `recent-projects.json` zachowano bez czytania prywatnych ścieżek; dodano ignorowanie pliku runtime. Wszystkie własne dane, kopie i logi są w `tests/_work`. Nie użyto prywatnej Library ani map, nie zamykano okien użytkownika, nie przeprowadzano sprzątania.

Zakres 0.7.1 zaimplementowany. Główny `HexMapEditor.exe`: aplikacja 0.7.1, FileVersion/ProductVersion **0.7.1.0**. SHA-256 głównego EXE i kopii użytej do końcowych prób:

`A904EAEF3167D42CCFD4EF3019971E8E6C559375F180F59ED0100AC88ED808DB`

Kod, EXE, runner i plan: lokalny commit **092c584**. Dokumentację końcową zapisano osobnym następnym commitem; jego hash podaje końcowe podsumowanie/Git. Bez push, tagu i Release'a.

## Start: reprodukcja, przyczyna i cykl życia

Ograniczona próba bazowego kodu 0.7.0 w runnerze (normalny start, maximize, resize800×600, krótka bezczynność) nie odtworzyła zamknięcia. Nie uznano resize lub czekania za przyczynę. Zabezpieczono konkretną lukę: wcześniejsze MainForm=editor przed sprawdzeniem skutecznego Show nie miało kompletnego rollbacku.

Obowiązkowa próba **zwykłego winexe** ujawniła jednak rzeczywistą awarię także w pierwszym kandydacie 0.7.1: Nowy → standardowy Save zapisywał plik i kończył proces przed pokazaniem edytora. Nie zaliczono jej na podstawie wcześniejszych testów metod. Kod wyjścia **0xC000041D**, bez kontrolowanego loop.exit. Opt-in diagnostyka zawęziła etap do pierwszej konstrukcji hosta WPF opisu, przed wejściem do body konstruktora MainWindow. Nieobsłużony InvalidOperationException w `Dispatcher.WndProcHook`: „Przetwarzanie dyspozytora zostało wstrzymane, ale komunikaty są wciąż przetwarzane”. Samo BeginInvoke nie wystarczyło. Konsolowy test z podstawionym wyborem pliku i pojedynczy przebieg z dodatkowym first-chance handlerem nie odtwarzały tego samego wyścigu — nie były dowodem naprawy zwykłego EXE.

Poprawka przygotowuje **jeden host opisu WPF przed wejściem w pętlę WinForms**, przekazuje go do docelowego edytora i zużywa raz. Pierwsza inicjalizacja nie następuje już w natywnym callbacku po dialogach. Nie wczytuje to Library, mapy ani pełnych obrazów na zapas, nie zmienia edytora formatowania i nie odtwarza dokumentu WPF przy przełączaniu zaznaczenia. Konkretnego zagnieżdżonego komunikatu uruchamiającego pompę WPF nie ustalono; potwierdzono wyjątek, fazę i skuteczne przejścia po tej zmianie. Nie przypisujemy całej historii sporadycznego zgłoszenia jednemu warunkowi resize/idle.

Start pozostaje właścicielem pętli do zakończonego Load/Shown oraz Visible/Handle edytora. Dopiero potem zmiana MainForm i zamknięcie startu. Guard blokuje ponowne otwieranie. Wyjątek/earlyClose kandydata powoduje Dispose/rollback i komunikat na działającym starcie. Obsługa błędów Load/Shown i natywnych callbacków jest ograniczona do przejścia; nie dodano globalnego pustego catch/restartu. Dla argumentu mapy MainForm pozostaje null do gotowego edytora, co zapobiega automatycznemu pokazaniu startu przez Application.Run.

Diagnostyka: 128 zdarzeń w pamięci, opcjonalny plik wskazany przez `HEXMAPEDITOR_START_TRACE`, około64KiB. Fazy konstrukcji/ready/rollback/closed/handleDestroyed/loop.exit, typ/HResult i stos wyjątku; bez tytułów, ścieżek/treści map i komunikatów wyjątku. Bez zmiennej nie ma zapisu logu. Szczegółowe sondy testowe usunięto z kodu produkcyjnego. W odczytanym dzienniku błędów Windows nie znaleziono wpisów tych awarii; nie traktowano tego jako braku awarii.

Start ma stały logiczny rozmiar650×550, bez resize/maximize; nadal można go przesuwać i zamknąć. WorkingArea/DPI ogranicza rozmiar, dostępne przewijanie. Główne przyciski280px zachowane; recent324px, wycentrowane (~50%), ellipsis/pełny tooltip. Model MRU5/30 bez zmian. Przeniesiono ograniczenie rozmiaru z Shown do Load przed pierwszym pokazaniem — to mała lokalna poprawka kolejności, bez dowodu całkowitego braku flasha.

## Nowy projekt z plikiem

Oba wejścia używają wspólnego `ProjectCreation`: parametry → standardowy SaveFileDialog → istniejący MapForSave/ValidateMap/serializer i atomowy writer → dopiero przyjęcie dokumentu. Writer wydzielono z dotychczasowej ścieżki zapisu, bez drugiego formatu/serializera. CreateNew pliku tymczasowego, Flush(true), Replace/Move, sprzątnięcie wyłącznie własnego temp po błędzie. Nadpisanie wymaga jawnego potwierdzenia; pojawienie się obcego nowego pliku nie daje cichego overwrite.

Udany zapis przypisuje ścieżkę, czysty stan i MRU. Ctrl+S korzysta z niej od razu. Cancel parametrów/zapisu lub błąd, także inicjalizacji katalogu/dialogu, nie podmienia poprzedniej mapy ani nie zamyka startu. Zachowano pytanie o niezapisany dokument. Początkowa mapa jest poprawnym map11. Nie powstają projekty z automatycznymi losowymi nazwami.

## Gumka: no-op, UI i handoff

Potwierdzony nadmiar 0.7.0: worker także dla pustego gestu, disabled całych canvas/shell/left/right, natychmiastowy pasek, pełne UpdateUI na końcu, usunięcie preview przed podmianą danych. To oddzielne koszty od dokładnego Exclude.

Nowy test kapsuły bada powierzchnię. Dla surowej kreski używa odległości odcinków; dla konturów z otworami cached flattened path z testem wnętrza/krawędzi, nie samej obwiedni. Flatten współdzieli dotychczasowe200000punktów cache, z lokalnym nativeRegion fallback przy zbyt dużym wyniku. Tolerancja max(.0005,width/65536) jest konserwatywna: w bardzo wąskim pasie granicy o wyniku nadal decyduje dokładny worker. Nie zmniejszono precyzji odejmowania. Nietrafione próbki przed pierwszym trafieniem nie budują WPF cuts. W testowanych pustych obszarach i wnętrzu otworu nie powstaje job/wait/history/dirty.

Podczas pracy nie zmienia się Enabled całych gałęzi. Wąski IMessageFilter i guardy poleceń chronią edycję/zapis/Undo/Nowa/Otwórz, z odpowiedzią w statusie. Escape/Anuluj i zamknięcie pozostają dostępne. Overlay bez Dock nie przesuwa layoutu; anulowany timer pokazuje go dopiero po180ms. Po wyniku aktualizowane są caption i przyciski historii, bez szerokiego UpdateUI.

Preview instalowane przed capture pozostaje przez worker i zastosowanie. Najpierw podmiana modelu/historii i Clear starego exact cache; klip przekazany do pierwszego rysowania dokładnego wyniku. Dopiero wtedy Dispose. Cancel/error świadomie przywraca oryginał; nieaktualny wynik odrzucony przez referencje/rewizję. Zachowano worker MTA, zamrożone Freezables, odłączone dane, brak kolejki, jeden gest/jeden Undo/Redo, Surface/kolory/Fit/zapis.

### Pomiar tej samej ograniczonej fixture

16 kresek ×180punktów, szerokość35 świata;100próbek zakrzywionego cięcia, średnica42. Czasy ms, pojedyncze lokalne przebiegi, nie benchmark dowolnej mapy ani FPS zwykłego EXE.

| Etap | 0.7.0 | 0.7.1 |
|---|---:|---:|
| Enqueue no-op poza rysunkiem |81,20 (job)|0,46 (bez job)|
| Enqueue wewnątrz otworu |nie mierzono|0,04 (bez job)|
| Przygotowanie/enqueue długiego gestu |78,92|0,88|
| Worker: test przecięcia |22,90|21,53|
| Worker: Exclude |77,63|72,45|
| Worker: serializacja |19,72|14,06|
| Worker: walidacja |7,38|5,54|
| Zastosowanie na UI |33,24|1,71|
| Zmiany Enabled gałęzi |8|0|
| Pełne UpdateUI przy operacji |obecne|0|

Ostatni Paint długiego przypadku6,22ms, maksymalna zmierzona przerwa timera UI30,01ms. To oddzielny pomiar pętli kontrolek i renderera. Dokładny boolean nadal kosztuje; cancel sprawdzany między kreskami, nie przerywa pojedynczego natywnego Exclude. Nie zadeklarowano usunięcia wszystkich opóźnień ani dowodu braku pojedynczej pośredniej klatki.

## Naprzemienny zoom

Naturalnego „zawieszonego” timera nie odtworzono w ograniczonej próbie. Potwierdzono ponowne rysowanie identycznych odsłoniętych pasów i pełne UpdateUI przy wheel. Dodano cache jednej przejściowej klatki oddzielony od exact; klucz obejmuje obie kamery. Powtarzany Paint nie wykonuje ponownie tej pracy. Łączny budżet obu bitmap nadal≤5,8mln pikseli +scratch256, dla większego viewportu fallback bez drugiej bitmapy. Dispose przy invalidacji/wyparciu. Brak danych autorytatywnych lub kolejki kamer w cache.

Wheel aktualizuje tylko zoomcaption i płótno. Baza kamery + całkowite kroki w tym samym punkcie eliminują dryf powrotu do0; ograniczenia zoomu nadal działają. Timer120ms kończy serię raz i zatrzymuje się. High/Low i finalna dokładna jakość zachowane.

Pomiar tej samej fixture, viewport1000×640,12naprzemiennych kamer .89/1, po5Paint (60łącznie): jednolity **266,25→74,87ms**, gradient **256,65→71,18ms**. Dwa preview builds i58hits łącznie; cztery exact builds (pierwszy+końcowy na tryb). Warm exact10 około10,5→10,8/10,6ms — brak deklaracji przyspieszenia każdego renderu. Realna pętla kontrolek:12wheel± w(500,300), baza1,137/pan30,2;32,7, bez dryfu,0UpdateUI, jeden final request, timer zatrzymany po300ms i brak dalszych exact builds do500ms.

## Testy metod / kontrolek / renderera

Nowy runner `Run-Patch071Tests.ps1` wymaga jednego jawnego Case; brak domyślnej kampanii. Nie uruchamiano Run-AllTests ani dawnych runnerów hurtowo. Źródła dawnych testów zachowane.

- `baseline`: pomiary wyżej i trzy przejścia kodu0.7.0; naturalnego crasha nie odtworzono w runnerze.
- `lifecycle`: końcowy PASS `a3455de2f1ef409c8a5e9cec9c351aaf`. Cztery przebiegi: normalny, injectedLoad, earlyClose, normalny; ready przed zamknięciem startu, plik/clean, rollback i kontrolowane wyjście pętli. Powtórzono tylko po wpływających zmianach lifecycle. Wczesna nieudana sonda injectedLoad ujawniła brak obsługi natywnego callbacku i została naprawiona; nie jest przedstawiana jako udana próba ani jako naturalny crash użytkownika.
- `argument`: końcowy PASS `1a22f42443034739b856f2775d614d9c`: ApplicationContext z mapą bez start.visible. Bez nowej kampanii skojarzeń.
- `new-file`: PASS `601be3b5d1174b3bb2498acc2526b311`: ta sama ChooseFile z podstawionym wynikiem dialogu, Cancel, writer/odczyt map11, ścieżka, odmowa overwrite, błąd zapisu bez podmiany starej mapy, późniejszy zapis.
- `ink`: PASS `c939c111e49443d4ab19e15101e9afea`: outside/hole no-op, małe/długie trafienie,0Enabled/0UpdateUI, strukturalny handoff preview przy zgodnym modelu, cancel/error/stale, jeden Undo/Redo, map11 round-trip.
- `ink-wait`: PASS `e01950230590481aaed12d60a392b8e7`: worker synchronizowany cancel (nie pomiar prędkości), brak paska przed150ms, ujawnienie po180ms, bez zmiany bounds/Enabled, blokada polecenia i rollback.
- `zoom`: PASS `23388e7504ac4961a4012fa04934eed1`: pomiary, odwracanie koła, brak dryfu, jeden final request i spoczynek timera.

Pełne prefiksy folderów `patch-071-<Case>-...` i szczegóły są w planie. Generator `fixture` tylko przygotował własne dane — nie jest testem zwykłego EXE. Zaliczone próby gumki/zoomu nie były powtarzane po niezależnych zmianach startu.

## Próby zwykłego finalnego EXE

Kopia w `tests/_work/patch-071-fixture-15900175ae144813af4caed1436be8a1`, hash zgodny z głównym EXE. Obsługa przez skill [Computer Use](C:/Users/admin/.codex/plugins/cache/openai-bundled/computer-use/26.1106.51428/skills/computer-use/SKILL.md), zgoda użytkownika przed wymazywaniem.

- Dwa niezależne zwykłe uruchomienia finalnej kopii → Nowy → własna nazwa → standardowy Save → działający edytor z plikiem, bez gwiazdki. Ctrl+S bez ponownego wyboru pliku.
- Cancel parametrów ze startu pozostawił start. Cancel początkowego Save z edytora pozostawił dotychczasowy projekt. Wcześniejsze Cancel Save ze startu także nie utworzyło pliku/wpisuMRU; ta wcześniejsza próba dotyczyła pierwszego kandydata.
- Stały start, wyłączony maximize, nagłówek i wycentrowane węższe recent widoczne.
- Własna „Próba gumki0.7.1”: średnica50, klik poza plamą bez dirty/widocznego wait, klik wycinający otwór, ponowny klik w otwór, długi prosty pas(800,270)→(1000,800). Jeden Ctrl+Z/Ctrl+Y usuwa/przywraca cały pas; otwór pod spodem pokazuje heksy.
- Sześć naprzemiennych scrolli±120 przy(800,500), około146%↔163%, następnie nowa kreska(450,125)→(1050,125), poprawny końcowy obraz i zapis.
- Własne mapy zapisano, własne okna zamknięto; końcowe inventory nie pokazało własnego procesu/okna. Nie odrzucano danych użytkownika.

To nie nagranie klatek. API jednego prostego drag i zrzuty po akcji nie dowodzą braku jednego mignięcia starego obrazu, pełnego braku flasha ani ciągłej płynności. Nie zaliczono wieloodcinkowego ręcznego gestu lub pomiaru FPS. Success/cancel/error/stale i liczniki są osobnymi testami kontrolek, a nie ręcznymi kliknięciami w EXE.

## Formaty, ograniczenia i odłożone próby

Map11/preset4 bez zmian; autorytatywne dane, dokładność, źródła i limity nie zostały podniesione. Brak migracji/usuwania danych. High/Low i12kolorów zachowane, bez powtarzania pełnego odbioru. Nie wykonano pełnej regresji, audytów WPF/DPI, kampanii grup/bibliotek/przenośności, długiej losowej serii startów ani pełnego nagrania UI. Nie symulowano ręcznie awarii dysku w natywnym Save finalnego EXE — błąd sprawdzono w metodach wspólnego writera. Bez ciągłego PPM, bo zakres nie zmienia wskaźnika.

Aktualizator0.13.0 ma dopisany wyłącznie backlog: kontrola w tle na starcie, offline bez blokady, informacja i pominięcie, ręczne sprawdzenie, bez automatycznej instalacji/wymuszenia welcome dla argumentu, zachowanie danych/MRU. Brak połączeń aktualizacyjnych. Backlogi0.8+ i zamknięte zgłoszenia bez rozszerzania.

## Krótka checklista odbiorcza użytkownika

- [ ] Kilka zwykłych startów → Nowy → zapis → edytor bez cichego zakończenia; w razie nawrotu zanotować ostatnią widoczną fazę.
- [ ] Cancel parametrów i Save ze startu zostawia start; z edytora zachowuje poprzedni dokument.
- [ ] Nowy ma wybrany plik, czysty tytuł i gotowe Ctrl+S; odmowa overwrite nie zmienia pliku.
- [ ] Start bez resize/maximize, główne przyciski bez zmniejszenia; recent około połowy szerokości z tooltipem.
- [ ] Gumka poza rysunkiem i wewnątrz pustego otworu: brak historii/dirty i zbędnego oczekiwania.
- [ ] Małe i długie rzeczywiste wymazanie: brak migania całych kontrolek i chwilowego powrotu starej plamy — ta ocena ruchu nadal wymaga ręcznego odbioru.
- [ ] Przy dłuższej operacji informacja/Anuluj bez przestawiania layoutu; konfliktujące polecenie daje odpowiedź, bez połowicznej zmiany.
- [ ] Jeden Undo/Redo przywraca dokładny pas i kolory; zapis/odczyt zachowuje otwór i obwiednię.
- [ ] Kilka naprzemiennych zoomów, postój, potem pan/nowa kreska: poprawny finalny obraz i brak pozostającego obciążenia.
- [ ] High/Low i historia kolorów pozostają jak w zaakceptowanym0.7.0; bez nowej pełnej kampanii.

## Git

Kod/EXE/testy/plan: **092c584**. Następny lokalny commit obejmuje README, specyfikację, plan wersji, końcowy raport i aktualizację punktu wznowienia. Przed nim pozostały wyłącznie te własne zmiany dokumentacji; prywatny plik MRU i tests/_work są ignorowane i zachowane. Końcowy odczyt statusu i hash commitu dokumentacji podaje podsumowanie zadania. Brak publikacji.
