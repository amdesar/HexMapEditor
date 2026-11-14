# Plan 0.8.2 — integracja menu z tytułem

Baza potwierdzona: d3b7435 / 0c52b28. Git na początku czysty. Brak AGENTS.md w repozytorium; przeczytano raport 0.8.1 i bezpośrednie wejścia CommandBars/EditorShell/MainWindow. Nie szukamy wycofanego prototypu.

## Plan i decyzje
Aktualizacja użytkownika: wyłącznie hybryda. Własny pasek, alternatywna architektura i rollback są anulowane. W razie istotnej nierozwiązanej regresji zatrzymujemy pracę z notatką, bez publikowania prototypu.
1. Nowa hybryda DWM, standardowy overlapped HWND i natywne przyciski. Jedno źródło geometrii ramki; wcześnie rzeczywiste SC_MAXIMIZE/SC_RESTORE, dwa rozmiary, kilka cykli.
2. Istniejący MenuStrip/handlery oraz quickStrip. Ikona/nazwa aplikacji, bezpieczny obszar nazwy mapy i HTCAPTION, natywne resize/system menu. Bez zmiany dokumentu, dialogów i startu.
3. Celowane próby ramki/menu i zwykłej izolowanej kopii EXE. Dopiero bezpieczny wariant otrzyma wersję 0.8.2 i główny artefakt.
4. Dokumentacja/raport/commity. Bez push/tagu/Release'a i testów niezwiązanych funkcji.

## Wykonane / ustalenia
- Potwierdzono stan bazy; znany przypadek +31 px jest testem regresyjnym, nie podstawą korekty stałą.
- Obecny stos .NET Framework WinForms, DarkForm; rzeczywisty MenuStrip i ToolStrip w shellToolbar. Brak aktywnego starego eksperymentu.
- Źródło techniczne: Microsoft Custom Window Frame Using DWM. Należy uwzględnić zarówno NCCALCSIZE, jak i przeliczenia WinForms client/outer bounds; nie wystarczy przypisanie WindowState.

## Pozostałe
Aktualny stan znajduje się w ostatnim etapie poniżej; wcześniejsze sekcje są zapisem przebiegu.

## Próby / ostatnia operacja / procesy
Brak wykonanych testów. Ostatnia operacja: odczyt bazy i utworzenie tego planu; wynik pewny. Brak własnych uruchomionych procesów/dialogów. Główny EXE pozostaje 0.8.1.

## Etap 1 — geometria hybrydy
Nowa implementacja EditorCaption: standardowy HWND z WS_CAPTION/THICKFRAME; NCCALCSIZE zachowuje natywne boki/dół, usuwa wyłącznie standardowy obszar tytułu; DWM rysuje i obsługuje natywne przyciski. MenuStrip i quickStrip bez kopiowania handlerów.
Odtworzono 1440×940→1440×971 (ff504fc6). Trace: Windows odtwarza właściwy prostokąt, Form podczas WINDOWPOSCHANGED przelicza zapamiętany klient z WS_CAPTION. Pierwsza osłona zależna od Form.WindowState nie działała, bo ten stan był już zaktualizowany wcześniej (4415c98a). Własna obserwacja natywnego stanu podczas WINDOWPOSCHANGED wykrywa przejście; jedynie w jego zakresie SetBoundsCore zachowuje aktualny zewnętrzny prostokąt Windows, bez odejmowania stałej, bez prywatnych pól Frameworka. Dotyczy również pozycji przy przywróceniu przez przeciąganie — do próby UI.
geometry PASS f5ffe8b223b54814b82a43843a2c507c: 3 cykle SC_MAXIMIZE/SC_RESTORE przy 1440×940 (klient1424×932) oraz1220×780 (1204×772), pozycja90,70 niezmienna. Maksymalizacja zewnętrzna -8,-8,2576×1408, klient2560×1392. To test okna metodami, nie obsługa zwykłego EXE. Powtórzenia wynikały z wykrytej regresji i korekty. Pierwszy build dodatkowo ujawnił brak SystemInformation.PaddedBorderWidth w Framework; użyto GetSystemMetrics(SM_CXPADDEDBORDER).
Pozostałe: rozszerzone testy ramki/hit-test/menu, rzeczywiste UI, DPI/symulacja, dokumentacja i finalny build. Brak własnych procesów po runnerze; główny EXE nadal0.8.1. Ostatnia operacja: geometry PASS, wynik pewny.

