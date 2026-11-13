# Plan HexMapEditor 0.7.1

## Baza i granice

HEAD03d4032, kod7bf009e potwierdzone. Zastany nieśledzony recent-projects.json pozostaje nietknięty (nie czytać prywatnych ścieżek, nie commitować); reszta Git czysta. Brak AGENTS.md w repozytorium. Odczytano raport0.7.0, spec40 i ścieżki Start/InkCommit/kamery/zapisu. Tylko tests/_work, bez prywatnych map/Library, szerokiego sprzątania i dawnych kampanii.

## Etapy

1. Bounded baseline: cykl ApplicationContext/ShowDialog/Show/Close i krótka seria resize/maximize na0.7.0; diagnostyka zdarzeń bez ścieżek danych. Niezależnie sprawdzić własność pętli i rollback błędnego Show. Naprawić handoff, stały start i recent~50%.
2. Wspólna transakcja Nowa: parametry→standardowy dialog zapisu→istniejący walidator/atomowy writer→podmiana dokumentu/udane pokazanie. Cancel/błąd zachowuje start lub wcześniejszy dokument. MRU po sukcesie.
3. Gumka: odtworzyć no-op/błyskanie i koszty prepare/worker/apply/paint. Tani hit-test faktycznej powierzchni podczas gestu (otwory także), brak zbędnego wait. Wąska logiczna blokada poleceń bez disabled całych drzew; opóźniony/anulowalny wskaźnik, dokładny preview zachowany do atomowego handoff. Bez nowego formatu/limitów.
4. Naprzemienny zoom: kontrola cache/timera i liczników exact/preview/idle na jednej fixture. Poprawka tylko potwierdzonego nadmiaru, zachowane High/Low i dokładny final.
5. Jawne pojedyncze Case nowych ścieżek, finalny EXE/hash i krótki zwykły UI z wymaganymi zgodami. README/spec/roadmap/backlog aktualizatora, raport/commity/Git.

## Ustalenia wstępne — jeszcze nie diagnoza crasha

StartupContext przypisuje MainForm do edytora przed Show i nie sprawdza jego skutecznego pokazania przed Close(start). Przy błędzie po przypisaniu brak rollback właściciela pętli. To konkretna luka do zabezpieczenia, nie dowód przyczyny sporadycznego zgłoszenia. StartWindow obecnie resize/maximize i pełne recent.

InkCommit bezwarunkowo tworzy worker, wyłącza całe canvas/shell/left/right, od razu pokazuje pasek, na końcu usuwa Preview przed aktualizacją Ink i wywołuje szerokie UpdateUI także przy no-op. To potwierdzone operacje kodu; obraz/pomiar do odtworzenia. ZoomPreview dorysowuje odsłonięte pasy od oryginalnej kamery cache przy każdym Paint, a wheel wywołuje pełne UpdateUI; faktyczny koszt/stan do pomiaru.

## Punkt wznowienia

Wykonane odczyty i plan. Pozostałe wszystkie etapy implementacji/testów. Ostatnia niepewna operacja: brak. Własnych procesów/dialogów0.7.1 nie uruchamiano; przed UI inventory. Nie oznaczono aplikacji ani EXE jako0.7.1.

## Etap 1 — lifecycle i Nowa z plikiem

Baseline0.7.0 `patch-071-baseline-9794bad223e14b16ba85ac1ee7960200` zakończony. Trzy rzeczywiste ChooseNew/NewMapDialog: normalny/startMax/start800×600; wszystkie doszły do edytora, brak spontanicznego crasha. Resize/idle nie uznane za przyczynę. Baseline gumki:16×180,width35,100próbek42; no-op outside enqueue81,20ms,pendingtrue,8zmianEnabled,apply46,03ms; real enqueue78,92ms,workerContains22,90/Exclude77,63/serialize19,72/validate7,38,apply33,24ms. Zoom60previewPaints(12naprzemiennych ×5render):solid266,25ms/gradient256,65ms;warmExact10~10,5ms. To metody/pętla komunikatów, nie zwykły EXE.

Zmiana lifecycle: MainForm pozostaje startem do skutecznego zakończenia Load/Shown i weryfikacji Visible/Handle; dopiero potem edytor przejmuje pętlę i zamykany jest start. Błąd/earlyClose przywraca start, kandydat Dispose, guardtransition. Lokalna diagnostyka128zdarzeń, opcjonalny HEXMAPEDITOR_START_TRACE do wskazanego pliku(max64KiB), bez tytułów/ścieżekmap/treści/komunikatówexception. Są start/ready/closed/handleDestroyed/rollback/loop.exit i typ/HResult wyjątku.

