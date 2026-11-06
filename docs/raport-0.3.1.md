# Raport HexMapEditor 0.3.1

Baza: ukończona 0.3.0, czysty master `14ba0cd24af4015047cc85eb5eba604d767c3930`. Origin: https://github.com/amdesar/HexMapEditor.git; przy rozpoczęciu lokalne HEAD/origin/master zgodne. Nie wykonywano fetch ani synchronizacji (nie było takiego wymagania w tym zadaniu). Przeczytano aktualny README, raport/plan 0.3.0 oraz odpowiednie wymagania; nie znaleziono AGENTS.md w sprawdzonym repozytorium/ścieżce nadrzędnej. Brak zastanych zmian do zachowania i uruchomionych procesów edytora/kompilacji w sprawdzonym stanie. Nie rozpoczęto ponownie prac 0.3.0. Bez push, tagu, Release'a i przepisywania historii.

## Wykonane zmiany

- Wiadro mgły wymaga jednocześnie zgodnego koloru, dokładnej nazwy rodzaju terenu i początkowego Fog. Nie porównuje wariantów/ID, ikon, opisu ani własnej nazwy heksa. Regiony są liczone z migawki sprzed gestu, na dotychczasowej kolejce i sześciu sąsiadach.
- Przeciąganie Wiadra terenów, ikon i mgły przechodzi przez kolejne regiony, jako jeden wpis historii. Granice nie zmieniają się wskutek własnego wypełnienia. Tryb dodawania/usuwania ikon lub zasłaniania/odkrywania jest stały dla całego gestu; mgła pomija przeciwny początkowy stan, który już odpowiada celowi. Tereny i ikony nadal używają wariantu terenu.
- Synchroniczne grupowanie kompensacji kamery i zmiany granic płótna z WM_SETREDRAW, następnie odrysowanie gotowego stanu. Bez opóźnień, timerów, automatycznego Dopasuj i zmiany zaakceptowanej kompensacji.
- Więcej miejsca pod separatorami; usunięty tekst materiału. Pogrubiona złota ramka dla terenu albo ikony. Obiekty nadal placeholder. Rozmiar i Wypełnienie Punktu nieaktywne z wyjaśnieniem; geometrie nieprzebudowane.
- Zamiast pionowego scrollbara terenów: dwie strzałki o stałym miejscu i kółko, własny offset i clipping bez zwężania kafelków. Na granicach strzałki są nieaktywne. Dodaj i pozostałe sekcje są nieruchome, kontrolki palety zachowane przy niezwiązanych aktualizacjach.
- Domyślny całkowicie tylko do odczytu po ID, w kontrolkach, skrótach i metodach. Blokada nazwy/definicji/dodawania/usuwania/nadpisania/usunięcia nie blokuje malowania. Świadomy zapis jako nowy wybiera własną edytowalną kopię dopiero po sukcesie.
- Ołówek Edytuj ikonę: nazwa, podmiana z pliku lub dostępnej biblioteki, podgląd oraz domyślnie wyłączona aktualizacja bieżącej mapy. Bez wyboru: Zaznacz ikonę. Bez checkboxa dawne wystąpienia zachowują migawki; z nim zmieniają się grafika i nazwa wystąpień logicznego ID tej pozycji, także jej starszych wariantów. Inne pozycje o tych samych bajtach oraz teren/mgła/tekst heksa pozostają.
- Czytelne pliki ikon `icon-Nazwa.json`, `icon-Nazwa (2).json`, stały mały prefiks, zachowane litery/znaki użytkownika. Wykorzystano istniejącą bezpieczną normalizację i limit ścieżek. Świadoma edycja zmienia jeden plik, bez dodatkowej aktywnej kopii.
- Sesyjna wspólna historia mapy i ikon odtwarza pliki importu/usunięcia/nazwy/podmiany oraz opcjonalną mapę w jednym wpisie. Migawki bajtowe są w RAM, nie w prywatnym Koszu. Same zmiany biblioteki nie oznaczają mapy gwiazdką. Pliki presetów nadal nie są cofane.
- Wszystkie źródła C# przeniesione do jednego `src/`, Build.ps1 używa src. Skrypty testowe nadal korzystają ze wspólnego Build, źródła testów pozostają w tests. Library pozostała obok EXE. Wersja EXE 0.3.1.0, bez zależności/usług.
- Lokalna interpolacja odcisków pędzla, opisana niżej.

