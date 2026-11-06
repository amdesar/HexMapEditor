# Raport HexMapEditor 0.2.2

Stan: 6 listopada 2026. Baza `fd23a23947217e166074f1834f7b89d3746be6a8`, gałąź master, origin sprawdzony i pobrany; baza była zgodna z origin/master. Kontynuowano zastaną pracę 0.2.2, także niezacommitowaną. Nie przywracano 0.2.1. Bez push, Release'a, resetu ani przepisywania historii.

## Ukończone zachowania

- Własne presety mają po jednym aktywnym pliku w `Library/Terrains/Presets/`. Odczyt jest nierekurencyjny; ręcznie dostarczony poprawny plik pojawia się po restarcie. Walidowane są wymagane/nieobsługiwane pola, wersja, ID/nazwy i definicje terenów. Kolizje są zgłaszane, nie nadpisywane po cichu. Plik niepoprawny pozostaje na dysku; konserwatywnie blokuje też zapis/migrację, jeśli mógłby ukrywać kolizję. Nie jest to ogólne wyciszenie ostrzeżeń.
- Nowy zestaw pyta o nazwę i skutecznie zapisuje jeden neutralny preset przed dodaniem/wybraniem wpisu. Późniejsze zmiany są robocze. Pierwszy zapis starego niezapisanego wpisu zastępuje ten wpis, a świadomy zapis kopii już zapisanego zestawu dostaje nowe ID. Błąd/kolizja/anulowanie nie oznaczają danych jako zapisanych.
- Usuń preset chroni Domyślny, podaje nazwę i skutki, uwzględnia zmiany robocze i zleca Kosz Windows. Nie ma aplikacyjnego `File.Delete` jako zastępstwa dla Kosza. Błąd zachowuje spójny stan; Undo/Redo operuje tylko paletą projektu. Odtworzony zestaw bez pliku jest niezapisany.
- Paleta ma jedną kolumnę i wiersze 182×42 px. Nazwa jest na kolorowym przycisku 126 px, obok są stałe kolumny ołówka i kosza; akcje nie zasłaniają nazwy, nie wybierają innego terenu. Długie nazwy mają wielokropek i tooltip. Zachowano F2/Delete, dostępność, stały Dodaj oraz istniejące kontrolki przy niezwiązanych odświeżeniach.
- Zwężanie prawej nakładki natychmiast odrysowuje odsłonięty pas płótna; bez opóźnienia, czekania do puszczenia lub odtwarzania edytora. Kamera, dane, historia, uchwyt/reset i dopasowanie tabel pozostają.
- Przycisk pokazuje Osie / W heksach / Brak, ma 110 px i pełne podpowiedzi. Etykiety wewnętrzne są mierzone bez zawijania i rysowane jako całe identyfikatory. Poniżej 80%, przy czcionce mniejszej niż 7,5 px lub wyjściu części napisu poza widok ukrywana jest całość. Nie zmieniono logicznych współrzędnych.
- Rozmiar ma przestrzenny układ czterech pól wokół lokalnego schematu GDI+, legendę i wynik. Zachowano atomowy plan, stare brzegi, walidację 1–200, ID, treść, parzystości i jedno Undo/Redo.
- Ostrzeżenie dzieli heksy z danymi na teren / tekst / teren i tekst. Grupy są rozłączne, mają liczby i pełne przewijane współrzędne sprzed operacji. Porównanie terenu obejmuje pełny kanoniczny stan neutralny, także ID/wersję, a tekst nazwę i obie reprezentacje opisu. Maski rodzaju danych umożliwiają przyszłe kombinacje, bez pustych sekcji obiektów teraz. Domyślne Nie pozostaje.
- Menu opisu ma Wklej jako zwykły tekst, korzystające z istniejącego `Paste(true)`, wyłącznie Unicode/Text. Jedna operacja historii opisu, bez historii mapy. Opcja jest nieaktywna bez tekstu; brak tekstu nie kasuje zaznaczenia. Wytnij/Kopiuj/Wklej i formatowany RTF pozostają.
- Specyfikacja zapisuje wieloprojektowe zakładki jako przyszłe wymaganie, bez wdrożenia ani przypisania wydania.

