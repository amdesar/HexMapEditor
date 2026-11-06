# Raport HexMapEditor 0.4.0

## Baza i wynik

Rozbudowano istniejący projekt, bez rozpoczynania prac 0.3.x ponownie. Baza: czysty master `c113c8e9bddb260defbb9bac07ab327eb452d712` (0.3.2), origin https://github.com/amdesar/HexMapEditor.git. Przeczytano instrukcje dostępne w repozytorium, README, raport 0.3.2 i odpowiednie fragmenty specyfikacji; brak dodatkowych AGENTS.md w sprawdzonym repozytorium/katalogach nadrzędnych. Nie było zastanych zmian. Nie wykonywano fetch/synchronizacji, nie wymagano ich w tym zadaniu. Praca wyłącznie w głównym HexMapEditor-Codex.

Zbudowano główny zwykły **HexMapEditor.exe 0.4.0.0**. Format mapy **4**, presetu nadal **1**, ikony bibliotecznej **2**, nowego wzorca obiektu **1**. Bez zależności, usług, push/tagu/Release'a i przepisywania historii. To selektywnie sprawdzony przyrost, nie kompleksowo przetestowana aplikacja.

## Wykonane funkcje

- Osobne wzorce ObjectTemplate, współdzielone grafiki IconAsset i niezależne MapObject. Własne ID instancji i TemplateId wzorca, nazwa, grafika, pozycja świata, proporcjonalny rozmiar, strona, blokada i odkrywanie/promień. Wiele instancji w tym samym miejscu i poza mapą. Bez ingerencji w teren/ikonę/opis heksa.
- Działająca sekcja Obiekty: miniatury, plus, edycja, czerwony kosz, pełny selektor i osobna lista instancji. Czysta biblioteka ma Namiot i Wieżę. Sekcje ikon/obiektów mają kompaktowy podgląd; pozostałe pozycje dostępne w pełnych selektorach. Zachowana szerokość panelu i osobne przewijanie terenów.
- Dodawanie z pliku lub grafiki drugiej biblioteki. Edycja nazwy/grafiki z pliku oraz wizualny wybór z obu bibliotek, podgląd i domyślnie wyłączona aktualizacja instancji. Wspólne selektory i edytor, bez równoległego systemu importu. Kopia grafiki ma niezależne ID wpisu; jednakowe bajty nie oznaczają wspólnej edycji. Wybrana grafika nie zmienia nazwy wpisu.
- Pliki object-Namiot.json / object-Drużyna (2).json ze wspólną ochroną nazw Windows i sufiksami. Mały prefiks, zachowane litery nazwy, wewnętrzne ID niezależne od pliku. Bez migracji/czyszczenia prywatnej biblioteki.
- Jeden aktywny materiał teren/ikona/obiekt albo brak materiału. Wybór wzorca wybiera Pędzel do pojedynczego umieszczania; klik tworzy jedną instancję, drag nie stempluje, Wiadro nie tworzy obiektów, ustawienia figur są nieaktywne. Instancję można umieścić poza heksami bez snapowania.
- Po udanym OK edycji ikony aktywny materiał jest pusty, także bez zmian. Narzędzie pozostaje, brak automatycznego zamiennika i dodatkowego wpisu/gwiazdki; Pędzel/Wiadro bez materiału nie edytują. Anulowanie zachowuje wybór, mgła nie wymaga materiału.
- Zaznaczanie klik/Ctrl, prostokąt i zamykane lasso według centrów. Wariant wybiera się prawym przyciskiem na Zaznaczaniu. Klik wybiera górną instancję; lista daje dostęp do pozostałych. Ruch grupy zachowuje wzajemne przesunięcia, pomija blokady z krótką informacją, działa poza mapą i jest jednym Undo. Delete usuwa instancje, nie wzorce; nie przechwytuje tekstu ani dawnego Delete terenu.
- Prawy panel instancji: nazwa, Sojusznik/Neutralny/Wróg, rozmiar, blokada, odkrywanie i promień. Jawne Zastosuj właściwości jest jednym wpisem; grupa pokazuje liczbę bez masowej edycji. Heks ma dodatkowo Ikona: nazwa/brak, pod mgłą ???.
- Lista instancji z nazwą, miniaturą, stroną i Poza mapą; Zaznacz bez centrowania oraz Pokaż na mapie, które uwzględnia oba panele. W menu Dopasuj jawne Dopasuj mapę i Dopasuj wszystko; drugie obejmuje całe grafiki i obiekty poza mapą. Bez obiektów identyczne z dopasowaniem mapy. Dopasowania nadal celowo pomijają prawą nakładkę.

