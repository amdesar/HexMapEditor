# Raport HexMapEditor 0.5.0

Baza `9c7a295` obejmuje interaktywne próby 0.4.3. Zastany lokalny EXE zachowano bajtowo w `tests/_work/patch050/backup/HexMapEditor.exe` (SHA-256 `1D7BF27D2560631935EFA6F4ED84CE7FD6CB1E45DB1316E9636C4A1C0C4FB95C`). Główny EXE 0.5.0.0 i kopia użyta w próbach miały SHA-256 `19CED5661AEF6F6432B3D36E247E206F500E52D750C3023034FAFB4F87290100`.

## WPF i szczegóły

W izolowanej kopii 0.4.3 heks E5 pokazał czarną powierzchnię zamiast edytora. Kompilacja porównawcza ograniczyła `WS_EX_COMPOSITED` i synchroniczne odrysowanie potomków do natywnych szczegółów obiektów. Na tym samym heksie pojawiły się kontrolki i powierzchnia WPF. Porównanie wskazuje na udział kompozycji gałęzi zawierającej ElementHost. W 0.5.0 natywna zawartość jest chowana przed pokazaniem heksa, a edytor ponownie używa załadowanego dokumentu dla tego samego heksa i stanu mgły.

## Grafiki i formaty

Własny teren wybiera kolor lub grafikę. Kadrowanie przesuwa proporcjonalny obraz pod maską i skaluje go kółkiem. Obie maski przycinają ten sam kwadrat 256 × 256. Zoom zaczyna się od pokrycia kwadratu i dochodzi do dwunastokrotności skali początkowej. Przełączenie maski nie zmienia kadru. Źródło może być nieruchomym PNG, JPEG, BMP lub GIF do 16 MiB i 4096 × 4096. Przezroczystość PNG pozostaje w wycinku. Program zwalnia źródło przed rozpoczęciem kadrowania.

`Library/Terrains/Graphics/terrain-Nazwa.json` jest pojedynczym plikiem z logicznym ID, nazwą, kolejnością oraz kwadratowym PNG Base64 i jego identyfikatorem treści. Kolizje nazw mają czytelne sufiksy. Edycja, dodawanie i usuwanie podlegają historii LibraryFiles. Usunięcie omija Kosz. Potwierdzony import jest samodzielnym zapisem bibliotecznym. Anulowanie późniejszej edycji terenu zachowuje taki wpis biblioteki. Ponowne kadrowanie zapisanego wycinka korzysta z dostępnego kwadratu.

Format mapy 7 i presetu 2 osadza potrzebne kwadraty w deduplikowanej tabeli. Na mapie tabela obejmuje także warianty występujące tylko w zapisanych paletach projektu. Heksy i warianty używają `TextureId`. Historia operuje na zastosowanej wersji terenu, a pliki biblioteki nie są odczytywane podczas malowania. Podczas rysowania program buforuje zdekodowane bitmapy. Wiadra zachowują reguły regionów. Widok i mgła kontrolują rysowanie tekstury. Odczyt map 5–6 i presetów 1 pozostaje dostępny. Limity tabel i plików opisano w README.

## Pozostałe zachowania

- Ponowny zwykły klik wybranej instancji lub dokładnie wybranej grupy odznacza i zamyka szczegóły. Drag pozostaje ruchem. Lista nadal czyta efektywny stan i jego kolor z nakładki grupowej.
- Prawy klik plusa udostępnia wykluczoną definicję biblioteczną po jej ID. Historia przywraca wykluczenie bez przypisania stanu obiektom. Zmiana bajtów bibliotecznej definicji przez inny program wymaga ponownego wczytania biblioteki.
- Teren, stan i ramka grupy używają jednego ciemnego dialogu HSV. Podgląd porównuje dwa kolory, a zapis wymaga dokładnego `#RRGGBB`. Ołówek Domyślnego wyświetla komunikat ochrony.

## Próby metod i kontrolek

