# Specyfikacja użytkowa — HexMapEditor
Wersja 2.5. Stan decyzji 11 listopada 2026.
Istniejący projekt HexMapEditor-Codex. Bieżący przyrost 0.5.1. Pierwszeństwo mają ustalenia sekcji 32.

## 1. Cel i zasady odczytywania wymagań
Budujemy uniwersalny, jednoosobowy edytor map heksagonalnych na Windows. Pierwsze zastosowanie to kampania RPG, w tym mapy kosmosu, kontynentów i powierzchni planet. Treści użytkownika nie ograniczamy do konkretnego świata ani systemu RPG. Nie implementujemy reguł podróży, dni, encounterów, kart postaci ani modułu walki.
Ten dokument określa zachowanie produktu, nie języki, frameworki, format wewnętrzny ani bibliotekę renderowania. Wykonawca ma dobrać i krótko uzasadnić technologie. Wymagania początkowe opisywały nowy prototyp; obecnie rozwijamy istniejącą aplikację Windows Forms / GDI+ bez rozpoczynania od nowa. Funkcje docelowe nie są automatycznie funkcjami ukończonego przyrostu.
Sekcje 2–15 opisują zakres docelowy. Sekcja 16 wyznacza pierwszy przyrost. Całości nie należy wykonywać jednym wielkim zadaniem. Funkcje odłożone oznaczono wprost. Przykładowe nazwy i treści nie są gotowymi pakietami do dołączenia.

## 2. Samodzielny program offline
Gotowy program uruchamia się na komputerze bez internetu. Nie wymaga konta, serwera sieciowego, abonamentu, API ani pobierania dodatkowej zawartości przy zwykłym uruchamianiu. Możliwy instalator lub wersja przenośna — wybór należy do wykonawcy. Uruchamianie ma być wygodne, przez dwuklik, bez ręcznego wpisywania komend i uruchamiania serwera.
Internet podczas tworzenia aplikacji, pobierania narzędzi i budowania jest dopuszczalny. Nie mylić trybu offline produktu z odcięciem internetu środowisku programistycznemu. Wszystkie elementy potrzebne do działania danej dostarczonej wersji muszą być dostępne lokalnie.
Interfejs, pomoc dla użytkownika i komunikacja z użytkownikiem po polsku. Nazwy wewnętrzne kodu dobiera wykonawca. Aplikację obsługuje jedna osoba; obraz można pokazywać na Discordzie przez udostępnianie okna.

## 3. Siatka, współrzędne i stan początkowy
Liczba kolumn i wierszy jest niezależna, od 1 do 200 w każdej osi. Dopuszczalne m.in. 1×1, 10×1, 20×30, 60×60 i 200×200. Wartości muszą być całkowite. Żadna oś nie może być zerowa, ujemna ani przekraczać 200.
Nowa mapa domyślnie: 15 kolumn × 10 wierszy; nazwa „Nowa mapa”. Każde otwarcie dialogu Nowa mapa proponuje te wymiary, bez dziedziczenia wymiarów poprzedniej mapy. Przy obu osiach widoczny zakres 1–200. Przy starcie i utworzeniu mapy pokaż całość wyśrodkowaną z marginesem, uwzględniając lewy panel. Prawy panel jest nakładką celowo pomijaną przy Dopasuj.
Obsługiwane dwie orientacje heksów: wierzchołkiem do góry i płaskim bokiem do góry. Można wybrać orientację nowej mapy i zmienić ją później. Zmiana zachowuje logiczne przypisanie danych do pól. Nie zakładać, że zmiana orientacji zachowa identyczne geometryczne sąsiedztwo: zastosować spójne, udokumentowane mapowanie.
Każdy heks ma współrzędne, np. E15, AE3. Konwencję techniczną i identyfikatory ustala wykonawca, ale muszą być stabilne i jednoznaczne.
Tryby etykiet przełączane w kolejności: Osie → W polach → Brak. Domyślnie Osie dla nowych map. Osie: przynajmniej litery nad mapą i liczby po lewej, zgodnie z pozycjami pól. W polach: etykieta blisko dołu heksu, nie na środku. Przy oddaleniu wolno ograniczać etykiety dla czytelności. Tryb Brak usuwa tylko napisy na mapie, nie dostępność współrzędnych w panelu i pasku stanu.

## 4. Nawigacja i płynność
Kółko myszy przybliża i oddala mapę względem kursora. Zoom nie zmienia danych ani wymiarów mapy. Można przesuwać widok. Przytrzymanie środkowego przycisku myszy pozwala przesuwać mapę niezależnie od aktywnego narzędzia.
Wskaźnik procentowy pokazuje skalę względem bazowego rozmiaru rysowania; tooltip wyjaśnia znaczenie. Kliknięcie ustawia 100%, zachowując środek bieżącego widoku. Przycisk Dopasuj pokazuje całą mapę z marginesem i centruje ją. Nie potrzeba osobnych przycisków plus/minus ani dublującego resetu kamery.
Przełączenie orientacji i dopasowanie kamery mają pojawić się razem, bez widocznej klatki ze zmienioną orientacją i starym dopasowaniem. Nie wymagamy ozdobnej animacji.
Celem jest wygodna, płynna praca; nie wymagamy konkretnej liczby FPS. Typowe mapy są mniejsze od maksimum. Widok całej bardzo dużej mapy może być mniej płynny, lecz zwykły przybliżony widok powinien działać sprawnie. Nie wolno przedstawiać czasu pojedynczego obliczenia jako dowodu płynności całej aplikacji.

## 5. Layout
Estetyka: ciemny, czytelny interfejs, bez przeładowania.
Górny pasek ma trzy grupy: po lewej Nowa/Otwórz/Zapisz/Zapisz jako/Undo/Redo; w środku nazwa mapy i gwiazdka przy niezapisanych zmianach; po prawej wymiary/orientacja/etykiety/procent zoomu/Dopasuj/Biblioteka. W węższym oknie całe grupy mogą przejść do kolejnego rzędu. Przyciski nie mogą być ucinane; teksty „Zapisz *” i wymiary nie zawijają się wewnątrz przycisku. Długą nazwę można skrócić z pełnym tooltipem. Zapisz ma wyróżniać niezapisane zmiany, a po zapisie być spokojniejszy wizualnie.
Lewy panel, względnie wąski: Przybornik, Teren, Ikony, Obiekty. Przycisk zwija go do wąskiego paska służącego wyłącznie ponownemu rozwinięciu. Nie zamieniamy panelu w miniaturowy zestaw ikon narzędzi. Długie sekcje przewijają się niezależnie, szczególnie Teren; zapewnić dostępność przy niskim oknie.
Zwinięcie/rozwinięcie lewego panelu nie zmienia skali ani pozycji mapy na ekranie; może tylko odsłonić lub zasłonić obszar. Nie uruchamia Dopasuj.
Prawy panel jest wyraźnie szerszy od lewego, do wygodnego czytania. Nie narzucamy pikseli ani proporcji. Jest ukryty bez zaznaczenia, pokazuje szczegóły klikniętego heksu lub obiektu, ma przycisk zamknięcia. Nakłada się na mapę bez jej skalowania, przesuwania czy ponownego dopasowania.
Dolny pasek: współrzędne pod kursorem, widoczne/wszystkie pola, orientacja oraz rzetelnie opisane informacje wydajnościowe, jeśli są mierzone.

## 6. Heks i jego dane
Heks posiada typ terenu, wygląd wypełnienia, opcjonalną nazwę własną i opis, stan mgły oraz przypisane ikony. Treść jest dowolna. Nie narzucamy kategorii planet, miast, gór itp.
Wypełnienie to ALBO kolor, ALBO grafika. Grafika zastępuje widoczne wypełnienie kolorem. To nie wyklucza oddzielnych ikon ani tła całej mapy.
Typ terenu to co innego niż nazwa własna i opis. Zmiana terenu nie wykonuje zamiany słów w notatce. Przykład: zmiana Gór na Wodę nie usuwa nazwy „Przełęcz Cieni” ani wpisanego opisu.
Opcjonalne, czysto informacyjne pole trudności terenu pozostaje do rozważenia, poza pierwszą wersją. Nie liczymy na jego podstawie czasu ani kosztu podróży.

## 7. Narzędzia
Wybór terenu i wybór narzędzia są niezależne. Zmiana terenu nie aktywuje Pędzla i nie wyłącza Wiadra.
Zaznacz: kliknięcie wybiera heks i pokazuje panel; przeciągnięcie przesuwa widok bez przypadkowego zaznaczania. Próg odróżnienia dobiera wykonawca.
Rączka: przesuwa widok bez wybierania pól.
Pędzel: maluje aktywnym terenem kliknięciem i przeciąganiem. Jedno ciągłe pociągnięcie jest jedną operacją Undo.
Wiadro: wypełnia spójny obszar sąsiadujących heksów tego samego logicznego typu terenu. Nie zmienia oddzielonych obszarów na całej mapie. Zastępuje typ i wypełnienie, zachowując nazwę własną, opis, mgłę, niezależne ikony i obiekty. Jedno użycie to jedna operacja Undo. Kolor sam w sobie nie definiuje tożsamości typu.
Pędzel i Mgła mają proste rozmiary: 1 pole; pole i pierwszy pierścień (7); pole i dwa pierścienie (19); pole i trzy pierścienie (37). To zakres heksowej odległości, niezależny od zoomu; na brzegach uwzględnia tylko istniejące pola. Podgląd zakresu przed kliknięciem nie zmienia danych.
Reset: czerwony akcent, kosz, nazwa „Reset”. Usuwa nazwę, opis, przypisane ikony i indywidualne wypełnienie, przywracając neutralny teren; usuwa też mgłę, odkrywając pole. Nie usuwa heksu, współrzędnych, tła mapy ani niezależnego obiektu nad polem. Domyślnie działa pojedynczym kliknięciem. Opcjonalne „Reset przeciąganiem” włącza ciągłe resetowanie; jedno przeciągnięcie to jedna operacja. Bez powiększonego pędzla resetowania. Undo przywraca całą zawartość, także mgłę.

## 8. Mgła wojny
Stan zakrycia jest zapisywany dla heksów, ale nie niszczy ich danych. Mgła całkowicie zakrywa wygląd: różne zakryte pola wyglądają jednakowo, bez ujawniania koloru, grafiki i przypisanych ikon.
Narzędzie ma jawne tryby Zakryj/Odkryj, kliknięcie i przeciąganie. Zakrywanie nie odkrywa zakrytego pola przez ponowne dotknięcie. Odkrycie pozostaje do czasu ponownego ręcznego zakrycia. Jedno pociągnięcie to jeden krok Undo.
Kliknięcie zakrytego heksu pokazuje współrzędne, lecz nazwę, opis i typ zastępuje „???”, bez próbki ujawniającej wygląd. To maskowanie interfejsu, nie szyfrowanie pliku. Edycja ukrytej treści wymaga świadomego odkrycia.
W panelu jest przełącznik Zakryty/Odkryty, również dla zakrytego pola. Obie drogi sterowania muszą używać tego samego stanu.
Automatyczne odkrywanie przez obiekty oraz oświetlenie nie należą do bieżącego zakresu. Zachowanie swobodnych obiektów względem mgły wymaga rozstrzygnięcia przed ich wspólnym wdrożeniem; nie zakładać automatycznie, że statek drużyny ma być niewidoczny.

## 9. Teren i zestawy terenów
Sekcja nazywa się Teren. Pozycje mają nazwę, próbkę koloru lub grafiki, edycję i usunięcie. Edycja nazwy/wyglądu typu NIE zmienia wcześniej pomalowanych pól. Usunięcie pozycji z listy również ich nie zmienia i nie usuwa użytych plików. Dane historycznie zastosowanego wyglądu muszą pozostać odtwarzalne.
Czytelne okno edycji terenu: nazwa, wybór koloru lub grafiki, podgląd, Zastosuj i Anuluj. Nie wymagać od użytkownika zgadywania, że musi zamknąć picker kliknięciem poza nim, aby znaleźć drugi zapis. Anuluj nie zmienia danych.
Zestawy terenów są trwałymi kolekcjami używanymi w wielu mapach. Dostarczamy tylko Domyślny, z neutralną bazą: Teren neutralny, Woda, Równina, Góry, Pustynia, Czerń/Pustka. Inne zestawy tworzy i nazywa użytkownik.
Można zapisać bieżącą paletę jako nowy zestaw, świadomie aktualizować zapisany zestaw, wybrać zestaw przy tworzeniu mapy i przełączać podczas pracy. Przełączanie nie przemalowuje mapy. Robocze kopie w projekcie są odseparowane od biblioteki. Powrót do wcześniejszej roboczej palety nie gubi jej niezapisanych do biblioteki zmian. Projekt odtwarza potrzebne palety i powiązania po otwarciu. Format przechowywania dobiera wykonawca.

## 10. Biblioteka i przygotowanie grafik
Biblioteka znajduje się w podfolderze Library obok gotowego programu, na tym samym dysku. W development jest w głównym folderze projektu. Nie w Dokumentach ani w bazie przeglądarki. Nie dodajemy teraz wyboru innej lokalizacji.
Kategorie co najmniej: tła całej mapy, grafiki heksów, ikony, obiekty. Nie narzucamy angielskich nazw folderów. Przycisk Biblioteka otwiera folder w Eksploratorze. Po imporcie zasoby pozostają do ponownego używania także przy innych mapach.
Program sprawdza zapis do folderu; brak uprawnień daje czytelny błąd, nie ciche przeniesienie danych ani wymaganie uruchamiania jako administrator. Dystrybucję należy dopasować do pracy w zapisywalnym folderze użytkownika. Aktualizacja, budowanie i deinstalacja nie mogą bez ostrzeżenia usuwać własnej biblioteki.
Import przez przyciski odpowiednich kategorii. PNG preferowany; JPG również obsługiwany. Zachowujemy przezroczystość PNG. Nie wymagamy od użytkownika przygotowania dokładnych wymiarów i proporcji.
Okno podobne do kadrowania awatara: przesuwanie, przybliżanie, oddalanie i wybór fragmentu; podgląd heksu lub ikony, zatwierdzenie/anulowanie. Obraz heksu prezentowany w sześciokątnym kształcie. Rozdzielczość, maskowanie i sposób przechowywania ustala wykonawca.
Ikona może mieć przezroczyste albo celowo zachowane tło. Nie usuwać tła automatycznie. PÓŹNIEJ: opcjonalne lokalne usuwanie tła, nieograniczone wyłącznie do jednego koloru. Dobór narzędzia, modelu, warunków rozpowszechniania i rozmiaru pozostawić na ten etap; podstawowy import ma działać bez tej funkcji. Jakość ważniejsza od minimalnego czasu; nie obiecywać idealnych wyników.

## 11. Ikony i obiekty
Ikona jest przypisana do heksu. Obiekt ma niezależne położenie i może swobodnie leżeć również na granicy pól. Nie zastępować obiektu ręcznym przestawianiem ikony między heksami.
Panel ikon: dwa rzędy ulubionych i dwa ostatnich; orientacyjnie 5–6 ikon w rzędzie, do dopasowania wizualnego. Limit ulubionych odpowiada pojemności dwóch rzędów. Nie usuwać ulubionych przez samo zwężenie okna. Pełna biblioteka w osobnym oknie z miniaturami, wyszukiwaniem po nazwie i zarządzaniem ulubionymi. Zestawy ulubionych nie należą do pierwszej implementacji.
Obiekt: własna grafika, nazwa, opis, położenie, opcjonalna informacja o właścicielu. Kliknięcie używa tego samego prawego panelu co heks. Bez systemu statystyk konkretnego RPG.
Obszar kontaktu obiektu jest kołem wokół jego środka, z określonym promieniem. Podświetlamy wszystkie heksy dotykane przez ten obszar, także przy styku z krawędzią. Nie tylko pole pod środkiem, nie prostokąt obrazu. Promień i jego kontrolkę projektuje wykonawca. Podgląd/podświetlenie nie zmienia właściwego wypełnienia. Po odejściu obiektu pole wygląda jak wcześniej.
Pozycje i kontakt są niezależne od zoomu. Przed wdrożeniem ustalić i przetestować zachowanie obiektów przy zmianie orientacji i przycinaniu mapy; rozszerzenie nie może zostawiać obiektu nad inną treścią przez przypadkowy błąd przeliczenia.