## Model, zapis i bezpieczeństwo ikon

**Format mapy pozostaje 3**, z opcjonalnymi IconEntryId i IconName komórki. To stałe źródło logiczne i migawka nazwy wystąpienia. IconId nadal wskazuje deduplikowany zasób PNG po SHA-256. Wspólne bajty nie oznaczają wspólnej pozycji biblioteki. Zapis nadal osadza wyłącznie użyte zasoby, jeden dla wielu wystąpień; brak zależności od biblioteki lub źródłowych grafik. Nowe opcjonalne pola nie uzasadniają zmiany numeru mapy. Stare dane 3 bez tych pól są dopuszczane, brak identyfikacji dawnych pozycji poza dawnym content-ID nie jest zgadywany dla nowych niezależnych pozycji. Nie dodano migracji map 1/2. Starszy 0.3.0 nie zachowa nowych opcjonalnych metadanych przy ponownym zapisie; do ich zachowania używaj 0.3.1.

Biblioteczny JSON ikony ma teraz **IconFormatVersion 2**, logiczne ID, nazwę i Asset. Wczytywanie starego płaskiego JSON zachowuje stare ID jako logiczne ID w pamięci. Nie przemianowuje i nie przepisuje plików przy starcie. Dopiero świadoma edycja zapisuje opakowanie 2 i czytelną nazwę. Nie wykonano migracji prywatnej Library ani operacji na jej danych.

Historia (wspólny limit 300 wpisów) zachowuje poprzednie/nowe bajty i metadane do chwili usunięcia odpowiadającego wpisu. Nowa rzeczywista operacja kasuje Redo; udane Nowa/Otwórz resetują historię, anulowanie i błąd odczytu nie. Reset/zamknięcie nie usuwa aktywnych plików biblioteki. Zwykłe pytanie o niezapisane zmiany mapy nadal obowiązuje. Ponieważ biblioteka jest już zapisana, sama jej zmiana nie wywołuje fałszywej gwiazdki mapy.

Usunięcie ikony nie korzysta teraz z Kosza: plik jest usuwany, a bajtowa kopia istnieje w pamięci sesyjnego Undo. Potwierdzenie opisuje brak Kosza i utratę możliwości cofnięcia po utracie historii. Jest to świadoma zmiana obsługi ikon; usuwanie presetów nadal używa dotychczasowej ścieżki Kosza.

Przejście dyskowe sprawdza oczekiwane bajty (zapamiętane przy odczycie, nie dopiero przy zapisie) i zajętość docelowej ścieżki. Źródło jest przenoszone do unikalnego `.icon-session-*.tmp`, ponownie sprawdzane; nowe bajty są zapisywane przez CreateNew, Flush i atomowy Move bez nadpisania. Po sukcesie nieaktywna kopia jest usuwana, a dopiero potem aktualizowana jest pamięć/lista/mapa/stos. Przy błędzie odtwarzana jest poprzednia ścieżka. Jeżeli sama zewnętrzna kolizja zablokuje rollback, komunikat wskazuje zachowany plik, nie nadpisuje obcych danych. Jeśli nie uda się posprzątać własnej nieaktywnej kopii po udanym przejściu, może pozostać `.tmp`; nie jest aktywną ikoną i nie jest rekurencyjnie skanowana. Nie ma automatycznego sprzątania biblioteki przy starcie.

## Przeskok panelu — przyczyna i ograniczenie weryfikacji