## Mgła, pozycje i geometria

Neutralny, odkrywanie wyłączone, odblokowany ruch to ustawienia początkowe. Rozmiar 48, promień 1. Rozmiar oznacza najdłuższy bok 8–1024 jednostek świata, z zachowaniem proporcji; promień 0–20 pełnych pierścieni. Do 5000 instancji, pozycje świata ±10 000 000. Środek to geometryczny środek prostokąta grafiki, nie jej nieprzezroczystych pikseli.

Odkrywa tylko Sojusznik z checkboxem. Neutralny/Wróg mają widoczne nieaktywne kontrolki, a warunek obowiązuje też w logice. Zmiana strony wyłącza checkbox i skuteczne odkrywanie, powrót nie włącza go sam. Wyłączenie zachowuje promień. Włączenie/zwiększenie promienia odsłania otoczenie i razem z ustawieniem stanowi jeden wpis. Nowe umieszczane instancje są bezpiecznie neutralne; wspólna logika umieszczenia obsługuje również uprawnioną instancję.

Promień 0 to rzeczywisty heks pod środkiem, 1 dodaje sześciu sąsiadów itd. Nie używa rozmiaru grafiki lub najbliższego heksa spoza mapy. Ruch interpoluje pozycję środka co najwyżej co 9 jednostek świata i odkrywa promień każdego uprawnionego członka grupy. Próbkowanie odcinka jest ograniczone do obwiedni mapy, aby odległe pozycje nie generowały milionów pustych próbek. Po wyjściu z płótna lub utracie capture nie ujawnia nieznanego łącznika. Utrata capture kończy gest. Odkrywanie jest skutkiem operacji, nie pętlą co klatkę. Odebranie uprawnień/usunięcie nie zasłania ponownie; Undo zmienia wyłącznie Fog zapisane w delcie gestu, bez wcześniejszych odkryć.

Obiekty i ich zwykłe obrysy są przycinane przez wielokąty zasłoniętych heksów, także gdy część dużej grafiki wystaje poza mapę; tam mgły nie ma. Zwykły hit-test nie przechodzi przez mgłę, wybór obszarem pomija całkowicie zasłonięte. Nie ma tooltipów/nazw ujawniających ukryte grafiki. Lista instancji jest świadomym narzędziem edytora, może pokazać także zasłonięte dane. Nie jest to zabezpieczony widok gracza lub szyfrowanie.

Resize zachowuje instancje także nad usuniętymi heksami. Przelicza współrzędne przez rozszerzoną siatkę z przesunięciem indeksów i parzystości; zachowane heksy zatrzymują obiekty i lokalny offset, pozamapowe przesuwają się spójnie. Orientacja zachowuje indeks rozszerzonej siatki i offset w lokalnych osiach heksa: obrót offsetu 30° między orientacjami, bez obracania samej grafiki. Surowe XY offsetu przy brzegu mogłoby znaleźć się już w sąsiednim heksie po zmianie wielokąta, stąd ten wybór. Nie dodajemy fikcyjnych komórek. Geometria i obiekty są jednym Undo i nie wykonują odkrywania drogi. Crop nie dodaje obiektów do listy utraconych danych.

## Historia, biblioteka i format

Wspólny stos 300 obejmuje obiektowe delta, fog-delta, plik wzorca oraz istniejące operacje mapy/ikon/presetów. Sam plik biblioteki nie daje fałszywej gwiazdki mapy. Aktualizacja wzorca i pasujących instancji to jeden wpis; pozycje, strona, blokada i odkrywanie pozostają. Usunięcie wzorca nie usuwa instancji lub osadzonych grafik.