Format mapy pozostaje **2**, odczyt **1**, format presetu **1**. Nie dodano biblioteki, płatnej licencji ani usługi. Kosz korzysta z lokalnego `IFileOperation` powłoki Windows (Windows 8+). Flagi RECYCLEONDELETE/ADDUNDORECORD i WANTNUKEWARNING wymagają odzyskiwalnej ścieżki albo ostrzeżenia powłoki przy jej braku; nie deklarujemy testu nieosiągalnego Kosza lub dysku sieciowego. Opis flag: [Microsoft IFileOperation::SetOperationFlags](https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nf-shobjidl_core-ifileoperation-setoperationflags).

## Przyczyny potwierdzone w kodzie i diagnostyce

**Powracający komunikat presetu:** rzeczywisty plik `Library/Terrains/37a8eec1ce19687d132fe29051dca629d164e2c4958ba141d5f4133a33f0688f.terrainset.json` miał ID `default`, ale sześć definicji z rzeczywistą zmianą Czerń / Pustka: revision 4, kolor `#2C2C2C` zamiast wzorcowego `#252735`, wariant `9aaf3689339f4b21be11558406730549`. Odczyt 0.2.1 chronił wzorzec przez pominięcie pliku, pozostawiał go aktywnym na dysku i zgłaszał go ponownie przy każdym starcie. Plik jest zgodny z dawnym zapisem roboczego Domyślnego, nie z niezmienioną kopią wzorca. Nie ustalamy autora/daty historycznego zapisu wyłącznie z zawartości.

**Podwojenie nowego zestawu:** poprzedni Nowy zestaw tworzył tylko roboczy wpis, a zapis jako nowy zawsze dodawał kolejne ID. Teraz nowy zapis poprzedza wpis; dla istniejącego niezapisanego ID pierwszy zapis nie dodaje kolejnej pozycji.

**Smugi:** diagnostyczne kopie kodu logowały początek/koniec przechwyconego gestu, zmiany szerokości i `PaintMap`. Przed poprawką ruchy zmieniały bounds nakładki, ale pomiędzy BEGIN a END nie występował Paint płótna, także z tabelą. To pozostawiało stary bufor odsłanianego płótna podczas ruchu sąsiednich okien potomnych WinForms/ElementHost. Nie przypisujemy winy samemu WPF bez dowodu. Po poprawce zwężenie 768 → 378 logowało `PAINT {X=656,Y=0,Width=390,Height=832}` **przed END**. `SetRightWidth` unieważnia tylko odkryty pas i wywołuje `canvas.Update()`. Rysowanie pomija heksy poza obszarem Paint, aby mały pas nie oznaczał rysowania całej dużej mapy. Nie odtwarzamy dokumentu WPF.

**Obcięte współrzędne:** poprzedni `DrawString` miał sztywny prostokąt 30×12 pomnożony przez zoom i czcionkę z minimum 6 punktów; brakowało pomiaru pełnego identyfikatora i wyłączenia zawijania. Przy dłuższych znakach/małym polu układ tekstu obcinał lub zawijał część napisu. Nowy `CellLabels` mierzy całość w pikselach, mieści ją w prostokącie wewnątrz obu heksów i ukrywa cały nieczytelny napis. Przycisk miał tylko 88 px przy ikonie i tekście; poszerzono go bez zmiany miejsca nazwy projektu.

## Wykonana lokalna migracja — nie test na prywatnych danych

Po syntetycznych regresjach wykonano jednorazowo dozwoloną migrację czterech prawdziwych plików. Nie używano ich w testach, nie zmieniano map ani żadnego wariantu terenów w mapie. Trzy własne pliki przeniesiono bajt w bajt. Dwie podobne, ale odrębne własne pozycje zachowano; nie uznano ich samowolnie za zbędne.