Pierwsza próba wstrzykniętego wyjątkuLoad pokazała, że nie wystarczy try/catch wokół Show — natywny callback WinForms z ThrowException zakończył proces. To wymuszona próba błędu, NIE reprodukcja historycznego crasha użytkownika. Dodano lokalne OnLoad/OnShown przechwytujące błąd tylko przy aktywnym handoff oraz przejściowy handler ThreadException wyłącznie podczas otwierania kandydata; po sukcesie/rollback/exit jest odpinany. Bez globalnego pustego catch/restartu aplikacji. Finalny `lifecycle` PASS `patch-071-lifecycle-4e53603d5ed94ba4aa6360a3e83782c6`:4przebiegi rzeczywistym NewMapDialog, udane pliki, injectedLoad i earlyClose zostawiają działający start, normalne zamknięcie dopiero po edytorze. Główny EXE nadal0.7.0.

Nowa: wspólne ProjectCreation→standardowy SaveFileDialog→wydzielony istniejący WriteProjectData/MapForSave/ValidateMap. Nie duplikowano serializerów. Parametry/kandydat nie podmieniają starej mapy przed skutecznym zapisem. Nadpisanie potwierdzane przedWrite, raceNewTarget nie nadpisuje pliku. Nowa start i edytor mają ścieżkę/clean/MRU poSave. Przenośne map11/preset4 bez zmiany. Start fixedSingle/noMax, logiczne650×550, viewportclamp+scroll dla małego ekranu, główne przyciski280 bez zmiany, recent324 (~50%) wycentrowane. MRUmodel bez zmiany. Zastany recent-projects.json nieczytany i nietknięty; dodano ignore pliku runtime, bez commitowania prywatnych ścieżek.

Ostatnia operacja: `new-file` uruchomiony w tej komórce; sprawdzić wynik wyżej przed ponawianiem. Własnych zwykłych okien nie uruchamiano; formularzetestowe lifecycle zakończone. Pozostałe gumka/no-op/UI/handoff, naprzemiennyzoom, finalnyEXE/UI/docs/commity. Nie powtarzać zaliczonego lifecycle bez zmiany tej ścieżki.

## Etap 2 — gumka i kamera

`new-file` PASS `patch-071-new-file-601be3b5d1174b3bb2498acc2526b311`:Cancel, shared writer/readmap11, ścieżka, odmowaOverwrite, brak połowicznej podmiany poSaveerror i późniejszyCtrlS.

Gumka ma szybki test kapsuły z rzeczywistą powierzchnią: surowe kreski przez odległości odcinków, powierzchnie z otworami przez cachedflattenedpath + point-in-fill/krawędzie. Flatten współdzieli dotychczasowy łączny budżet200000punktów InkPathCache (nie zwiększony); tolerancja konserwatywna taka jak istniejąca geometria. Nietrafione próbki przed pierwszym trafieniem nie budują WPFcuts; nie mogą zmienić żadnego rysunku. Worker pozostaje autorytatywny w pasie tolerancji. No-op outside i hole nie uruchamiają pending/workera/wait/history/dirty. Niezmieniony wynik worker kończy bez serializacji/walidacji całego dokumentu.

Nie wyłącza się żadnych głównych drzew Enabled. Wąski IMessageFilter blokuje edycyjne wejście tylko do tego okna (Anuluj/Escape/AltF4 wyjątki), RunModal/save/history i żywe teksty mają logiczne guardy. Ruch myszy nie nadpisuje statusu/odrysowuje stale w czasie pending. Wskaźnik oczekiwania overlaybezDock pokazuje się dopiero po180ms, timeranulowany po wyniku. Commit nie wywołuje pełnego UpdateUI — tylko caption/historiabuttony. Preview jest instalowany przed Capture, pozostaje podczas zastosowania, przechodzi na inkHandoff aż do pierwszego Paint dokładnego nowego wyniku; stary cache Clear dopiero z nowymi danymi. Cancel/error celowo przywraca oryginał. Reset/Dispose/ukrycie zwalniają hold.

