# HexMapEditor 0.4.0

Samodzielny edytor map heksagonalnych dla Windows. C# / Windows Forms / GDI+, osadzony edytor opisu WPF z lokalnego .NET Framework. Offline, bez usług i pobierania zasobów lub zależności przy uruchamianiu. Rozwijamy istniejący projekt.

## Uruchamianie

Uruchom `HexMapEditor.exe` lub `Uruchom HexMapEditor.bat`. Wymagany pełny .NET Framework 4.x z WPF (zalecany 4.8). Aplikacja **0.4.0**, mapy **formatu 4**, presety **1**, ikony biblioteczne **2**, wzorce obiektów **1**.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Build.ps1
```

Build.ps1 używa lokalnego kompilatora i źródeł `src/*.cs`. Library pozostaje obok EXE, niezależnie od katalogu roboczego. Główny EXE jest śledzony w Git. Ikony startowe i symbole interfejsu są rysowane lokalnie, bez zewnętrznych zasobów.

**0.4.0 otwiera tylko format 4.** Nowa obowiązkowa kolekcja niezależnych instancji uzasadnia zmianę formatu: dawny program nie powinien po cichu pomijać tej warstwy. Nie ma konwersji map 1–3; odrzucenie pliku nie zmienia otwartego dokumentu ani źródła. Zachowaj starszy program, jeśli potrzebujesz dawnych map.

## Podstawowa obsługa i materiał

- Nazwa projektu na górnym pasku pochodzi z pliku, bez końcowego `.hexmap`. Nowa mapa: nazwa początkowa, 15×10, zakres 1–200, orientacja i preset; etykiety domyślnie Osie.
- Jeden aktywny materiał: teren, ikona albo wzorzec obiektu, ze złotą ramką. Wybór terenu/ikony po Mgle lub Wiadrze mgły przełącza na Pędzel; zwykłe Wiadro pozostaje Wiadrem. Świadomy wybór wzorca obiektu wybiera Pędzel do pojedynczego umieszczania. Sam wybór nie edytuje mapy.
- Po udanym OK w Edytuj ikonę wybór materiału jest pusty, również bez zmiany wartości. Narzędzie nie zmienia się, nie wybiera się automatycznie teren lub obiekt. Pędzel/Wiadro bez materiału niczego nie edytują; mgła nadal działa. Anuluj zachowuje wybór.
- Rączka lub środkowy przycisk przesuwa kamerę; kółko przybliża względem kursora. Sekcje lewego panelu pozostają osobne; tylko tereny przewijają się kółkiem i strzałkami o jeden wiersz. Pasek zwijania zajmuje własne miejsce. Miniatury ikon/obiektów są kompaktowe; pełna biblioteka pod `…` daje dostęp do pozostałych.
- Prawy panel jest nakładką, ma uchwyt i reset szerokości. Jego otwarcie nie porusza kamery. Dopasuj otwiera jawne opcje **Dopasuj mapę** oraz **Dopasuj wszystko**. Pierwsza liczy tylko geometrię heksów, druga także pełne prostokąty wszystkich obiektów, również poza mapą. Obie celowo pomijają prawą nakładkę; bez obiektów wyniki są takie same.

## Obiekty na planszy

Sekcja Obiekty ma plus, ołówek, czerwony kosz, pełny selektor `…` oraz przycisk **Lista instancji na mapie**. To dwie różne rzeczy: biblioteka zawiera wzorce, lista — konkretne instancje bieżącego dokumentu. Czysta biblioteka dostaje Namiot i Wieżę; znacznik inicjalizacji nie odtwarza świadomie skasowanych wzorców.

Wybierz wzorzec i kliknij płótno. **Jeden klik tworzy jedną instancję**, również poza mapą; przeciąganie nie stempluje kopii. Wiadro nie tworzy obiektów, a kształt i rozmiar pędzla nie mają zastosowania. Nowa instancja: Neutralny, ruch odblokowany, odkrywanie wyłączone, rozmiar 48 jednostek świata, promień 1. Wiele obiektów może zachodzić na ten sam heks lub siebie nawzajem. Nie są polami heksa i nie zastępują ikon.

Wybierz Zaznaczanie, aby kliknąć/przesuwać istniejące instancje. Górny z nakładających się obiektów ma pierwszeństwo. Ctrl+klik przełącza przynależność. Przeciągnięcie od pustego miejsca wybiera centra prostokątem. **Prawy przycisk na Zaznaczaniu** wybiera Klik/prostokąt albo Swobodne lasso; lasso domyka się przy puszczeniu. Ctrl przełącza również wybór obszarem. Klik pustego miejsca lub Escape czyści wybór. Ruch zaznaczonej grupy zachowuje wzajemne przesunięcia; zablokowane obiekty pozostają na miejscu. Jeden gest to jeden Undo/Redo, bez edycji heksów. Delete usuwa instancje, nie wzorce. Skróty nie przechwytują edycji tekstu.

Pojedynczy obiekt ma w prawym panelu nazwę, Sojusznik/Neutralny/Wróg, rozmiar najdłuższego boku **8–1024** jednostek świata (proporcje grafiki zachowane), blokadę ruchu, odkrywanie i promień **0–20**. Zatwierdź przez **Zastosuj właściwości** — jedna operacja historii. Wybór grupy pokazuje liczbę, bez masowej edycji. Limit 5000 instancji; współrzędne świata ±10 000 000, bez obowiązkowego snapowania.

Lista instancji pokazuje nazwę, miniaturę, stronę i Poza mapą. Zaznacz bez centrowania nie zmienia kamery. **Pokaż na mapie** zaznacza i centruje instancję z uwzględnieniem obu paneli, bez zmiany danych, historii lub mgły. Lista jest narzędziem edytora i daje dostęp także do obiektów całkowicie zasłoniętych.

### Biblioteki i kopiowanie grafik

Dodawanie wzorca: plik graficzny albo grafika z biblioteki ikon. Dodawanie ikony: plik albo grafika z biblioteki obiektów. Edycja obu rodzajów ma nazwę, import, wizualny wybór z obu bibliotek, podgląd i domyślnie wyłączone Zaktualizuj wystąpienia na bieżącej mapie. Wybór grafiki nie zmienia nazwy wpisu; anulowanie nie zapisuje zmian.

Kopiowana jest tylko grafika, z nową logiczną tożsamością wpisu, bez zachowań lub powiązanej edycji. Zasób PNG można współdzielić. Aktualizacja instancji wzorca zmienia wyłącznie nazwę/grafikę powiązanych przez jego ID instancji bieżącej mapy; zachowuje pozycję, stronę, blokadę i odkrywanie. Bez checkboxa dawne instancje zostają niezmienione. Usunięcie wzorca nie usuwa instancji.

Pliki: `Library/Objects/object-Namiot.json`, `object-Drużyna (2).json`; ikony: `Library/Icons/icon-Czaszka.json`. Mały prefiks, zachowane litery nazwy, bezpieczne nazwy Windows i sufiksy kolizji, wewnętrzne ID niezależne od ścieżki. Bez masowej migracji prywatnych danych. Import nadal: nieruchome PNG/JPG/BMP/GIF do 8 MiB i 2048×2048, proporcjonalne PNG do 512×512, bez blokady źródła.

## Mgła i obiekty

Tylko **Sojusznik z włączonym Odkrywa mgłę** może odkrywać. Neutralny/Wróg mają kontrolki widoczne, ale nieaktywne. Promień 0 = tylko rzeczywisty heks pod geometrycznym środkiem grafiki, 1 = środek i sześciu sąsiadów itd. Rozmiar obrazu jest niezależny. Wyłączenie odkrywania zachowuje promień; zmiana strony wyłącza funkcję, powrót do Sojusznika nie włącza jej sam.

Włączenie funkcji lub zwiększenie promienia odkrywa otoczenie w tym samym wpisie co ustawienie. Ruch odkrywa drogę między zdarzeniami myszy i własny promień każdego uprawnionego obiektu grupy. Po wyjściu z płótna/utracie capture nie powstaje łącznik nieznanej drogi. Środek poza rzeczywistym heksiem nie korzysta z najbliższego pola. Odkryte pola pozostają odkryte; usunięcie obiektu/wyłączenie/zmiana strony ich nie zasłania. Ręczna mgła nie jest nadpisywana co klatkę. Undo ruchu przywraca tylko mgłę zmienioną przez ten ruch.

Mgła przycina **części** grafiki i obrysu zaznaczenia nad zasłoniętymi heksami. Poza mapą mgły nie ma. Zwykły hit-test nie wybiera przez mgłę; brak tooltipów/nazw ujawniających obiekty. Lista instancji pozostaje świadomym narzędziem edytora, nie zabezpieczonym widokiem gracza. Mgła nie szyfruje mapy.

## Geometria, historia i zapis

Resize zachowuje wszystkie obiekty, także nad usuniętymi heksami. Przesuwa je razem z geometrią starej siatki; lokalny offset zostaje. Orientacja przelicza pozycje przez rozszerzoną siatkę i **lokalne osie heksa**: offset obraca się o 30° między Pointy Top i Flat Top, aby także punkt przy brzegu pozostał nad tym samym heksiem. Grafika nie obraca się. Poza mapą stosuje się tę samą regułę, bez dodawania fikcyjnych komórek. Resize/orientacja i obiekty są jednym Undo, bez odkrywania technicznej drogi. Ostrzeżenie crop dotyczy faktycznie usuwanych danych heksów; obiekty nie są usuwane.

Wspólna chronologiczna historia obejmuje mapę, paletę, instancje, ich fog-delta oraz pliki bibliotek ikon/presetów/obiektów. Limit 300. Brak rzeczywistej zmiany nie tworzy wpisu. Usunięcie pliku jest **bez Kosza**, z bajtową kopią w pamięci historii. Cofanie przestaje być dostępne po resecie, wypadnięciu wpisu lub zamknięciu programu. LibraryFiles wykrywa zmienione źródła/zajęte cele i nie nadpisuje obcych danych; błąd nie przesuwa historii, można ponowić po rozwiązaniu konfliktu. Błąd rollback wskazuje zachowaną własną kopię `.library-session-*.tmp`. Same pliki biblioteki nie tworzą fałszywej gwiazdki mapy. Udane Nowa/Otwórz zeruje historie i zaznaczenie bez kasowania aktywnych plików; błąd/anulowanie zachowuje dokument.

Format **4**: `Objects` zawiera stabilne ID instancji i wzorca, nazwę, AssetId, X/Y, Size, Side, Locked, RevealsFog, RevealRadius. `IconAssets` jest wspólnym magazynem PNG ikon i obiektów po SHA-256. Zapisuje tylko użyte zasoby, jeden na wiele wystąpień, nie całą bibliotekę. Sam plik mapy wystarcza, również dla obiektów poza mapą. Limit wspólny 128 zasobów, 2 MiB PNG/zasób i około 32 MiB PNG łącznie, plik do 128 MiB. Odczyt waliduje przed podmianą dokumentu i **nie wykonuje odkrywania ponownie**. Bezpieczny zapis z Flush/atomowym zastąpieniem i potwierdzaniem końcowej ścieżki pozostał.

## Zachowane funkcje

Osiem kształtów pędzla terenów/ikon/mgły: Punkt, Kwadrat, Prostokąt, Koło, Heksagon, Pięciokąt foremny i dwie linie. Wspólne rozmiary 1–15 oraz wypełnienie/obrys; Punkt zawsze jeden heks. Geometrii i interpolacji nie przebudowano. Wiadro terenów/ikon używa wariantu terenu; mgły — koloru, nazwy rodzaju terenu i początkowego stanu mgły. Gest z przeciąganiem ma jeden wpis historii, stały tryb i odwiedzone regiony. Regionów nie diagnozowano ponownie.

Domyślny preset nadal całkowicie tylko do odczytu; własna kopia edytowalna. Czytelne pliki presetów `Library/Terrains/Presets/Kosmos.json`, odświeżanie bez utraty roboczej palety, stabilne ID/wersje i kontrola konfliktów. Opis heksa pozostaje formatowany z tabelami. Nazwa/opis mają oddzielne historie tekstu; Ctrl+Z/Y w edytorze obsługuje wyłącznie tekst, górne strzałki wspólną historię mapy. Ctrl+Shift+V i menu zwykłego wklejania bez zmian. Szczegóły heksa mają nowy wiersz Ikona: nazwa/brak, pod mgłą `???`.

## Biblioteka i testowanie

Aplikacja tworzy brakujące katalogi Library bez usuwania/nadpisywania istniejących danych: Backgrounds, Terrains/Presets, Icons, Objects, Hexmaps, Exports/Images, Exports/Text. Nowy projekt proponuje Hexmaps; otwarty respektuje swoją ścieżkę. Library jest lokalne i ignorowane przez Git.

**Tylko testy minimalne, selektywne. Nie uruchamiaj Run-AllTests bez osobnego polecenia.** Nowe krótkie scenariusze wybierane pojedynczo:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tests\Run-Object040Tests.ps1 -Case geometry
```

Dostępne Case: geometry, gestures, library, view, input — nie są uruchamiane automatycznie razem. Źródła/skrypty są śledzone, własne EXE/mapy/logi tylko w ignorowanym tests/_work z podkatalogami przebiegów. Create-Object040Ui.ps1 przygotowuje zwykły EXE i własną mapę/bibliotekę; nie jest testem okna. `_work` można usunąć po zakończeniu jego procesów. Program nie sprząta przy starcie. Prywatna Library nie służy do testów. Paczka użytkownika nie zawiera tests/artefaktów/prywatnej biblioteki. Główny bin nie jest potrzebny.

Dokumentacja: `docs/raport-0.4.0.md`, `docs/plan-0.4.0.md`, bieżące wymagania sekcji 27 `docs/specyfikacja.md`. Raport rozdziela wykonane próby metod, zwykłego EXE i pominięte obszary; nie deklaruje pełnego przetestowania.

## Później

Osobna warstwa światła/ciemności, źródło i zasięg światła obiektu oraz powracająca ciemność, niezależne od trwałego odkrywania mgły. Nadal odłożone: zakładki wielu map, tekstury/high-low detail, widok MG/gracza, kadrowanie/arkusze grafik, opisy/zdjęcia/formatowanie, pełny redesign paneli, globalny podgląd bez mgły, przeskok prawego nagłówka, siedem heksów w schemacie rozmiaru, wyróżnienie przycinanych pól i stylistyka ostrzeżeń. Bez mechaniki walki, statystyk/ekwipunku/inicjatywy, pathfindingu, animacji ruchu i zaawansowanych tras; nie są to przyszłe zadania. Bez generatorów treści lub usług AI.

## Historia zmian

- **0.4.0** — niezależne instancje, biblioteka obiektów i kopiowanie grafik, zaznaczanie/ruch/grupy, trwałe odkrywanie mgły przez sojuszników, lista instancji, dwa dopasowania, historia plików obiektów i przenośny format mapy 4.

- **0.3.2** — strzałki o jeden wiersz i wspólny rysunek, wybór materiału po narzędziu mgły przełącza na Pędzel, wizualny selektor grafiki współdzielony z edycją ikony oraz sesyjne Undo/Redo plików presetów ze wspólnym mechanizmem ochrony plików. Generator startowych ikon potwierdzony bez zmiany i bez migracji prywatnej biblioteki. Format mapy 3, presetu 1 i ikony 2.

- **0.3.1** — region mgły według koloru/nazwy/stanu, przeciąganie wiader jako jeden gest, synchronizacja płótna/kamery, strzałki i kółko terenów, złoty wybór materiału, Domyślny całkowicie tylko do odczytu, wyłączony rozmiar Punktu, edycja ikon i sesyjna historia ich plików, czytelne icon-Nazwa.json oraz źródła w src. Lokalna interpolacja pędzla. Format mapy nadal 3, biblioteka ikon 2.

- **0.3.0** — sekcje lewego panelu z osobnym przewijaniem terenów i bocznym paskiem, wspólne osiem kształtów pędzla, biblioteka i malowanie ikon, dwa narzędzia mgły, wspólna historia warstw, samowystarczalny format mapy **3** i presety **Kosmos.json**. Odczyt wyłącznie map 3, bez migracji. Testy selektywne zamiast pełnej kampanii.

- **0.2.3** — czytelne bezpieczne nazwy plików presetów i porządkowanie po jawnym zapisie, odświeżanie bez restartu z ochroną danych roboczych, spójne napisy i przyciski 2×2, zwarty lewy panel z czerwonym koszem i krótkimi podpowiedziami oraz Ctrl+Shift+V w opisie. Format mapy **2**, odczyt **1**, preset **1**.

- **0.2.2** — bezpieczna migracja presetów i katalog Presets, tworzenie bez podwajania oraz usuwanie własnego presetu do Kosza, paleta jednokolumnowa ze stałymi akcjami, natychmiastowe odrysowanie po zwężeniu nakładki, pełne etykiety, przestrzenny dialog rozmiaru i grupy danych oraz zwykły tekst w menu opisu. Format mapy **2**, odczyt **1**, format presetu **1**.

- **0.2.1** — jeden wiersz paska, Dopasuj bez limitu 170%, regulowana prawa nakładka, stabilna kompaktowa paleta z akcjami kafelków, chroniony wzorzec i jawny zapis presetów, wspólne Undo palety/mapy oraz jednoczesny rozmiar czterech stron z pełną listą współrzędnych usuwanych danych. Format nadal **2**, odczyt **1**.

- **0.2.0** — dwa wiersze paska, nazwa projektu z pliku, własne ikony, dwukolumnowa paleta i poprawione panele. Formatowany opis z rzeczywistymi tabelami, listami i osobną wielopoziomową historią tekstu. Lokalne zestawy terenów z wersjonowaniem, Wiadro oraz dodawanie/usuwanie wierszy i kolumn z Undo/Redo. Odczyt formatu 1, zapis formatu **2**. Zachowano zabezpieczenia 0.1.2–0.1.4.

- **0.1.4** — gest płótna musi rozpocząć się na płótnie; powrót z dialogów nie maluje, nie zaznacza i nie przesuwa mapy zdarzeniami zamykającego je gestu. Zakresy wymiarów są osobnymi tekstami kursywą po prawej stronie pól. Artefakty testów trafiają wyłącznie do ignorowanego `tests/_work/`; usunięto potwierdzone stare artefakty z głównego `bin`. Całe główne `Library/` pozostaje lokalne. Format mapy: **1**.
- **0.1.3** — pojedyncze potwierdzenie nadpisania końcowej ścieżki po dopisaniu `.hexmap`; jawne katalogi początkowe zapisu; przygotowanie struktury lokalnej biblioteki; nowa mapa zawsze proponuje 15×10 z widocznym zakresem 1–200; nazwy orientacji Pointy Top / Flat Top. Geometria, Dopasuj i historia malowania pozostają bez zmian. Format projektu: **1**.
- **0.1.2** — historia malowania przywraca wyłącznie teren i kolor, zachowując późniejsze nazwy, opisy i `NotesFormat`; Ctrl+Z/Ctrl+Y w polach tekstowych nie uruchamiają historii mapy; puste pociągnięcia nie zmieniają historii ani stanu zapisu. Format projektu pozostaje w wersji **1**.
- **0.1.1** — poprawiona prostokątna geometria obu orientacji, wybór i dopasowanie heksów, przyciski orientacji i etykiet, zapamiętywanie etykiet, przełączanie panelu szczegółów, skróty zapisu, rozszerzenie `.hexmap` oraz uczciwy licznik łącznej liczby heksów.
- **0.1.0** — pierwszy prototyp edytora.