## 12. Rozszerzanie i zmniejszanie mapy
Dodawanie i usuwanie wierszy/kolumn z KAŻDEJ strony: góra, dół, lewo, prawo. Chodzi o zmianę istniejącej mapy, nie tworzenie nowej.
Formularz każdorazowo startuje od czterech zer. Wartości odnoszą się do bieżącego stanu: +2 dodaje dwa z danej strony, −2 usuwa dwa, 0 nic nie zmienia. Ponowne otwarcie po Zastosuj lub Anuluj znów zeruje formularz. Podgląd obecnego i wynikowego rozmiaru, czytelny kierunkowy układ i informacja „+ dodaje, − usuwa”. Dopiero zatwierdzenie modyfikuje mapę.
Końcowy rozmiar w każdej osi musi wynosić 1–200. Przykład: mapa 10×1 nie może stracić jedynego wiersza. Nie przyjmować zera, ujemnych wartości wynikowych ani przekroczenia maksimum; blokować z wyjaśnieniem.
Rozszerzanie zachowuje treść, względny układ i sąsiedztwo starych pól. Dotyczy to także dodania pojedynczego wiersza/kolumny od góry lub lewej: naprzemienne przesunięcia heksów nie mogą rozsypać starego obszaru. Współrzędne wyświetlane mogą się przeliczyć; tożsamość i dane nie mogą się zgubić. Mechanizm geometryczny dobiera wykonawca.
Nowe pola mają stan domyślny. Usuwanie ostrzega o stronie, zakresie i utracie danych, wymaga potwierdzenia. Anulowanie nic nie zmienia. Cała zmiana wymiarów to jeden krok Undo, przywracający także usunięte opisy, grafiki i mgłę. Zmiana orientacji jest zapisywana i oznacza projekt jako zmieniony, ale nie musi należeć do Undo/Redo.

## 13. Notatki jako czytelna strona
Prawy panel domyślnie pokazuje tytuł i treść do czytania, nie stale aktywne pola formularza. Ołówek uruchamia edycję; nazwa i opis mogą mieć osobne wejścia. Zastosuj/Anuluj. Nie gubić po cichu roboczej treści przy zmianie zaznaczenia czy zamykaniu panelu — zapewnić jawne rozstrzygnięcie.
Prosty edytor: pogrubienie, kursywa, podkreślenie, akapity i wcięcia, listy punktowane/numerowane, kilka rozmiarów lub nagłówków, separator. Bez tabel, osadzanych obrazów i rozbudowanego zarządzania linkami w pierwszej wersji.
Wklejanie z Worda zachowuje obsługiwane proste formatowanie, m.in. wielkość tekstu, pogrubienie, kursywę i podkreślenie. Nie obiecujemy identycznego odtworzenia dowolnego dokumentu. Wklejana treść nie może uruchamiać kodu ani niejawnie pobierać zasobów z sieci. Notatki muszą mieć trwały zapis formatowania i możliwość wyprowadzenia zwykłego tekstu do eksportu. Dobór reprezentacji i edytora należy do wykonawcy.

## 14. Zapis, eksport i ochrona danych
Edytowalny plik projektu ma odtwarzać całą mapę do dalszej pracy, nie tylko obraz. Wczytanie przywraca wymiary, orientację, dane heksów, nazwy, opisy i formatowanie, wypełnienia, ikony, mgłę, obiekty i potrzebne palety. Format, rozszerzenie i biblioteki nie są narzucone; struktura ma być udokumentowana i wersjonowana.
Wariant lekki: dane i odwołania do lokalnej biblioteki, bez duplikowania obrazów.
Wariant przenośny: komplet danych i wszystkich UŻYTYCH obrazów, do otwarcia na innym komputerze bez osobnego kompletowania grafik. Nie musi zawierać całej nieużytej biblioteki. Brakujące zasoby i kolizje mają być obsługiwane czytelnie, bez cichego nadpisywania.
Zapisz aktualizuje bieżący plik; przy braku ścieżki pyta o nią. Zapisz jako zawsze pyta, a po sukcesie nowy plik jest bieżący. Zapis nie zeruje Undo. Nieudany zapis nie może niszczyć istniejącego poprawnego pliku. Nie nadpisywać po cichu nieobsługiwanej nowszej wersji.
Osobny eksport obrazu CAŁEJ mapy: co najmniej PNG i JPG, wybór rozsądnej rozdzielczości i bezpieczne limity; nie tylko screenshot widocznego fragmentu. Inne formaty graficzne opcjonalne według uzasadnionej propozycji.
Osobny eksport tekstowy: uporządkowane współrzędne, nazwy i opisy heksów, np. kolumna A, potem B. TXT i/lub czytelny format tekstowy dobiera wykonawca. Eksport obrazu i tekstu nie zastępuje pliku edytowalnego. Przed implementacją ustalić uwzględnianie zakrytych danych w eksportach; nie ujawniać ich przypadkiem.
Przed nową mapą, otwarciem innej i zamknięciem (X, Alt+F4) ostrzegać o niezapisanych zmianach: Zapisz/Odrzuć/Anuluj. Błąd zapisu i anulowanie wyboru pliku pozostawiają aplikację otwartą. Gwiazdka i wyróżnienie Zapisz odpowiadają faktycznym różnicom względem zapisanego stanu, także po Undo.

## 15. Historia i sprawdzanie
Undo/Redo do 300 operacji w bieżącej sesji projektu, bez niepotrzebnego kopiowania całej mapy dla małych zmian. Historia nie jest zapisywana do pliku. Nowy projekt lub wczytanie pliku zaczynają nową historię. Zapisz i Zapisz jako jej nie czyszczą. Zamknięcie programu kończy historię sesji. Skróty w edytorze tekstu nie powinny omyłkowo cofać działań mapy.
Sprawdzać funkcje podczas ich dodawania, nie dopiero na końcu. Kluczowe scenariusze: zapis/odczyt rzeczywistej treści; limity wymiarów; geometria rozszerzania z każdej strony w obu orientacjach; reset i mgła; brak retroaktywnych zmian terenów; spójny flood fill; cofanie operacji grupowych; panele bez przeskoków kamery; brak ucinania kontrolek; samodzielne uruchamianie. Raportować tylko wykonane testy, oddzielać testy automatyczne od oceny UI. Nie obiecywać pełnej odporności na każdą awarię.

## 16. Pierwszy przyrost — nie cały zakres naraz
Najpierw przeczytać dokument i krótko uzasadnić wybór technologii. Pytania tylko o sprawy blokujące; nie prowadzić wielu rund ogólnego planowania.
Pierwszy działający fragment: okno desktopowe z układem stanowiącym bazę opisanego layoutu; nowa mapa 15×10 z wyborem wymiarów i orientacji; pan i zoom; wybór pola; prosta nazwa i zwykły opis; malowanie kolorem; edytowalny zapis i odczyt; wygodny sposób uruchomienia do testu. Panele powinny od początku respektować brak przesuwania/skalowania przy otwieraniu i zamykaniu. Puste przyszłe sekcje nie mogą udawać gotowych funkcji.
Pozostałe wymagania pozostają wiążącym planem docelowym, lecz wchodzą w kolejnych małych przyrostach. Nie trzeba w pierwszym zadaniu wdrażać formatowania, wszystkich narzędzi, zestawów, bibliotek obrazów i eksportów. Zapis rozwijać razem z nowymi danymi, zachowując kompatybilność własnych wcześniejszych wersji.
Po każdym przyroście: wykonać build/testy, uruchomić jeśli możliwe, podać rzeczywiste wyniki i krótką checklistę użytkownika, następnie zatrzymać się do oceny. Zachowywać punkty powrotu kodu przed większymi zmianami. Nigdy nie usuwać biblioteki użytkownika jako części porządkowania projektu.

## 17. Ustalenia dla prototypu 0.1.1
Poniższe ustalenia zostały przyjęte dla pakietu poprawek 0.1.1 i mają pierwszeństwo przed sprzecznymi opisami wcześniejszych sekcji:

- Numer aplikacji (0.1.0, 0.1.1 itd.) jest niezależny od wersji formatu mapy. 0.1.0 oznacza pierwszy prototyp, 0.1.x kolejne pakiety poprawek, 0.2.0 większy przyrost, a 1.0.0 przyszłą wersję do normalnego użytkowania.
- Heksy nie mogą tworzyć narastającego skosu. Dla orientacji wierzchołkiem do góry stosuj naprzemienne przesunięcie co drugiego wiersza; dla płaskiego boku do góry — co drugiej kolumny. Logiczna kolejność wierszy i kolumn oraz dane pól pozostają bez zmian.
- Orientację i tryb etykiet zmieniają przyciski cykliczne na pasku. Etykiety przechodzą Osie → W polach → Brak → Osie. Tryb etykiet jest zapisywany w projekcie; brak tego pola w starszym pliku oznacza Osie. Pozostaje częścią formatu mapy 1, dopóki zmiana struktury tego nie wymaga.
- Zmiana orientacji i przeliczenie Dopasuj są wykonywane przed jednym odświeżeniem widoku. Zamknięcie prawego panelu usuwa zaznaczenie; ponowne kliknięcie zaznaczonego heksu je przełącza. Zoom, Dopasuj, zapis i odświeżanie nie otwierają panelu ponownie.
- Prawy panel nakłada się bez zewnętrznych szczelin od paska narzędzi do paska stanu i dochodzi do prawej krawędzi obszaru roboczego. Informacja o terenie jest wyświetlana w jednej linii. Pełne formatowanie notatek pozostaje poza tym przyrostem.
- Jeśli liczba faktycznie widocznych heksów nie jest mierzona, pasek stanu pokazuje uczciwe „Łącznie heksów: X”.
- Rozszerzeniem projektu jest `.hexmap` (JSON w środku). Nie dopisuje się go ponownie, jeśli użytkownik już je podał.
- Ograniczenia z pakietu 0.1.1 nie rozszerzają zakresu funkcji 0.1.0 o wiadro, mgłę, zestawy terenów, grafikę, obiekty ani eksport.

## 18. Ustalenia dla aktualizacji technicznej 0.1.3

Te decyzje mają pierwszeństwo przed sprzecznymi opisami wcześniejszych sekcji. Wersja aplikacji wynosi 0.1.3, a format mapy nadal 1. Zachowujemy C# / Windows Forms / GDI+, działanie offline i poprawki historii malowania z 0.1.2.

- Zapisz jako ustala końcową ścieżkę po dopisaniu `.hexmap`, a dopiero potem wyświetla jedno potwierdzenie nadpisania istniejącego pliku. Dotyczy to także `mapa.txt` → `mapa.txt.hexmap`. Rozszerzenie jest rozpoznawane bez rozróżniania wielkości liter. Odmowa/anulowanie nie zmieniają celu, bieżącej ścieżki ani stanu zapisu. Zwykłe Zapisz do bieżącego pliku nie pyta ponownie. Zachowujemy bezpieczny zapis i obsługę błędów.
- Biblioteka jest wyznaczana względem uruchomionego EXE, bez zależności od bieżącego katalogu roboczego. Tworzymy brakujące `Library/Backgrounds`, `Terrains`, `Icons`, `Objects`, `Hexmaps`, `Exports/Images` i `Exports/Text`, zachowując istniejącą zawartość. Niepowodzenie daje czytelny komunikat, bez cichej zmiany lokalizacji.
- Dla niezapisanego projektu dialog zapisu proponuje `Library/Hexmaps`; Zapisz jako otwartego projektu proponuje katalog bieżącego pliku. Użytkownik może wybrać inny katalog. Nie migrujemy wcześniejszych map ani nie usuwamy `bin`.
- Nowa mapa zawsze proponuje 15×10, pokazuje zakres 1–200 przy obu polach i domyślnie używa `Axes`. Nie dodajemy nazwy projektu ani presetów do dialogu w tym przyroście.
- Widoczne nazwy orientacji to Pointy Top (wierzchołek na górze) oraz Flat Top (pozioma krawędź na górze). Przycisk, dialog, status i rysunek muszą być spójne. Identyfikatory danych pozostają `PointyTop` i `FlatTop`; geometria pozostaje bez zmian.
- Prawy panel jest nakładką celowo pomijaną przy Dopasuj. Lewy panel jest już prawidłowo uwzględniany i zachowujemy to zachowanie. Otwarcie lub zamknięcie paneli nie dopasowuje kamery.
- Zmiana orientacji jest zapisywana i oznacza projekt jako zmieniony; nie musi należeć do Undo/Redo. Historia malowania nie przywraca starszej nazwy, opisu ani `NotesFormat`.
- Niewielka tolerancja trafiania przy zewnętrznym brzegu heksu jest obecnie akceptowana i nie jest zadaniem do poprawienia w 0.1.3.
- Testy metod nie zastępują testów rzeczywistego dialogu Windows. Bieżące miejsce pracy testów określa sekcja 19; sprzątamy tylko własne dane.
- Zakres 0.2.0 pozostaje odłożony: formatowane notatki i rozbudowana historia tekstu, nazwa projektu i drugi rząd paska, Wiadro, presety i edycja terenów, zmiana rozmiaru mapy, ikony, większa przebudowa paneli, własne ostrzeżenia i pasek tytułu. Katalogi zasobów nie oznaczają gotowego importu ani eksportu. Optymalizacji nie wdrażamy na podstawie przypuszczeń.

## 19. Mała łatka 0.1.4

Te decyzje mają pierwszeństwo przed sprzecznymi opisami wcześniejszych sekcji. Wersja aplikacji: 0.1.4; format mapy nadal 1. Nie dodajemy funkcji edytora z 0.2.0.

- Obsługa płótna wymaga własnego początku gestu na płótnie. Zdarzenia przycisku użytego w dialogu nie mogą malować, zaznaczać ani przesuwać nowo otwartej mapy. Po dialogu wymagane są zwolnienie przycisków i nowy gest. Nie wyłączamy otwierania dwuklikiem i nie opieramy ochrony wyłącznie na czasie. Zachowujemy narzędzie i czyścimy stan gestów oraz przechwycenie; zasada dotyczy Otwórz, Nowa mapa, zapisu i ostrzeżeń.
- Nowa mapa zachowuje 15×10, zakresy 1–200, Axes i angielskie nazwy orientacji. Wiersze formularza mają kolejność: etykieta Kolumny/Wiersze, pole liczbowe, osobny zakres `(1–200)` kursywą po prawej, z odstępem i bez obcinania.
- Źródła i skrypty testów pozostają śledzone w `tests/`. Wszystkie tworzone przez nie artefakty trafiają do oddzielnych przebiegów w `/tests/_work/`, ignorowanego i odtwarzanego przez skrypty. Nie używamy systemowego TEMP, innych folderów ani użytkowego Library do własnych map kontrolnych; nie zmieniamy globalnych TEMP/TMP. Docelowa paczka użytkownika nie zawiera `tests/`.
- Wolno usunąć wyłącznie sprawdzone stare artefakty i znane mapy kontrolne z głównego `bin`; pusty, niepotrzebny `bin` również można usunąć. Nie usuwamy danych o nieustalonym pochodzeniu ani głównego EXE. Sprzątanie nie jest częścią startu aplikacji.
- Główne `/Library/`, `/tests/_work/` i `/bin/` są ignorowane. Biblioteka jest tworzona przez aplikację w strukturze ustalonej dla 0.1.3, bez usuwania istniejących danych. Jej śledzenie w Git kończymy bez przepisywania historii i bez usuwania lokalnych zasobów. Lokalizacja map i zachowanie Zapisz/Zapisz jako pozostają bez zmian.

## 20. Rozbudowa 0.2.0

Ta sekcja opisuje zaakceptowany przyrost i ma pierwszeństwo przed sprzecznymi wcześniejszymi ustaleniami. Aplikacja 0.2.0, zapis mapy w formacie 2, odczyt formatu 1. Sekcje o szerszym zakresie docelowym (grafiki, obiekty, mgła, eksport itd.) pozostają planem późniejszych przyrostów; ich brak nie jest błędem 0.2.0.

### Interfejs i nazwa