`ink` PASS `patch-071-ink-c939c111e49443d4ab19e15101e9afea`:outside0,46ms/hole0,04ms enqueue,pendingfalse; mały hit2,82ms,apply2,59ms; długi0,88ms,workerContains21,53/Exclude72,45/serial14,06/validate5,54,apply1,71ms. 0branchEnabledChanges i0szerokichUpdateUI, paintostatni6,22ms dla długiego, maxUITimergap30,01ms. Success/cancel/syntheticerror/stale, strukturalny holdPreview do nowych danych, jedno Undo/Redo i map11roundtrip. Nie jest to dowód braku pojedynczej migającej klatki. Po tym Case dodano tylko bezpieczne pomijanie pustego klipu/zwolnienie hold przy hiddenInk i opisCancel na opóźnionym pasku; brak ponownej kampanii.

Kamera: baseline nie odtworzył stale aktywnego timera; potwierdzono koszt wielokrotnego renderowania tych samych odsłoniętych pasów w identycznym preview. Dodano oddzielny cacheJEDNEJprzejściowejklatki, nigdy nie traktowany jakoexact. Suma obu bitmap nadal≤5,8mln pikseli(+scratch256); dla większych widoków poprzedni fallbackbezdrugiejbitmapy. Wheel aktualizuje sam zoomcaption/płótno zamiast UpdateUI/panelu. Seria przy tym samym punkcie ma bazękamery i całkowity krok; odwrócenie do0 odtwarza dokładnie pan/scale bez floatdrift. Timer120ms zatrzymuje się po jednym końcowym żądaniu.

`zoom` PASS `patch-071-zoom-23388e7504ac4961a4012fa04934eed1`:na tej samej ograniczonej fixture16×180,width35,1000×640,12naprzemiennych .89/1 i5Paint/kamerę:solid266,25→74,87ms,gradient256,65→71,18ms; warmExact10~10,8/10,6ms. 2previewBuilds/58hits dla obu kolorów,4exactbuilds (pierwszy+finalna każdą). Rzeczywista pętla UI:12scroll± przypunkcie(500,300),baza1,137/pan30,2/32,7;0UpdateUI,brakdryfu,1finalrequest, po300ms timerstop i do500ms brak nowychExactbuilds. Nie deklarować historycznego źródła 'zawieszonego stanu' bez reprodukcji.

Ostatnia operacja: Case ink-wait uruchomiony w tej komórce — sprawdzić wynik wyżej przed ponowieniem. Pozostałe: ocena kodu krytycznych granic, wersja/buildkandydat/finalne zwykłeUI, dokumentacja/updaterbacklog, commity/Git. Własne zwykłe okna nieuruchomione, formularzetestowe zakończone. Główny EXE0.7.0, nie oznaczać jeszcze gotowym0.7.1.

## Etap 3 — przegląd granic i kandydat do UI

`ink-wait` PASS `patch-071-ink-wait-e01950230590481aaed12d60a392b8e7`: realna pętla WinForms, synchronizowany worker oczekujący na cancel (nie pomiar wydajności), brak pokazania paska przed150ms, ujawnienie po180ms, niezmienioneEnabled/bounds, odfiltrowanie konfliktującej komendy i rollback poAnuluj.

Wykryto ryzyko w nowym handoff dla argumentu: Application.Run może automatycznie pokazać MainForm=start przed queuedShown edytora. Dla uruchomienia z argumentem MainForm pozostaje null do gotowego edytora, a błąd przywraca start. `argument` PASS `patch-071-argument-b16fa0e2260e45c0b0ccd4b1dc4fddb8`: rzeczywista pętla ApplicationContext i brak zdarzenia start.visible. Nie wracamy do kampanii skojarzeń; to zależna kontrola zmiany lifecycle. Clampstartu przeniesiony zShown doLoad przed pierwszą klatką — lokalna próba flasha, nie dowód klatka-po-klatce. Catch problemów diagnostycznego pliku zawężony do IO/access/argument/unsupported/security; brak logu bez opt-in.

Przegląd zapisu przeniósł catch ChooseFile na całą lokalną ścieżkę, także EnsureDirectories/Show dialogu, aby odmowa uprawnień w edytorze nie wychodziła zButtonClick. Serializer/atomowy writer bez zmiany. `fixture` generator własnej mapy16×180 `patch-071-fixture-15900175ae144813af4caed1436be8a1`, nie próbaUI.