Biblioteka używa tego samego LibraryFiles: dokładne bajty w RAM, oczekiwane źródło/ścieżka, zapis bez nadpisania obcego celu, rollback i jawny konflikt. Dodatkowo kontroluje duplikaty logicznego ID wzorca. Stos/pamięć zmieniają się po udanym przejściu dyskowym; można rozwiązać konflikt i ponowić. Usuwanie bez Kosza; potwierdzenie mówi o limicie/sesji i utracie cofania po resecie/wypadnięciu/zamknięciu. Reset nie usuwa aktywnych plików. Nowa/Otwórz resetują zaznaczenie i historie dopiero po sukcesie; błędny odczyt/anulowanie zachowują dokument.

Format **4** dodaje obowiązkowe Objects z pełnymi właściwościami i stabilnymi logicznymi źródłami. Uzasadnienie: starszy program nie powinien otworzyć mapy i zgubić całej nowej warstwy przy zapisie. Nie implementowano konwersji starszych map. IconAssets pozostaje wspólnym magazynem PNG po SHA-256; grafika wspólna dla heksowej ikony i obiektów zapisywana jeden raz, bez wspólnej tożsamości edycyjnej. Zapisuje się wyłącznie użyte zasoby, nie biblioteka. Wszystkie instancje, także poza mapą, trafiają do pliku. Limity wspólne: 128 grafik, 2 MiB PNG/zasób i około 32 MiB PNG łącznie, plik do 128 MiB. Odczyt waliduje jawne pola, ID, pozycje, rozmiary, strony/promienie i referencje przed podmianą oraz nie wykonuje odkrywania ponownie. Bezpieczny zapis i zwalnianie źródeł pozostały.

## Testy metod i kontrolek — rzeczywiście wykonane

Nie uruchamiano Run-AllTests, starych zestawów ani równoważnej pełnej kampanii. Nowy runner wymaga pojedynczego Case. Dane w tests/_work, po testach pozostał tylko results.log. Końcowe wyniki:

| Przypadek | Wynik i dokładny przebieg |
| --- | --- |
| geometry | PASS, object-040-3e47550be06c4d65bf9eb2b2a5002ebb: obie orientacje/lokalny offset, mieszany resize i Undo/Redo, wspólne PNG ikony i dwóch obiektów, jeden poza mapą, zapis format 4 i odczyt bez biblioteki bez ponownego odkrycia, błędny Side nie zmienia dokumentu. |
| gestures | PASS, object-040-9de5623a165f4ef78c3a523c095121ab: jeden klik/bez drag-stamp/bez wiadra obiektów; Neutralny/Wróg i Sojusznik, promień 0/1 i niezależność od rozmiaru; droga z jednym Undo/Redo i zachowanie wcześniejszej mgły; blokada, prostokąt/lasso/Ctrl, ukryty hit-test i widoczność poza mapą. |
| library | PASS, object-040-09e0d70ad8d64c72813e70d26f6dd44f: czytelny plik, brak fałszywej gwiazdki, edycja z/bez aktualizacji i zachowanie innych właściwości, usunięcie bez usunięcia instancji, dokładne bajty Undo/Redo, obcy plik pod odtwarzaną ścieżką bez nadpisania/przesunięcia stosu i ponowienie; kopia grafiki do niezależnej ikony, brak materiału nie maluje; Pokaż, fit i reset historii bez kasowania aktywnego pliku. |
| view | PASS, object-040-f77fd4cb92604ad6bcd47d163e6ed1fd: piksele clippingu części obiektu i widoczność poza mapą; proporcje, bounds pełnych grafik i fit bez obiektów; ruch dwóch odblokowanych i jednego zablokowanego z offsetami/jednym wpisem; brak odkrycia nieznanego łącznika, odległy odcinek z ograniczonym próbkowaniem i środek poza mapą, Undo orientacji. |
| input | PASS, object-040-07b5b66359684750ad283d0341850982: fokus nazwy obiektu/terenu/płótna i routing Delete, usunięcie instancji i Undo; wspólna blokada gestu zamykanego dialogu i pozostałości dwukliku nie umieszcza obiektu. To test metod/kontrolek, nie fizycznego klawisza w zwykłym EXE. |