- Wycinek zachował bajty po zmianie maski i pokrywał narożniki dla nieprzezroczystego źródła. Sprawdzono malowanie, wypełnienie regionu i Undo/Redo oraz zachowanie opisu.
- Odczyt mapy i presetu bez źródła oraz biblioteki przywrócił teksturę. Współdzielenie zasobu przez heks i paletę przeszło. Uszkodzone ID zasobu odrzucono przed podmianą dokumentu.
- Renderowanie do bitmapy zachowało poziome pasy w obu orientacjach, zasłoniło teksturę mgłą i pokazało neutralny kolor po ukryciu Terenów.
- Sprawdzono zapis, zmianę nazwy i usunięcie pliku bibliotecznego, odtworzenie bajtów przez Undo/Redo i odmowę zajętej ścieżki bez nadpisania. Mapa nadal miała wycinek.
- Sprawdzono przywrócenie wykluczenia po ID, Undo/Redo przywracania, odrębną definicję o tej samej nazwie, efektywny stan i kolor wiersza grupy, ponowny klik grupy oraz walidację HEX.
- Próba grupy uruchomiła także pozostałe celowane przypadki przez nieprzekazany argument skryptu. Wyniki powtórzonych przypadków są zgodne. Pełna kampania pozostała poza zakresem.

## Próby zwykłego EXE

1. W 0.5.0 wpisano tekst z polskimi znakami w heksie B1. Kontrolki i tekst były widoczne po przejściu do obiektu i powrocie. Odczyt pliku przywrócił ten sam opis.
2. Zaimportowano własne PNG, przesunięto obraz lewym przyciskiem, przybliżono kółkiem i przełączono Pointy Top na Flat Top bez resetu. Wybrano miniaturę biblioteki. Przy ponownym kadrowaniu anulowano zmianę i potwierdzono niezmieniony SHA-256 pliku biblioteki.
3. Pędzlem pomalowano B1. Pasy pozostały poziome po zmianie orientacji mapy. Zapisany projekt otwarto w osobnej kopii EXE bez PNG źródłowego i bez skopiowanego wpisu bibliotecznego. Tekstura, miniatura palety i opis były widoczne.
4. Przeciągnięcie pojedynczego obiektu zachowało wybór. Ponowny zwykły klik odznaczył go i zamknął szczegóły. Jedno Undo cofnęło ruch. W edycji terenu wpisano małymi literami HEX i anulowano zmianę. Ołówek Domyślnego pokazał żądany komunikat.

Pierwsza aktywacja kopii bazowej zwróciła `failed to activate captured window`. Po informacji o zamknięciu okien ponownie uruchomiono własną kopię i wykonano opisane próby. Zrzuty potwierdzają odrysowaną zawartość WPF po akcjach. Nie oceniano płynności animacji klatka po klatce ani drżenia nagłówka. Usunięcie grafiki, stan efektywny i klik grupy sprawdzono metodami. W zwykłym oknie nie powtarzano usuwania pliku, edycji koloru stanu i ramki ani przywracania przez menu plusa. Nie prowadzono pełnych prób formatowania, DPI, wydajności ani zgodności wszystkich dawnych map.

## Checklista

1. Wpisz opis heksa, wybierz obiekt i wróć do heksa. Sprawdź tekst, kursor i pasek narzędzi.
2. Własny teren przełącz na Grafikę. Zaimportuj obraz, przesuń go, powiększ i zmień maskę. Sprawdź też anulowanie.
3. Pomaluj Pędzlem i Wiadrem, cofnij i ponów. Zmień orientację, zakryj pole mgłą i wyłącz Tereny w Widoku.
4. Przenieś mapę lub preset do czystej kopii aplikacji. Sprawdź widoczność tekstur bez źródła i biblioteki.
5. Sprawdź drugi klik grupy, drag, efektywny stan listy, prawy klik plusa i wybór HSV/HEX. Spróbuj ołówka Domyślnego.

Zapis lokalnego commita można sprawdzić przez `git log -1`. Repozytorium bazuje na `9c7a295`.