Kod zakresu zaimplementowany, wersja/AppFile0.7.1/0.7.1.0 ustawione do obowiązkowych prób. Rootbuild i kopia/hash w tej komórce — ustalić wynik powyżej przed ponawianiem. To kandydat, nie deklaracja ukończenia przed UI/raportem. InventoryComputerUse przed buildem: brak jakichkolwiek okienHexMapEditor; użytkownika niczego nie zamykano. Następne: zwykły EXE wfolderzefixture (Nowy→CancelSave→Nowy+Save/CtrlS, stałystart/recent, własna gumkaNoop+hit po zgodzie i naprzemiennyzoom), dokumentacja/backlogupdatera/commity. Brak ostatniej niepewnej operacji po potwierdzeniu build/hash. Nie powtarzać zaliczonych testów modeli.

## Etap 4 — zwykły EXE: nowe ustalenie (nie zamykać jako gotowe)

Kandydat root/kopia SHA256 FB7708DD827244B8A32420B08B5FE8BDDDDD261747DA02EEB2BE4BCE09758DA9. Anulowanie standardowego zapisu zostawiło start, zero pliku i bez nowego MRU. Stały start/recent~50% potwierdzone w oknie. Nowy→Zapisz zapisał poprawny plik, ale proces zakończył się przed pojawieniem edytora — próba NIEZALICZONA. Ponowienie z opt-in diagnostyką: otwarcie zapisanego recent działa, Ctrl+S bez dialogu i clean; zamknięto własny edytor. Następny Nowy→Zapisz z nazwą Nowy zapis 0.7.1 ponownie kończy proces. Log kończy się na transition.begin, bez editor.showReturned, wyjątku zarządzanego i loop.exit; to NIE kontrolowane zamknięcie startu. Nie ustalono jeszcze przyczyny. Dziennik Application Windows odczytany po zgodzie: brak zdarzeń błędu w tym przedziale. Potrzebne zawężenie etapów konstrukcji/Show i kodu wyjścia. To unieważnia deklarację ukończonej ścieżki realnego Nowy z wcześniejszego etapu, testy metod pozostają historycznymi faktycznymi wynikami.

Własne procesy obecnie zakończone (inventory brak okien i CIM brak HexMapEditor). Niepewna operacja: brak po potwierdzeniu trace/inventory. Pozostałe: krytyczna naprawa rzeczywistego Nowy, zależna krótka kontrola, UI gumki/zoom po zgodzie, docs/commity. Nie ponawiać zaliczonych testów gumki/zoom/metod bez zmiany ich kodu.

## Etap 5 — rzeczywista reprodukcja i lokalna poprawka startu

Awaria zwykłego winexe została odtworzona wielokrotnie na własnych danych, także po samym odroczeniu Open przez BeginInvoke. Kod wyjścia -1073740771 (0xC000041D), opt-in trace/AppDomain: InvalidOperationException w Dispatcher.WndProcHook podczas pierwszej inicjalizacji RichNoteEditor, komunikat „Przetwarzanie dyspozytora zostało wstrzymane, ale komunikaty są wciąż przetwarzane”. Konstruktor MainWindow nie dochodził do body; nie był to kontrolowany loop.exit ani zamknięcie startu po Show. To konkretny odtworzony błąd reentrancji natywnego callbacku/pierwszego hosta WPF; nie przypisujemy go resize/idle. Sonda konsolowa i pojedynczy przebieg z first-chance nie odtwarzały awarii — istotna różnica względem ordinary winexe, nie dowód naprawy.

Poprawka: jednorazowo przygotowany host opisu WPF przed Application.Run/WinForms modal callbacks, przekazywany do docelowego edytora (nie odbudowywany przy przełączaniu heksa). Nie przygotowuje dokumentu, Library ani pełnych źródeł. Sam handoff pozostaje kolejkowany/z guardem i start jest właścicielem pętli aż do gotowego Shown. Zachowane błędy Load/earlyClose z rollback. Przejściowe szczegółowe sondy native-exception/firstchance usunięto z kodu produkcji; pozostaje opt-in bounded trace bez treści/ścieżek map. Właściwa przyczyna konkretnego zagnieżdżonego komunikatu WPF pozostaje ograniczeniem diagnozy; stack i faza są potwierdzone.

