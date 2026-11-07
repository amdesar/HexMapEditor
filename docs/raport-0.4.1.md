# Raport HexMapEditor 0.4.1

## Baza i wykonanie

Baza: czysty master `8d422b10de2bf37f281c5f7b98b065859b114ffe` (0.4.0). Praca wyłącznie w HexMapEditor-Codex, bez zastanych zmian; przeczytano README, raport 0.4.0, odpowiednią specyfikację i kod. Nie znaleziono dodatkowych AGENTS.md w sprawdzonym repozytorium/katalogach nadrzędnych. Origin: https://github.com/amdesar/HexMapEditor.git. Nie wykonywano fetch ani synchronizacji; lokalne origin/master wskazywało bazę. Nie jest to sprawdzenie aktualnego stanu serwera.

Zbudowano główny zwykły **HexMapEditor.exe 0.4.1.0**. Format mapy **5**, pozostałe formaty bibliotek bez zmiany. Windows Forms/GDI+ i lokalny WPF opisu, offline, bez nowych zależności/usług. Bez push/tagu/Release'a i przepisywania historii. To celowany przyrost, nie kompleksowo przetestowana aplikacja.

## Zmiany względem zlecenia

- Osobne Zaznaczanie, Prostokąt i Lasso w dwurzędowym Przyborniku, z ikonami/podpowiedziami/ramką. Usunięto wybór lassa prawym przyciskiem. Rączka przesuwa trafiony widoczny obiekt/grupę lub kamerę nad tłem, respektuje blokady i nie otwiera automatycznie panelu. Pusty klik poza mapą zamyka heksowy panel; przeciąganie nie jest takim kliknięciem.
- Właściwości instancji działają bez Zastosuj. Nazwa i liczby mają logiczne sesje; checkbox i stan to osobne operacje. Niepoprawne/przejściowo puste wartości nie trafiają do modelu. Sesja kończona przed inną operacją/zapisem/historią/zamknięciem. Dla bieżących właściwości sprawdzamy nagłówek i zmienione komórki, bez serializowania opisów całej mapy po każdym znaku. Nowe instancje mają 30, stare rozmiary zachowane. Czerwony Usuń instancję i poprawne Enabled kosza wzorca.
- Nazwa wzorca jest nazwą początkową; późniejsze nazwy instancji pozostają niezależne. Aktualizacja wzorca zmienia tylko grafikę pasujących instancji, nie ich nazwy; poprawiono napis checkboxa. Semantyka ikon heksowych bez zmiany.
- Kafelki Sojusznik/Neutralny/Wróg i plus własnego stanu dokumentu. Nazwa, kolor, uprawnienie odkrywania i stabilne ID, z zawijaniem. Dodanie nie wybiera stanu za użytkownika; definicja podlega historii i zapisowi. Bez edycji/usuwania stanów.
- Trwałe grupy z własnym ID/nazwą/członkami, najwyżej jedna grupa na instancję, bez nesting. Grupuj co najmniej dwa niezgrupowane; mieszany wybór ostrzega. Klik/Ctrl traktują grupę jako jednostkę, Alt wybiera członka, również przed wyborem grupy. Prostokąt/lasso rozszerzają wybór do grupy. Panel grupy i członka udostępnia wybór członka, przejście, odłączenie, rozwiązanie oraz usunięcie. Delete usuwa instancje atomowo i czyści referencje; jeden członek może pozostać grupą.
- Odłącz/Rozwiąż ma Zachowaj wynikowe / Przywróć indywidualne / Wybierz dla każdej właściwości / Anuluj. Kategorie: stan, rozmiar, blokada, odkrywanie, promień. Nazwy/pozycje i wcześniejsze odkrycia pozostają. Członkostwo, właściwości i potrzebna delta Fog są jednym wpisem.
- Aktywne efektywne zasięgi są uzgadniane po ręcznej mgle, ruchu/umieszczeniu i właściwych zmianach ustawień/członkostwa. Ręczna mgła nie zasłania aktywnego odkrywcy także w trakcie gestu. Bez pętli klatkowej i bez nowego uzgadniania podczas Undo/Redo/odczytu; odtwarzana jest końcowa delta, wcześniejsze odkrycia nie są cofane.
- Podgląd bez mgły jest wyraźną flagą widoku, nie zmianą dokumentu. Pokazuje realne dane i pozwala je edytować; zapisuje rzeczywiste Fog, resetuje się po udanym Nowa/Otwórz. Próba ręcznej mgły pyta o normalną widoczność. Nie nic nie zmienia, Tak przywraca widoczność, ale wymaga ponownego gestu.
- Lista ma nazwę/miniaturę, efektywny stan, pozycję i mgłę. Częściowe/pełne ukrycie daje ??? i neutralną miniaturę, bez zdradzającego tooltipu. Podgląd pokazuje realne dane, kolumna Mgła nadal stan rzeczywisty. Klasyfikacja przez geometrię przecięcia prostokąta z heksami, bez analizy pikseli. Poza mapą brak mgły; lista nadal pozwala świadomie wskazać/Pokaż ukryty obiekt.
- Wspólna obwiednia i algorytm Dopasuj mapę/wszystko, z tym samym marginesem. Obie funkcje pomijają prawą nakładkę. Obiekty mieszczące się w obwiedni mapy nie zmieniają kadru; wystające uwzględniają efektywne rozmiary. Menu otrzymało lokalny ciemny styl; Otwórz nadal dopasowuje mapę.