Pierwszy rząd paska: operacje plikowe i historia mapy po lewej, ustawienia widoku i zmiana rozmiaru po prawej. Drugi, zwarty rząd zawiera wyśrodkowaną, nieedytowalną nazwę projektu. Nie ma pola edycji nazwy mapy na głównym pasku. Nowa mapa otrzymuje nazwę początkową (domyślnie Nowa mapa), każdorazowo 15×10, osobne kursywą `(zakres: 1–200)` na prawo od obu pól, orientację i wybór działającego presetu. Etykiety bez selektora, zawsze domyślnie Axes. Dialog zachowuje szerokość 420; nie poszerzamy go z powodu zakresów.

Do pierwszego zapisu wyświetlamy nazwę początkową i proponujemy ją jako nazwę pliku. Po udanym zapisie lub otwarciu źródłem nazwy jest nazwa pliku z usuniętym wyłącznie końcowym `.hexmap` (bez rozróżniania wielkości liter). `mapa.txt.hexmap` daje `mapa.txt`. Wewnętrzne `Name` nie nadpisuje tej nazwy. Zapisz zachowuje ścieżkę; Zapisz jako aktualizuje nazwę/ścieżkę wyłącznie po sukcesie. Anulowanie/błąd niczego tu nie zmieniają; ponowne otwarcie respektuje zmianę nazwy pliku w Eksploratorze.

Przyciski plikowe i Biblioteka mają ikonę i polski tekst; historia mapy same zakrzywione strzałki; orientacja heks i Pointy Top / Flat Top; etykiety ikonę i krótki tryb; zoom procent; Dopasuj ikonę i tekst. Ikony narzędzi mają polskie podpowiedzi, nazwy dostępne i czytelny stan aktywny także przez ramkę. Własne symbole rysujemy w GDI+, bez emoji i pobierania zasobów. Reszta interfejsu pozostaje polska. Systemowy pasek tytułu i ostrzeżenia pozostają systemowe.

Paleta jest dwukolumnowa, narzędzia mają całe ramki. Zwinięty lewy panel to wąski pasek rozwijania na całej wysokości mapy. Dopasuj uwzględnia bieżącą szerokość lewego panelu. Prawy panel pozostaje nakładką, z marginesami i przewijaniem; jego otwieranie/zamykanie nie zmienia kamery, a Dopasuj celowo nie odejmuje szerokości nakładki. Układ ma mieścić się przy minimalnym rozmiarze okna 1180×680. Niewielka dotychczasowa tolerancja trafiania przy zewnętrznym brzegu heksu pozostaje zaakceptowana.

### Dokument i edytor

Zachowujemy C# / Windows Forms / GDI+, offline i lokalne pliki. Formatowany edytor to WPF RichTextBox / FlowDocument osadzony przez ElementHost z istniejącego pełnego .NET Framework, bez pobierania nowej biblioteki, płatnego komponentu lub usługi. Weryfikacja lokalna przed integracją obejmuje rzeczywiste Table/Row/Cell, listę numerowaną i zapis/odczyt RTF; natywne Undo nie stanowi podstawy historii produktu.

Opis obsługuje rozmiar czcionki, pogrubienie/kursywę/podkreślenie/przekreślenie, akapity, cztery podstawowe wyrównania, listy punktowane i numerowane. Wstawianie tabeli pyta o liczbę wierszy/kolumn. Można edytować komórki, dodawać/usunąć wiersz lub kolumnę przy bieżącej komórce i usunąć tabelę. To struktura dokumentu, nie tekst z tabulatorami.

Wklej korzysta z RTF lub zwykłego tekstu. Przed importerem usuwa grupy obiektów/obrazów, metadane i instrukcje pól, zachowując tekst wynikowy pól; po imporcie dokument ogranicza się do obsługiwanego modelu. Nie ładuje aktywnego XAML, HTML, pakietów, obiektów ani zasobów internetowych. W razie nieudanej konwersji używa tekstu Unicode, a przy jego braku zgłasza błąd bez zmiany opisu. Oddzielne Sam tekst wkleja bez formatowania. Nie zapewniamy pełnego układu Worda: nieobsługiwane obrazy, obiekty, scalenia tabel, kroje/kolory tekstu i zaawansowany układ są upraszczane. Zachowujemy możliwy do odczytania tekst. Próba syntetycznego RTF nie jest testem Worda.

Zapis opisu jest zamkniętym modelem JSON: akapit, run/break, lista/element, tabela/wiersz/komórka, obsługiwane właściwości stylu. Brak wykonywalnych odwołań. Limit 2 Mi znaków JSON, 20 000 elementów i głębokość 32; wymiary pojedynczej tabeli do 100×100 w ramach wspólnego limitu dokumentu. Odrzucenie zbyt dużej/niepoprawnej edycji przywraca poprzednią treść z komunikatem.

### Oddzielne historie

Historia nazwy i historia opisu (łącznie ze stylem, listami i tabelami) są wielopoziomowe i przypisane do stabilnego ID heksu oraz pola. Ctrl+Z/Ctrl+Y przy aktywnym edytorze dotyczą wyłącznie jego historii; poza edytorami historii mapy. Górne przyciski dotyczą zawsze mapy, panel ma własne przyciski tekstu. Nie używamy dwustanowego Undo natywnego TextBox jako historii produktu.

Przejścia między heksami nie mieszają historii, a powrót w sesji ją zachowuje. Nowa/Otwórz i restart zerują ją; zapis jej nie zeruje ani nie utrwala. Nowa edycja po Undo usuwa odpowiednie Redo. Pisanie grupujemy przy przerwie poniżej 750 ms, najwyżej przez 5 s, z przerwaniem po nawigacji/zmianie pola. Formatowanie, tabele i wklejanie opisu mają osobne operacje. Limit 100 kroków na pole i 32 MiB dodatkowych migawek Undo/Redo łącznie, z pominięciem bieżącej treści dokumentów; przekroczenie usuwa najstarsze dostępne kroki i w razie potrzeby Redo. Przy pisaniu porównujemy stan pojedynczego heksu, nie serializujemy całej dużej mapy.

Historia mapy do 300 operacji obejmuje malowanie, Wiadro, aktualizację konkretnej wersji terenu na mapie i zmianę rozmiaru. Małe operacje przywracają wyłącznie właściwości terenu, nigdy własną nazwę/opis/NotesFormat. Zmiana orientacji jest zapisywana i oznacza zmianę, ale pozostaje poza Undo/Redo. Edycja samych palet też nie jest cofaniem mapy. Zapis/Undo/Redo tekstu i mapy aktualizują oznaczenie niezapisanych zmian względem zapisanego stanu, a nie liczby wykonanych operacji.

### Tereny i presety

Selektor zestawu zastępuje dawny selektor pojedynczych terenów; kafelki wybierają teren. Dostępne tworzenie/wybieranie zestawów, zmiana nazwy i jawny zapis lokalny, dodawanie/edycja/usuwanie terenów z nazwą i kolorem. Zestaw domyślny odpowiada dotychczasowym sześciu terenom i jest odtwarzany z kodu. Pliki `Library/Terrains/*.terrainset.json` mają `PresetFormatVersion: 1`, stabilne ID zestawu i listę definicji. Edycje roboczych palet są też przechowywane w projekcie; Zapisz zestaw utrwala je w bibliotece.

Definicja ma stabilny `TerrainId`, niezmienny `VariantId`, rosnącą `Revision` i rozszerzalne `FillKind` (obecnie wyłącznie color). Zmiana nazwy/koloru tworzy nowy wariant. Właściwości tego samego wariantu muszą być spójne w pliku. Tożsamość nie opiera się na nazwie ani kolorze.

Zmiana zestawu, usunięcie terenu i domyślna edycja nie przemalowują istniejących heksów. Przełącznik Zastosuj do bieżącej mapy zawsze startuje wyłączony. Po włączeniu aktualizacja obejmuje tylko heksy bieżącej mapy używające dokładnie edytowanego starego VariantId i stanowi jeden wpis Undo. Inne warianty, pliki, własne nazwy i opisy pozostają nietknięte. Undo tej operacji dotyczy heksów, nie cofa palety. Mapa przechowuje własny wygląd i dane terenów, więc usunięcie lokalnego presetu nie odbiera jej czytelności.

### Wiadro i rozmiar

Wiadro od klikniętego heksu wypełnia jedynie spójny obszar tego samego VariantId, z sześcioma sąsiadami według orientacji i parzystości siatki. Stosujemy iteracyjną kolejkę, nie rekurencję. Nazwy/opisy pozostają; całe wypełnienie to jedna operacja Undo. Pusta operacja nie tworzy historii, nie usuwa Redo i nie zmienia stanu zapisu. Ochrona gestów z 0.1.4 działa również dla Wiadra i wszystkich nowych dialogów modalnych.

Zmiana rozmiaru wybiera górę/dół/lewo/prawo, dodawanie/usuwanie i liczbę. Wynikowe osie 1–200. Zachowane heksy pozostają tymi samymi obiektami i ID, ze wszystkimi danymi; indeksy są przeliczane. Nowe heksy mają nową tożsamość i kanoniczny neutralny teren, bez skopiowanych notatek/historii. RowParity/ColumnParity są korygowane przy przesunięciu od góry/lewej, zachowując względny układ i sąsiedztwo w obu orientacjach.

Podgląd usuwania liczy usuwane heksy i zawierające dane. Za dane uznajemy teren inny niż kanoniczny stan neutralny, własną nazwę, zwykły lub formatowany opis. Usuwanie takich pól wymaga ostrzeżenia tekstowego z obiema liczbami i potwierdzenia, domyślnie odmowa. Anulowanie nie zmienia mapy ani historii. Cały rozmiar jest jednym Undo/Redo, odtwarza usunięte heksy i wymiary. Historia malowania używa ID, a historia tekstu nadal dotyczy właściwego heksu; późniejsze notatki zachowanych heksów nie są cofane wraz z rozmiarem.

### Zgodność i granice przyrostu

Format 2 dodaje ID heksów, utrwalone wersje terenów, palety projektu, parzystości początku i strukturalne opisy `richDocument1`; czytelny Description pozostaje obok. Format 1 jest migrowany bez zmiany zwykłych opisów. Znane dawne pary terenu/koloru otrzymują domyślne ID, pozostałe deterministyczny wariant legacy z pary; format 1 nie pozwala odzyskać nieistniejącej informacji o historycznym pochodzeniu. Po otwarciu starej mapy informujemy o formacie 2 i braku odczytu przez 0.1.x. Zapisz jako umożliwia zachowanie oryginału.

Limit całego pliku 128 MiB dotyczy zarówno zapisu, jak odczytu, aby nie zapisać mapy, której aplikacja potem nie odczyta. Nieobsługiwany format, brak jawnej wersji, brak utrwalonych ID formatu 2 i niepoprawny dokument są błędami odczytu przed zastąpieniem bieżącej mapy. Zachowujemy bezpieczny zapis i potwierdzenie końcowej ścieżki z 0.1.3 oraz wszystkie zasady Library/Gitignore i tests/_work z 0.1.4. Paczka programu nie zawiera testów ani lokalnego Library. Nie implementujemy teraz eksportu obrazów/tekstu, importu grafik, obiektów, mgły ani własnych okien systemowych.

Testy 0.2.0 obejmują zapis dokumentów, oddzielne historie, presety/wersje, Wiadro, sąsiedztwo, wszystkie strony rozmiaru w obu orientacjach, Undo/Redo i ochronę danych, mapy formatu 1 oraz powroty z dialogów. Faktyczne wykonanie i ograniczenia prób okna/Worda/DPI określa raport, nie ta specyfikacja.
## 21. Poprawka po testach 0.2.1

Te ustalenia mają pierwszeństwo przed sprzecznymi wcześniejszymi zapisami. Rozwijamy istniejącą 0.2.0, zachowując technologię, offline, format mapy 2, odczyt 1, formatowane opisy i ochronę danych 0.1.2–0.1.4. Brak nowych bibliotek/usług.

- Pasek ma jeden rząd: pliki i wspólna historia po lewej, widok po prawej, a nieedytowalna nazwa projektu wyśrodkowana w dostępnym środku. Usuwamy drugi rząd i jego pustą wysokość. Długa nazwa ma wielokropek i pełną podpowiedź; nazwa/ścieżka nadal zmieniają się wyłącznie po udanym otwarciu/zapisie. Grupy nie nachodzą na siebie przy minimum okna. Przyciski mają wspólną wysokość, pionowe wyrównanie, rozmiar ikon i odstępy.
- Dopasuj nie ma stałego maksimum/minimum skali: liczy ją z granic mapy i wolnego prostokąta, z marginesem 32 px i miejscem na etykiety (28 px lewo, 24 px góra przy osiach). Zachowuje proporcje, uwzględnia bieżący lewy panel i pomija prawą nakładkę. Koło po Dopasuj nie może skoczyć do innego stałego zakresu; stosuje czynnik 1,12 z łagodnym wejściem do zwykłego zakresu ręcznego. Panele nie uruchamiają automatycznego Dopasuj.
- Prawy panel ma uchwyt na lewym brzegu i przycisk przywrócenia 440 px przy Zamknij, z podpowiedzią i nazwą dostępności. Szerokość jest pamiętana w sesji, minimum 340 px, maksimum 72% okna lub okno minus 280 px. Prawa krawędź pozostaje przy prawej krawędzi obszaru. Marginesy, edytor i szerokości tabel reagują na zmianę. Uchwyt ma własne przechwycenie i reset gestu; nie maluje/nie przesuwa mapy i nie zmienia kamery. Nie rozbudowujemy paska formatowania ani wysokości pustego edytora.
- Paleta zachowuje dwie kolumny, kafelki mają wysokość 52 zamiast 65 px. Dodaj teren jest nad nimi w stałym miejscu. Zamiast globalnych Edytuj/Usuń na kafelkach są ujawniane na hover/fokusie ołówek i kosz, także dostępne klawiaturą (F2, Delete i Tab). Hover nie wykonuje akcji. Kliknięcie akcji nie wybiera innego terenu ani nie otwiera dwóch dialogów. Ikony mają stałe pozycje, podpowiedzi i nazwy dostępne, bez zmian rozmiaru/układu kafelka. Nie przebudowujemy palety przy ruchu kursora, edycji opisu ani zwykłym wyborze kafelka; rzeczywiste zmiany aktualizują kontrolki i zbiorczo przeliczają układ, bez maskowania opóźnieniem.
- ID default wskazuje chroniony wzorzec Domyślny, niezależnie od jego roboczej nazwy. Nie można go trwale nadpisać/usunąć ani obejść ochrony zmianą nazwy. Robocza kopia może być edytowana i używana w projekcie. Nowy zestaw zaczyna od kanonicznego neutralnego terenu. Zapisz zestaw otwiera jawny wybór: nadpisanie bieżącego własnego zapisanego presetu lub zapis roboczej palety jako nowego z nazwą i własnym ID. Dla default i niezapisanego zestawu nadpisanie jest wyłączone z wyjaśnieniem. Kolizja nazw nie nadpisuje po cichu; błąd/anulowanie zachowują stan. Zachowujemy stare pliki default bez zmian, ale nie używamy ich do zastępowania chronionego wzorca i pokazujemy komunikat.
- Biblioteka i projekt mają osobne stany zapisu. Wskaźnik pokazuje, gdy robocza paleta różni się od zapisanego presetu. Zapis presetu nie oznacza projektu jako zapisanego. Migawki historii nie zapisują/usuwają plików; po Undo można świadomie ponownie zapisać różniącą się paletę. Zapis jako nowy tworzy/wybiera roboczą kopię z nowym ID, a cofnięcie tej zmiany projektu nie usuwa pliku.
- Dodanie/edycja/usunięcie roboczego terenu należy do tego samego chronologicznego stosu co malowanie, Wiadro i rozmiar. Nie tworzymy osobnego stosu. Zmiany roboczych zestawów, nazw i ich wyboru są także w tej historii. Przykład: jasne malowanie, ciemna edycja palety bez aktualizacji, ciemne malowanie — Undo cofa malowanie, kolejne Undo paletę, Redo odtwarza kolejność. Edycja z aktualizacją właściwych heksów jest jednym atomowym krokiem palety/mapy. Warianty są przywracane dokładnie, bez losowania nowych ID przy Redo. Zachowujemy własne nazwy/opisy/NotesFormat. Cofnięcie usunięcia odtwarza kolejność i aktywny wybór. Nowa rzeczywista operacja usuwa Redo; brak zmiany nie. Historie tekstu pozostają odrębne i skróty w edytorze nie uruchamiają stosu mapy/palety.
- Rozmiar ma cztery pola Góra/Dół/Lewo/Prawo, startowo 0: dodatnie dodają, ujemne usuwają. Wyświetla bieżący i wynikowy rozmiar. Wycinanie dotyczy brzegów starego prostokąta; suma usuwanych wierszy/kolumn nie może usunąć całej starej osi, także przy jednoczesnym dodawaniu po drugiej stronie. Plan jest walidowany jako całość przed zmianą danych, wynik 1–200. Wszystkie zera to brak operacji/historii. Pozostałe heksy zachowują ID i całą treść, nowe są neutralne, parzystości obu orientacji są korygowane. Całość to jedno Undo/Redo, zgodne ze starszymi wpisami i historiami tekstu.
- Ostrzeżenie o wycinaniu pokazuje liczbę wszystkich usuwanych heksów, liczbę heksów z danymi i pełną przewijaną listę ich współrzędnych PRZED zmianą. Narożnik liczy się raz. Lista nie jest skracana do początku ani nie powiększa okna ponad ekran. Domyślne Nie i anulowanie nie zmieniają mapy. Neutralny teren z nazwą/opisem też jest daną do ochrony.