## Etap 2 — rozszerzone testy i narzędzie UI
caption PASS6126164f: sześć prawdziwych menu; minimum1180×748 i1440×940; długa nazwa bez kolizji, krótka centralnie; HTCAPTION/menu/top/left/corner; systemowe zamknięcie respektuje Cancel niezapisanej mapy. geometry rozszerzone22386c7e: dodatkowo minimize/restore, WM_NCLBUTTONDBLCLK i dwa cykle po symulowanym Scale1.25; nie test monitora125%. Powtórzono geometry wyłącznie przy rozszerzeniu o te zależne przypadki. WPF/gumka/historie i inne niezależne obszary nie testowane.
Computer Use początkowo GPU0x887A0005, retry nie pomógł, klik bez screenshotu zgłaszał brak geometrii. Użytkownik zamknął własną kopię testową wraz z Open dialogiem. Po reset sesji node_repl przechwytywanie działa; nowa izolowana kopia patch-082-ui-next. Własna mapa otwarta z syntetycznego MRU; screenshot rzeczywiście pokazuje menu+natywne przyciski w pierwszym wierszu, quick po lewej w drugim, nazwa mapy centralnie. Zamknięto czysty prototyp AltF4; inventory brak własnych okien. Pozostałe rzeczywiste gesty wykonać na kandydacie wersji0.8.2, a główny EXE podmienić dopiero po powodzeniu. Ostatnia operacja pewna; główny nadal0.8.1.

## Etap 3 — natywne przyciski i zwykły kandydat
Odtworzono rzeczywiste nieskuteczne kliknięcie Przywróć: DwmDefWindowProc po maksymalizacji zwracał HTCLIENT. Celowany caption-af713 potwierdził 1 zamiast 9. Lokalna poprawka czyta WM_GETTITLEBARINFOEX i używa rzeczywistych ekranowych prostokątów natywnych przycisków do HTMINBUTTON/HTMAXBUTTON/HTCLOSE. Windows nadal rysuje i wykonuje przyciski. caption-32d0e069b93d47cb968cddadeb36519c PASS po tej zmianie (normal=9, maximized=9). Nie było kolejnej architektury.

Zbudowany zwykły kandydat `tests/_work/patch-082-final2/HexMapEditor.exe`, FileVersion/ProductVersion0.8.2.0. Rzeczywiste Max→Przywróć zaliczone: widoczna ramka DWM1426×933 wraca do tego samego rozmiaru i pozycji(-1673,202). Odróżnić od zewnętrznego RECT1440×940 z testu Win32. Użytkownik potem zamknął okno; wznowiona ta sama kopia wyłącznie dla brakujących prób. Klik Plik, strzałka do Edycja, Escape, Alt+Spacja/Escape działają bez przeciągania menu. Natywne Min i ponowna aktywacja przywracają1426×933. Resize prawym bokiem osiąga1166px widocznej szerokości; lewy górny narożnik osiąga741px wysokości (zewnętrzne minimum1180×748). Menu/nazwa/przyciski nie kolidują. Drag nazwy przesuwa o dokładną deltę70×65px. Dwuklik maksymalizuje; drag z maksymalizacji przywraca1166×741. Jednoodcinkowe API nie potwierdza ciągłej trajektorii pod kursorem. Snapping niezaliczony; API nie pozwala kończyć drag poza granicami wyjściowego okna. Próby trafienia dolnej/prawej górnej krawędzi z widocznego screenshotu nie potwierdziły resize narożnika; zaliczony lewy górny oraz test Win32 dolnego prawego hit-testu.

Natywny Zamknij zamknął własny czysty final2; lista okien i Get-Process potwierdzają brak HexMapEditor. Nie zamykano okien użytkownika. Runner bez Case sprawdzony raz: czytelny błąd, bez builda/prób. Żadna zaliczona kampania niezwiązana nie została powtórzona. Pozostałe: publikacja tych samych przetestowanych bajtów głównego EXE, dokumentacja i lokalne commity. Ostatnia operacja pewna; główny EXE jeszcze0.8.1. Brak własnych otwartych procesów/dialogów.

## Zakończenie
Hybryda ukończona w uzgodnionym zakresie. Bez własnych przycisków i bez alternatywnego wariantu. Główny EXE0.8.2.0 atomowo podmieniony bajtami z final2; SHA-256 obu399222EB72C6C19869700DBC4DA929059464C5F76216B6DC4C81E67C40BBC957. Nie wykonano dodatkowego builda zmieniającego bajty po próbie UI. Wyłącznie końcowe puste linie metadanych uporządkowane, bez zmiany kodu/stałych.

Commit implementacji **511cdda**. README/specyfikacja/plan wydań/raport zaktualizowane; ograniczenia i checklista w raporcie. Pozostało wyłącznie zapisanie commita dokumentacji i sprawdzenie czystego Git, bez dalszych testów. Pierwszy zapis Git odrzucony przez sandbox (index.lock Permission denied); ponowienie z uprawnieniem zakończone powodzeniem. Nie było niepewnej operacji publikacji: obie sumy i FileVersion/ProductVersion sprawdzone. Brak własnych procesów/dialogów. Główny EXE jest finalnym0.8.2, nie wcześniejszą wersją ani prototypem. Bez push/tagu/Release'a.
