# HexMapEditor 0.4.1

Samodzielny edytor map heksagonalnych Windows: C# / Windows Forms / GDI+ i osadzony edytor opisu WPF z lokalnego .NET Framework. Offline, bez usług i pobierania zależności/zasobów przy uruchamianiu. Rozwijamy istniejący projekt.

## Uruchamianie

Uruchom HexMapEditor.exe albo Uruchom HexMapEditor.bat. Wymagany pełny .NET Framework 4.x z WPF (zalecany 4.8). Aplikacja **0.4.1**, mapy **formatu 5**, presety **1**, ikony biblioteczne **2**, wzorce obiektów **1**.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Build.ps1
```

Build.ps1 kompiluje src/*.cs lokalnym kompilatorem. Library pozostaje obok EXE, niezależnie od katalogu roboczego. Główny EXE jest śledzony. Grafiki startowe i symbole interfejsu są rysowane lokalnie, bez zewnętrznych zestawów.

**0.4.1 otwiera tylko format 5.** Nowe obowiązkowe grupy i definicje stanów uzasadniają zmianę formatu: starszy program mógłby zgubić te dane. Nie ma konwersji map 1–4; błędny/starszy plik nie zmienia otwartego dokumentu ani źródła. Zachowaj starszy program dla dawnych map.

## Narzędzia, kamera i materiał

- Osobne widoczne przyciski: Zaznaczanie, Prostokąt, Lasso i Rączka w pierwszym rzędzie; Pędzel, Wiadro, Mgła i Wypełnienie mgłą w drugim. Podpowiedzi, nazwy dostępności i aktywne ramki. Prawy przycisk nie jest potrzebny do wyboru lassa.
- Rączka nad dostępną instancją przesuwa ją lub jej trwałą grupę, bez otwierania prawego panelu. Nad tłem przesuwa kamerę. Respektuje efektywne blokady i mgłę. Środkowy przycisk nadal przesuwa kamerę. Pusty klik poza mapą odznacza heks/zamyka jego panel, ale rozpoczęcie panowania tego nie robi.
- Jeden materiał: teren, ikona albo wzorzec obiektu ze złotą ramką lub brak materiału. Wybór terenu/ikony po narzędziu mgły wybiera Pędzel; zwykłe Wiadro pozostaje Wiadrem. Wybór wzorca wybiera Pędzel do pojedynczego umieszczania. Sam wybór nie edytuje mapy.
- Po udanym OK edycji ikony brak wybranego materiału, także bez zmiany wartości. Narzędzie pozostaje; Pędzel/Wiadro bez materiału nic nie edytują, mgła działa niezależnie. Anuluj zachowuje wybór.
- Tylko lista terenów ma własne kółko/strzałki o jeden wiersz. Sekcje i szerokość treści panelu zachowane; miniatury ikon/obiektów kompaktowe, pełna biblioteka pod … . Pasek zwijania ma własne miejsce.
- Prawy panel jest nakładką z uchwytem i resetem szerokości; samo otwarcie nie rusza kamery. Menu Dopasuj ma styl aplikacji i polecenia Dopasuj mapę / Dopasuj wszystko. Oba pomijają prawą nakładkę. Używają tego samego algorytmu i marginesu: jeśli grafiki mieszczą się w obwiedni mapy, zoom/pan są dokładnie takie same. Dopasuj wszystko uwzględnia rzeczywiste, efektywne rozmiary także poza mapą. Otwórz nadal dopasowuje samą mapę.

## Instancje i biblioteki

Wzorzec biblioteki, grafika i instancja na planszy są osobnymi rzeczami. Wybierz wzorzec i kliknij płótno: jeden klik tworzy jedną instancję, także poza mapą, bez snapowania. Drag nie stempluje, Wiadro nie tworzy obiektów, figury pędzla ich nie dotyczą. Nowa instancja: Neutralny, ruch odblokowany, odkrywanie wyłączone, rozmiar **30**, promień 1. Istniejące rozmiary nie są zmniejszane. Wiele obiektów może dzielić heks lub nakładać się, bez zmiany jego terenu/ikony/tekstu.

Prawy panel pojedynczej instancji działa **na bieżąco**, bez Zastosuj: nazwa, kolorowe kafelki stanów, rozmiar 8–1024 jednostek świata (najdłuższy bok z proporcjami), blokada, odkrywanie i promień 0–20. Czerwony Usuń instancję usuwa obiekt z mapy, nie wzorzec. Checkbox/stan to pojedynczy wpis; nazwa i liczby są scalane do sesji pola, kończonej przez utratę fokusu, inną operację, zapis, Undo/Redo lub zamknięcie. Poprawny tekst liczbowy działa bez wychodzenia z pola; przejściowe puste/- i wartości poza zakresem nie trafiają do modelu. Ctrl+Z/Y w tych polach obsługuje wspólną historię właściwości; heksowy opis/nazwa zachowują własne historie.

Nazwa wzorca jest wyłącznie początkową nazwą nowych instancji. Goblin/Ork nie zmieniają się po przemianowaniu wzorca Przeciwnik na Wróg. Checkbox edycji wzorca aktualizuje **grafikę** pasujących instancji bieżącej mapy, bez zmiany ich nazw, położeń, stron lub ustawień. Nazwy ikon heksowych zachowują dotychczasowe reguły.

Biblioteki ikon/obiektów mają dodawanie z pliku lub grafiki drugiej biblioteki, edycję nazwy/grafiki i wspólne wizualne selektory/podgląd. Kopiowana jest grafika do niezależnego ID; identyczne PNG nie tworzą powiązania edycyjnego. Wybór grafiki nie zmienia nazwy wpisu. Anulowanie niczego nie zapisuje. Kosz wzorca jest nieaktywny bez wyboru. Usunięcie wzorca nie usuwa instancji. Pliki: Library/Objects/object-Namiot.json, object-Drużyna (2).json i Library/Icons/icon-Czaszka.json — bezpieczne nazwy Windows, zachowane litery, mały prefiks, sufiksy kolizji. Import nieruchomych PNG/JPG/BMP/GIF do 8 MiB/2048×2048, PNG proporcjonalnie do 512×512, bez blokowania źródła. Czysta biblioteka startowa jest poprawnym scenariuszem, bez masowej migracji prywatnych plików.

## Stany i trwałe grupy

Wbudowane kafelki: zielony Sojusznik (pozwala odkrywać), niebieski Neutralny i czerwony Wróg (nie pozwalają). Nazwa, podpowiedź i aktywna ramka uzupełniają kolor. Plus dodaje własny stan dokumentu: nazwę, kolor i Pozwala odkrywać mgłę. Nowy kafelek zajmuje miejsce plusa, który przechodzi dalej; sekcja zawija się. ID stanu jest stabilne i niezależne od nazwy/koloru. Samo utworzenie stanu nie przypisuje go do obiektu. Nie ma teraz edycji/usuwania definicji ani globalnej biblioteki stanów.

Prostokąt/lasso wybierają centra. Zwykły klik członka wybiera całą trwałą grupę; Ctrl dodaje/odejmuje grupę lub pojedynczy niezgrupowany obiekt. **Alt+klik** wybiera pojedynczego członka również przed wcześniejszym wyborem grupy; nie rozwiązuje jej. Obszarowy wybór rozszerza trafionego członka do grupy. Mgła nadal przycina obrysy ukrytych członków. Ruch wielu obiektów ma jeden wpis historii i zachowuje ich rozstawienie; zablokowani są pomijani. Delete na wyborze grupy usuwa instancje razem, Alt-wybór pozwala usunąć jednego. Puste grupy są usuwane, grupa z jednym członkiem zachowuje ustawienia.

Grupuj wybrane jest dostępne dla co najmniej dwóch instancji. Wymagane są niezgrupowane obiekty: mieszany wybór daje komunikat, bez ukrytego rozłączania. Grupa ma ID, nazwę i członków, bez grup w grupach. Panel grupy: nazwa, lista, wybór pojedynczego członka, nakładki i Rozwiąż grupę. Panel członka pokazuje grupę, przejście do niej i Odłącz.

**Nakładki nie zmieniają bazy członków.** Każda może być Indywidualnie: stan, blokada, odkrywanie, promień i rozmiar. Nowa grupa ma wszystko wyłączone. Rozmiar może być przesunięciem od bazowego (np. +10) z clamp 8–1024 albo wspólnym (np. 40); nie kumuluje się i nie przesuwa środków. Grupowa blokada true blokuje wszystkich, jawne false może odblokować indywidualnie zablokowanego. Alt-edycja zmienia bazę, nie wyłącza nakładek. Panel członka wyraźnie pokazuje wartości efektywne, które mogą przykrywać edytowaną bazę.

Odłącz/Rozwiąż pyta o Zachowaj wynikowe / Przywróć indywidualne / Wybierz dla każdej właściwości / Anuluj. Kategorie: stan, rozmiar, blokada, odkrywanie, promień; przy rozwiązaniu decyzja dotyczy całej grupy. Zachowaj zapisuje efektywne wartości jako bazę, z kontrolą uprawnień; Przywróć odsłania zachowane bazowe. Pozycje/nazwy i dawne odkrycia zostają. Cała zmiana członkostwa/właściwości/nowej mgły jest jednym Undo. Anuluj niczego nie zmienia.

## Odkrywanie i podgląd mgły

Najpierw efektywny stan określa uprawnienie, potem działają efektywny checkbox i promień. Stan bez uprawnienia zawsze uniemożliwia odkrywanie. Indywidualne przejście do niego wyłącza bazowy checkbox; powrót nie włącza go sam. Ograniczenie grupowe nie niszczy bazowego checkboxa członka. Panel pojedynczego członka edytuje jego wartości indywidualne i wskazuje przykrywającą je nakładkę.

Promień 0 = rzeczywisty heks pod środkiem grafiki, 1 = pierwszy pierścień sześciu sąsiadów itd. Rozmiar jest niezależny. Poza mapą nie używa się najbliższego pola. Ruch odkrywa drogę między zdarzeniami i zasięg każdego uprawnionego członka; brak łącznika po utracie capture/nieznanej drodze. Odkryte pola zostają po odejściu/usunięciu/wyłączeniu. Undo ruchu cofa jego nową mgłę, nie wcześniejsze odkrycia.

**Aktywny zasięg jest chroniony przed ręczną mgłą.** Po właściwych operacjach (mgła, ruch/umieszczenie, ustawienia wpływające na odkrywanie, odłącz/rozwiąż) wynik jest uzgadniany z aktualnymi efektywnymi zasięgami. Malowanie i wiadro nie zasłaniają ich, także w trakcie gestu. Bez pętli co klatkę. Końcowa delta jest częścią tej samej operacji; Undo/Redo odtwarza ją bez nowego przeliczania, więc nie niszczy sensu cofania. Odczyt nie odkrywa ponownie.

**Podgląd bez mgły** to wyraźnie oznaczona flaga widoku, bez historii/gwiazdki/zmiany Fog i bez kopii całej mapy. Pokazuje rzeczywiste grafiki i szczegóły oraz pozwala edytować normalnie ukryte dane; ruch nadal może odkrywać rzeczywiste Fog. Zapis zachowuje mgłę. Udane Nowa/Otwórz wyłącza podgląd. Ręczna próba mgły pyta o przywrócenie widoczności: Nie nic nie zmienia, Tak przywraca widoczność i **wymaga ponownego gestu**.

Lista instancji: Nazwa z miniaturą / Stan efektywny / Pozycja (Poza mapą lub pusto) / Mgła (Widoczny, Częściowo ukryty, Ukryty). Częściowe/pełne zasłonięcie maskuje nazwę, stan i pozycję przez ??? oraz używa neutralnej miniatury. Rozróżnienie wynika z geometrycznego clippingu, bez analizy pikseli. Poza mapą nie ma mgły, wystająca grafika może być częściowo ukryta. Podgląd pokazuje realne dane, a kolumna Mgła nadal stan rzeczywisty, zgodnie z tytułem listy. Świadomy wybór/Pokaż nadal znajduje ukrytą instancję bez zmiany Fog. Nie jest to widok zabezpieczony dla gracza; mgła nie szyfruje pliku.

## Zapis, geometria i historia

Format **5** zachowuje Objects z bazowymi właściwościami, ObjectStates i Groups z ID/członkami/nakładkami. Side instancji jest ID definicji stanu. IconAssets nadal deduplikuje użyte PNG ikon/obiektów po SHA-256 bez łączenia ich edycji; nie zapisuje całej biblioteki. Sam plik mapy wystarcza, również dla obiektów poza mapą. Walidacja sprawdza ID, referencje, pojedyncze członkostwo, brak zagnieżdżeń, skończone pozycje/rozmiary/promienie przed podmianą. Podgląd nie jest danymi mapy. Limity: 5000 instancji, 2500 grup, 100 stanów, świat ±10 000 000, 128 wspólnych zasobów, 2 MiB PNG/zasób i około 32 MiB PNG łącznie; plik do 128 MiB. Bezpieczny zapis, pojedyncze potwierdzenie końcowej ścieżki i ochrona gestów dialogów pozostają.

Resize/orientacja zachowują zaakceptowane zasady obiektów i lokalnych osi offsetu; crop ich nie usuwa. Techniczne przeliczenie nie odkrywa nowej drogi. Wspólna historia 300 obejmuje mapę, instancje, grupy/stany, końcowe delty mgły i pliki bibliotek. Nazwa/pole liczbowe ma logiczną sesję; pusty/niezmieniony zapis nie tworzy operacji. Sam wybór/nawigacja/podgląd bez historii. Pliki bibliotek są cofane z bajtów sesji przez LibraryFiles, bez Kosza i cichego nadpisania obcego celu; konflikt nie przesuwa stosu, można ponowić. Reset/limit/zamknięcie usuwa możliwość cofania, nie aktywne pliki. Udane Nowa/Otwórz zeruje historie/wybór, błąd/anulowanie zachowuje dokument.

Zachowano chroniony Domyślny, czytelne presety i odświeżanie, osiem geometrii figur i ciągłość pędzla, regiony wiader terenów/ikon/mgły, osobne historie tekstu, formatowane opisy i Ctrl+Shift+V. Nazwa projektu nadal pochodzi z pliku; nowy projekt proponuje 15×10, orientację/preset i etykiety Osie. Nie diagnozowano ponownie zaakceptowanych obszarów 0.4.0.

## Biblioteka, testy i dalsza praca

Library jest lokalne/ignorowane; program tworzy brakujące Backgrounds, Terrains/Presets, Icons, Objects, Hexmaps, Exports/Images, Exports/Text bez kasowania danych. Nowy projekt proponuje Hexmaps, otwarty respektuje swoją ścieżkę. Prywatna biblioteka nie służy do testów.

Tylko minimalne, selektywne próby. **Nie uruchamiaj Run-AllTests bez osobnego polecenia.** Nowe scenariusze wybierane pojedynczo:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tests\Run-Groups041Tests.ps1 -Case model
```

