# HexMapEditor 0.2.0 — raport wykonania

Data: 5 listopada 2026. Aplikacja **0.2.0**, metadane EXE **0.2.0.0**, format mapy **2**.

## Baza i wykonany zakres

Pracowano wyłącznie w `C:\ALTERNATYWNA PARTYCJA\HexMapEditor-Codex`. Początek: czysty master, HEAD i origin/master `1b1e09c8384a33dbc7f7cf2aa7527e769268140e`. Referencje pobrano, fast-forward nie był potrzebny. Po wznowieniu zachowano wszystkie rozpoczęte zmiany, ponownie przeczytano plan, sprawdzono diff, commity i procesy oraz odświeżono origin — bez nowych commitów zdalnych. Nie przywracano 0.1.4 ani nie przepisywano historii.

Ukończono kod zakresu 0.2.0: dwa wiersze paska i nazwę z pliku, własne ikony i podpowiedzi, dwukolumnową paletę i rozwijany pasek, marginesy/przewijanie prawej nakładki, dialog nowej mapy z nazwą i presetem, formatowany opis z rzeczywistymi tabelami, wielopoziomowe historie nazwy/opisu, zestawy i wersje terenów, opcjonalną aktualizację mapy, Wiadro, rozmiar z czterech stron oraz format 2 i migrację formatu 1. Zachowano bezpieczny zapis, ścieżki Library i zabezpieczenie gestów 0.1.4.

Nie dodawano importu grafik, ikon/obiektów umieszczanych na mapie, eksportów, mgły/Resetu ani własnych ostrzeżeń/paska tytułu. Ich brak nie jest błędem tej wersji. Nie pobrano nowej biblioteki, zasobów ani zależności przy uruchamianiu. Nie wykonano push ani Release'a.

## Decyzje techniczne

- **Edytor:** wstępny lokalny test istniejącego .NET Framework potwierdził rzeczywiste WPF Table/Row/Cell, edycję zawartości i struktury oraz RTF z tabelą i listą numerowaną. Natywnego Undo nie uznano za sprawdzone rozwiązanie. WPF RichTextBox/FlowDocument jest osadzony przez ElementHost, Windows Forms/GDI+ pozostają aplikacją i rendererem. Nie dodano zewnętrznego komponentu ani dodatkowej płatnej licencji. Dokumentacja użyta podczas wyboru: [RichTextBox](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/richtextbox), [Table](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/table-overview).
- **Bezpieczna treść:** projekt utrwala własny, ograniczony model akapitów, tekstu, list i tabel, a nie XAML/RTF. Wklejanie najpierw filtruje RTF (obiekty, obrazy, instrukcje pól, metadane), następnie konwertuje do dozwolonych elementów. Czytelny tekst wynikowy pól/obiektów jest zachowywany. Nie ładuje HTML/XAML/pakietów, nie wykonuje obiektów ani nie pobiera zasobów. Awaria konwersji używa tekstu Unicode lub daje komunikat bez zmiany opisu.
- **Format mapy:** jawny format 2 jest potrzebny dla ID heksów, wersji terenów, palet projektu, parzystości początku i strukturalnych opisów. Format 1 jest odczytywany, a zwykły tekst migracji zachowuje także nowe linie i tabulatory. Po otwarciu komunikat ostrzega, że 0.1.x nie odczyta kolejnego zapisu; Zapisz jako pozwala zachować oryginał. Nieobsługiwany format, brak wersji/ID formatu 2 i sprzeczny wariant są odrzucane przed zastąpieniem mapy. Zapis i odczyt mają wspólny limit pliku 128 MiB.
- **Tereny:** stabilne TerrainId, niezmienne VariantId i rosnąca Revision. Heks zachowuje kopię właściwości, więc mapa jest samowystarczalna po usunięciu bibliotecznego presetu. Edycja domyślnie dotyczy przyszłego malowania. Opcjonalna aktualizacja obejmuje dokładnie poprzedni wariant w bieżącej mapie, jako jedno Undo, bez notatek. Stare mapy nie miały tożsamości wersji: migracja jawnie przypisuje znane domyślne pary nazwa/kolor lub deterministyczny wariant legacy. Nie można odtworzyć informacji, której format 1 nie zapisywał.
- **Historie:** mapa do 300 operacji, zmiany terenu tylko jako właściwości terenu; rozmiar zachowuje odniesienia i ID heksów. Tekst osobno per heks/pole, do 100 kroków i 32 MiB dodatkowych migawek łącznie. Bieżąca treść jest poza tym budżetem. Pisanie grupowane przy przerwie <750 ms, maksymalnie 5 s; nawigacja i zmiana pola przerywają grupę. Formatowanie/tabele/wklejanie opisu są osobnymi krokami. Historia nie jest serializowana. Stan niezapisany przy pisaniu porównuje pojedynczy heks, bez serializacji całej mapy po znaku.
- **Rozmiar i Wiadro:** kolejka zamiast rekurencji, sześć sąsiadów według orientacji/parzystości, identyfikacja obszaru przez VariantId. Rozmiar od góry/lewej koryguje parzystości, zachowując względny układ. Ostrzeżenie liczy wszystkie usuwane heksy i heksy z danymi; domyślnie odmawia. Historia malowania używa ID, więc przeliczanie indeksów nie kieruje Undo do innego pola.