Poza tą poprawką pozostają: całościowy wygląd narzędzi opisu, aktywne stany formatowania, zgłoszone nieprawidłowe pogrubienie przy pustym zaznaczeniu, wysokość/przewijanie pustego edytora, ewentualna paleta jednokolumnowa i dalsze nieomówione funkcje. Nie przedstawiamy ich jako naprawionych.

Testy obejmują Dopasuj powyżej 170% i pierwsze koło, chroniony preset/nowy zapis/błędy/anulowanie, wspólną chronologię, atomową aktualizację, cztery strony z mieszanymi znakami, koordynaty i unikalne liczenie, parzystości/opisy/Undo/Redo oraz gesty uchwytu. Wykonane próby metod i zwykłego końcowego EXE są oddzielone w raporcie. Własne pliki testowe wyłącznie w tests/_work; testy UI zapisu presetów korzystają z identycznej kopii EXE i biblioteki obok niej w _work, bez modyfikacji prywatnego Library. Bez push i Release'a.

## 22. Poprawka po testach 0.2.2

Ta sekcja ma pierwszeństwo przed sprzecznymi wcześniejszymi ustaleniami. Rozwijamy istniejącą 0.2.1; aplikacja 0.2.2, format mapy nadal 2 i odczyt 1, format presetu nadal 1. Zachowujemy offline, technologię oraz zabezpieczenia danych, końcowej ścieżki zapisu i gestów z 0.1.2–0.1.4. Nie przebudowujemy całego edytora opisów ani miejsca nazwy projektu. Ograniczone zawijanie i wielokropek nazwy są zaakceptowane.

### Biblioteka i migracja

Własne presety mają jeden aktywny plik na ID w `Library/Terrains/Presets/`, z rozszerzeniem `.terrainset.json`. Poprawny plik można przekazać innemu użytkownikowi; skopiowany ręcznie jest odczytywany co najmniej przy następnym starcie. Nie skanujemy rekurencyjnie `_backups` ani innych podfolderów. Walidujemy schemat, wymagane pola, ID/nazwy i warianty; błędy i kolizje nie mogą prowadzić do cichego nadpisania. Wzorzec Domyślny nadal pochodzi z kodu, jest chroniony przez ID i nie może być zastąpiony plikiem ani zmianą nazwy roboczej.

Starsze pliki bezpośrednio w `Library/Terrains/` migrują bez utraty danych. Przed zmianą każdego źródła powstaje sprawdzona bajtowa kopia w `Library/Terrains/Presets/_backups/`. Poprawny plik kolidujący z ID Domyślnego zostaje odzyskany jako własny preset z nową tożsamością, przy zachowaniu definicji/wersji terenów. Jedna równoważna własna kopia jest używana ponownie; nie powielamy jej. Niejednoznaczny konflikt lub uszkodzenie pozostawia źródło bez zmian i podaje problem do decyzji. Migracja jest idempotentna: drugi start nie odtwarza ostrzeżenia dla poprawnie przeniesionych danych, plików ani kopii. Nie ukrywamy pozostałych błędów. Mapy oraz zapisane w nich warianty pozostają bez zmian. Prywatne dane wolno dotknąć wyłącznie w ramach tej zabezpieczonej migracji, nigdy jako materiał testowy.

### Tworzenie, zapis, usuwanie i historia

Nowy zestaw pyta o nazwę i po zatwierdzeniu skutecznie zapisuje nowy własny preset z neutralnym terenem. Po sukcesie jest jeden wpis, od razu wybrany. Anulowanie, kolizja lub błąd nie pozostawiają fałszywego zapisanego wpisu. Późniejsze zmiany są robocze, bez automatycznego zapisu. Zapisz jako nowy z istniejącego zapisanego presetu świadomie tworzy kopię. Pierwszy zapis starszego niezapisanego wpisu nie pozostawia jego zbędnego duplikatu. Własny zapisany preset można jawnie nadpisać; Domyślny wymaga nowego ID.

Usuń preset dotyczy aktywnego własnego zapisanego pliku. Potwierdzenie wymienia jego nazwę, usunięcie z biblioteki, zachowanie danych mapy, brak cofania pliku przez Undo oraz ewentualne zmiany robocze. Preferowane jest przeniesienie do Kosza; nie stosujemy cichego trwałego zastępstwa. Błąd/anulowanie zachowują spójny stan i dają komunikat. Po sukcesie lista biblioteki jest aktualizowana bez zbędnego tworzenia wszystkich kafelków od nowa.

Wspólna historia mapy/palety pozostaje chronologiczna; historie nazwy/opisu oddzielne. Undo/Redo nigdy nie tworzy, nie usuwa ani nie nadpisuje plików biblioteki. Odtworzony roboczy stan usuniętego presetu jest oznaczony jako niezapisany, z nieaktywnym usuwaniem pliku. Projekt pozostaje samowystarczalny; usunięcie presetu/terenu nie przemalowuje mapy. Nowa/Otwórz zerują wszystkie historie sesji. Zachowujemy stabilne ID/wersje i wyłączone domyślnie Zastosuj do bieżącej mapy.

### Paleta, panel i etykiety

Paleta ma jedną kolumnę przy tej samej szerokości lewego panelu. Każdy wiersz zawiera szeroki kolorowy prostokąt z wyśrodkowaną nazwą oraz dwie stałe zewnętrzne kolumny przycisków edycji i usunięcia. Ikony są stale widoczne, nie zasłaniają nazwy ani nie zmieniają wymiarów. Długie nazwy są skracane z pełną podpowiedzią; nie ma poziomego przewijania. Dodaj pozostaje nad listą. Zachowujemy klawiaturę, nazwy dostępności, ostrzeżenia i wybór; akcja nie wybiera innego terenu, nie maluje i nie otwiera podwójnego dialogu. Niezwiązane odświeżenie zachowuje istniejące kontrolki. Nie dodajemy grafik terenów.

Zwężanie nakładki odrysowuje odsłaniany pas płótna podczas gestu, bez arbitralnego opóźnienia, czekania do puszczenia i przebudowy edytora co piksel. Zachowujemy wydajność, kamerę, dane, historię, uchwyt/reset oraz poprawne dopasowanie tabel. Prawa nakładka nadal jest pomijana przez Dopasuj; lewy panel jest uwzględniany.

Przycisk etykiet pokazuje pełne krótkie nazwy Osie / W heksach / Brak i pełne wyjaśnienie w podpowiedzi, bez nakładania grup. Napisy wewnątrz mapy są mierzone, bez zawijania i ucinania końcowych znaków, także dla wieloliterowych kolumn i wiersza 200 w obu orientacjach. Przy normalnym zoomie cały identyfikator mieści się w heksie. Poniżej 80% lub gdy nie mieści się cały czytelny napis (minimum 7,5 px czcionki, granice widoku), ukrywamy całość, nigdy fragment sugerujący inne pole. Nie zmieniamy logicznych współrzędnych.

### Rozmiar i ostrzeżenie

Cztery wartości ze znakami otaczają lokalnie narysowany schemat: Góra nad nim, Dół pod nim, Lewo/Prawo po bokach. Legenda: dodatnie dodają, ujemne usuwają, 0 bez zmiany. Pokazujemy bieżący i wynikowy rozmiar. Zachowujemy atomowy plan czterech stron, walidację starych brzegów, limity 1–200, parzystości, ID, pełną zawartość i jedną operację Undo/Redo.

Ostrzeżenie zachowuje sumy i współrzędne PRZED operacją. Rozłączne grupy: tylko zmieniony teren, tylko tekst, teren i tekst. Tekst oznacza nazwę lub opis, także formatowany. Teren jest porównywany z pełnym kanonicznym neutralnym stanem, nie tylko kolorem. Każdy heks i narożnik należą do jednej grupy i są liczone raz; suma grup zgadza się z liczbą heksów z danymi. Każda niepusta grupa ma liczbę i pełną przewijaną listę w ograniczonym oknie. Maski rodzaju zawartości pozwalają później rozwinąć kombinacje, bez pustych sekcji ikon/obiektów teraz. Domyślne Nie i anulowanie nie zmieniają niczego.

### Zwykły tekst i wymagania przyszłe

Menu kontekstowe opisu zachowuje Wytnij/Kopiuj/Wklej i dodaje Wklej jako zwykły tekst. Używa istniejącego mechanizmu Sam tekst, wyłącznie tekstu Unicode/zwykłego ze schowka, zachowuje polskie znaki i wiersze, nie przenosi stylów ani tabel jako tabel. To jedna operacja historii opisu, bez historii mapy. Opcja nieaktywna bez odpowiedniego tekstu; zwykłe formatowane wklejanie pozostaje.

Przyszłe wymaganie, bez przypisania do 0.3.0 lub 0.4.0: wiele jednocześnie otwartych projektów z zakładkami, np. na dole jak arkusze, dla map wszechświata, planety, miasta, budynku i pokoju. Każdy projekt ma własne dane, ścieżkę, stan niezapisanych zmian, kamerę, zaznaczenie, historię mapy/palety oraz historie tekstu. Przełączanie zachowuje wszystkie historie; zamknięcie uwzględnia niezapisane zmiany. Nie implementujemy zakładek w 0.2.2.

Odłożone pozostają treść i wygląd statusu presetu oraz przebudowa edytora opisu, stanów formatowania i tabel, w tym zgłoszony problem pogrubienia bez zaznaczenia oraz wysokość/przewijanie pustego edytora. Nie oznaczamy ich jako naprawionych.

Własne pliki testowe wyłącznie w tests/_work, bez prywatnej biblioteki. Regresje i próby zwykłego końcowego EXE raportujemy oddzielnie; nie deklarujemy Worda, większego DPI ani płynnego ciągłego ruchu, jeżeli nie zostały faktycznie sprawdzone. Bez push i Release'a.

## 23. Poprawka obsługi 0.2.3

Pierwszeństwo przed wcześniejszymi sprzecznymi ustaleniami. Rozwijamy ukończoną 0.2.2; format mapy 2, odczyt 1, preset 1, bez nowych zależności/usług. Użytkownik potwierdził działanie presetów, palety, smug, etykiet i rozmiaru 0.2.2. Nie powtarzamy całej poprzedniej diagnostyki. Użytkownik świadomie usunął lokalną Library, w tym kopie migracji; nie odzyskujemy danych ani nie traktujemy tego jako błędu. Pusta struktura odtwarzana obok EXE i wzorzec z kodu są prawidłowym stanem startowym/dystrybucyjnym.

- Plik presetu ma czytelną nazwę wyprowadzoną z jego nazwy, np. Kosmos.terrainset.json. Wewnętrzne ID pozostaje tożsamością, niezależnie od przemianowania pliku. Zachowujemy format zawartości. Normalizacja obejmuje znaki Windows, nazwy urządzeń (także z rozszerzeniem), końcowe kropki/spacje, pusty wynik, polskie znaki i spacje, rozsądną długość oraz brak wyjścia poza katalog. Kolizja po normalizacji, również tylko wielkością liter, dostaje czytelny sufiks liczbowy bez nadpisania innego pliku. Konflikty logicznej nazwy/ID nadal są jawnie odrzucane.
- Istniejący preset zapisuje jeden aktywny plik. Zmiana nazwy jest robocza; świadomy zapis zmienionej nazwy aktualizuje nazwę tego pliku z zachowaniem ID, danych i bezpieczeństwa błędu. Stare nazwy są czytane; nie ma masowej migracji przy uruchomieniu. Można je uporządkować przy jawnym zapisie. Undo/Redo nigdy nie zmienia plików. Plik przekazany innej osobie działa po skopiowaniu do Presets i odświeżeniu, z istniejącą walidacją i obsługą konfliktów.
- Napisy związane z zapisanymi zestawami konsekwentnie używają „preset”: nagłówek Presety, Nowy preset / Zmień nazwę, Zapisz preset / Usuń preset w dwóch wierszach po dwa przyciski. Obejmuje dialogi, komunikaty i podpowiedzi; pojedynczy teren nadal nazywa się terenem. Nie przemianowujemy klas ani pól formatu tylko dla tego napisu.
- Kompaktowy Odśwież presety mieści się przy selektorze, ma tooltip i nazwę dostępności. Używa obecnego odczytu/walidatora, bez obserwatora i osobnego importera. Nie zmienia mapy, jej historii, aktywnego ID ani roboczych palet, nie wybiera nowego presetu i nie resetuje zmian z dysku. Zniknięcie lub zewnętrzna zmiana aktywnego pliku jest komunikowana; dane robocze pozostają. Powtórzenia nie dublują wpisów, błędne pliki/kolizje są zgłaszane bez blokowania aplikacji i uznawania ich za poprawny import.
- Lewy panel ma tę samą szerokość i jedną kolumnę. Zbliżone boczne marginesy i szerokość zawartości uwzględniają faktyczny pionowy pasek przewijania. Niższe wiersze i odstępy wyraźnie zmniejszają wysokość palety. Zachowujemy czytelne nazwy, osobne akcje i wygodne obszary, F2/Delete, pełne podpowiedzi skróconych nazw, minimum okna, przewijanie, dostęp do Dodaj i istniejące kontrolki przy niezwiązanych odświeżeniach. Funkcje przybornika bez zmian.
- Kosz terenu jest czerwony, z czytelnym hover/fokusem i stanem nieaktywnym. Podpowiedzi ikon: Edytuj teren (F2), Usuń teren (Delete), bez nazwy terenu. Nazwa pozostaje w potwierdzeniu i nazwie dostępności identyfikującej cel.
- Ctrl+Shift+V tylko przy fokusie opisu wywołuje dokładnie tę samą operację Paste(true) co menu/Sam tekst, raz i jako jeden krok historii opisu, bez historii mapy. Nie zmienia Ctrl+V ani formatowanego wklejania. Brak tekstu nie kasuje zaznaczenia. Menu pokazuje skrót; przechwycenie zatrzymuje dalszą obsługę tego zdarzenia.
- Prawy nagłówek sprawdzamy krótko pod kątem prostej zbędnej aktualizacji. Jeśli nie ma małej uzasadnionej poprawki, pozostawiamy lekki przeskok i opisujemy ograniczenie. Nie pogarszamy naprawy smug, tabel ani uchwytu. Przycisk Rozmiar pozostaje bez zmian.

Odłożone bez implementacji: siedem heksów zamiast schematu 3×3 w dialogu rozmiaru, chwilowe wyróżnienie na mapie zagrożonych heksów z danymi, już opisane wieloprojektowe zakładki bez numeru wydania, dalsze grupy/stylistyka ostrzeżeń i przebudowa edytora opisów.

Celowane regresje: nazwy/kolizje/stare pliki, zapis po zmianie nazwy i błędy, bezpieczne odświeżanie/powtórzenia/konflikty, Ctrl+Shift+V i jedna historia. Własne dane wyłącznie tests/_work; prywatna Library nie jest materiałem testowym. Rzeczywisty końcowy EXE sprawdzamy tylko w nowym zakresie; bez szerokiej kampanii Worda/tabel. Zbiorcze regresje raz na końcu, powtarzanie tylko po błędzie lub zmianie wpływającej na wynik. Bez push, tagu i Release'a.