## Baza, nakładka i odkrywanie

MapObject przechowuje bazowe właściwości. ObjectGroup zawiera opcjonalne StateId, Locked, RevealsFog, RevealRadius oraz SizeMode/SizeValue. Brak nadpisania oznacza Indywidualnie. Effective tworzy wynik bez modyfikowania bazy. Rozmiar jest bazowy, bazowy plus przesunięcie (clamp 8–1024) albo wspólny; bez kumulacji i ruchu środka. Grupowe false blokady może odblokować bazowo zablokowanego. Panel Alt-członka pokazuje wartości bazowe i wyjaśnienie wyniku.

Efektywny stan wyznacza uprawnienie, dopiero potem działa efektywny checkbox/promień 0–20. Zakaz stanu nigdy nie jest obchodzony nakładką. Indywidualna zmiana do stanu bez uprawnienia wyłącza bazowy checkbox; ograniczenie grupowe go zachowuje. Przy odłączaniu zachowanie wynikowych ustawia efektywną bazę z kontrolą uprawnienia, przywrócenie odsłania dawną bazę. Nie zasłaniamy dawnych odkryć. Wspólny limit historii 300 i bajtowy LibraryFiles/ochrona konfliktów pozostają; pliki bibliotek nie są przebudowywane przez grupowanie.

## Zapis

Format 5 jest potrzebny dla obowiązkowych ObjectStates i Groups oraz Side jako ID definicji. Zawiera bazowe instancje, referencje/nakładki i dotychczasowe dane heksów. IconAssets nadal deduplikuje użyte grafiki ikon/obiektów, bez całej biblioteki lub powiązania edycji po identycznych bajtach. Podgląd nie jest serializowany. Nie dodano odczytu dawnych formatów ani konwersji; starszy plik pozostaje nietknięty. Walidacja ID/referencji/jednego członkostwa/liczb/zakresów przed podmianą. Odczyt nie odgrywa odkrywania ponownie. Bezpieczny zapis pozostaje.

## Celowane testy metod i kontrolek

Wykonano tylko nowe scenariusze Run-Groups041Tests, bez Run-AllTests ani starych kampanii:

| Case | Wynik i zapis w tests/_work |
|---|---|
| model | PASS, groups-041-813bfb481a0144a2b8e7a1244b44b910/results.log |
| fog | PASS; końcowa próba po zmianie śledzenia dirty: groups-041-a8ecbc28e88f40e5a8150ef735ebb9e0/results.log |
| controls | PASS; końcowa próba po zmianach live number/dirty: groups-041-7b7042d7810d4d5fa1d17278676b3341/results.log |

Model łączy Ctrl/Alt, grupę bez zmiany bazy, własne stany/overlay/lock/rozmiar, keep/restore/kategorie/cancel, rozwiązanie, czyszczenie referencji, Undo/Redo i format 5 bez biblioteki oraz odmowę powtórzonego członkostwa. Fog łączy uprawnienia i zasięgi, ręczne wiadro/końcową deltę, Rączkę/ruch grupy z zachowaniem wcześniejszych odkryć, preview/odmowę/zgodę, listę partial/full/outside, zapis prawdziwej mgły i reset podglądu przy odczycie bez ponownego odkrywania. Controls sprawdza scalanie nazwy/wpisywanej liczby, dyskretny checkbox, gwiazdkę/Undo do zapisu, fit przy efektywnym rozmiarze, pusty klik, przyciski, niezależne nazwy wzorca i kosz.

Pierwsza próba controls nie przeszła: punkt fixture nazwany pustym mieścił się wewnątrz celowo powiększonej grafiki. Poprawiono punkt fixture i próba przeszła; log błędu zachowany. W rzeczywistym oknie wykryto liczbę stosowaną dopiero po opuszczeniu pola; poprawiono jedną ścieżkę wpisywania i powtórzono tylko dotknięty controls oraz sam wpis liczby w oknie. Fog powtórzono po zmianie bezpośrednio dotyczącej śledzenia delty/dirty. Nie powtarzano model po zmianach wyłącznie UI/dirty. Źródła dawnych testów zachowane; ich oczekiwania formatu 4 nie stanowią wymagania konwersji do 5.

## Rzeczywiste próby zwykłego EXE

Własny zwykły EXE i mapa w tests/_work/ui-041-2d77aaabd332462794a5b24dd455d677, bez prywatnej Library. Faktycznie wykonano:

1. Widoczne osiem narzędzi w dwóch rzędach. Rączka przesunęła instancję bez otwarcia prawego panelu; prostokąt wybrał dwie.
2. Utworzenie nazwanej trwałej grupy, początkowo wszystkie nakładki Indywidualnie. Zmiana grupowego stanu i przesunięcia rozmiaru, bez Zastosuj. Po lokalnej poprawce wpis 20 zmienił grafikę natychmiast przed opuszczeniem pola.
3. Zapis i ponowne otwarcie grupy. Rączka przesunęła grupę pod mgłę, zachowując panel. Lista pokazała ???/neutralne miniatury oraz Częściowo ukryty i Ukryty. Podgląd pokazał realne Goblin/Ork, miniatury i efektywny stan; rzeczywista kolumna Mgła pozostała.
4. Zapis podczas aktywnego podglądu; kontrola własnego pliku: format 5, jedna grupa, bazowe rozmiary 30/30, przesunięcie 20, niezmienione nazwy i 20 zasłoniętych pól z 40.

Rozwiązanie testowej grupy w zwykłym EXE: po wybraniu Przywróć indywidualne ustawienia obie instancje pozostały na mapie, grafika wróciła do rozmiaru bazowego i pojawiła się gwiazdka. Jedno Undo przywróciło członkostwo Goblina i Orka, stan Sojusznik i przesunięcie rozmiaru +20. Gwiazdka zniknęła. Po Undo ponowny wybór członka otworzył panel grupy. Końcowy główny EXE ma SHA256 5C0A53B212DAA9B2EA301C764819CC807CE6A4517ED5D46372A49858014E888F. Jego identyczna kopia została uruchomiona i czysto zamknięta we własnym smoke-041-eca1077a3d6747458c007ea3e147b5cb; log pozostał, artefakty tej próby i build-041 sprzątnięto. Po próbie rozwiązania grupy zamknięto własne okno i usunięto jego artefakty, zachowując results.log w katalogu ui-041.

## Ograniczenia i niewykonane próby

Nie uruchomiono pełnej kampanii, dawnych regresji, Worda/opisów/DPI, kampanii dużych map ani ponownej diagnostyki zaakceptowanych geometrii, interpolacji, regionów i zwijania 0.4.0. Ctrl/Alt i warianty odłączania, dodawanie stanów oraz odmowa/zgoda preview sprawdzone metodami, nie fizycznymi gestami/dialogami w oknie. Brak pełnej macierzy grup/blokad i wszystkich kombinacji UI. Minimalne okno i styl menu nie mają osobnej próby wizualnej w tej łatce. Klasyfikacja częściowej mgły dotyczy prostokąta grafiki, wlicza przezroczyste marginesy. Nie jest to zabezpieczony widok gracza.

Nie wdrażano pozycji poza zakresem (nesting, światło, redesign, opisy, zakładki, tekstury, kadrowanie, optymalizacja dużych map). Brak dodatkowych brakujących funkcji zamówionego modelu; wymienione ograniczenia dotyczą zakresu weryfikacji i przyjętej geometrii maskowania.

## Checklista użytkownika

- Zaznacz/Prostokąt/Lasso, Ctrl grupa, Alt członek; Rączka obiekt/grupa/tło i pusty klik poza mapą.
- Bieżąca nazwa/liczby/checkbox, Undo/Redo sesji, nowy rozmiar 30, Usuń instancję; nazwy wzorca i instancji niezależne.
- Grupuj, stan/offset/wspólny rozmiar/blokada, Alt-baza i brak jej utraty. Własny stan z uprawnieniem i bez.
- Odłącz/Rozwiąż: keep/restore/kategorie/anuluj i Undo; usunięcie członka/grupy bez pustych referencji.
- Ręczna mgła nad aktywnym zasięgiem, ruch i jedno Undo bez cofania wcześniejszych odkryć.
- Podgląd bez mgły: brak gwiazdki, zapis rzeczywistej mgły, odmowa/zgoda ręcznego gestu, konieczność powtórzenia; reset po otwarciu.
- Lista: partial/full maskowane, poza mapą jawny, podgląd rzeczywistych danych; dwa identyczne kadry dla grafik mieszczących się w mapie.
- Zapis kopii i ponowny odczyt grup/stanów/bazy bez biblioteki. Starsze mapy otwierać starszym programem.

Uruchamianie: HexMapEditor.exe lub Uruchom HexMapEditor.bat; build lokalny .\Build.ps1. Commit implementacji: 7d07c59 (Implement HexMapEditor 0.4.1 persistent groups and fog preview). Po nim osobny lokalny zapis niniejszego stanu dokumentacji; końcowy identyfikator podaje odpowiedź oraz git log -1. Kontrola stagingu: tylko 28 plików źródeł/testów/dokumentacji i główny EXE, żadnych plików Library ani tests/_work. git diff --cached --check bez błędów, reguły ignorowania potwierdzone. Próba rozwiązania grupy i Undo zakończyła się pozytywnie. Bez publikacji.