W 0.3.0 kompensacja pan.X była wykonywana przed SetBounds natywnego okna płótna. Pomiędzy tymi zmianami istniał niespójny stan transformacji i granic, a zmiana rozmiaru/przesunięcia mogła odrysować lub skopiować stare piksele. Samo poprawne końcowe położenie ani SuspendLayout nie wykluczały takiej klatki. W 0.3.1 CanvasUpdate zatrzymuje natywne odrysowywanie na czas całej synchronicznej zmiany i w finally włącza je oraz odmalowuje gotowe płótno. Brak osobnego ruchu kamery lub opóźnienia maskującego. Mechanizm WM_SETREDRAW zweryfikowano w [dokumentacji Microsoft](https://learn.microsoft.com/windows/win32/gdi/wm-setredraw); płótno nie ma własnego nieklienckiego obramowania.

W zwykłym EXE wykonano zwinięcie i rozwinięcie. Położenie krzyża i siatki na dostępnych zrzutach było stabilne; metoda sprawdziła również kompensację końcową. **Computer Use zwraca obrazy po akcji, nie zapis każdej klatki przejścia. Nie potwierdzono nagraniem rzeczywistej błędnej klatki 0.3.0 ani jej całkowitego braku w 0.3.1.** Diagnoza opiera się na kolejności zmian kodu/natywnego odrysowania; to mała poprawka usuwająca wskazany niespójny etap. Warto sprawdzić płynność wzrokowo u użytkownika; nie uznajemy samych końcowych bounds za pełne potwierdzenie wizualne.

## Ocena ciągłości pędzla

Odstępy wynikały z nanoszenia odcisków wyłącznie w pozycjach dostarczonych przez MouseMove; system może pominąć pozycje przy szybkim ruchu. Poprawka okazała się lokalna: PaintAlong próbkuje odcinek w jednostkach świata co najwyżej 0,4 bazowego promienia heksa (11,2 jednostki) i używa istniejącego odcisku oraz visited. Nie zmienia kształtów i nie tworzy dodatkowych wpisów historii. Poprzednia pozycja jest zerowana przy wyjściu poza płótno/siatkę, MouseLeave, utracie capture i końcu/nowym geście. Warto było wdrożyć teraz, bo nie wymaga nowego systemu narzędzi.

Ryzyko: więcej odcisków przy bardzo szybkim ruchu i dużej figurze, a więc większy koszt obliczeń. Nie wykonywano długiej kampanii wydajnościowej. Test potwierdził ciągły krótki odcinek oraz brak łącznika po wyjściu/powrocie; nie wykonano pełnej macierzy wszystkich figur i skal. Interpolacja odwzorowuje odcinek między zdarzeniami, nie rekonstruuje nieznanej zakrzywionej drogi kursora.

## Faktycznie wykonane testy metod/kontrolek

Pięć krótkich scenariuszy, każdy uruchomiony **raz**, przez Run-Patch031Tests.ps1 z konkretnym Case; wszystkie PASS, kod 0. Nie wywołano Run-AllTests, równoważnika kampanii, historycznych zestawów ani dziewięciu grup 0.3.0. Późniejsza drobna osłona przerwania iteracji regionu po zatrzymaniu gestu przez limit zasobów i tekst potwierdzenia nie zmieniały zaliczonych ścieżek; import i zwykły build używały już ich bieżącej wersji.

- **regions**: dwie plamy mgły rozdzielone odkrytymi polami, zgodny kolor/różna nazwa, różne warianty bez podziału mgły, jeden gest przez kolejne regiony ze stałym trybem i Undo/Redo; przeciąganie ikon i terenów ze wspólną historią.
- **controls**: niezmienność Domyślnego przez metody i F2/Delete, blokady przycisków, zapis/wybór edytowalnej kopii, Punkt i przywracanie kontrolek innej figury, złota ramka. Minimalny formularz, granice strzałek i stały viewport, kompensacja końcowa płótna. Krótki szybki odcinek mgły i reset po wyjściu bez linii przez mapę. To nie jest pełny test wizualny.
- **icons**: niezależne ID przy takich samych PNG, czytelne nazwy/sufiks, edycja nazwy/grafiki bez mapy i z nią, dokładne Undo/Redo oraz zachowanie Fog/tekstu/innej pozycji o tych samych bajtach. Zapis dwóch użytych zasobów z metadanymi w formacie 3; usunięcie i bajtowe odtworzenie pliku. Zajęta ścieżka Undo i zewnętrznie zmienione bajty nie są nadpisane i nie przesuwają stosu. Plik zablokowany przeciw zmianie nazwy nie zmienia pamięci/mapy/historii. Stary płaski plik pozostaje po odczycie, porządkuje się dopiero po edycji.
- **import**: własny PNG 24×40, zwolnione źródło, Undo/Redo dodania biblioteki i pliku, metadane i jeden współdzielony zasób po zapisie/odczycie, zachowanie historii po błędnym otwarciu, reset po udanym otwarciu i Nowa bez usuwania aktywnych ikon.

Dodano osobny **names** po znalezieniu podczas przeglądu brzegowego skracania prefiksu: polskie litery/wielkość, nazwy Windows i brak wyjścia z katalogu oraz odmowa kolizji w ścieżce bliskiej limitu, jeżeli sufiks skróciłby icon-. Wcześniejszych scenariuszy nie powtarzano. Log tej próby: tests/_work/patch-031-7674919738cb4fa19588d3d72410a1f6/results.log.

Logi pozostają w tests/_work/patch-031-24ac67c109e848cf9e349d836c14a8aa (regions), patch-031-2dee6a1847a145ca9a52c261cc5ba44a (controls), patch-031-86608ae9dbca4487abced694c36ed6d1 (icons), patch-031-d1baddc4c7764431ac33fe1317c5d6e7 (import). Runner sprząta własne binaria/dane/Library, zostawia results.log. Historyczne źródła zachowano; helpery FogIconTests dostosowano do nowych kontrolowanych zmian, bez deklaracji ponownego zaliczenia.

## Faktyczne próby zwykłego EXE

Zwykły build z src zakończył się powodzeniem; wersja 0.3.1.0. Poniższy scenariusz wykonano przed ostatnią osłoną skrajnej długości nazwy; nie zmieniała ona działania panelu/edycji w badanej lokalizacji. Kopia do izolowanego `tests/_work/ui-031-5d80850ae45f4878a28d8d43cd7810cf/HexMapEditor.exe`, bez kompilacji diagnostycznej. SHA-256 kopii i głównego EXE identyczne: `7BF74093D470ACD932CA2D41D13FFAEEB0B0DFCE17484345AA816A7C66A0BFD0`.

1. Start: odstępy separatorów, brak tekstu materiału, złota ramka terenu, Punkt z wyłączonymi polami, Domyślny z nieaktywnymi akcjami definicji (wybór terenów dostępny).
2. Otwarcie własnej mapy z 18 terenami. Przewinięcie kółkiem do końca (Teren 18), użycie górnej strzałki, powrót kółkiem na początek (Teren 01), użycie dolnej strzałki. Strzałki miały stałe miejsca i poprawne stany graniczne; Dodaj, pozostałe sekcje i mapa nie przemieszczały się. W dostępnych obrazach brak nachodzenia/oscylacji listy; nie ma nagrania każdej pośredniej klatki.
3. Zwinięcie/rozwinięcie panelu; siatka i krzyż zachowały pozycję na zrzutach. Ograniczenie oceny chwilowego przeskoku opisano osobno powyżej.
4. Edytuj bez aktywnej ikony pokazało Zaznacz ikonę. Wybór własnej pozycji usunął złotą ramkę terenu i zaznaczył tylko ikonę, bez gwiazdki.
5. Dialog edycji: widoczny podgląd, nazwa, plik i lista dostępnych grafik, checkbox początkowo wyłączony. Zmieniono nazwę Próba edycji → Próba zmieniona, wybrano Gwiazdę z listy, włączono aktualizację mapy. Po OK gwiazda zastąpiła krzyż w bibliotece i A1, mapa otrzymała gwiazdkę.
6. Jedno Ctrl+Z przywróciło krzyż w obu miejscach i usunęło gwiazdkę; jedno Ctrl+Y odtworzyło gwiazdę i zmianę. Zapis własnej mapy Ctrl+S zakończył się bez gwiazdki; kontrola pliku potwierdziła logiczne ID/nazwę Próba zmieniona i samowystarczalny zasób. Biblioteka zawierała jeden icon-Próba zmieniona.json zamiast starego pliku.
7. Zakończono proces testowego okna przed sprzątaniem. Nie użyto prywatnej Library, nie wykonano na niej migracji ani testów.

Po osłonie długości wykonano końcowy build i tylko start/zamknięcie jego zwykłej kopii, bez ponawiania zaliczonych scenariuszy. Końcowy SHA-256 głównego EXE i kopii: F289F5C26E61E94B38C1A8638154202FBBF080D7A9D9AAB411558788B71906EB. Start prawidłowy, czysta biblioteka utworzona z kodu, widoczna wersja 0.3.1 i blokady Domyślnego/Punktu.

## Niewykonane próby i ograniczenia

Nie uruchomiono pełnych ani dawnych zestawów, Worda, kampanii edytora opisów/formatowania/tabel, DPI, prawego panelu, wydajności dużych map lub 200×200. Nie powtórzono zaakceptowanych geometrii 0.3.0. Nie przeprowadzono interaktywnej macierzy wszystkich wiader, figur i orientacji; nowe regiony/ciągłość sprawdzono metodami. Nie potwierdzono wysokoczęstotliwościowym nagraniem braku każdej przejściowej klatki panelu/listy. Nie wykonano prób awarii zasilania, wszystkich faz równoczesnych zmian plików i blokady samego rollbacku; te przypadki mają ochronę w kodzie i komunikat zachowanej kopii, ale nie są tu deklarowane jako przetestowane. Nie symulowano braku pamięci po udanym zapisie.

Przycisk podmiany grafiki z pliku widziano w dialogu, ale interaktywnie użyto listy biblioteki; import PNG i zapis były sprawdzone metodą. Bez checkboxa, usunięcie, odtwarzanie po usunięciu i konflikty sprawdzono metodami, nie potwierdzano usuwania przez Computer Use. Nie wykonano ponownie testów anulowania zamknięcia aplikacji/systemowych dialogów; istniejący mechanizm modalny i ConfirmDiscard pozostają. To selektywnie sprawdzona łatka, nie kompleksowo przetestowana aplikacja.

Pozostają odłożone: obiekty, dwie kolumny/podział sekcji, graficzna mgła/szczegółowość, współrzędne na ikonach, kadrowanie/arkusze i pełny selektor, optymalizacja dużych map, opisy/formatowanie oraz znany przeskok prawego nagłówka. Dalsze wymagania są w sekcjach 24–25 specyfikacji; nie oznaczamy ich jako naprawionych.

## Checklista użytkownika

1. Na równinie przygotuj dwie plamy mgły rozdzielone odkrytymi polami i teren o tej samej barwie, ale innej nazwie. Kliknięcie odkrywa tylko właściwą plamę; drag przechodzi przez kolejne właściwe regiony, z jednym Undo/Redo i stałym trybem. Powtórz krótki drag Wiadra terenów/ikon.
2. Zwiń/rozwiń panel i obserwuj ruch **w trakcie**, nie tylko po kliknięciu. Sprawdź Dopasuj i trafianie. Kółkiem/strzałkami przejdź od początku długiej palety do końca i z powrotem, szczególnie przy minimalnym oknie.
3. W Domyślnym sprawdź blokady edycji/F2/Delete i dostępne malowanie. Zapisz jako nowy, wybierz kopię i edytuj ją. Punkt ma nieaktywny Rozmiar; wybór figury przywraca pole. Ramka materiału ma być tylko jedna.
4. Na własnej ikonie zmień nazwę/grafikę bez checkboxa, potem z nim. Sprawdź zachowanie dawnych wystąpień, mapy, tekstu i mgły; osobna ikona z tymi samymi bajtami nie powinna zostać podmieniona. Plik ma czytelną nazwę i jeden aktywny egzemplarz.
5. Sprawdź wspólne Undo/Redo importu, edycji i usunięcia pliku ikony. Przed Nowa/Otwórz/zamknięciem pamiętaj o sesyjności kopii usunięcia. Przy zajętej lub zewnętrznie zmienionej ścieżce Undo ma odmówić bez nadpisania; po rozwiązaniu konfliktu można ponowić.
6. Szybko przeciągnij pędzlem, wyjdź poza płótno i wróć: odcinek w obrębie płótna powinien być ciągły, po powrocie nie powstaje łącznik przez mapę. Zapisz i otwórz mapę w czystej kopii aplikacji, sprawdzając osadzone grafiki i nazwy.

Po potwierdzeniu zakończenia procesu usunięto własną mapę, EXE i Library z ui-031-5d80850ae45f4878a28d8d43cd7810cf; pozostawiono krótki results.log. Reguły ignorowania sprawdzono przez git check-ignore, a git ls-files nie wykazał Library/_work/bin. Końcowy commit i stan Git są podane po zatwierdzeniu w podsumowaniu rozmowy; ten raport należy do tego commita. Źródła/skrypty i główny EXE są śledzone, Library oraz tests/_work i główny bin ignorowane. Bez push/tagu/Release'a.