Pierwsza geometria PASS (object-040-d7814f501e21453a8a710f3e20c07b76) została powtórzona po istotnej korekcie lokalnych osi offsetu. Pierwsza biblioteka PASS (object-040-2fc5202c894a4ab7980dc2f76412cf3f) powtórzona po kontroli kolizji ID. Wczesne nieudane próby zachowano w logach: lasso częściowo poza płótnem (231b1fed…), Ctrl z już wypełnionym wyborem (5b49560c…) — poprawiono warunki prób; Pokaż w niewidocznym formularzu (a0755c8e…) — obliczenie uwzględnia teraz logicznie otwarty prawy panel. Ponowiono tylko te krótkie scenariusze. Nowe późniejsze view/input sprawdzały bezpośrednio dodane zabezpieczenia, nie stare kampanie. Historyczne testy pozostały; dawne fixture formatu 3 nie wymusiły konwertera.

## Faktyczne próby zwykłego EXE

Własny przebieg `tests/_work/ui-040-8695843684594b94bdc4e71b594aeed6`, zwykły EXE bez diagnostycznych haków, własna mapa 8×5 z mgłą po prawej, własna czysta biblioteka:

1. Wybór Namiotu i klik poza mapą utworzył widoczny obiekt poza heksami; kolejny klik utworzył drugą instancję nad B2. Wzorzec miał pojedynczą złotą ramkę, ustawienia figur nieaktywne.
2. Zaznaczanie pokazało właściwości instancji, Neutralny i nieaktywne odkrywanie. Wybrano Sojusznik, checkbox i Zastosuj; drag z B2 do D2 odkrył otoczenie/drogę. Jedna górna strzałka Undo cofnęła ruch i jego mgłę, Redo je odtworzyło.
3. Prostokąt objął dwa obiekty, w tym poza mapą. Panel pokazał Wybrane obiekty: 2 i nieaktywne pola. Nie wykonano fizycznego Ctrl/lassa lub ruchu całej grupy w oknie; są sprawdzone metodami.
4. Zapis Ctrl+S: format 4, dwie instancje i jeden zasób PNG; mapa bez gwiazdki. Po zamknięciu i ponownym uruchomieniu odczyt zachował oba obiekty i mgłę, bez ponownego odkrywania.
5. Pierwsze otwarcie listy wykryło ArgumentException w ImageList.CreateHandle: miniatury były Dispose przed utworzeniem natywnego ImageList. Poprawiono czas życia do zamknięcia dialogu. Po przebudowie ponowiono listę: nazwy/miniatury/strona/Poza mapą poprawne. Pokaż na mapie wycentrowało poza-mapowy obiekt w obszarze pomiędzy lewym i prawym panelem, bez gwiazdki. Nie powtarzano zaliczonych gestów po tej lokalnej naprawie.
6. Wybrano ikonę Czaszki, otwarto edycję i OK bez zmian: złota ramka materiału zniknęła, nie wybrano zamiennika, narzędzie zostało Zaznaczaniem, mapa czysta i historia pusta po odczycie. Widoczne były przyciski obu bibliotek.

Po późniejszej ochronie Delete końcowy główny EXE przebudowano do 0.4.0.0. Jego identyczną kopię (SHA256 `7425AD5AE71E9C1B77B853F4274F3994BB9C694827ED3714248BCE1B370C0D47`) uruchomiono w tym samym własnym katalogu i sprawdzono start; nie powtarzano wcześniejszych scenariuszy. Nie uruchamiano głównego EXE z prywatną Library jako danych testowych. Nie zamykano okien użytkownika, nie było blokady głównego builda. Weryfikowano rzeczywiste okna Computer Use, nie tylko niepełną listę procesów.

## Ograniczenia, pominięte próby i porządek

Nie wykonano pełnej macierzy, dawnych kampanii, Worda/opisów/tabel, DPI lub wydajności dużych map. Nie wykonywano ponownej diagnostyki zaakceptowanych figur/interpolacji/regionów/przeskoku panelu. Próba przeciągnięcia ramki w celu zmniejszenia okna nie zmieniła jego rozmiaru — nie zaliczono wizualnej próby minimum. Ręcznie nie przećwiczono wszystkich wariantów bibliotek/importów, obu kierunków kopiowania, checkboxów aktualizacji, usuwania plików, konfliktów lub geometrycznych zmian; wskazane przypadki mają sprawdzenia metod albo pozostają w checkliście. Fizycznego utraconego capture nie testowano nagraniem; test view sprawdza reset interpolowanej drogi, input wspólną blokadę dialogu. Nie deklarujemy gwarancji każdej możliwej awarii dysku/wyścigu procesów lub kompleksowego testu aplikacji.