Kolidujący zestaw odzyskano pod nazwą **Domyślny — odzyskany**, z własnym deterministycznym ID wyprowadzonym z zawartości definicji, w pliku:

`C:\ALTERNATYWNA PARTYCJA\HexMapEditor-Codex\Library\Terrains\Presets\6c3ba270f60e4e3d0c7a3af77365c5f44b6590092d1101bc37391074981d9987.terrainset.json`

Kopie bajtowe znajdują się wyłącznie lokalnie w:

`C:\ALTERNATYWNA PARTYCJA\HexMapEditor-Codex\Library\Terrains\Presets\_backups\`

| Dokładna nazwa kopii w powyższym katalogu | Rozmiar | SHA-256 oryginalnych bajtów |
| --- | ---: | --- |
| `37a8eec1ce19687d132fe29051dca629d164e2c4958ba141d5f4133a33f0688f.terrainset.json.a80be5ee6f6eda80.bak` | 882 | `A80BE5EE6F6EDA80FB24F2D3A23400B3FA43A37DDFCF1201AE3D4DB556D7DD05` |
| `6481f5b008e7d4775afeff1b4166947914f6baa0013bb5b9df43f9a972fb9a87.terrainset.json.e6293b0e0d81bc86.bak` | 576 | `E6293B0E0D81BC8687DD10364786B269755F86D4BAF37070BE056C876D979A9D` |
| `69615ac36773e9ed374fab1f7a44f691a10199a18cebcfbf3613d6d8b55ea07f.terrainset.json.f4f18a3140a784c6.bak` | 566 | `F4F18A3140A784C68432978A22219D7BE54C80201BED9BFC1380AE12001417D4` |
| `ce08837a23afb46fa2edc2499ec42aa57a6ff54e8a0e7b376590202990a1d1e7.terrainset.json.57706b7cd4009335.bak` | 251 | `57706B7CD40093353E30F537C591025E1649F10DABBE65F8A9DDB07C6B32C505` |

Pierwszy przebieg: `Migrated=4; loaded=5; warning=`. Ponowny odczyt/start mechanizmu: `Migrated=0; loaded=5; warning=`. Cztery aktywne własne pliki i cztery kopie, bez ponowienia migracji po wznowieniu pracy. Stare źródła przestały być aktywne dopiero po sprawdzeniu kopii/nowego celu; oryginalne bajty pozostały w `_backups`. Kopie i presety nie trafiają do Git. To nie była interaktywna próba startu głównej prywatnej biblioteki — końcowe UI testowano w bibliotece izolowanej.

## Regresje metod i kontrolek

`tests/Run-AllTests.ps1`: **104/104, kod wyjścia 0**.

| Zestaw | Wynik |
| --- | ---: |
| History | 11 |
| Technical | 12 |
| Gesture | 11 |
| Feature | 35 |
| Patch (0.2.1) | 21 |
| Maintenance (0.2.2) | 14 |

Nowe testy obejmują bajtowy backup i idempotencję, ponowne użycie równoważnej kopii, przeniesienie/duplikat, niejednoznaczność/błędny plik/nieudany backup, plik przenośny i kolizje, jeden nowy wpis i pierwszy stary zapis, błędy/anulowanie usunięcia oraz brak dysku przez Undo/Redo, jedną kolumnę bez rekonstrukcji, pełne etykiety w obu orientacjach (w tym GR200 i próg), rozłączne grupy, przestrzenny układ/atomową walidację, reset wszystkich historii przez Nowa/Otwórz, rzeczywisty schowek Unicode i jedną historię opisu oraz Paint po każdym z 30 kolejnych ruchów uchwytu przed puszczeniem — z pustym opisem i tabelą. Test błędu/usunięcia używa wstrzykniętej operacji na własnym archiwum, nie jest testem Kosza.

Próba schowka uruchomiona początkowo w sandboxie nie dostała dostępu i została przerwana, bez uznania sukcesu. Powtórzenie całego zestawu w sesji z dostępem do schowka zakończyło się powyższym wynikiem. Test zachowuje i przywraca poprzedni schowek. Log końcowego pełnego przebiegu pozostaje ignorowany w `tests/_work/all-c730df5f1da14868aefed58ae71b1abe/results.log`.

## Faktyczne próby zwykłego końcowego EXE

Użyto zwykłej, nieinstrumentowanej kopii głównego `HexMapEditor.exe` 0.2.2.0, o identycznym SHA-256 `DDE0F5506319814282C8DF20DF34B47CA93DF00BE7CF483DA8D45B144DFEB13C`, w osobnym `tests/_work/ui-022-0fe8117ee63241b5aee98364b1026b30/`, z własną biblioteką i mapami utworzonymi przez `tests/Create-UiFixtures.ps1`. Diagnostyczne kopie przed/po naprawie panelu pozostawały oddzielnie w `_work`; nigdy nie zastąpiły głównego EXE.

- Kilka startów bez ostrzeżeń w izolowanej bibliotece. Ręcznie skopiowany własny plik Przenośny po restarcie pojawił się raz na liście i dał się wybrać. Cofnięcie wyboru zachowało plik.
- Nowy własny preset „Próba usunięcia 0.2.2” utworzono i zapisano jako jeden wpis/plik z neutralnym terenem. Dodanie terenu było robocze. Potwierdzenie usunięcia uwzględniło niezapisane zmiany. Po restarcie sprawdzono świadome nadpisanie własnego pliku.
- Po osobnej zgodzie użytkownika wykonano usunięcie tego jednego własnego pliku przez akcję Kosza; bez błędu, plik zniknął. Undo przywróciło roboczy zestaw jako niezapisany z nieaktywnym Usuń preset, plik nadal nie istniał. Redo także nie tworzyło pliku. Nie przeglądano prywatnej zawartości Kosza ani nie weryfikowano niezależnie fizycznego wpisu w Koszu.
- Paleta jednokolumnowa ma stale widoczne akcje poza nazwą i działa w dużym oknie oraz minimalnym 1180×680. Nie stwierdzono poziomego przewijania/nałożenia grup; przy małej wysokości pojawia się pionowe przewijanie.
- Poszerzano i zwężano prawy panel z pustym opisem i rzeczywistą tabelą 2×3. Tabela dopasowała kolumny, kamera nie przesunęła się, mapa nie dostała gwiazdki ani historii od gestu. Obraz po ruchu był poprawny. Narzędzie `drag` w zarejestrowanej diagnostyce dostarczało głównie ruch końcowy, więc **nie jest to pełne wizualne potwierdzenie płynnego ciągłego przeciągania przy wielu pośrednich klatkach**. Osobna regresja metod sprawdza odrysowanie każdego z 30 ruchów bez pompowania kolejki.
- Pełne przyciski Osie / W heksach / Brak, także W heksach w minimalnym oknie. Na mapie 1×99 przy 100% widoczne kompletne A49/A50 w obu orientacjach. Na 200×200 przy 100% widoczne m.in. CA100 i sąsiednie wieloliterowe/trzycyfrowe etykiety; przy Dopasuj 9% napisy zniknęły całkowicie. Nie oglądano osobno każdej współrzędnej do GR200 — to sprawdza geometria testów.
- Rzeczywisty przestrzenny dialog, zera startowe, zmiana Góra=-1 i Lewo=-1: wynik 14×9. Ostrzeżenie pokazało 24 usuwane heksy, 4 z danymi: teren B1/D1 (2), tekst A1 (1), teren i tekst C1 (1). Przejrzano wszystkie trzy zakładki i odmówiono; mapa 15×10, nazwy/opis/tabela i czysty stan zostały zachowane. Zatwierdzenia wycięcia danych nie wykonywano w UI; wykonanie/Undo/Redo pokrywają testy metod.
- Menu opisu miało Wytnij/Kopiuj/Wklej/Wklej jako zwykły tekst. Skopiowano własny opis z polskimi znakami i tabelą, po czym wklejono przez nową opcję: tekst tabeli został zwykłymi wierszami, bez dodatkowej tabeli. Jeden Ctrl+Z usunął całe wklejenie i gwiazdkę, Ctrl+Y odtworzył; ponowne Undo przywróciło czysty stan. Historia mapy i teren pozostały bez zmian.
- Otwierano własne pliki również dwuklikiem, bez widocznej edycji. W tym przebiegu aktywne było Zaznaczanie; nie deklarujemy ponownego pełnego interaktywnego testu dwukliku z Pędzlem. Regresje gestów 0.1.4 i uchwytu przechodzą.

Nie uruchamiano Microsoft Worda, testu większego DPI, awarii zasilania, nieosiągalnego Kosza/dysku sieciowego ani wszystkich kombinacji systemowych dialogów i schowka. Nie oceniano płynności na podstawie czasu pojedynczej metody. Nie oznaczamy odłożonych problemów edytora jako naprawionych.

## Pliki, uruchamianie i Git

Zmiany produktu: `AssemblyInfo.cs`, `HexMapEditor.cs`, `MapModel.cs`, `EditorShell.cs`, `EditorOperations.cs`, `PresetOperations.cs`, `TerrainTile.cs`, `PanelSizing.cs`, `ResizeDialogs.cs`, `RichNotes.cs`; nowe `PresetLibrary.cs`, `RecycleFile.cs`, `CellLabels.cs`, `RemovalGroups.cs`. Nowy zestaw Maintenance i generator własnych map UI oraz skrypty pozostają w `tests/`; zmieniono zbiorczy runner i oczekiwania istniejącego testu palety. README, specyfikacja, plan i ten raport opisują przyrost; główny EXE jest zbudowany i nadal śledzony.

Uruchom `HexMapEditor.exe` lub `Uruchom HexMapEditor.bat`; build lokalnie przez `Build.ps1`, regresje przez `tests/Run-AllTests.ps1`. Nie ma pobierania zależności. Library jest zawsze obok danego EXE; domyślne ścieżki map i bezpieczny zapis pozostały bez zmian.

`git ls-files Library tests/_work bin` nie zwraca plików; `git check-ignore` potwierdza wszystkie trzy główne reguły. Nie tworzono głównego bin. Prywatna Library i kopie są lokalne, źródła/skrypty testów śledzone, artefakty ignorowane. Do commita nie trafiają mapy, logi, testowe EXE ani prywatne dane. Końcowy commit i status podano w odpowiedzi po jego utworzeniu, aby raport nie odwoływał się do własnego przyszłego hasha.

Po potwierdzeniu zakończenia procesów usunięto własny przebieg UI (mapy, przenośny preset, izolowana Library i kopie EXE), pozostałość przerwanego testu Maintenance oraz kopie kodu/EXE diagnostyki panelu. Z diagnostyki zachowano tylko `drag.log` przed/po; log regresji i metadane migracji pozostały ignorowane. Nie dotknięto prywatnej Library/_backups ani wcześniejszej diagnostyki 0.1.4. Generator UI pozwala odtworzyć własne materiały od nowa.

## Krótka checklista użytkownika

1. Uruchomić główny EXE dwa razy: brak ostrzeżenia dla odzyskanego zestawu, dostępny „Domyślny — odzyskany”.
2. Nowy zestaw → nazwa → jeden wpis; zmienić teren i świadomie zapisać. Sprawdzić własną kopię pliku w Presets.
3. Usunąć zbędny własny preset, przeczytać potwierdzenie; Undo ma odtworzyć tylko niezapisany stan roboczy.
4. Płynnie zwężać/poszerzać prawy panel z pustym opisem i tabelą, ocenić odsłaniany obraz podczas ruchu.
5. Sprawdzić pełne etykiety przy 100%, przestrzenny rozmiar i listy grup; Nie ma pozostawić mapę bez zmian.
6. Wkleić własny materiał przez menu zwykłego tekstu i sprawdzić jedno Undo/Redo. Osobno ocenić materiał z używanej wersji Worda.