Po zmianie hosta powtórzono WYŁĄCZNIE zależne lifecycle i argument: PASS lifecycle-a3455de2f1ef409c8a5e9cec9c351aaf (normal,Loaderror,earlyClose,normal), argument-1a22f42443034739b856f2775d614d9c. Gumka/zoom/metody niepowtarzane. Root i kopia finalcandidate przebudowane SHA256 A904EAEF3167D42CCFD4EF3019971E8E6C559375F180F59ED0100AC88ED808DB. Ordinary finalwinexe: Cancel parametrów zostawia start; Nowy→nazwa Finalny projekt 0.7.1→standardowy Zapisz→edytor widoczny. Dalsze CtrlS i gumka/zoom jeszcze oczekują. Własne okno edytora aktualnie otwarte na tej nowej czystej mapie. Niepewna operacja: brak po inventory. Następne: CtrlS, własna fixture gumki po zgodzie i zoom, docs/backlog/commity.

## Etap 6 — finalne zwykłe UI gumki/zoomu

Finalcandidate SHA256 A904EAEF3167D42CCFD4EF3019971E8E6C559375F180F59ED0100AC88ED808DB i FileVersion0.7.1.0 potwierdzone root/kopia. Ctrl+S po utworzeniu Finalny projekt 0.7.1 zapisuje bez dialogu, brak gwiazdki. Otworzono własną fixture Próba gumki 0.7.1 standardowym dialogiem, po zgodzie użytkownika wykonano: gumka50outside (clean/no widocznego wait), rzeczywisty klik otworu, ponowny klik tego otworu, długi prosty pas (800,270)→(1000,800), CtrlZ/CtrlY odtwarza cały pas jednym krokiem. Sześć naprzemiennych scrolli ±120 przy(800,500), około146%↔163%, następnie nowa kreska(450,125)→(1050,125) poprawna. Zapisano tylko własną fixture, zamknięto okno. Inventory puste dla własnej kopii.

To rzeczywista obsługa zwykłego EXE przez ComputerUse, nie pomiary FPS. API pojedynczego drag/obrazów po akcji nie daje nagrania handoff/frame-by-frame, dlatego brak dowodu nieobecności pojedynczej migającej klatki lub ciągłej płynności. Liczniki0UpdateUI/0EnabledChanges i holdPreview są oddzielnymi testami metod. Nie wywoływano Run-AllTests, dawnych kampanii, nie dotykano privateLibrary/rootMRU.

Pozostała jeszcze jedna krótka finalna próba cold Start→Nowy (ze względu na sporadyczny charakter i zmianę hosta), raport/spec/README/planwersji/commity. Aktualizator wyłącznie opis. Ostatnia niepewna operacja: brak. Własne procesy zamknięte; źródła od rootbuild bez zmian.

## Etap 7 — zamknięcie prób UI

Drugi niezależny start finalnego zwykłego EXE→Nowy→Ponowny start 0.7.1→standardowy Save→edytor także poprawny, clean ze ścieżką. Następnie Nowa z tego edytora→parametryOK→Cancel w standardowym Save zachował dokument Ponowny start 0.7.1 bez gwiazdki. Zamknięto własny edytor, inventory brak własnych okien. Dwa finalne coldprocess przejścia udane; nie jest to gwarancja wykluczenia wszystkich sporadycznych awarii. Nie powtarzano gumki/zoomu. Pliki prób zachowane w tests/_work dla odtwarzalności, brak sprzątania. Pozostałe tylko dokończenie spec/raportu i lokalne commity/Git. Ostatnia niepewna operacja: brak.

## Zakończenie 0.7.1

Wykonane wszystkie etapy uzgodnionego zakresu; finalny główny EXE0.7.1.0 zgodny bajtowo z kopią dwóch zwykłych prób Start→Nowy. Kod/EXE/testy/plan zapisane lokalnie w092c584. README/specsekcja41/planwersji i raport0.7.1 aktualizowane w końcowym commicie dokumentacji. Aktualizator opisany tylko jako backlog0.13.0. Map11/preset4 i budżety bez zmiany. Testy i ograniczenia rozdzielone w raporcie: nie zaliczono pełnej płynności, braku pojedynczej migającej klatki ani pełnej regresji. Nie powtarzano dawnych kampanii, nie sprzątano tests/_work/prywatnych danych. Własne procesy/dialogi zamknięte i inventory potwierdzone. Pozostałe: wyłącznie zapis końcowego commitu dokumentacji i odczyt Git/statusu; bez push/tagu/Release'a. Ostatnia niepewna operacja: brak.