Nie ma znanego pozostawionego wyjątku listy — naprawę potwierdzono ponownym otwarciem. Brak masowej edycji właściwości grupy jest uzgodnioną granicą. Przy wielokrotnym wyborze stare wartości pól są wyszarzone, wiążąca jest liczba wybranych instancji. Użytkownik powinien zweryfikować wygodę wariantu lassa dostępnego przez prawy przycisk. Nie dodano światła, widoku gracza, tekstur, zakładek, kadrowania lub pełnego redesignu. Światło/ciemność zapisano jako niezależne przyszłe wymaganie. Walki/pathfindingu/animacji/tras nie dopisano do planów.

Wszystkie własne artefakty tylko tests/_work. Źródła testów/skrypty pozostają w Git; logi są ignorowane. Prywatna Library, mapy, kopie migracyjne i Kosz pozostawione bez testów/modyfikacji/czyszczenia. Główny bin nie był używany. Po potwierdzeniu końca własnych procesów pozostawiono wyłącznie logi własnych przebiegów; usunięto własną mapę/EXE/Library ui-040 oraz build-040. git ls-files nie wykazał śledzonych Library/tests/_work/bin; git check-ignore potwierdził te reguły. Paczka aplikacji nie zawiera tests lub prywatnej biblioteki.

## Checklista użytkownika

- Wybierz wzorzec, kliknij nad heksiem i poza mapą; drag nie tworzy serii, Wiadro nie umieszcza. Jeden materiał ze złotą ramką.
- Zaznacz klik/Ctrl, prostokątem i lasso (prawy przycisk na Zaznaczaniu). Przesuń grupę z blokadą; sprawdź pominięcie zablokowanego i jedno Undo/Redo. Delete usuwa instancje, pisanie i skróty terenów działają osobno.
- Zmień nazwę/stronę/rozmiar/blokadę przez Zastosuj. Neutralny/Wróg nie odkrywają; Sojusznik z checkboxem ma promień 0/1, niezależny od rozmiaru. Powrót do Sojusznika nie włącza checkboxa.
- Przesuń odkrywającego sojusznika szybko przez mgłę; sprawdź drogę, pierścienie, pozostawione odkrycie oraz jedno Undo/Redo. Wyjdź z płótna i wróć: brak łącznika nieznanej drogi.
- Duża grafika nad mgłą ma zasłonięte części i obrys; fragment poza mapą widoczny. Lista instancji daje świadomy dostęp do ukrytych obiektów.
- Sprawdź listę/Pokaż poza mapą, Dopasuj mapę i Dopasuj wszystko, także dla dużego obiektu. Otwarcie szczegółów samo nie rusza kamery.
- Zmień orientację i cztery strony resize; zachowany heks i lokalny offset, obiekty nad przyciętymi polami nie znikają; Undo/Redo odtwarza geometrię i obiekty bez nowej drogi mgły.
- Dodaj/edytuj wzorzec i ikonę z pliku/obu bibliotek; wybór grafiki nie zmienia nazwy, kopia ma niezależną edycję. Checkbox aktualizacji jest domyślnie wyłączony; aktualizacja nie zmienia innych właściwości instancji.
- Na własnym testowym wzorcu sprawdź nazwę/podmianę/usunięcie i Undo/Redo pliku. Usunięcie bez Kosza, tylko sesyjne odzyskiwanie do 300; obcy plik pod ścieżką powoduje konflikt, nie nadpisanie.
- OK edycji ikony odznacza materiał także bez zmian, Anuluj zachowuje. Pędzel/Wiadro bez materiału nic nie robią. Szczegóły heksa pokazują ikonę, pod mgłą ???.
- Zapisz mapę z obiektem poza mapą, otwórz sam plik w czystej kopii 0.4.0; grafiki i właściwości pozostają, mgła nie jest odkrywana ponownie. Nie używaj starszego programu do zapisu formatu 4.

## Uruchomienie i commit

Uruchom HexMapEditor.exe lub Uruchom HexMapEditor.bat; budowanie przez Build.ps1 z lokalnego .NET Framework. Commit obejmujący raport i źródła 0.4.0 zostanie podany w końcowej odpowiedzi i jest dostępny przez git log -1. Nie wykonano push, tagu lub Release'a.