## 24. Przyrost funkcjonalny 0.3.0 — obowiązujące decyzje

Ta sekcja ma pierwszeństwo przed wcześniejszymi sprzecznymi ustaleniami. Rozwijamy ukończoną 0.2.3. Technologia Windows Forms / GDI+ i osadzony edytor WPF, działanie offline i lokalne pliki pozostają. Nie dodajemy zależności ani usług. Wersja aplikacji 0.3.0, mapy 3, zawartości presetu 1.

### Zgodność, biblioteka i testowanie

Nie wymagamy odczytu starszych formatów map ani specjalnej zgodności ze starymi nazwami presetów. Odczyt mapy wymaga jawnego formatu 3; nie ma migracji, fallbacku ani kopii migracyjnych. Starszy/nieprawidłowy plik nie zastępuje otwartego dokumentu i pozostaje nietknięty. Nie kasujemy Library ani map użytkownika. Czysta biblioteka jest prawidłowym stanem startowym.

Preset zapisuje się jako `Library/Terrains/Presets/Kosmos.json`, bez `.terrainset`. Zachowujemy bezpieczne nazwy Windows, sufiksy kolizji, stabilne ID i wersje terenów, jeden plik po świadomym zapisie/przemianowaniu, ochronę Domyślnego i odświeżanie bez restartu. Skanujemy bez rekurencji *.json; nie dopisujemy mechanizmu starych rozszerzeń i nie migrujemy plików przy starcie. Undo/Redo nie wykonuje operacji dyskowych biblioteki. Kolejność sortowania pozostaje dotychczasowa.

Testujemy selektywnie nowe zachowania i bezpośrednio dotknięte zależności: zapis/odczyt, historię, renderowanie i gesty. Run-AllTests i równoważna pełna kampania wymagają osobnego wyraźnego polecenia użytkownika, również po zmianie wspólnego modelu. Nie powtarzamy zaliczonych prób bez istotnej zmiany/błędu/niepewności, także po wznowieniu. Stare testy pozostają w Git; założenia formatów 1/2 oznaczamy jako historyczne. Nie przeprowadzamy kampanii Worda, formatowania, DPI ani dawnej diagnostyki prawego panelu. Własne dane wyłącznie tests/_work, bez prywatnej Library. Raport rozdziela metody/kontrolki, zwykły EXE i próby pominięte. Pełny audyt będzie zlecony osobno w pobliżu 1.0.0.

### Sekcje, narzędzia i materiał

Lewy panel: Przybornik / Presety / Tereny / Ikony / Obiekty, każda sekcja ma nagłówek i delikatny separator. Obiekty to tylko niewielki placeholder bez modelu i atrap przycisków. Tereny mają własne przewijanie w elastycznym obszarze; reszta pozostaje na miejscu i osiągalna przy minimalnym oknie. Ikony mają podgląd do czterech rzędów i pełną bibliotekę.

Treść zachowuje 220 px; dołączony pasek 18 px jest po jej prawej stronie, na pełnej wysokości obszaru roboczego. Nie jest nakładką. Rozwinięty zespół zajmuje 238 px, zwinięty 18 px. Płótno, mysz i Dopasuj używają rzeczywistego pozostałego prostokąta. Zwinięcie/rozwinięcie nie zmienia danych/historii; kompensacja lokalnego początku płótna zachowuje położenie mapy na ekranie. Prawa nakładka nadal jest celowo pomijana przez Dopasuj.

Presety: selektor z kompaktowym Odśwież; pod nim jeden rząd czterech ikon: plus Nowy preset, ołówek Edytuj nazwę, dyskietka Zapisz preset, czerwony kosz Usuń preset. Polskie tooltipy, nazwy dostępności, właściwe stany aktywności. Nazwa projektu i Rozmiar pozostają na swoich miejscach.

Pędzel ma rysunek pędzla; istniejące Wiadro jest rozszerzone o materiał ikonowy. Wybór narzędzia jest oddzielony od ostatniego wybranego terenu/ikony. Wybór materiału nie edytuje mapy i nie przełącza niepotrzebnie narzędzia. Aktywny materiał wskazują opis w Przyborniku i ramka.

### Wspólny kształt i rozmiar

Pędzel terenów/ikon i Mgła wojny współdzielą Kształt, Rozmiar 1–15 i Wypełnienie. Prostokąt pokazuje drugi wymiar (wysokość). Linie wyłączają Wypełnienie; Punkt zawsze jeden heks. Wiadra ignorują te ustawienia. Pod kursorem jest podgląd odcisku.

Osiem kształtów: Punkt, Kwadrat, Prostokąt, Koło, Heksagon, Pięciokąt foremny, Linia pozioma, Linia pionowa. Bez trójkąta. Heksagon rozmiaru N zawiera środek i N−1 pełnych pierścieni, według odległości na siatce. Pozostałe figury wybierają centra w bazowych współrzędnych mapy: kwadrat/prostokąt o podanych wymiarach, koło i foremny pięciokąt wpisane w okrąg o podanej średnicy. Jednostka to około 48,5 jednostki mapy (bazowy promień heksa 28). Linie próbkują odcinek długości (N−1) jednostek i wybierają najbliższe centra wzdłuż widocznej osi poziomej/pionowej.

Wynik nie zależy od zoomu, jest deterministyczny w obu orientacjach i parzystościach. Obrys obejmuje członków figury mających sąsiada poza figurą: jeden heks grubości. Granicę wyznaczamy w wirtualnej siatce przed przycięciem do mapy. Przy małych rozmiarach figura może być jednym heksem, ale nie jest przypadkowo pusta. To dyskretna aproksymacja, bez obietnicy idealnego kształtu euklidesowego.

### Ikony i mgła

Biblioteka ikon: plus importu, miniatury/wybór, czerwony kosz usunięcia i wielokropek pełnej listy. Własne sylwetki z kodu: krzyż, namiot, wieża, czaszka, gwiazda. Pierwsza inicjalizacja biblioteki zapisuje pięć zasobów i znacznik; usunięte pozycje nie wracają przy następnym starcie z istniejącym znacznikiem.

Import nieruchomych PNG/JPG/BMP/GIF: do 8 MiB i 2048×2048, proporcjonalna normalizacja PNG do 512×512, bez blokad źródła. Brak metaplików, SVG, animacji i zasobów sieciowych. Błąd nie zmienia dokumentu. Ikona na mapie zajmuje znaczną część wnętrza heksa z zachowaniem proporcji.

Najwyżej jedna ikona na heks. Pole startowe z wybraną ikoną ustala usuwanie tej ikony; inne ustala dodawanie/zastępowanie. Tryb obowiązuje cały gest i region; nie usuwamy innych ikon w trybie usuwania wybranej. Teren, nazwa i opis pozostają niezależne. Usunięcie bibliotecznej kopii nie usuwa mapowego zasobu, a Undo nie zmienia biblioteki.

Dwa narzędzia mgły: malowanie wspólnym odciskiem i Wypełnienie mgłą. Stan jest osobny dla każdego heksa. Mgła nieprzezroczysta szara przykrywa całe wnętrze z kolorem, ikoną i etykietą, pozostawia roboczą siatkę. Kolejność renderowania ma później zasłaniać także obiekty, których teraz nie implementujemy. Odkryty start nakłada, zasłonięty odkrywa, jednolicie przez cały gest/region. Odwiedzone pole nie jest przełączane ponownie.

Szczegóły pod mgłą: stan ukrycia/znaki zapytania, brak ujawnienia nazwy, terenu i opisu, nieaktywna edycja i blokada historii tekstowej ukrytego pola. Stara widoczna treść jest zastępowana w kontrolce przy zmianie na zasłonięte pole, bez nadpisywania prawdziwych danych. Bez przełącznika mgły w szczegółach i bez globalnego podglądu w tym wydaniu.

Wszystkie wiadra używają istniejącej kolejki i sześciu rzeczywistych sąsiadów. Tereny i ikony: region utrwalonego wariantu terenu. Mgła: region równego koloru terenu pod spodem (bez rozróżnienia wielkości liter zapisu koloru). Ikony/mgła nie wyznaczają granic regionu; nie przechodzimy do oddzielonych obszarów. Bez rekurencji.

### Historia, przycinanie i format mapy

Tereny, robocze palety, ikony, mgła i rozmiar są we wspólnej chronologii mapy, do 300 operacji. Jeden gest albo jedno wypełnienie to jeden wpis; zbiory odwiedzonych heksów uniemożliwiają wielokrotne przełączenia w nakładających się odciskach. Pusta operacja zachowuje Redo. Zmiany warstw przechowują tylko właściwe pola i stabilne ID heksa; nie przywracają starszego tekstu ani niezależnych warstw. Tekst ma dotychczasowe oddzielne historie. Nowa/Otwórz zerują historię i pamięć zasobów dokumentu. Dotychczasowa ochrona gestów dialogów obejmuje nowe narzędzia.

Przycinanie zachowuje wszystkie dane/ID pozostawionych pól. Nowe są neutralne, bez ikony i mgły. Ostrzeżenie o usuwaniu uznaje ikonę i mgłę za dane; rozłączne maski rodzaju zawartości tworzą kombinacje z terenem/tekstem, bez podwójnego liczenia. Pełne współrzędne i domyślne Nie pozostają; stylistyki ostrzeżenia nie przebudowujemy.

Format 3 dodaje IconId i Fog w każdym heksie oraz IconAssets: ID oparte na SHA-256 bajtów PNG, nazwa, wymiary, PNG base64. Nie ma ścieżek do zewnętrznych grafik. Wiele wystąpień współdzieli jeden zasób. Zapis obejmuje wyłącznie użyte zasoby, nie bibliotekę i nie nieużywane zasoby zachowane dla Undo. Po skopiowaniu samej mapy ikony pozostają dostępne. Limit 128 zasobów na dokument/sesję, 2 MiB PNG każdy, około 32 MiB PNG łącznie (44 MiB base64), 128 MiB całego pliku. Walidacja referencji, ID, wymiarów i dekodowania przed podmianą dokumentu. Bezpieczny zapis i potwierdzanie końcowej ścieżki pozostają.

### Dalsze wymagania — poza 0.3.0

- Obiekty prawdopodobnie 0.4.0; później przesuwanie obiektów i odkrywanie heksa wraz z sześcioma sąsiadami.
- Tekstury terenów i zakładki kilku map przed 1.0.0. Każda zakładka ma własne dane, ścieżkę, stan zmian, kamerę, zaznaczenie, historię mapy/palety i historie tekstu; przełączenie ich nie zeruje, zamknięcie uwzględnia zmiany.
- Po 1.0.0: high/low detail tła heksów, widok MG/graczy i prezentacja, eksperymentalne łączenie heksów.
- Akcent koloru wybranego terenu w prawych szczegółach; zdjęcia i dalsza przebudowa opisów, stanów formatowania i tabel; decyzja o stylu zwykłego wklejania.
- Globalny tymczasowy podgląd bez mgły. Bez regulacji przezroczystości i automatycznego odkrywania przez obiekty teraz.
- Przeskok przycisków prawego nagłówka, siedem heksów w schemacie rozmiaru, wyróżnienie pól zagrożonych przycinaniem i dalsza stylistyka ostrzeżeń.

Bez generatorów treści, spotkań, NPC i usług AI.
## 25. Łatka 0.3.1 — obowiązujące decyzje

Ta sekcja zastępuje sprzeczne ustalenia wcześniejsze, w szczególności zasady regionu mgły, roboczej edycji Domyślnego i dyskowej historii **ikon**. Bazą jest ukończona 0.3.0. Nie wracamy do zakończonych prac, zaakceptowanych geometrii ani przebudowy aplikacji. Format mapy pozostaje 3, presetu 1; nowy format opakowania bibliotecznej ikony 2. Źródła C# są w jednym `src/`, EXE/Build/README/docs/Library/tests pozostają w katalogu głównym. Build i skrypty używają src bez zmiany lokalizacji Library względem EXE.

### Region mgły i gesty

Region mgły jest spójny przez sześciu sąsiadów i wymaga jednocześnie koloru, dokładnej nazwy rodzaju terenu oraz początkowego stanu Fog zgodnych ze startem. Kolor porównujemy bez rozróżniania wielkości liter zapisu, nazwę terenu dokładnie. Nazwa oznacza np. Woda, nie nazwę własną heksa. Ikona, indywidualna nazwa i opis nie dzielą regionu; nie dodajemy warunku wariantu/ID. Woda i neutralny w tym samym kolorze są oddzielne, tak samo plamy mgły rozdzielone odkrytymi heksami. Granice są liczone z migawki **przed modyfikacją gestu**, więc kolejka nie rozszerza się wskutek własnych zmian.

Istniejące Wiadro terenów/ikon i Wypełnienie mgłą działają także przez przeciąganie przez kolejne regiony. Jeden gest to jeden wpis wspólnej historii. Przetworzone regiony są pamiętane, nie przełączane wielokrotnie. Tryb ikon i mgły jest ustalony na początku całego gestu. Mgła pomija regiony o przeciwnym początkowym stanie, które już odpowiadają docelowemu stanowi. Tereny/ikony zachowują dotychczasowy predykat wariantu terenu i kolejkę/sąsiedztwo.

Osiem geometrii, ich rozmiary, obrys i orientacje pozostają zaakceptowane. Punkt wyłącza Rozmiar i Wypełnienie, z wyjaśnieniem jednego heksa; inne figury przywracają właściwe kontrolki. Mgła malowana odciskiem nadal korzysta z istniejących kształtów. Nie naprawiamy nieistniejącego braku ich obsługi.

Ciągłość pędzla jest małą lokalną poprawką: między otrzymanymi pozycjami próbkujemy odcinek w jednostkach mapy co najwyżej 0,4 bazowego promienia heksa i nakładamy ten sam odcisk. Wspólny visited zapobiega ponownemu przełączaniu ikon/mgły. Historia pozostaje jedna. Po wyjściu poza płótno/siatkę, MouseLeave, utracie capture lub nowym geście punkt poprzedni jest zerowany, aby nie rysować linii przy powrocie. Bez kampanii wydajnościowej i zmiany geometrii figur.

### Panel i Domyślny

Pod separatorami jest niewielki odstęp. Usuwamy tekstowy wiersz materiału pod Przybornikiem; pogrubiona złota ramka wskazuje teren **albo** ikonę, nigdy dwa równorzędne materiały. Obiekty pozostają placeholderem. Bez dwóch kolumn i pełnej przebudowy proporcji.

Tereny zamiast standardowego pionowego scrollbara mają górną/dolną kompaktową strzałkę w stałym miejscu oraz kółko. Strzałki są nieaktywne na granicach, nie znikają i nie zmieniają zakresu/rozmiaru viewportu. Nie ma bocznego paska zabierającego szerokość kafelkom. Dodaj i inne sekcje pozostają nieruchome. Wyniku testu bounds nie przedstawiamy jako pełnej próby wizualnej.

Zmiana zwinięcia panelu synchronizuje kompensację kamery i zmianę natywnego okna płótna w jednej synchronicznej sekcji z wyłączonym odrysowywaniem WM_SETREDRAW, następnie odrysowuje gotowy stan. SuspendLayout samo nie chroni przed odmalowaniem/ kopiowaniem pikseli w trakcie SetBounds. Bez timerów/opóźnień i automatycznego Dopasuj. Zachowujemy kompensację, Dopasuj, hit-testing i przeliczenie myszy. Test końcowego położenia nie dowodzi braku klatki przejściowej; ograniczenie narzędzia trzeba raportować.

Domyślny jest całkowicie tylko do odczytu po ID: blokujemy nazwę, dodawanie/usuwanie/edycję wszystkich definicji, nadpisanie i usunięcie, także przez F2/Delete i metody. Można wybierać tereny i malować mapę, zapisać jako nowy własny preset i potem go edytować/nadpisywać. Własna kopia jest wybierana dopiero po udanym zapisie; błąd/anulowanie nie zmieniają Domyślnego. Nie resetujemy automatycznie własnych presetów ani nie blokujemy edycji mapy.

### Edycja, tożsamość i pliki ikon

