# HexMapEditor 0.8.1 — poprawki po odbiorze

Baza potwierdzona lokalnie: **42933cd** dokumentacja, **9ae0d8b** kod0.8.0; początkowy Git czysty. Data wydania14.11.2026 z jednego `ReleaseMetadata`. FileVersion/ProductVersion **0.8.1.0**. Map11/preset4, struktura Library i limity bez zmian. Nie wykonywano migracji, sprzątania prywatnych danych, push, tagu ani Release'a.

## Ramka — świadomy fallback

Wykonano jedną ograniczoną implementację według [Microsoft Custom Window Frame Using DWM](https://learn.microsoft.com/en-us/windows/win32/dwm/customframe) oraz [DwmDefWindowProc](https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/nf-dwmapi-dwmdefwindowproc): zwykły overlapped HWND, DWM, zachowane natywne przyciski, prawdziwy MenuStrip, HTCAPTION za Pomoc i na nazwie. Prototyp rzeczywiście pokazywał dwa wiersze i natywne przyciski. Nazwa miała pozycję środka całego okna, ograniczenie/ellipsis/ukrycie przy braku miejsca.

Próba rzeczywistych poleceń systemowych **SC_MAXIMIZE/SC_RESTORE** wykryła istotną regresję: okno **1440×940 → 1440×971** po przywróceniu. Sam test przypisania `WindowState` wcześniej jej nie ujawniał. To konflikt przeliczenia klienta/RestoreBounds WinForms z NCCALCSIZE; nie ustalono bezpiecznej pełnej integracji w uzgodnionej jednej próbie. Bez kolejnego reworku wycofano cały aktywny eksperyment (plik HybridCaption i wszystkie wywołania). Końcowy EXE zachowuje sprawną standardową ramkę0.8.0 i dwa własne wiersze pod nią. Nie oddajemy pozornej hybrydy.

**Nie wdrożono hybrydowego wiersza ani jego wizualnie centralnej nazwy.** To dozwolony wariant zakończenia z prompta. Rzeczywisty tytuł Windows/Alt+Tab pozostaje `HexMapEditor — Nazwa *`, z dirty i needsFirstSave. Nie dodano zastępczej nazwy w innym panelu. Natywne przyciski/resize/system menu pozostają standardowe. Fallback przeszedł test systemowego maximize/restore bez zmiany rozmiaru. Brak globalnego WS_EX_COMPOSITED, zmian WPF/runtime lub imitacji przycisków Windows.

## Zoom, styl i rename

Zoom ma stabilną szerokość z pomiaru `12800% ▾` bieżącym fontem +12px marginesu. Zakres/presety i zachowanie kamery bez zmian. Jedna trwała instancja dropdownu; zamknięcie AppClicked na tym samym przycisku zapamiętuje znacznik wiadomości, a Click tej sekwencji nie otwiera ponownie. Kolejne fizyczne MouseDown usuwa starą osłonę. Escape/outside i brak Dispose podczas dispatchu pozostają.

Niepożądaną zewnętrzną krawędź szybkiego paska rysował domyślny ProfessionalRenderer ToolStrip; użyto istniejącego FitButtonRenderer, który pomija OnRenderToolStripBorder. Nie dodawano maskującego panelu ani globalnego odrysowania. Zwykłe separatory poleceń zachowane.

Zapis dirty/needsFirstSave i aktywny Low używają **dokładnie ChoiceStyle** przełączników Jednolity/Gradient: tłoRGB38/65/96, ramka90/145/205, hover43/77/114, grubość2, WhiteSmoke. Clean/High neutralne. Usunięto warianty koral/turkus, a ikona Zapisz nie alokuje już tintowanej bitmapy przy zmianie stanu. Lewy przybornik zachowuje zaakceptowany wybór — dodatkowa ramka to0.9.

Rename: „Obecna nazwa:” i „Nowa nazwa:” wyrównane do prawej; dwukropki w tej samej kolumnie, pole pionowo dopasowane do wiersza. Oba wiersze bez jednego końcowego .hexmap, nowy tekst wstępnie aktualną nazwą. Istniejący PathForName nadal dodaje pojedyncze rozszerzenie, zachowuje inne kropki. Nie zmieniano move/writera/fingerprint/MRU/autosave ani semantyki dirty i historii.

## Wspólne liczby i historie

NumberTextBox dopuszcza cyfry, początkowy minus, jeden przecinek lub kropkę; Decimal numpada używa separatora kultury. Wklejka sprawdzana w całości na proponowanym tekście z uwzględnieniem zaznaczenia; `1a5` jest odrzucone, nie zamienione w15. Natywne skróty tekstowe/strzałki/Home/End/Delete zachowane. Poprawna wklejka korzysta z [EM_REPLACESEL z undo=TRUE](https://learn.microsoft.com/en-us/windows/win32/controls/em-replacesel), ponieważ SelectedText czyścił bufor natywnego Undo; bezpośrednia asercja CanUndo i przywrócenia tekstu wykryła i potwierdziła tę poprawkę. Bez liter/wykładnika/grupowania.

Podczas wpisu wartość narzędzia/draftu nie zmienia się z każdą cyfrą. Enter, opuszczenie, neutralny klik i dezaktywacja kończą tekst: parsowanie → precyzja z połówkami od zera → clamp. Przykłady:3,5→4, -3,5→-4, RGB-5→0,360→255, kolumny205→200. Bardzo długa poprawna liczba ograniczana według znaku bez overflow. Pusty/minus/końcowy separator przywracają poprzednią zatwierdzoną wartość; Escape przywraca początek. Liczby nadal po prawej.

Lokalny IMessageFilter aplikacji zatwierdza wpis po kliknięciu panelu/etykiety, nie połyka kliknięcia i nie jest hookiem systemowym. Nie zatwierdza całego dialogu. Anuluj/zamknięcie odrzuca draft rodzica; scrub nadal anuluje po utracie capture. Zakończenie tekstu oddaje fokus rodzicowi zamiast ukrytemu TextBox; główny handler rozpoznaje też zakończony ScrubNumber przed natywnym tekstowym Undo. Zachowano canvas.Focus przed inicjalizacją gestu.

Zakres wspólnego mechanizmu bez osobnych kopii: wszystkie istniejące ScrubNumber z0.8.0 (Nowa/Resize/TableSize, rozmiary narzędzi, instancji i grup, promienie mgły/widzenia, RGB i custom zoom); minima, maksima, kroki/precyzje bez zmian. Slidery graficzne, HEX, zwykły tekst, WPF i systemowe Open/Save nie są zmieniane w liczby.

**Globalny stos:** sesyjne sizePicker/heightPicker1–15 i inkSize1–200(default5) zapisują ToolNumbers w dotychczasowym EditAction. Jeden zakończony drag/wpis jeden krok, Cancel/no-op żaden, nowa akcja usuwa Redo jak pozostałe. Undo/Redo odświeża dane narzędzia i pola w poprawnej chronologii z dokumentem. Akcja ustawień omija dokumentową rewizję i dirty; nie ustawia dirty=false i nie kasuje wcześniejszych zmian. Brak serializacji ustawień, historii zoomu/pan/jakości/wyboru narzędzia. Dotychczasowe dokumentowe LiveNumber i rollback pozostają.

**Historia koloru:**100 lokalnych kroków na sesję całego dialogu, nie tylko natywny TextBox. RGB/HEX/HSV i klik próbki sesji; gest graficzny jeden krok, zatwierdzony wpis jeden krok. Po zakończeniu liczby CtrlZ/Y działa poza polem, nie propaguje do mapy. Natywne tekstowe Undo pozostaje podczas niezatwierdzonego tekstu. No-op/cancel bez wpisu, zmiana po Undo usuwa Redo. Osadzony PaintPicker utrzymuje spójny snapshot koloru/trybu/punktów/kąta, aby cofnięcie koloru nie trafiało w inny punkt gradientu. Bez kopiowania obrazów. OK zachowuje dotychczasowy jeden commit rodzica, Anuluj odrzuca, ponowne otwarcie nowa historia. Operacje biblioteczne nadal odrębne i konflikt-safe.

## Switche — inwentaryzacja i granice

| Miejsce | Zamienione binarne opcje |
|---|---|
| Edytor puli terenu/mgły/ograniczenia widoczności | Zastosuj do mapy, Zachowaj rozmieszczenie wariantów |
| Przybornik | Wypełnienie |
| Instancja obiektu | Blokada ruchu, Odkrywa mgłę, Pole widzenia |
| Grupa | Nadpisz promień mgły, Nadpisz promień widzenia, Ramka członków |
| Definicja stanu | Pozwala odkrywać mgłę, Zaktualizuj definicję na mapie |
| Odłączanie z grupy | Zachowaj wynikowe: Stan, Rozmiar, Blokada ruchu, Odkrywanie, Promień mgły, Pole widzenia, Promień widzenia |
| Wzorzec ikony/obiektu | Pole widzenia nowych instancji, Zaktualizuj wystąpienia na mapie |
| Usuwanie stanu | Usuń również z biblioteki |

Jeden SwitchBox dziedziczy semantykę CheckBox: checked, zdarzenia, Tab/Space, rola dostępności, klik napisu. Tor/knob wysokości tekstu, On używa ChoiceStyle. Off ma normalny tekst, disabled przygaszony czytelny, On+disabled zachowuje rozpoznawalne włączenie. Zależność apply/preserve bez zmian. W rzeczywistym disabled wykryto natywne domalowywanie tła tekstu; końcowa kontrolka ma jawny pełny paint i tekst GDI+.

**Wyjątki trójstanowe: brak** w miejscach zamiany; nie prowadzono dodatkowego audytu. Radio/ComboBox/menu checkmarks/systemowe dialogi/WPF bez zamiany. Jasne próbki/grafiki i natywne Open/Save/MessageBox pozostają zgodnie ze wcześniejszym zakresem; bez nowej kampanii motywu.

## Stempel poza mapą

Istniejący ObjectGeometry.Nearest wylicza nieograniczony offsetowy środek. BrushGeometry przyjmuje Point, tworzy pełny kształt/obrys, potem przecina z istniejącymi komórkami; indeksowe wejście nadal deleguje do wspólnego algorytmu. Nie przyklejamy do ostatniego heksa i nie tworzymy nowych. Preview i malowanie korzystają z StampAt; invalidacja działa również gdy środek poza mapą zmienia się przy niezmiennym hit=-1.

Zakres: Brush terenów i ikon, FogBrush, DarkBrush, wszystkie istniejące kształty/fill/outline. Start i kontynuacja poza mapą w płótnie; jeden gest ta sama transakcja. Dla maski/ikony bez środkowego heksa kierunek nakładania/usuwania ustala pierwszy stabilny trafiony heks. Bucket/FogBucket/DarkBucket nadal wymagają startowego heksa; swobodny obiekt pozostaje pojedynczy. Wybór terenu albo ikony z DarkBrush/DarkBucket przełącza do Brush jak Fog.

## Weryfikacja selektywna — metody i kontrolki

Runner `Run-Patch081Tests.ps1` wymaga jednego jawnego Case. Osłona bez Case sprawdzona raz: odmowa bez builda i uruchamiania prób. Nie wykonano Run-AllTests ani historycznej kampanii.

- **fields PASS**: leksyka, odrzucenie całej wklejki, pl-PL Decimal, dwa separatory, rounding/clamp/overflow/puste; neutralny klik, Escape/capture; globalny tool-history bez dirty/rewizji i przeplatanie z dokumentem; lokalne RGB/HEX/HSV i osadzony picker, gałąź Redo, nowa sesja. Ostatni wynik: `patch-081-fields-df97415c70d9455795a345449385f352`.
- **stamp PASS**: bok/róg, koło/prostokąt/heksagon, wypełnienie/obrys, obie orientacje; produkcyjne CanvasMouseDown/Up terrain/icon/Fog/Dark, zgodność listy z preview i Undo, wybór ikony/terenu z narzędzi widzenia. Wynik `patch-081-stamp-bff9d2175cb846cc99b3befea3604393`.
- **controls PASS**: On/disabled paint i klik switcha, rename układ/pojedyncze rozszerzenie, zoom open/close/reopen bez Dispose, sześć menu, dokładny ChoiceStyle; końcowa standardowa ramka przechodzi systemowe maximize/restore. Wynik `patch-081-controls-4898c8b96ac04b8fb762a7b38910a5dc`.

Bez uruchamiania dawnego runnera dostosowano wyłącznie oczekiwania Patch080 dotyczące zmienionej normalizacji liczb i niebieskich akcentów; źródła testów zachowane.

Powody powtórzeń zapisane w planie: błędna ścieżka fixture z końcowym separatorem; geometria hybrydy/RestoreBounds; disabled paint; realnie znaleziony fokus ukrytego pola. Po niezależnych zmianach nie ponawiano zaliczonych prób. Próby metodyczne nie są klikaniem w zwykły EXE ani dowodem płynności.

## Zwykły EXE — sesja i końcowa weryfikacja

Kopia tylko `tests/_work/patch-081-ui-00f59a0f4a53409fa82cd3272275eb0d/HexMapEditor.exe`; mapy/Library/MRU wyłącznie tam. SHA256 głównego i końcowej kopii: **F3B31AECE259E5318813EB74CA2476EAD1FA53DCA710A6AABFC6016F69F433F3**. Przed końcową podmianą sprawdzono wynik nieudanej operacji File.Replace w piaskownicy: poprzedni EXE zachowany, brak procesu root; build po wymaganym uprawnieniu powiódł się. Żadnego okna użytkownika nie zamykano.

W jednej pogrupowanej sesji kopiiCE4DD… wykonano faktyczne czynności: start/recent→edytor, standardowa ramka i sześć menu; globalny inkSize6→CtrlZ5→CtrlY6 bez gwiazdki; roboczy RGB255→80+Enter, neutralne tło→CtrlZ255→CtrlY80, Anuluj; koło5 ze środkiem poza lewym bokiem siatki pomalowało6 istniejących pól z odpowiadającym preview, dirty i niebieskim Zapisz; rename wizualnie obie nazwy bez rozszerzenia i wyrównane etykiety, Anuluj bez malowania pod dialogiem; Low ten sam niebieski styl, CtrlS zapisał własną mapę i zgasił gwiazdkę/akcent Zapisz; zoom otwarto i zamknięto drugim kliknięciem, bez wyjątku/ponownego otwarcia. Switch Off/On/Space i disabled sprawdzono w poprzedniej kopii tego samego niezmienionego modułu; końcowy paint jest czytelny bez czarnego fragmentu. UIA bywało opóźnione względem screenshotu i źle wyliczało współrzędne elementu na monitorze z ujemną pozycją; po błędzie wykonywano świeży odczyt, używano właściwego okna modalnego i współrzędnych z jego screenshotu.

Po tej sesji jedyną zmianą produkcyjną był EM_REPLACESEL zachowujący natywne Undo poprawnej wklejki. Bez wpływu na stempel/rename/zoom/switche nie powtarzano ich całej sesji (zgodnie z zakazem nieuzasadnionych powtórzeń); końcowa kopiaF3B31… jest bajtowo identyczna z głównym EXE. W końcowej kopiiF3B31… wykonano Nowa ze startu wyłącznie do otwarcia draftu: w polu kolumn skopiowano15, wpisano9, CtrlA/CtrlV przywróciło15, natywne CtrlZ przywróciło9. Następnie kliknięto Anuluj przy aktywnym wpisie: wrócił start, bez pliku i nowego wpisu recent. Własne okno zamknięto, inventory potwierdziło[]. Brak deklaracji płynności lub pojedynczych klatek na podstawie screenshotu.

## Ograniczenia i checklista ręczna

Nie deklarujemy pełnej regresji, płynności ze screenshotów ani testu fizycznych monitorów/DPI. Nie ponawiano starego5min autosave, pełnego rename/conflict, WPF/gumki/renderera/bibliotek/masek ani przenośności. Hybryda jawnie wycofana; brak nowego testu snapping w niej. Zachowane zaakceptowane semantyki0.8.0.

- [ ] Standardowa ramka: minimize/maximize/restore, resize, Alt+Spacja; sześć menu i szybki pasek pozostają.
- [ ] Zoom: węższy stabilny przycisk; pierwszy klik otwiera, drugi zamyka, Escape/outside; brak prawej niezamierzonej kreski.
- [ ] Dirty/needsFirstSave i Low ten sam niebieski, clean/High neutralne, gwiazdka bez zmian.
- [ ] Rename: etykiety/dwukropki, oba teksty bez rozszerzenia, nazwy z innymi kropkami.
- [ ] Liczba: przecinek/kropka/Decimal, rounding/clamp, neutralny klik, Escape, poprawna wklejka i odrzucenie niepoprawnej.
- [ ] Globalny rozmiar/szerokość/wysokość/rysik Undo/Redo bez dirty, chronologia z mapą.
- [ ] Lokalny RGB/HEX/HSV Undo poza aktywnym polem, Redo, Anuluj i nowa sesja; bez globalnego kroku na każdą liczbę.
- [ ] Switch: napis/Space/Tab, Off/On/disabled/On+disabled i zależność apply/preserve.
- [ ] Teren/ikona/Fog/widzenie: stempel poza bokiem/rogiem, preview i wynik; bucket nadal start z heksa.
- [ ] Wybór terenu/ikony z obu narzędzi widzenia włącza pędzel.

Backlog zapisany wyłącznie dokumentacyjnie:0.9 ramka narzędzia;0.11 lokalizacja zakładek do oceny;0.12 źródła/kolejka i docelowe foldery menu; po1.0 alternatywne skróty/kontrola konfliktów. Poprzednie backlogi bez implementacji.

## Commity i stan Git

- **0c52b28** — implementacja, źródła/testy i główny EXE0.8.1.
- Dokumentacja — osobny lokalny commit zawierający ten raport, README, specyfikację, plan i backlog; jego identyfikator podany w końcowej odpowiedzi oraz git log.
- Końcowy Git czysty po obu commitach; tylko ignorowane własne dane prób w tests/_work. Własne procesy/okna zamknięte i potwierdzone[]. Bez push, tagu, Release'a i przepisywania historii.