## Testy metod i kontrolek

Końcowe `tests/Run-AllTests.ps1`: **69/69**.

| Zestaw | Wynik | Sprawdzony zakres |
|---|---:|---|
| Historia | 11/11 | Regresje 0.1.2: późniejsze nazwy/opisy/NotesFormat, no-op/Redo, grupowanie, zapis/Undo/Redo i stan zapisu; skróty nazwy w nowej historii aplikacji |
| Techniczne | 12/12 | Końcowa ścieżka i pojedyncze potwierdzenie, odmowa/anulowanie, zablokowany cel/błąd/brak katalogu, wyścig nowego celu, dialogi i ścieżki Library, zachowanie danych biblioteki, 15×10/zakresy i orientacje |
| Gesty | 11/11 | Obce Move/Up dla wszystkich narzędzi, modalne powroty, świeży Down, zwolnienie przycisku, końcówka dwukliku, capture, pan/select, zagnieżdżone/wyjątkowe wyjścia; osobna regresja Wiadra po dialogu |
| Funkcje 0.2.0 | 35/35 | Nazwa z pliku i anulowanie; trwałość wszystkich stylów, tabel/list; struktura tabeli; filtrowany RTF; historie i izolacja heksów, limity, reset po otwarciu; presety/wersje i aktualizacja mapy; sąsiedztwo i Wiadro obu orientacji; rozmiar każdej strony obu orientacji, parzystość, ochrona danych i zgodne Undo/Redo; format 1, niepoprawne ID/wersje; iteracyjne 200×200; układ minimalnego okna |

To testy metod i konfiguracji kontrolek, nie automatyzacja systemowego dialogu ani test Microsoft Worda. Pierwszy przebieg funkcji ujawnił błąd samego testu (próba podmiany FlowDocument wewnątrz BeginChange); poprawiono test, aby edytował zawartość dokumentu. Wczesne przerwane przebiegi zostawiły własne artefakty, które po sprawdzeniu usunięto. Ostateczne zestawy zakończyły się kodem 0.

Zachowany lokalnie log: `tests/_work/all-0efcb5dd6025400889f51168875fe1a2/results.log`, ignorowany. Standardowe przebiegi usunęły swoje EXE i mapy, nie odtworzyły głównego bin.

## Rzeczywiste próby okna

Computer Use obsługiwało zwykły główny `HexMapEditor.exe` 0.2.0, nie kompilację Trace. Próby wykonano w kilku kolejnych kompilacjach; po końcowych poprawkach filtra RTF i kierowania skrótów całego edytora ponowiono wszystkie testy metod. Sprawdzono dialog edycji terenu z wyłączoną aktualizacją mapy. W ostatnim EXE sprawdzono też Ctrl+Shift+S po kliknięciu przycisku historii opisu i brak zmian po anulowaniu dialogu zapisu.