Przy akcjach ikon jest ołówek Edytuj ikonę. Bez aktywnej ikony komunikat Zaznacz ikonę, bez wyjątku i zmiany materiału. Dialog: nazwa, grafika z pliku, zastępcza grafika z dostępnej biblioteki i checkbox Zaktualizuj wystąpienia na bieżącej mapie, przy każdym otwarciu wyłączony. Anulowanie dialogu/importu nie zmienia niczego. Bez checkboxa dawne wystąpienia zachowują grafikę/metadane. Z nim zmieniają się wystąpienia logicznego ID edytowanej pozycji w bieżącym dokumencie, także starsze warianty tej pozycji, wraz z ich nazwą. Nie skanujemy innych map. Nie zmieniamy terenu, Fog, własnej nazwy/opisu heksa. Cała edycja biblioteki i mapy to jedna operacja historii.

LibraryIcon ma stałe logiczne ID, Name i Asset. Asset zachowuje content-hash i PNG; to odrębna tożsamość grafiki, deduplikowana przy osadzaniu w mapie. Wiele niezależnych pozycji może mieć takie same bajty i różne ID. Nie aktualizujemy wszystkich podobnych PNG przez przypadek. Heks ma opcjonalne IconEntryId i IconName, a IconId nadal wskazuje zasób. Stare dane formatu 3 bez nowych pól są dopuszczalne: stare content-ID określa dawną pojedynczą pozycję, nazwa jest z jej dawnego zasobu. To minimalne rozszerzenie formatu 3, bez migratora map i zależności od ścieżek biblioteki.

Biblioteczny JSON ma IconFormatVersion 2 i powyższe opakowanie. Dotychczasowy płaski plik ikony jest czytany w pamięci, bez startowego kasowania/przemianowania. Dopiero świadoma edycja zapisuje nowe opakowanie i czytelny plik. Nazwy `icon-Czaszka.json`, `icon-Namiot.json`, `icon-Namiot (2).json`: zawsze mały prefiks icon-, pozostała wielkość liter i polskie znaki zgodne z nazwą użytkownika/źródła. Nazwa importu pochodzi ze źródła bez rozszerzenia. Normalizacja Windows i długości ścieżki oraz sufiksy kolizji bez nadpisania. Zmiana nazwy aktualizuje jeden właściwy plik, bez zbędnej aktywnej kopii i bez długiego hasha jako nazwy. Nie rozszerzamy selektora ani nie wdrażamy kadrowania.

### Wspólna sesyjna historia ikon i bezpieczeństwo

Świadomy wyjątek od wcześniejszej zasady: historia ikon odtwarza **również pliki** importu, usunięcia, nazwy i podmiany grafiki. Jest częścią tej samej chronologii co mapa, a opcjonalna aktualizacja wystąpień jest atomowym wpisem. Nie przenosimy tej reguły na pliki presetów. Nie ma drugiego stosu ani skrótu do wyboru.

Migawki poprzednich/nowych bajtów, metadanych i ścieżek są w pamięci, dostępne tak długo jak odpowiadający wpis historii (do 300 wpisów). Usunięcie aktywnego pliku nie polega na odnajdywaniu go w Koszu; potwierdzenie mówi o kopii w pamięci, braku Kosza i utracie sesyjnego Undo po wyczyszczeniu historii. Undo usuwa/odtwarza pozycję i plik, Redo ponawia. Nowa/Otwórz zerują historię dopiero po sukcesie, błąd/anulowanie ją zachowuje. Zamknięcie/anulowanie zamknięcia respektuje pytania o zmiany mapy. Nie usuwamy aktywnych plików biblioteki podczas sprzątania historii; sprzątamy tylko własne dane tymczasowe.

Przed przejściem porównujemy oczekiwane bajty istniejącego źródła i zajętość celu. Plik źródłowy jest etapowany pod unikalną nazwą, ponownie sprawdzany, a nowy plik zapisany/Flush/atomowo przeniesiony bez prawa nadpisania obcego celu. Błąd przywraca poprzednią ścieżkę; nieudany rollback wskazuje zachowany plik do odzyskania, bez nadpisania zajętej ścieżki. Pamięć mapy/lista i stos zmieniają się dopiero po udanym przejściu dyskowym. Konflikt zewnętrzny nie przesuwa historii jak po sukcesie. Biblioteczna zmiana bez wystąpień nie oznacza projektu gwiazdką. Mapa nadal osadza tylko użyte zasoby, bez zależności od biblioteki.

### Poza zakresem i weryfikacja

Pozostają odłożone: obiekty, dwie kolumny i podział wysokości panelu, graficzna mgła/szczegółowość, współrzędne na ikonach, kadrowanie/arkusze ikon, przebudowa pełnego selektora, optymalizacja dużych map, opisy/formatowanie i przeskok prawego nagłówka. Wymagania przyszłe z sekcji 24 pozostają. Typowe mapy użytkownika 60×60–70×70; brak kampanii 200×200 nie jest nowym zadaniem ani błędem łatki.

Tylko minimalne celowane scenariusze nowych i zmienionych zachowań, bez Run-AllTests i równoważnej kampanii, również z powodu przenosin/modelu/historii. Nie usuwamy starych testów, nie przedstawiamy dawnych wyników jako nowych. Własne dane tylko tests/_work, bez prywatnej biblioteki i czyszczenia map. Krótka próba strzałek i ruchu panelu na zwykłym EXE; osobno metody/kontrolki i ograniczenia próbkowania obrazu. Bez push/tagu/Release'a. Pełny audyt wyłącznie na osobne polecenie.
## 26. Łatka 0.3.2 — obowiązujące decyzje

Pierwszeństwo przed sprzecznymi wcześniejszymi ustaleniami. Rozwijamy ukończoną 0.3.1; format mapy 3, presetu 1 i ikony bibliotecznej 2 bez zmian, źródła w src i Library obok EXE. Bez nowych zależności/usług, migracji hipotetycznych starych map, push/tagu/Release'a. Użytkownik potwierdził poprawne regiony i przeciąganie mgły, maskowanie, brak przeskoku panelu, stabilność przewijania, jedną złotą ramkę, blokady Domyślnego, edycję/historię ikon, ciągłość i reset odcinka po wyjściu z mapy oraz jeden wpis gestu. Nie diagnozujemy ich ponownie i nie zmieniamy geometrii/interpolacji. Limit 300 i utrata cofania po wypadnięciu wpisu są zaakceptowane.

### Strzałki i świadomy wybór materiału

Kliknięcie strzałki przesuwa dokładnie o Height + Margin.Vertical jednego wiersza terenu (obecnie 36 px), nie o dwa; na brzegu ograniczamy do pozostałego zakresu. Stałe miejsce i stany aktywności, kółko, nieruchome inne sekcje i Dodaj pozostają. Obie kontrolki mają takie same wymiary, padding, marginesy i wyrównanie. Używają jednego lokalnego rysunku 9×5 i jego dokładnego pionowego odbicia, nie dwóch glifów. Bez pełnego redesignu panelu.

Świadome kliknięcie terenu/ikony przy FogBrush lub FogBucket wybiera materiał i zwykły Brush. Przy zwykłym Bucket pozostaje Bucket, przy Brush pozostaje Brush; nie rozszerzamy na inne narzędzia. Pełny selektor ikon w kontekście głównego okna podlega tej samej regule. Nie wywołujemy jej przez odbudowę/odświeżanie/synchronizację, odczyt lub historię. Sam wybór nie zmienia mapy, gwiazdki ani historii i zachowuje wszystkie ustawienia pędzla. Przyszłe świadome wybieranie obiektowego materiału powinno stosować tę zasadę, ale obiektów teraz nie implementujemy.

### Wizualny wybór grafiki i nazwy ikon

Edytuj ikonę zamiast rozwijanej listy nazw ma Wybierz z biblioteki. Otwiera ten sam pełny IconPickerDialog z miniaturami i nazwami co główne okno, bez drugiego widoku biblioteki. W tym kontekście zwraca tylko grafikę do dialogu edycji, nie zmienia głównego materiału/narzędzia i nie zapisuje niczego przed OK edycji. Anuluj selektora zachowuje podgląd; anulowanie edycji zachowuje wszystkie dane. Pozostają grafika z pliku, nazwa, podgląd, domyślnie wyłączona aktualizacja wystąpień i jeden spójny wpis historii. Bez kadrowania/arkuszy lub przebudowy stylistyki całego selektora.

Generator startowych ikon w 0.3.1 już zapisuje icon-Namiot.json, icon-Czaszka.json itd. Sprawdzamy nowy pusty katalog i zachowanie znacznika, nie dodajemy niepotrzebnej poprawki. Pliki z długim content-ID są pozostałością mechanizmu 0.3.0: sam program nie przepisuje ich przy starcie; świadoma edycja i OK porządkują właściwy plik. Nie resetujemy znacznika i nie przywracamy usuniętych ikon. Zachowujemy bezpieczne nazwy, mały prefiks icon-, zapis liter nazwy i kolizje.

### Historia plików presetów

Wspólna chronologia obejmuje od teraz także pliki: nowy preset, kopię jako nowy, nadpisanie własnego, zapis zmienionej nazwy z przemianowaniem pliku i usunięcie. Operacje robocze (np. nazwa przed zapisem) zachowują dotychczasowy sens i nie wykonują dodatkowej operacji dyskowej. Jeden rzeczywisty zapis/usunięcie to jeden wpis. Identyczne bajty/ścieżka i niezmieniony projekt nie tworzą pustego wpisu.

LibraryFiles jest wyodrębnionym istniejącym mechanizmem ikon, używanym również przez presety, z bajtowymi stanami i ścieżkami w RAM. Nie tworzymy innego stosu, skrótu ani archiwum. PresetFileChange zapisuje pojedynczy stan biblioteki/pliku; EditAction łączy go z opcjonalnym stanem palety projektu, przygotowanym przed zapisaniem bez mutacji mapy. Nadpisanie samego pliku nie cofa wcześniejszej roboczej palety ani nie tworzy fałszywej gwiazdki. Nowy/kopia/usunięcie mogą rzeczywiście zmieniać projekt i mają odpowiednie oznaczenie. Nie cofamy przy okazji heksów, ikon, mgły, nazw/opisów. Domyślny całkowicie chroniony; jego własną kopię można cofnąć jak utworzenie.

Undo/Redo przywraca dokładne bajty, nazwę pliku i odpowiedni stan programu. Undo usunięcia odtwarza zapisany preset na dysku bez dodatkowego Zapisz; jeśli paleta robocza wcześniej różniła się od pliku, zachowuje ten stan różnic. Usuwanie presetów, analogicznie do ikon, odbywa się bez Kosza z kopią w pamięci. Potwierdzenie wyjaśnia brak Kosza, sesyjność, limit 300 i utratę cofania po resecie/wypadnięciu wpisu/zakończeniu programu. Nie sprzątamy prywatnej biblioteki ani Kosza Windows.

Bajty oczekiwane są zapamiętane przy odczycie/zapisie, z wykrywaniem zewnętrznych zmian i zajętych ścieżek. Historia presetów sprawdza też kolizje logicznych ID/nazw. Źródło jest etapowane i ponownie sprawdzane; nowe bajty Flush i atomowy Move bez nadpisania obcego celu; błąd przywraca poprzednią ścieżkę. Jeśli zewnętrzna kolizja zablokuje sam rollback, wskazujemy zachowaną kopię .library-session-*.tmp, bez nadpisania. Pamięć i pozycja stosu zmieniają się dopiero po udanym przejściu dyskowym; błąd pozwala rozwiązać konflikt i ponowić. Nie ma trwałego odzyskiwania poza zachowaną historią. Nowa/Otwórz zerują ją dopiero po sukcesie; błąd/anulowanie zachowuje historię. Reset nie usuwa aktywnych presetów/ikon.

### Granice i weryfikacja

Tylko kilka celowanych scenariuszy kontrolek/materiału/selektora, pustej biblioteki, dokładnej historii presetów i jednego konfliktu oraz resetu bez usuwania plików. Krótka reprezentatywna próba zwykłego EXE: mgła → materiał, wizualny wybór grafiki, testowy preset usuń/Undo z plikiem, jeden krok strzałki. Nie deklarujemy wykonania niemożliwej próby. Nie Run-AllTests, nie dawne zestawy ani ponawianie zaliczonych prób bez istotnej zmiany/błędu/niepewnego wyniku. Nie usuwamy historycznych testów; ich dawna zasada bez plikowego Undo presetów jest od 0.3.2 historyczna, nie wymaganiem przywrócenia tamtego zachowania.

Poza zakresem: obiekty, dwie kolumny, pełna stylistyka, kadrowanie/arkusze/tekstury, geometria i dalsza interpolacja, ponowna diagnoza przeskoku panelu, opisy, optymalizacja dużych map i trwałe odzyskiwanie poza limitem. Mapy nadal osadzają użyte grafiki i logiczne metadane wystąpień. Własne dane wyłącznie tests/_work, bez prywatnej Library i jej czyszczenia.
## 27. Obiekty 0.4.0 — obowiązujące wymagania

Ta sekcja zastępuje sprzeczne decyzje wcześniejszych etapów: Obiekty są funkcją, nie placeholderem. Zachowujemy ukończone 0.3.2, technologię/offline/src/Library obok EXE, bez zależności/usług i ponownego wykonywania starych prac. Testowanie tylko minimalne i celowane; bez Run-AllTests lub równoważnej kampanii bez wyraźnego osobnego polecenia. Prywatna Library i mapy nie służą do prób; własne dane tylko tests/_work. Bez push/tagu/Release'a/przepisywania historii. Zajętego EXE użytkownika nie zamykamy i nie odrzucamy danych.

### Wzorzec, grafika, instancja

ObjectTemplate to biblioteka (ObjectFormatVersion 1, logiczne Id, Name i Asset), IconAsset to wspólne PNG z content-ID, MapObject to konkretna instancja ze stałym własnym ID i TemplateId. Instancja ma Name, AssetId, swobodne X/Y świata, Size najdłuższego boku z zachowaniem proporcji, Side Ally/Neutral/Enemy, Locked, RevealsFog i RevealRadius. Nie jest polem heksa; może być wiele obiektów w jednym miejscu i poza mapą. Teren/ikona/tekst heksa są niezależne. Środek jest geometrycznym środkiem prostokąta grafiki, niezależnie od przezroczystości. Początkowo Neutralny, ruch odblokowany, odkrywanie wyłączone, Size 48, promień 1. Size 8–1024, promień 0–20, świat ±10 000 000, do 5000 instancji. Walidacja przed podmianą dokumentu.

### Biblioteki i wybór

Sekcja Obiekty: miniatury, plus, ołówek, czerwony kosz, pełny wizualny selektor i osobna lista instancji. Bez przebudowy całego lewego panelu na dwie kolumny. Dwie kompaktowe sekcje miniatur pozostawiają elastyczne miejsce przewijanym terenom; pełne selektory dają dostęp do dalszych pozycji. Czysta biblioteka ma Namiot i Wieżę oraz znacznik inicjalizacji, bez odtwarzania świadomie usuniętych wzorców. Pliki Library/Objects/object-Namiot.json, object-Drużyna (2).json: mały prefiks object-, zachowane litery nazwy, wspólna normalizacja Windows i sufiksy kolizji. ID nie zależy od ścieżki. Nie ma masowej migracji/czyszczenia prywatnych danych.

Dodawanie wzorca: z pliku lub grafika z ikon. Dodawanie ikony: z pliku lub grafika z obiektów. Edycja obu: nazwa, plik, wizualny wybór z obu bibliotek, podgląd i Zaktualizuj wystąpienia na bieżącej mapie domyślnie wyłączone. Wspólne IconPickerDialog i IconEditDialog nie tworzą drugiego systemu wyboru. Kopiujemy wyłącznie grafikę do nowego logicznego ID, bez zachowań/powiązania edycyjnego. Wybór grafiki nie przemianowuje wpisu. Anulowanie nie zapisuje zmian. Aktualizacja wzorca wpływa na nazwę/grafikę pasujących TemplateId tylko w bieżącym dokumencie, nie na pozycje/stronę/blokadę/odkrywanie ani niezależne identyczne PNG. Bez checkboxa dawne instancje zostają migawkami. Usunięcie wzorca nie usuwa instancji.

