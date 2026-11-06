# HexMapEditor 0.3.1

Samodzielny edytor map heksagonalnych dla Windows. C# / Windows Forms / GDI+, osadzony edytor opisu WPF z lokalnego .NET Framework. Działa offline, bez konta, usług i pobierania zależności lub zasobów przy uruchamianiu. Rozwijamy istniejący projekt; aplikacja nie została napisana od nowa.

## Uruchamianie i budowanie

Uruchom `HexMapEditor.exe` lub `Uruchom HexMapEditor.bat`. Wymagany pełny .NET Framework 4.x z WPF (zalecany 4.8), dostępny w Windows. Wersja aplikacji **0.3.1**, format mapy **3**, format presetu nadal **1**.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Build.ps1
```

Źródła C# znajdują się w jednym katalogu `src/`; Build.ps1 kompiluje `src/*.cs`. Library pozostaje obok głównego EXE. Build używa lokalnego kompilatora .NET Framework. Główny EXE jest śledzony w Git. Ikony interfejsu i pięć startowych ikon mapy są naszymi rysunkami z kodu, bez zewnętrznych licencji i usług.

**0.3.1 otwiera wyłącznie mapy formatu 3.** Starsze pliki są odrzucane bez zmieniania pliku i otwartego projektu. Nie ma migracji ani konwertera. Nie należy zastępować starego programu, jeśli potrzebny jest jeszcze odczyt dawnych map.

## Podstawowa obsługa

- Górny pasek: pliki i wspólna historia po lewej, widok i Rozmiar po prawej, nieedytowalna nazwa projektu pośrodku. Nazwa po otwarciu/zapisie pochodzi z pliku, bez końcowego `.hexmap`.
- Nowa mapa: nazwa początkowa, zawsze domyślne 15×10, zakres 1–200, orientacja i preset. Etykiety domyślnie Osie.
- Zaznaczanie otwiera prawą nakładkę szczegółów. Rączka lub środkowy przycisk przesuwa, kółko przybliża względem kursora. Pointy Top / Flat Top zachowują dotychczasową geometrię.
- Lewy panel ma sekcje Przybornik, Presety, Tereny, Ikony, Obiekty. **Tylko lista terenów ma własne przewijanie**: kółko i dwie strzałki nad/pod listą zamiast bocznego scrollbara. Strzałki stale zajmują miejsce i wyłączają się na granicach. Dodaj teren i pozostałe sekcje pozostają na miejscu. Obiekty to wyłącznie nagłówek i małe zarezerwowane miejsce.
- Pasek na prawej krawędzi lewego panelu zwija/rozwija panel. Treść zachowuje 220 px; pasek zajmuje dodatkowe 18 px. Płótno zaczyna się za nim. Zmiana widoczności nie edytuje mapy ani historii i zachowuje położenie mapy na ekranie; Dopasuj liczy aktualnie dostępny obszar. Prawa nakładka nadal jest celowo pomijana przy Dopasuj.

## Materiał, pędzel i kształty

**Narzędzie i materiał są niezależne.** Kliknięcie terenu lub miniatury ikony wybiera materiał dla tego samego Pędzla/Wiadra; nie zmienia narzędzia ani danych mapy. Pogrubiona złota ramka wybranej pozycji wskazuje dokładnie jeden materiał; usunięto tekstowy wiersz materiału w Przyborniku. Mgła ma dwa własne narzędzia i nie zależy od wybranego materiału.

Wspólne ustawienia Pędzla terenów, ikon i Mgły wojny: Kształt, Rozmiar **1–15**, Wypełnienie. Punkt wyłącza Rozmiar i Wypełnienie, z wyjaśnieniem w podpowiedzi. Wyłączenie Wypełnienia dla figur powierzchniowych pozostawia obrys grubości jednego heksa. Pod kursorem widać zasięg odcisku. Wiadra nie korzystają z rozmiaru i kształtu.

| Kształt | Znaczenie rozmiaru |
| --- | --- |
| Punkt | Zawsze jeden heks. |
| Heksagon | 1 = środek, 2 = środek i pierwszy pierścień, 3 = dwa pierścienie itd. |
| Kwadrat / Prostokąt | Szerokość w jednostkach siatki; prostokąt ma dodatkowe pole wysokości. |
| Koło / Pięciokąt foremny | Średnica okręgu opisującego w jednostkach siatki; pięciokąt ma wierzchołek w górę. |
| Linia pozioma / pionowa | Długość wzdłuż widocznych osi ekranu; Wypełnienie nieaktywne. |

Jednostka geometryczna wynosi około 48,5 jednostki bazowej mapy (odstęp centrów w nieprzesuniętej osi). Figury wybierają centra heksów wewnątrz obszaru; linie wybierają najbliższe centra wzdłuż odcinka. Wynik jest dyskretny, deterministyczny i niezależny od zoomu, w obu orientacjach i po zmianie parzystości początku mapy. Małe figury mogą sprowadzać się do jednego pola. Obrys jest wyznaczany przed przycięciem do brzegów mapy. Przy szybkim ruchu interpolujemy odcinek między kolejnymi pozycjami w jednostkach mapy i nanosimy ten sam odcisk, ze wspólnym zbiorem odwiedzonych pól. Po wyjściu z płótna, utracie przechwycenia lub nowym geście odcinek jest zerowany.

## Ikony mapy

Sekcja Ikony ma do czterech rzędów miniatur oraz **plus**, **ołówek Edytuj ikonę**, **czerwony kosz** i **…** otwierający pełną bibliotekę. Pięć startowych sylwetek: krzyż, namiot, wieża, czaszka, gwiazda. Są tworzone z kodu przy pierwszym uruchomieniu w nowej bibliotece; znacznik `.starter-icons-v1` zapobiega ponownemu dodawaniu świadomie usuniętych pozycji.

Import: nieruchome PNG/JPG/BMP/GIF do **8 MiB i 2048×2048**. Obraz jest proporcjonalnie zmniejszany do najwyżej 512×512 i zapisywany jako PNG z przezroczystością tam, gdzie była dostępna. Brak SVG, metaplików, animacji i aktywnej zawartości. Plik źródłowy nie pozostaje otwarty. Każdy import tworzy logiczną pozycję z własnym ID; nawet takie same bajty nie oznaczają tej samej pozycji. Mapowe PNG nadal są współdzielone po skrócie zawartości.

Jeden heks zawiera najwyżej jedną ikonę, dużą i proporcjonalną. Pędzel: brak/inny symbol → dodaj/zastąp; ta sama ikona na polu początkowym → usuń wybraną ikonę. Tryb obowiązuje cały gest, a nakładające się odciski nie przełączają ponownie odwiedzonych pól. Teren i tekst pozostają bez zmian. Usunięcie z biblioteki wymaga potwierdzenia; istniejące mapy zachowują osadzony zasób. **Ikony mają sesyjne Undo/Redo plików**: poprzednie bajty i metadane są w pamięci historii, a nie w prywatnym Koszu Windows. Usunięcie nie przenosi pliku do Kosza. Po utracie odpowiadającej historii nie ma sesyjnej kopii do cofnięcia.

Przytrzymanie i przeciągnięcie każdego Wiadra wypełnia kolejne napotkane regiony, jako jeden krok wspólnej historii. Tryb ikon/mgły ustalony na początku całego gestu nie zmienia się. Regiony i ich granice pochodzą z migawki sprzed modyfikacji; region nie przełącza się ponownie. Wiadro ikon używa spójnego regionu **wariantu terenu** i sześciu sąsiadów, z istniejącej kolejki Wiadra. Ikony i mgła nie są granicami. Pole startowe ustala dodawanie/zastępowanie albo usuwanie wybranej ikony; przy usuwaniu inne ikony pozostają.

## Mgła wojny

Mgła wojny maluje wspólnym kształtem; Wypełnienie mgłą działa na spójnym regionie zgodnego **koloru, nazwy rodzaju terenu i początkowego stanu mgły**. Region używa tej samej kolejki i sąsiedztwa co Wiadro; różni się predykatem. Ikony, indywidualna nazwa i opis nie dzielą regionu; ID/wariant terenu nie jest warunkiem mgły. Woda i neutralny o tym samym kolorze są rozdzielone, tak jak dwie plamy mgły rozdzielone odkrytymi polami. Odkryte pole startowe ustala zasłanianie, zasłonięte — odkrywanie. Jeden gest/jedno wypełnienie to jeden krok historii, bez ponownego przełączania odwiedzonych pól.

Mgła jest nieprzezroczysta i szara, zasłania kolor, ikonę i etykietę wewnętrzną. Pozostają robocze granice siatki i współrzędne osi. Szczegóły zasłoniętego heksa pokazują `???` i stan ukrycia zamiast nazwy/terenu/opisu; pola są nieaktywne. Rzeczywiste dane nie są zastępowane znakami zapytania. **Mgła jest funkcją prezentacji, nie szyfrowaniem pliku**: zawartość mapy nadal obejmuje ukryte dane. Nie ma teraz widoku MG/graczy ani globalnego podglądu bez mgły.

## Presety, tekst i historia

Presety: selektor z Odśwież obok, pod nim jeden rząd ikon Nowy preset / Edytuj nazwę / Zapisz preset / Usuń preset. Nowe pliki mają postać **`Library/Terrains/Presets/Kosmos.json`**, bez `.terrainset`. Zawartość presetu nadal ma wersję 1. Zachowano stabilne ID, bezpieczne nazwy Windows, sufiksy kolizji, świadomy zapis i przemianowanie jednego pliku dopiero przy zapisie. Skanowanie `*.json` jest nierekurencyjne, bez specjalnego mechanizmu starych rozszerzeń i bez migracji startowej.

Domyślny pochodzi z kodu i jest chroniony po ID. Nie można zmieniać jego nazwy ani definicji, dodawać/usuwać terenów, nadpisywać go lub usuwać — również przez F2/Delete i metody edycji. Można malować jego terenami oraz zapisać go jako nowy własny preset, wybierany po sukcesie i w pełni edytowalny. Anulowanie/błąd nie zmienia Domyślnego. Edycja terenu tworzy nowy wariant i domyślnie wpływa na przyszłe malowanie. Zastosuj do bieżącej mapy jest domyślnie wyłączone. Odświeżanie nie resetuje roboczej palety ani historii. Usunięcie pliku presetu nie zmienia samowystarczalnej mapy.

Wspólna historia mapy obejmuje tereny, robocze palety i ich wybór, ikony, mgłę oraz rozmiar. Limit: **300 operacji**. Jeden gest/wypełnienie/zmiana rozmiaru to jeden krok. Różnice terenów przywracają tylko teren, różnice warstw tylko ikonę lub mgłę; nazwy i opisy są niezależne. Puste operacje nie kasują Redo. Nowa/Otwórz zerują historie dokumentu i jego pamięć zasobów. Historia ikon odtwarza także pliki, natomiast plików presetów nadal nie zmienia.

Formatowany opis zachowuje dotychczasowe akapity, style, listy i edytowalne tabele. Ctrl+Shift+V / Sam tekst / menu Wklej jako zwykły tekst korzystają z tej samej operacji. Nazwa i opis mają osobne wielopoziomowe historie przypisane do ID heksa. Ctrl+Z/Y w polu tekstowym dotyczą jego historii, górne strzałki zawsze mapy. Limit tekstu: 100 kroków na pole, 32 MiB dodatkowych migawek łącznie; pisanie grupowane do 750 ms przerwy i 5 s trwania. Historie nie są zapisywane. Pełna przebudowa edytora nie należy do 0.3.1.

Rozmiar pozostaje atomową operacją czterech stron ze znakami. Zachowuje ID i wszystkie dane pozostałych pól, w tym ikonę i mgłę. Ostrzeżenie ma rozłączne grupy kombinacji terenu, tekstu, ikony i mgły z pełnymi współrzędnymi sprzed operacji; każdy heks liczony raz. Domyślnie Nie. Nowe pola są neutralne, odkryte, bez ikony.

## Edycja i sesyjna historia biblioteki ikon w 0.3.1

Edytuj ikonę umożliwia nazwę, podmianę z pliku albo grafikę już dostępną w bibliotece. Checkbox **Zaktualizuj wystąpienia na bieżącej mapie** jest przy każdym otwarciu wyłączony. Bez niego mapowe grafiki i nazwy pozostają migawką; po włączeniu aktualizowane są wystąpienia logicznego ID tej pozycji (również starsze warianty tej samej pozycji), a nie wszystkie podobne PNG. Teren, mgła, nazwa i opis heksa pozostają niezależne. Całość to jeden wpis historii. Bez aktywnej ikony przycisk mówi „Zaznacz ikonę”. Anulowanie dialogu lub wyboru pliku nie zmienia biblioteki ani mapy.

Biblioteczny JSON ma teraz `IconFormatVersion: 2`, stałe ID pozycji, nazwę oraz zasób PNG. Pliki: **icon-Czaszka.json**, **icon-Namiot (2).json**. Prefiks jest małymi literami, nazwa zachowuje zapis użytkownika, polskie znaki i spacje. Normalizacja Windows, limit ścieżki i czytelne sufiksy chronią przed kolizją. Stary płaski JSON i jego nazwa są czytane w pamięci, bez startowego przemianowania; dopiero świadoma edycja zapisuje nowe opakowanie i czytelną nazwę jednego pliku. Stare ID pozostaje logicznym ID, więc istniejące przypisania mogą być aktualizowane bez dopasowywania nowej pozycji wyłącznie po bajtach.

Import, usunięcie, zmiana nazwy/grafiki oraz opcjonalna aktualizacja mapy korzystają ze wspólnej chronologii. Migawki w pamięci są dostępne, dopóki odpowiadający wpis znajduje się w historii (limit 300 wpisów). Nowa rzeczywista operacja usuwa Redo. Udane Nowa/Otwórz zerują historię, anulowanie i błąd odczytu nie. Samo zamknięcie nie cofa zapisanej biblioteki i respektuje pytanie o niezapisane zmiany mapy. Biblioteczna edycja bez zmiany wystąpień nie oznacza mapy gwiazdką.

Przed zmianą sprawdzamy oczekiwane bajty i ścieżkę. Konflikt lub zewnętrzna zmiana blokuje operację, bez nadpisania i przesunięcia stosu. Należy rozwiązać konflikt pliku i ponowić operację. Zapis etapuje plik z unikalną nazwą, zapisuje nowe bajty bez nadpisania obcego celu, a przy błędzie przywraca starą ścieżkę. Jeśli sam rollback zablokuje zewnętrzna operacja, komunikat wskazuje zachowaną kopię `.icon-session-*.tmp`; nie kasuj jej. Nie ma sprzątania aktywnych ikon przy Nowa/Otwórz ani przy starcie. Historyczne reguły bez dyskowego Undo pozostają dla **presetów**.

## Format 3 i bezpieczny zapis

`.hexmap` to samowystarczalny JSON UTF-8: geometria i parzystości, ID i właściwości terenów, robocze presety, nazwy i formatowane opisy oraz `IconId` i `Fog` każdego pola. Opcjonalne `IconEntryId` i `IconName` pola przechowują logiczne źródło i nazwę wystąpienia. `IconAssets` zawiera deduplikowane PNG w base64, wymiary, pomocniczą nazwę i ID będące SHA-256 bajtów; nazwą wystąpienia jest `IconName`. Dodanie opcjonalnych pól nie wymaga zmiany numeru 3; starsze mapy 3 mają je puste i zachowują zasoby. Nowe mapy zapisuj w 0.3.1 — starszy program 0.3.0 nie zachowa tych nowych metadanych przy ponownym zapisie. **Zapisuje się tylko użyte zasoby**, jeden na wiele wystąpień. Nie ma zewnętrznych ścieżek grafik. Kopia samej mapy wystarcza do wyświetlenia ikon.

Walidacja sprawdza referencje, unikalność ID, sumy, wymiary i dekodowanie PNG. Limit 128 zasobów, 2 MiB PNG na zasób, około 32 MiB PNG łącznie (44 MiB base64); cały plik do 128 MiB. Nieużywane zasoby potrzebne Undo pozostają w pamięci sesji, lecz nie trafiają do zapisu. Po dojściu do budżetu sesji można zapisać i ponownie otworzyć mapę, aby zwolnić nieużywane zasoby. Błędny import/odczyt nie zastępuje otwartego projektu.

Zachowano zapis tymczasowy obok celu, Flush(true), atomowe zastąpienie i pojedyncze potwierdzenie końcowej ścieżki po dopisaniu `.hexmap`. Anulowanie, odmowa i błąd nie zmieniają ścieżki/nazwy/stanu zapisu. Gest zamykający dialog nie maluje, nie zaznacza ani nie przesuwa mapy; potrzebne jest nowe naciśnięcie na płótnie.

## Lokalna biblioteka i testy

Library jest obok uruchomionego EXE, niezależnie od katalogu roboczego. Program tworzy brakujące katalogi bez kasowania zawartości: Backgrounds, Terrains/Presets, Icons, Objects, Hexmaps, Exports/Images, Exports/Text. Nowy projekt proponuje Library/Hexmaps. Główne `/Library/` pozostaje lokalne i ignorowane przez Git. Nie wdrożono obiektów, tekstur, tła ani eksportów.

**Testowanie 0.3.1 jest minimalne i selektywne. Nie uruchamiamy Run-AllTests ani równoważnej pełnej kampanii bez osobnego polecenia użytkownika.** Przykład pojedynczej próby:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tests\Run-Patch031Tests.ps1 -Case regions
```

Szczegóły filtrów i historycznych testów: `tests/README.md`. Źródła/skrypty testów są śledzone; własne EXE, mapy, grafiki i logi powstają wyłącznie w ignorowanym `tests/_work/`, w osobnych przebiegach. Nie używamy prywatnej Library ani głównego bin. `_work` można usunąć po zakończeniu jego procesów; aplikacja nie sprząta go przy starcie. `tests/Create-Patch031Ui.ps1` przygotowuje kopię zwykłego EXE i własną grafikę do prób okna. Wyniki, ograniczenia i checklistę zawiera `docs/raport-0.3.1.md`; plan: `docs/plan-0.3.1.md`, wymagania: `docs/specyfikacja.md`.

Paczka użytkownika: główny EXE, plik uruchomieniowy i dokumentacja, bez tests/artefaktów/prywatnej Library. Główny bin był dawnym katalogiem budowania testów, nie biblioteką; obecne skrypty go nie tworzą.

## Dalej, bez implementacji w 0.3.1

Obiekty prawdopodobnie 0.4.0; ich przesuwanie i odkrywanie sąsiadów później. Tekstury terenów i zakładki wielu map przed 1.0.0. Po 1.0.0: high/low detail tła, widok MG/graczy i prezentacja, eksperymentalne łączenie heksów. Odłożone: akcent terenu w szczegółach, zdjęcia i przebudowa opisów/stany formatowania/tabele, decyzja o stylu zwykłego wklejania, globalny podgląd bez mgły, przeskok prawego nagłówka, siedem heksów w schemacie rozmiaru i wyróżnienie pól zagrożonych przycinaniem. Poza tą łatką pozostają też dwie kolumny i pełna przebudowa proporcji panelu, graficzna mgła/szczegółowość, współrzędne na ikonach, kadrowanie/arkusze ikon, pełny selektor oraz optymalizacja dużych map. Nie dodajemy generatorów treści ani usług AI.

## Historia zmian

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