- Start, górny pasek i panele przy dużym oknie oraz minimum 1180×680. Grupy przycisków nie nachodziły na siebie. Prawy panel miał marginesy i przewijał zawartość; zwinięty lewy panel zostawiał pasek na całą wysokość. Otwarcie/zamknięcie nakładki nie przesuwało mapy. Dopasuj działał z lewym panelem.
- Nowa mapa: 15×10, osobne kursywą `(zakres: 1–200)`, nazwa początkowa „Próba 0.2.0”, orientacja i preset. Utworzona mapa pokazała nazwę początkową bez edytowalnego pola na pasku.
- Opis: wpisanie tekstu, pogrubienie Ctrl+B, Ctrl+Z/Ctrl+Y formatowania, wstawienie tabeli 2×2, wpis do komórki, dodanie wiersza i kolumny. Powstała tabela 3×3.
- Po ponownym uruchomieniu i otwarciu pliku opis z pogrubieniem i tabelą pozostał. Ctrl+A/Ctrl+C i Ctrl+V do innego heksu zachowały pogrubienie, tekst i rzeczywistą tabelę. Zapisany model drugiego heksu potwierdził 3 wiersze i Bold=true. To schowek wewnątrz aplikacji, **nie Word**.
- Ctrl+S z fokusem opisu zapisał plik i usunął gwiazdkę. Ctrl+Shift+S otworzył rzeczywisty dialog z katalogiem bieżącego pliku; anulowanie zachowało projekt. Ctrl+Z usunęło wklejony opis, bez edycji terenu i bez historii mapy; Ctrl+Y odtworzyło zapisany stan i usunęło gwiazdkę.
- Zapis przez systemowy dialog do własnego `tests/_work/.../sprawdzona.txt.hexmap` pokazał nazwę `sprawdzona.txt`. Dialog nowego projektu proponował Library/Hexmaps; map testowych tam nie zapisywano.
- Utworzono roboczy zestaw, dodano teren i zaobserwowano zmianę kafelków bez przemalowania mapy. W ostatnim EXE dialog Edycja terenu miał wyłączone Zastosuj do bieżącej mapy; anulowanie nie zmieniło mapy/historii. Zapis zestawu do katalogu użytkowego Library nie był klikany w tej próbie; trwałość presetów badano metodami w `_work`.
- Wiadro wypełniło spójny obszar; jedno Cofnij przywróciło całość i zapisany stan. Rozmiar dodał wiersz od góry (15×10 → 15×11). Próba usunięcia górnego wiersza danych pokazała 15 usuwanych / 15 z danymi; odmowa pozostawiła 15×11. Faktyczne usuwanie i wszystkie pozostałe strony sprawdzono metodami.
- Z aktywnym Pędzlem plik otwarto dwuklikiem w pozycji nad heksem mapy. Teren pozostał bez zmiany, brak gwiazdki, Cofnij/Ponów nieaktywne, Pędzel pozostał wybrany. Pierwszy świadomy klik pomalował heks; Undo przywróciło zapisany stan. Sprawdzono też otwarcie Enterem po restarcie. Powroty z testowanych dialogów nie dopisały edycji.

W próbach okna znaleziono i poprawiono lewy margines przewijanej treści oraz przekazanie Ctrl+S/Ctrl+Shift+S z WPF do formularza; obie poprawki ponownie sprawdzono w zwykłym EXE. Zdarzające się opóźnienia/stare indeksy drzewa Computer Use rozwiązywano świeżym odczytem lub wejściem współrzędnymi, bez zmiany uprawnień aplikacji.

## Ograniczenia i niewykonane próby