Ostatni materiał to teren / ikona / obiekt z jednym złotym zaznaczeniem albo brak materiału. Wybór wzorca przełącza na Pędzel do pojedynczego umieszczania, także po Wiadrze i narzędziach mgły. Jeden MouseDown tworzy jedną instancję, również na pustym płótnie poza mapą; drag nie stempluje, Wiadro nie umieszcza, geometrie pędzla nie dotyczą obiektów i ich kontrolki są nieaktywne. Wybór terenu/ikony zachowuje zaakceptowane reguły 0.3.2. Po udanym OK edycji ikony (także bez zmiany wartości) brak aktywnego materiału, bez automatycznego wyboru zamiennika/narzędzia/historii/gwiazdki. Pędzel/Wiadro bez materiału nic nie zmieniają; mgła nie wymaga materiału. Anuluj zachowuje wybór.

### Zaznaczanie, szczegóły i kamera

Zaznaczanie ma klik/prostokąt i lasso, wybierane prawym przyciskiem na istniejącym narzędziu; bez duplikowania narzędzi. Klik górnego w stabilnej kolejności rysowania; Ctrl przełącza przynależność. Prostokąt/lasso wybierają według centrów; lasso domykane na puszczeniu, bez Ctrl wybór zastępuje, Ctrl przełącza. Puste miejsce/Escape czyści wybór. Ruch zaznaczonej grupy zachowuje wzajemne przesunięcia, jeden gest/jeden wpis, także poza mapą. Locked można wybrać, ale nie ruszyć; grupa pomija zablokowane z krótką informacją. Brak ruchu nie tworzy pustego wpisu. Po utracie capture gest kończy się, po opuszczeniu płótna nie interpolujemy nieznanej ścieżki. Delete usuwa instancje, nie wzorce. Obiektowy skrót nie przechwytuje tekstu ani Delete kafelka terenu.

Pojedynczy wybór pokazuje szczegóły obiektu zamiast heksa: Name, Side, Size, Locked, RevealsFog, RevealRadius; jawne Zastosuj właściwości jest jednym wpisem historii. Grupa pokazuje liczbę bez masowej edycji. Szczegóły heksa mają dodatkowo Ikona: nazwa/brak, pod mgłą ???. Obiektu nie dodajemy jako pojedynczego pola heksa.

Osobna lista instancji pokazuje nazwę, miniaturę, stronę i Poza mapą. Zaznacz bez centrowania nie rusza kamery. Pokaż na mapie wybiera i centruje z rozsądną skalą, uwzględniając lewy i prawy panel, także poza mapą. Nie zmienia danych/mgły/historii. Dwa jawne polecenia w menu Dopasuj: Dopasuj mapę (tylko geometria) i Dopasuj wszystko (geometria oraz całe prostokąty wszystkich instancji, niezależnie od wyboru, także poza mapą). Bez obiektów równoważne. Fit nadal celowo pomija prawą nakładkę; jej otwarcie nie porusza kamery. Nie jest to widok MG/gracza.

### Mgła i geometria obiektów

Odkrywa wyłącznie Side Ally i RevealsFog=true; Neutralny/Wróg mają widoczne, nieaktywne kontrolki i nie odkrywają innym wejściem. Checkbox wyłączony zachowuje promień, który jest wtedy nieaktywny. 0 = heks pod środkiem, 1 = pierwszy pierścień itd. Rozmiar obrazu nie zmienia promienia. Zmiana strony wyłącza funkcję; powrót do Ally nie włącza jej automatycznie. Włączenie lub zwiększenie promienia odsłania otoczenie w tej samej operacji co ustawienie; umieszczenie uprawnionej instancji stosuje tę samą regułę (obecne bezpieczne nowe instancje są Neutralne).

Ruch każdego uprawnionego członka grupy odkrywa odcinek między zdarzeniami z próbkowaniem co najwyżej 9 jednostek świata i sześcioma rzeczywistymi sąsiadami. Próbkowanie jest ograniczone do obwiedni mapy, a właściwy hit-test wymaga rzeczywistego heksa, bez ujawniania najbliższego pola spoza mapy. Po nieznanej drodze kursor/leave/capture nie powstaje łącznik. Odkrycie jest trwałym skutkiem operacji; nie ma pętli odwracającej ręczną mgłę co klatkę, ponownego zasłaniania ani linii wzroku. Undo ruchu odtwarza tylko zmienione przez niego Fog, nie wcześniejsze odkrycia i niezwiązany tekst/teren/ikony.

Renderowanie przycina grafikę i obrys zaznaczenia po zasłoniętych wielokątach heksów, nie tylko po środku. Poza mapą nie ma mgły. Zwykły hit-test nie zdradza całkowicie ukrytego obiektu; obszarowe zaznaczanie pomija całkowicie zasłonięte. Brak nazw/tooltipów nad ukrytymi grafikami. Lista instancji celowo daje edytorowi dostęp do ukrytych danych. Mgła nie szyfruje pliku.

Resize nie usuwa obiektów. Pozycje przesuwają się razem ze starą rozszerzoną siatką, z zachowaniem heksa i lokalnego offsetu dla zachowanych komórek; nad usuniętymi i poza mapą pozostają na planszy. Orientacja przelicza ten sam indeks rozszerzonej siatki i offset w lokalnych osiach heksa (obrót offsetu o 30° między orientacjami, bez obracania grafiki), aby przybrzeżny punkt nie trafił do innego heksa. Bez snapowania lub fikcyjnych komórek dokumentu. Obiekty i geometria trafiają do tego samego wpisu Undo. Techniczne przeliczenie nie odkrywa drogi. Crop nie raportuje obiektów jako usuwanych danych.

### Historia, pliki, format

Wspólny stos 300 obejmuje utworzenie/usunięcie/ruch grupy/właściwości/fog-delta, bibliotekę obiektów i jej pliki, obok ikon/presetów/terenów. Biblioteka używa LibraryFiles: bajty w RAM, oczekiwane ścieżki i źródła, konflikty bez cichego nadpisania, brak przesunięcia stosu po błędzie i możliwość ponowienia. Także kontrola duplikatu ID wzorca. Biblioteka+aktualizacja wystąpień jest jednym wpisem po udanym przejściu dyskowym. Sama biblioteka nie oznacza mapy gwiazdką. Usuwanie bez Kosza, z jasnym potwierdzeniem sesyjności: reset/limit/zamknięcie usuwa możliwość cofnięcia, nie aktywne pliki. Nowa/Otwórz zerują zaznaczenie i historie dopiero po sukcesie; błąd/anulowanie zachowują dokument. Bez trwałego archiwum historii.

Format mapy 4 ma obowiązkowe Objects: wszystkie instancje, także poza mapą, ich pełne właściwości i logiczne źródła. IconAssets pozostaje wspólnym magazynem osadzonych PNG ikon i obiektów, deduplikowanym po SHA-256 bez powiązania edycji. Zapis tylko użytych grafik, jeden zasób dla wielu wystąpień, samowystarczalny plik bez biblioteki/źródeł. Wspólny limit 128 zasobów, 2 MiB/PNG i około 32 MiB łącznie, plik do 128 MiB. Odczyt wymaga jawnych pól obiektów i poprawnych referencji/rozmiarów/pozycji/stron/promieni przed podmianą mapy; nie wykonuje ponownie odkrywania. Brak migracji map 1–3. Import grafiki i bezpieczny zapis pozostają z 0.3.2; źródła są zwalniane.

### Granice

Nie planujemy walki, statystyk, ekwipunku/inicjatywy, wyszukiwania ścieżek, animacji ruchu lub zaawansowanych tras; nie dopisujemy ich jako przyszłych zadań. Później: niezależna warstwa światła/ciemności, źródło/zasięg światła obiektu, ciemność powracająca po odejściu, niezależnie od trwałego odkrywania. Światła teraz nie implementujemy. Nadal odłożone: widok MG/gracza, tekstury/detail, zakładki, kadrowanie/arkusze, opisy, pełny redesign i wydajnościowa kampania dużych map. Testy tylko kilku nowych scenariuszy i krótki zwykły EXE; raport uczciwie oddziela metody/okno/pominięte obszary.

## 28. Przyrost 0.4.1 — trwałe grupy, stany i podgląd mgły

Ta sekcja zastępuje sprzeczne ustalenia sekcji 27: aplikacja 0.4.1, format mapy 5. Zachowujemy Windows Forms/GDI+, lokalny WPF opisu, offline, src i Library obok EXE. Bez usług, nowych zależności, konwerterów i masowych migracji.

### Narzędzia i bieżące właściwości

Osobne widoczne przyciski Zaznaczanie, Prostokąt i Lasso; prawy przycisk nie wybiera już wariantu. Przybornik ma dwa rzędy, podpowiedzi i aktywne ramki. Rączka nad dostępną instancją przesuwa ją lub jej grupę, nad tłem kamerę. Nie otwiera szczegółów ani nie zamyka panelu podczas panowania. Respektuje efektywne blokady i mgłę. Pusty klik poza mapą odznacza heks/zamyka jego szczegóły, bez edycji dokumentu; klik obiektu nie jest pustym kliknięciem.

Właściwości instancji działają na bieżąco, bez Zastosuj. Checkbox/stan tworzy pojedynczą operację; nazwa i ciągła zmiana liczbowa są scalane do sesji pola. Sesję kończy inna operacja, utrata fokusu, wybór, zapis, historia lub zamknięcie. Puste/przejściowo niepoprawne wartości liczbowe i pusta nazwa nie trafiają do modelu; synchronizacja kontrolek nie wykonuje edycji. Nowe instancje mają rozmiar 30, istniejące pozostają bez zmian; zakres 8–1024 zachowany. Czerwony Usuń instancję dotyczy mapy, kosz wzorca jest nieaktywny bez jego wyboru.

Nazwa wzorca jest tylko początkową nazwą nowej instancji. Późniejsze nazwy są niezależne. Aktualizacja wystąpień wzorca aktualizuje grafikę powiązanych TemplateId, bez przemianowania instancji; checkbox mówi o grafice. Reguły nazw ikon heksowych pozostają bez zmian.

### Definicje stanów

Sojusznik (zielony, pozwala odkrywać), Neutralny (niebieski, nie pozwala) i Wróg (czerwony, nie pozwala) są kafelkami z nazwą, podpowiedzią i ramką wyboru. Plus dodaje dokumentowy stan: nazwa, kolor i Pozwala odkrywać mgłę, ze stabilnym ID niezależnym od wyglądu. Kafelki zawijają się; plus idzie za ostatnim. Dodanie stanu nie przypisuje go automatycznie do instancji. Nie implementujemy edycji/usuwania definicji ani globalnej biblioteki stanów.

Najpierw efektywny stan daje uprawnienie, następnie działa efektywny checkbox i promień 0–20. Stan bez uprawnienia zawsze blokuje skuteczne odkrywanie. Indywidualna zmiana na taki stan wyłącza bazowy checkbox; powrót go nie włącza. Kontrolki pozostają widoczne. Grupa może ograniczyć skuteczność bez niszczenia bazowego checkboxa. Promień 0 to heks pod środkiem, 1 pierwszy pierścień itd.; poza mapą nie ma najbliższego heksa.

### Model i wybór trwałej grupy

Grupa ma stabilne ID, nazwę i listę ID członków. Obiekt należy najwyżej do jednej grupy; brak zagnieżdżania. Grupuj wymaga co najmniej dwóch niezgrupowanych instancji. Mieszany wybór daje komunikat, bez ukrytego rozłączania. Nowa grupa nie zmienia właściwości członków i ma wszystkie nadpisania wyłączone.

Klik członka wybiera grupę; Ctrl dodaje/odejmuje grupę lub niezgrupowany obiekt; Alt wybiera pojedynczego członka także przed wcześniejszym wyborem grupy. Prostokąt/lasso rozszerzają trafionego członka do grupy. Obrysy ukrytych członków pozostają przycięte mgłą. Panel członka pokazuje grupę, przejście do niej i Odłącz; panel grupy nazwę, członków, wybór pojedynczego, nakładki i Rozwiąż. Delete usuwa wybrane instancje jednym wpisem. Alt-wybór umożliwia usunięcie jednego; puste referencje/grupy są usuwane, jeden członek może pozostać grupą.

### Odwracalne nakładki

Bazowe właściwości instancji są zachowane. Grupa ma opcjonalne nadpisania stanu, blokady, odkrywania, promienia i rozmiaru, każde z Indywidualnie. Rozmiar: Indywidualnie, Przesunięcie od bazowego (±1016, wynik ograniczany do 8–1024) albo Wspólny (8–1024). Przeliczanie nie kumuluje offsetu, nie przesuwa środków i zachowuje proporcje. Jawna blokada true blokuje wszystkich; false może odblokować bazowo zablokowanego. Alt-edycja zmienia bazę, nie wyłącza nakładki. Panel członka wyjaśnia wartości efektywne.

Odłącz/Rozwiąż pyta: Zachowaj wynikowe ustawienia / Przywróć indywidualne / Wybierz dla każdej właściwości / Anuluj. Kategorie: stan, rozmiar, blokada, odkrywanie, promień. Przy rozwiązaniu decyzja kategorii dotyczy całej grupy. Zachowaj zapisuje efektywne do bazy z respektowaniem uprawnień; Przywróć odsłania zachowaną bazę. Nazwy, pozycje i dawne odkrycia pozostają. Zmiana członkostwa, właściwości i wynikającej nowej mgły jest jednym Undo. Anulowanie niczego nie zmienia.

### Aktywne odkrywanie i flaga widoku

Po ręcznej mgle, ruchu/umieszczeniu, zmianach wpływających na odkrywanie i odłączeniu/rozwiązaniu uzgadniamy wynik ze wszystkimi aktualnymi efektywnymi zasięgami. Ich heksy pozostają odkryte także podczas ręcznego gestu. Nie ma pętli co klatkę. Jeden gest/ustawienie zawiera końcową deltę mgły, a Undo/Redo odtwarza tę deltę bez nowej operacji uzgadniania. Undo ruchu nadal nie zasłania wcześniejszych odkryć. Odczyt nie uruchamia ponownie odkrywania.

Podgląd bez mgły to oznaczona flaga widoku: bez kopiowania mapy, zmiany Fog, historii, gwiazdki lub zapisu flagi. Pokazuje i pozwala edytować rzeczywiste heksy/obiekty. Ruch nadal odkrywa rzeczywiste Fog. Nowa/Otwórz po sukcesie wyłącza podgląd. Ręczny gest mgły pyta o normalną widoczność: odmowa nie zmienia danych; zgoda wyłącza podgląd, ale gest jest anulowany i trzeba go ponowić.

### Lista i dopasowanie

Lista: Nazwa z miniaturą / efektywny Stan / Pozycja / Mgła. Pozycja jest pusta na mapie, Poza mapą poza nią. Mgła: Widoczny / Częściowo ukryty / Ukryty. Częściowe i pełne zasłonięcie maskuje nazwę, stan i pozycję przez ???, daje neutralną miniaturę i nie ujawnia danych tooltipem. Klasyfikacja dotyczy przecięcia prostokąta grafiki z geometrią zasłoniętych heksów, bez analizy pikseli alfa. Poza mapą brak mgły. Podgląd pokazuje realne dane, lecz kolumna Mgła nadal opisuje zapisany stan, zgodnie z tytułem okna. Lista pozwala świadomie zaznaczyć/Pokaż ukryty obiekt bez odkrywania heksów. To narzędzie edytora, nie zabezpieczony widok gracza.

Dopasuj mapę i wszystko celowo pomijają prawą nakładkę. Wspólna suma obwiedni i jeden algorytm/margines dają dokładnie identyczny kadr, jeśli efektywne prostokąty wszystkich grafik mieszczą się w obwiedni mapy. Wystające grafiki prawidłowo zmieniają kadr. Menu Dopasuj ma styl aplikacji; Otwórz nadal dopasowuje samą mapę.

### Format i historia

Format 5 dodaje obowiązkowe ObjectStates i Groups, zachowuje bazowe Objects i wspólny IconAssets. Side wskazuje ID stanu. Zapis zawiera członkostwo/nakładki i użyte osadzone grafiki, bez zależności od Library. Starsze mapy nie są konwertowane ani nadpisywane podczas odmowy odczytu. Walidacja przed podmianą obejmuje unikalne ID, poprawne referencje stanów/członków, brak wielu grup/nesting, skończone liczby i zakresy. Limity: 100 stanów, 2500 grup, 5000 instancji; dotychczasowe limity zasobów i bezpieczny zapis pozostają.