Case: model / fog / controls, bez automatycznej pełnej kampanii. Źródła/skrypty są śledzone, artefakty tylko w ignorowanym tests/_work z osobnymi podkatalogami przebiegów. Create-Groups041Ui.ps1 przygotowuje zwykły EXE i własną mapę; to nie test okna. Wyniki i ograniczenia: docs/raport-0.4.1.md; plan: docs/plan-0.4.1.md; bieżąca specyfikacja: sekcja 28. Paczka programu bez tests/artefaktów/prywatnej Library, główny bin niepotrzebny.

Poza zakresem pozostają zagnieżdżone grupy, światło/ciemność, widok MG/gracza, tekstury/detail, zakładki, kadrowanie, przebudowa opisów i pełny redesign. Nie planujemy walki/statystyk/ekwipunku, tras/pathfindingu/animacji. Bez usług AI. Warstwa światła jest przyszłym, niezależnym od trwałego odkrywania wymaganiem.

## Historia zmian

- **0.4.1** — trwałe grupy i odwracalne nakładki, własne stany, bieżące właściwości, osobne zaznaczanie/prostokąt/lasso, kontekstowa Rączka, ochrona aktywnych zasięgów, podgląd bez mgły i maskowana lista, wspólny kadr Dopasuj; format mapy 5.

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