- **Nie uruchomiono Microsoft Worda ani prób kopiowania z Worda.** RTF sprawdzono syntetycznie i przez rzeczywisty schowek tej aplikacji. Złożone dokumenty Worda wymagają oceny użytkownika.
- **Nie wykonano testu większego DPI** ani pracy na wielu monitorach. Nie zmieniano globalnych ustawień ekranu. Minimalne/duże okno sprawdzono w dostępnej konfiguracji.
- W oknie nie sprawdzono każdej kombinacji skrótów nazwy, wszystkich stylów/list, wszystkich stron/ilości rozmiaru, wszystkich wariantów palet i błędów zapisu. Ich wymienione wyżej regresje są testami metod, nie deklaracją pełnego testu interaktywnego.
- Nie badano interaktywnej płynności pełnej 200×200, odporności na odcięcie zasilania ani długiego obciążenia pamięci. Test kolejki 40 000 pól nie jest pomiarem płynności UI.
- Model nie odwzorowuje obrazów/obiektów, krojów i kolorów tekstu, scaleń ani pełnego układu Worda. Import upraszcza je do obsługiwanych elementów/tekstu. Obowiązują udokumentowane limity opisu, historii i pliku. Po wyczerpaniu historii nie będzie można cofnąć najstarszych kroków.

## Pliki, artefakty i Git

- `HexMapEditor.cs`: model projektu, bezpieczny odczyt/zapis, nazwa z pliku, integracja geometrii i historii; zachowane istniejące działające elementy.
- `MapModel.cs`: presety, ID i migracja, flood fill, rozmiar, stan zapisu i historie tekstu.
- `RichNotes.cs`, `RtfClipboard.cs`, `HistoryNameBox.cs`: edytor/dokument, filtrowanie schowka i oddzielenie historii nazwy od natywnego Undo.
- `EditorShell.cs`, `EditorDialogs.cs`, `EditorOperations.cs`: pasek/panele/ikony, dialogi i integracja operacji. Podział dotyczy rozbudowy istniejącego MainWindow, nie nowego projektu.
- `AssemblyInfo.cs`, `Build.ps1`, `HexMapEditor.exe`: jawna wersja i powtarzalny lokalny build.
- `tests/`: zachowane i dostosowane stare regresje, nowe FeatureTests i wspólny runner/log. README, specyfikacja, plan i ten raport opisują stan 0.2.0 i ograniczenia.

Usunięto wyłącznie sprawdzone własne kompilacje/probę edytora, pozostałości przerwanych przebiegów i mapy obu prób UI z `tests/_work`. Zachowano ostatni log regresji oraz wcześniej uzgodnione `repro-.../HexMapEditor.Trace.exe` i `input.log` z 0.1.4. Procesy zamknięto przed sprzątaniem. Nie dotykano map ani zasobów użytkownika w Library. Główny bin nie istnieje i nie został odtworzony.

`git ls-files Library tests/_work bin` nie wykazało śledzonych plików. `git check-ignore -v` potwierdziło `/Library/`, `/tests/_work/`, `/bin/`. Źródła/skrypty testów i główny EXE pozostają śledzone. Do commita nie należą mapy, logi, testowe EXE ani lokalne presety. Końcowy hash commita i stan repozytorium podano w odpowiedzi po utworzeniu commita; raport należy do tego samego commita (bez samoodwołującego się hasha).

## Uruchomienie i checklista użytkownika

Uruchom główny `HexMapEditor.exe` lub bat. Build: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Build.ps1`. Regresje: analogicznie `tests\Run-AllTests.ps1`. Paczka programu bez tests i lokalnego Library.

1. Otwórz kopię własnej mapy formatu 1 i zapisz pod nową nazwą; sprawdź nazwy i zwykłe opisy.
2. Wklej typowy opis z Worda, sprawdź listy/tabelę oraz tekst po uproszczeniu nieobsługiwanych elementów.
3. Edytuj dwa heksy, przełączaj je i użyj oddzielnych Ctrl+Z/Ctrl+Y; sprawdź gwiazdkę po powrocie do zapisanego stanu.
4. Utwórz i zapisz własny zestaw, sprawdź go po restarcie; edytuj teren z przełącznikiem aktualizacji mapy i bez niego.
5. Sprawdź Wiadro, przycinanie z potwierdzeniem i Undo/Redo na kopii mapy w obu orientacjach.
6. Oceń układ przy swoim DPI, minimalnym oknie i długich opisach; sprawdź dwuklik Otwórz z aktywnym Pędzlem/Wiadrem.