Wspólna historia 300 obejmuje grupowanie/nakładki/odłączanie/własne stany/usunięcia i potrzebne delty mgły obok mapy oraz chronionych bajtowych operacji plików bibliotek. Wybór, kamera i podgląd nie są historią. Osobne historie nazwy/opisu heksa pozostają. Nowa/Otwórz resetują dokument po sukcesie, błąd/anulowanie go zachowuje.

### Granice i weryfikacja

Bez zagnieżdżonych grup, światła/ciemności, widoku MG/gracza, tekstur/detail, zakładek, kadrowania, redesignu i optymalizacji dużych map. Walki/statystyk/ekwipunku, tras/pathfindingu/animacji nie planujemy w tym programie. Nie wznawiamy zaakceptowanych prac 0.4.0. Testy tylko celowane nowych zachowań; raport rozdziela metody/kontrolki, faktyczne próby zwykłego EXE i niewykonane obszary, bez deklaracji kompleksowej weryfikacji.

## 29. Przyrost 0.4.2

- Program korzysta z dostarczonej ikony `assets/app/icon.ico`. Kompilator osadza ją w EXE. Okno odczytuje ICO bez blokady pliku i przy błędzie używa zasobu EXE. `Rebuild-Program.bat` uruchamia `Build.ps1` względem swojego katalogu. Wynik zastępuje poprzednią binarkę po udanej kompilacji.
- `Library/Objects/States` przechowuje definicje w plikach `state-Nazwa.json`. Podstawowe ID można edytować i są chronione przed usunięciem. Czysta biblioteka bierze ich definicje z kodu. Nowa mapa bierze stany z biblioteki. Otwarta zachowuje swoje definicje. Plus dodaje stan w obu miejscach. Edycja aktualizuje mapę po zaznaczeniu checkboxa, a usunięcie pliku zachowuje definicję mapy. Stan z dokumentu można świadomie zapisać do biblioteki. Pliki, dokument i delty mgły podlegają wspólnej historii do 300 operacji.
- Format 6 zapisuje ramki i definicje podstawowe z możliwością edycji. Odczyt formatu 5 przyjmuje brak ramki jako wyłączenie. Zapis osadza użyte grafiki, definicje podstawowe i własne stany użyte przez obiekt lub grupę. Uwzględnia bazę przykrytą nakładką i zapisuje pełną paletę terenów projektu.
- Do zaznaczonej grupy można dodać niezgrupowane obiekty. Kilka zaznaczonych grup można połączyć, zachowując ID i ustawienia wybranej grupy lub tworząc nową nazwaną grupę ze świeżymi ustawieniami. Kolejność propozycji wynika z kolejności zaznaczenia, a przy pojedynczym geście obszarowym z kolejności grup w dokumencie. Operacja obejmuje wszystkie wybrane instancje, zachowuje bazę i uzgadnia mgłę w jednym wpisie. Opcjonalna kreskowana ramka z wybranym kolorem otacza każdą grafikę i jest przycinana przez mgłę.
- Lista instancji maskuje dane i miniaturę do 50% widoczności włącznie. Liczy sumę przecięć prostokąta grafiki z rozłącznymi wnętrzami zasłoniętych heksów. Próg używa wartości przed zaokrągleniem. Etykieta częściowego ukrycia podaje całkowity procent ukrytej powierzchni, z połówką w górę. Podgląd ujawnia dane, a kolumna mgły zachowuje opis geometrii.
- Usuwanie z mapy przez przycisk lub Delete prowadzi do jednego potwierdzenia. Ctrl+Z i Ctrl+Y rozpoznają dokładne modyfikatory. Ctrl+Shift+Z nie zmienia historii. Dopasuj przełącza menu tym samym przyciskiem. Minimalna wysokość okna wynosi 720 px.
- Do późniejszej pracy: zapis ikon, wzorców obiektów i przyszłych tekstur lub tła z odebranej mapy do biblioteki, mapa jako wizualny katalog grafik, wybór sposobu odłączenia dla każdego członka, przeprojektowanie paneli, światło, tekstury, zakładki i kadrowanie.

## 30. Przyrost 0.4.3

- Przerywana ramka grupy jest bliżej grafiki, złoty obrys wyboru dalej. Obrysy są odrębne dla każdego członka i przycinane przez mgłę.
- Lista obiektów na mapie pokazuje miniaturę, nazwę, efektywny stan z kolorowym znacznikiem, grupę, pozycję i mgłę. Maskowane teksty oraz znaczniki nie ujawniają danych. Nagłówki przełączają rosnąco, malejąco i porządek dokumentu. Klucz tekstowy to tekst widoczny, a klucz mgły to liczbowy udział zakrycia. Remisy zachowują kolejność dokumentu. Zaznacz i Pokaż używają ID.
- Prawy panel jest pozycjonowany przed pokazaniem. Tło i natywne kontrolki tworzą kompozytową gałąź okien z pełnym synchronicznym odrysowaniem przy otwarciu i zmianie struktury. Istniejące kontrolki oraz WPF pozostają w tym samym drzewie.
- Usuń stan z mapy pokazuje referencje i zamiennik przed zmianą. Domyślne ID to Neutral. Chronione ID są wyłączone z usuwania. Jedna transakcja zastępuje referencje bazowe i grupowe, uwzględnia mgłę i opcjonalnie usuwa plik. Format 6 może zawierać `ExcludedStateIds`, aby biblioteka nie przywracała usuniętego stanu tej mapie. Odczyt 5 i 6 traktuje brak pola jako pustą listę.
- Pliki bibliotek mogą zawierać `LibraryOrder`. Dodawanie dopisuje po bieżącej kolejności, edycja zachowuje klucz, a historia przywraca plik i pozycję. Starsze pozycje mają deterministyczny porządek ID. Domyślne stany i preset pozostają na początku. Tereny zachowują kolejność tablicy w palecie. Rozruch nie dopisuje klucza dawnym plikom.
- Menu Widok pozostaje otwarte po przełączeniu warstwy. Flagi Tereny, Ikony, Obiekty i Mgła dotyczą widoku. Edycja ukrytej warstwy może przywrócić jej widoczność, po czym wymaga ponowienia operacji. Aktualizacja mapy z dialogu biblioteki sprawdza warstwę przed zamknięciem dialogu.
- Budowanie oczekuje `assets/app/icon.ico`. Weryfikuje format i publikuje EXE po kompilacji. Eksplorator może odczytywać ikonę z pamięci podręcznej powłoki.
- Na później pozostają bardziej intuicyjne akcje edycji i usuwania stanów dostępne bez prawego przycisku.

## 31. Przyrost 0.5.0

- Kompozycja HWND obejmuje tylko natywne szczegóły obiektów. ElementHost i WPF w szczegółach heksa są poza tą gałęzią. Ponowne pokazanie już załadowanego opisu zachowuje jego treść i historię. Zmiana heksa lub stanu mgły może załadować właściwy opis.
- Ponowny zwykły klik wybranej instancji lub członka dokładnie wybranej grupy odznacza ją i zamyka szczegóły. Decyzja zapada na końcu gestu, po odróżnieniu kliknięcia od przesunięcia. Ctrl i Alt zachowują swoje role. Lista używa efektywnego stanu grupowego, a mgła zachowuje dotychczasowy próg maskowania 50%.
- Prawy klik plusa stanów pozwala świadomie usunąć wykluczenie po stabilnym ID, jeżeli w bibliotece pozostaje plik stanu. Cofnięcie przywraca wykluczenie. Wybór nie przypisuje stanu obiektom i nie nadpisuje skonfliktowanej definicji. Lewy klik tworzy nowy stan.
- Teren ma jednolity kolor albo kwadratowy wycinek PNG 256 × 256. Obie orientacje korzystają z tego samego środka, skali i nieobróconego obrazu. Maski odcinają tylko fragmenty poza heksami. Przezroczystość źródłowa jest zachowana, a pod grafiką pozostaje kolor podkładu terenu.
- Import nieruchomych PNG, JPEG, BMP i GIF ma limit 16 MiB i 4096 × 4096. Wypełnij ustawia początkowy kadr z pokryciem całego kwadratu. Przeciągnięcie przesuwa obraz pod nieruchomą maską, a kółko zmienia przybliżenie. Maska Pointy Top / Flat Top nie resetuje kadru. Zatwierdzenie zapisuje wycinek do biblioteki i zwalnia źródło. Anulowanie kadru niczego nie zapisuje.
- `Library/Terrains/Graphics` zawiera `terrain-Nazwa.json` z ID, nazwą, kolejnością i osadzonym PNG. Wybór, dodawanie, edycja i usuwanie są dostępne z edycji terenu. Można kadrować zapisany wycinek lub ponownie wskazać źródło. Sesyjna historia plików chroni obce zmiany i zajęte ścieżki. Potwierdzone zapisy do biblioteki są odrębne od zatwierdzenia edycji terenu.
- Heks i paleta odwołują się do treści wycinka. Zmiana zasobu biblioteki nie przemalowuje heksów ani nie zmienia zapisanych presetów. Zmiana terenu na mapie zachowuje inne warstwy. Region Wiadra terenu zależy od wariantu, a Wiadra mgły od dotychczasowych danych regionu. Widok Tereny zastępuje teksturę neutralnym kolorem bez utraty danych. Mgła zasłania teksturę.
- Mapa 7 zapisuje jedną tabelę `TerrainAssets` dla grafik heksów i wszystkich palet dokumentu. Preset 2 ma własną tabelę dla swojej palety. Odwołania `TextureId` pozwalają deduplikować dane według zawartości. Pliki są niezależne od biblioteki. Limity to 128 grafik i 44 MiB Base64 na tabelę, 48 MiB na preset i 128 MiB na mapę. Program zachowuje odczyt map 5–6 i presetów 1.
- Teren, stan i ramka grupy używają wspólnego dialogu HSV z podglądem poprzedniego i nowego koloru. Pole HEX przyjmuje dokładnie `#RRGGBB` bez rozróżniania wielkości liter. Niepełny lub błędny HEX blokuje zatwierdzenie. Ołówek terenu Domyślnego pokazuje wyjaśnienie i nie otwiera edycji.
- Odłożono poziomy szczegółowości, tło mapy, światło, zakładki, przebudowę opisów i paneli, drżenie nagłówka prawego panelu, dalszą diagnozę ikony powłoki oraz zapis osadzonych zasobów z otrzymanej mapy do biblioteki.

## 32. Przyrost 0.5.1 — pełne źródła, pule i tło

Ta sekcja zastępuje zapis wyłącznie małego wycinka z sekcji 31. Każde źródło zachowuje oryginalne bajty nieruchomego PNG/JPEG/BMP/GIF (16 MiB, do 4096×4096); treściowa tożsamość SHA-256 jest potwierdzana długością i bajtami. Logiczna pozycja biblioteki ma niezależne ID. Źródło jest wspólne, nie tożsamość edycyjna. Duplikat definicji wymaga identycznych źródeł, obu transformacji i połączenia; dialog daje istniejącą/nową/anuluj, z miniaturami i nazwami. Inny kadr nie jest identyczną definicją. Bez podobieństwa wizualnego i automatycznego czyszczenia.

Dwa podglądy obok siebie: Pointy Top, Flat Top. Transformacje zachowują znormalizowany środek, zoom 1–24 z ograniczeniem pokrycia i obrót modulo 360°. Lewy/środkowy przycisk przesuwa obraz, kółko zmienia zoom, suwak obrót, reset daje 0°. Nowy import jest połączony. Połączenie/rozłączenie nie resetuje wcześniejszych różnic. Obrót stosuje deltę, zoom wspólny współczynnik, ruch wspólną deltę źródła ograniczoną przez oba kadry; pokrycie obróconego kwadratu może wymagać wspólnego zwiększenia skali. Wybór orientacji mapy używa właściwego kadru, bez automatycznych 30°. Ponowna edycja korzysta z pełnego źródła i poprzednich parametrów, także bez biblioteki.

Teren ma 1–32 warianty kolor/grafika/mieszanka, edytowane kafelkami i plusem. Można zmienić typ; nie można usunąć ostatniego. Nie ma globalnego przełącznika typu całego terenu. Anuluj wariantu nie zmienia puli. Zatwierdzony import biblioteczny pozostaje niezależną operacją. Kafelek terenu pokazuje pierwszy wariant, pełne proporcjonalnie przycięte tło i pogrubioną nazwę na ciemnym podkładzie; miniatura nie losuje.

Warianty mają równe prawdopodobieństwo, niezależne losowanie bez wymuszonej równowagi. Heks przechowuje wybrany ID i migawkę wyglądu. Jeden gest odwiedza pole raz; redraw/orientacja/odczyt/Redo niczego nie losują. Pula może zmieniać przyszłe malowanie bez przemalowania starych pól; jawna aktualizacja ma jedno Undo i zapamiętane wyniki. Regiony terenu/ikon używają logicznego TerrainId, a mgły TerrainId i początkowego Fog. Nazwa, kolor lub tekstura wybranego wyglądu nie rozbijają jednego rodzaju terenu. Nie analizujemy pikseli.

Prawy klik narzędzi mgły otwiera Wygląd mgły, wspólny edytor wariantów. Domyślnie szarość. Losowanie następuje przy rzeczywistym nałożeniu Fog i jest zapisywane; pod PNG zawsze nieprzezroczysta baza. Edycja puli przyszła, jawny domyślnie wyłączony checkbox może zmienić wygląd już zasłoniętych heksów w jednym Undo, bez zasłaniania nowych. Ochrona odkrywców i warstwy pozostają.

Tło obok Biblioteki zachowuje pełne źródło i prostokąt Left/Top/Width/Height. Przycinanie narożnikami lub nowym prostokątem myszy, proporcjonalne cover, krycie 0–100, nasycenie 0–200 (0 szarość), rozmycie 0–24. Efekty nie zmieniają źródła. Obraz wypełnia ekranowy obszar płótna, nie zależy od pan/zoom i nie wchodzi do obwiedni Dopasuj. Bufor efektów odbudowywany po ustawieniach/rozmiarze, nie po każdym MouseMove. Edycja/usunięcie jedno Undo, Anuluj niczego nie zmienia. Widok → Tło jest flagą bez gwiazdki/historii; Nowa/Otwórz przywracają widoczność.

Mapa 8 i preset 3 przechowują deduplikowane GraphicSources oraz TerrainAssets z dwoma kadrami. Uwzględniają zastosowane wyglądy, wszystkie palety projektu, mgłę i tło; nie całą bibliotekę. Cache jest pochodny. Źródło raz na tabelę, nie raz na heks. Biblioteka format 2: terrain-Nazwa.json + Sources/source-SHA256.json, bezwzględne ścieżki nie trafiają do danych. Czytelne sufiksy, kolejność i LibraryFiles pozostają. Magazyn Sources jest niezmienny i zachowuje źródła do ponownego użycia po cofnięciu/usunięciu wpisu; brak automatycznego sprzątania prywatnych danych.

Budżety: 64 pełne źródła /64 MiB danych, 256 definicji, 128 aktywnych wpisów biblioteki, 32 warianty/pulę, mapa 128 MiB, preset 112 MiB. Wyniki 256×256, LRU do 256 kafelków/64 MiB; bufor tła do 2048 w większym wymiarze. Przekroczenie odrzuca operację czytelnym błędem. Przyjęto jawnie nowe formaty bez odczytu starych map/presetów/wycinków. Stare pliki nie są usuwane ani migrowane; nie odtwarzamy utraconego pełnego źródła.

Minimalne testy mają jawny Case; brak parametru nie uruchamia kampanii. Raport rozdziela metody i zwykły EXE. Bez pełnych testów, DPI i benchmarków. Odłożone: high/low detail, światło, widok gracza, zakładki, eksport osadzonych zasobów do biblioteki, wagi, przebudowa opisów, nagłówek prawego panelu i dalsza diagnoza ikony powłoki. Bez ogólnego edytora obrazów, AI, walki, tras i animacji.

Zasada wydania: funkcje, poprawki i uzgodnione audyty kończą się przed 1.0.0. Sam przyszły numer 1.0.0 oznacza wyłącznie zmianę oznaczeń, bez dodatkowych funkcji lub stylistyki. Nie wykonujemy teraz tego przejścia ani audytu.
