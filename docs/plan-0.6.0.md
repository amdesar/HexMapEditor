# Plan 0.6.0

Baza sprawdzona: czysty master `1d6cc6a`, lokalny origin/master zgodny. Przeczytano raport0.5.4 i wymagania bieżącego zlecenia. Zachowujemy źródła, prywatne dane, ochronę gestów i LibraryFiles; bez pełnych kampanii, publikacji i sprzątania.

## Decyzje modelu

- Rysunki: ograniczone listy wektorowych polilinii w świecie, kolor i grubość. Gumka rozcina segmenty geometrycznie wzdłuż dysku/kapsuły ruchu; zapisujemy tylko pozostałe fragmenty, bez bitmapy świata lub pozornych kresek kolorem tła. Snapshot rysunków przed/po jednym geście w wspólnej historii300.
- Wskaźnik: ekranowe próbki z czasem, maksymalna liczba i wygasanie; timer tylko na czas aktywnego wskaźnika/smugi. Żadnego zapisu ani dirty.
- Ciemność: osobny bool i FillVariant każdego heksa, osobna pula dokumentu; wykorzystanie wspólnych odcisków, regionów i historii warstw. Nie kopiujemy Fog jako substytutu.
- Światło: bazowe LightEnabled/LightRadius instancji i wzorca, opcjonalne nadpisania grup. Wyliczony cache sumy zasięgów, niezależny od widoczności Obiektów/Fog i od stanów. Środek wskazuje jeden heks nieskończonej geometrii; iteracja zasięgu ograniczona do rzeczywistej mapy. Efektywna ciemność = baza i brak światła. Wynik nie jest autorytatywnym zapisem ani oddzielnym krokiem historii.
- Maski: wspólna geometria sumy Fog i efektywnej ciemności (według flag widoku), bez podwójnego liczenia. Oddzielne jawne procenty Fog/Ciemności w liście, bez wycieku maskowanych danych.
- Nowy format mapy będzie konieczny dla rysunków/ciemności/świateł; planowany9 z jawnymi polami i walidacją. Presety nadal3, grafiki2; wzorce obiektów dostają pola światła z bezpiecznymi ustawieniami początkowymi. Bez migracji map i usuwania plików użytkownika.

## Etapy

1. Poprawki odbiorcze: dokładne skróty, ×, wyrównana mozaika bez obrysów, stała geometria podglądu tła, bezpośrednia ciemna biblioteka grafik.
2. Wektorowy rysik/gumka, wskaźnik, ustawienia przybornika i historia/zapis/limity.
3. Ciemność i źródła światła instancji/wzorców/grup, dynamiczny cache, gesty/pula/zasoby.
4. Łączne maskowanie, lista, flagi widoku i FitEverything według widoczności.
5. Kilka jawnych Case, reprezentatywny round-trip, zwykły finalny EXE, dokumentacja/raport/wersja i lokalne commity.

## Punkt wznowienia — integracja modelu i narzędzi

Etapy 1–4 zaimplementowane w źródłach, wymagają celowanej weryfikacji i ewentualnych poprawek. Pierwsza robocza kompilacja `tests/_work/Development060.exe` przeszła (przed ostatnim drobnym uzupełnieniem optymalizacji gumki i opisów masek). Główny EXE nadal 0.5.4, wersji nie podniesiono. Nie ma jeszcze zaliczonych testów 0.6.0 ani lokalnego commita.

Dodano InkDrawing/InkTools/InkSettings i Darkness; mapa9, presety3 bez zmian. Gumka rozcina kapsułą segmenty, historię stanowią niezmienne fragmenty (bez kopii obrazów); światło liczone w cache po zmianie obiektów/geometrii, nie w timerze. Podłączono bibliotekę grafik w trybie zarządzania, menu siedmiu warstw, światło instancji/wzorca/grupy i odłączanie z siedmioma kategoriami.

Pozostało: testy jawnych Case (odbiór, ink, pointer, darkness, visibility/save), przegląd konkretnych błędów ujawnionych próbami, finalna wersja/build, zwykła kopia EXE, dokumentacja/raport/commit. Zweryfikować maskowanie panelu grupy oraz zachowanie końca gestu i cache; pomiar płynności tylko rzeczywistym gestem, nie bounds.

Ostatnia operacja: lokalne edycje źródeł zakończyły się sukcesem; brak niepewnej operacji dyskowej. Własnych okien/dialogów/procesów 0.6.0 nie uruchomiono. Skrypty pomocniczych zamian wyłącznie w tests/_work; nie są źródłami testów. Brak pełnych kampanii i sprzątania.

### Etap weryfikacji metod
Przeszło sześć jawnych Case: acceptance (po poprawieniu wyłącznie typu i sygnatury helpera testu), ink, pointer, darkness, visibility, gestures. Każdy log w odpowiadającym `tests/_work/patch-060-CASE-GUID/results.log`. Ostatni gestures obejmuje uzupełnienia cache rysika, maskowanego panelu grupy, gest ruchu światła i kończenie wskaźnika. Osłona runnera bez Case raz odrzuciła wywołanie bez testów. Nie uruchomiono dawnych kampanii.

Kontrola okien Sky i procesów: brak uruchomionej aplikacji HexMapEditor; okien użytkownika nie dotykano. Teraz ustawiono wersję źródeł0.6.0, następna operacja: build głównego EXE i odizolowana identyczna kopia UI. Pozostały rzeczywiste próby EXE, dokumentacja/raport oraz commit. Żaden wynik UI nie jest jeszcze zaliczony.

### Wznowienie 2026-11-12 — stan zweryfikowany
Główny EXE oraz własna kopia UI istnieją, FileVersion0.6.0.0, SHA-256 `366A89AA311CC7A23AE0993A252B23F0433BF500AD571EAA923FA743150BEA7B`. Ostatni build i kopiowanie zakończyły się sukcesem. Kod ostatnio zmieniono wyłącznie po zauważeniu przyciętego napisu ustawień wskaźnika (wyrównano szerokości i podpis; nowy wygląd sprawdzono już w finalnej kopii).

Przed tą korektą napisu w zwykłym EXE: biblioteka na Domyślnym; ruch lampy, powrót bazowej ciemności i Ctrl+Z; cztery kombinacje flag Fog/Ciemność; rysik nad maskami; po zgodzie użytkownika częściowa gumka i natychmiastowe Undo. Po korekcie: ponowny odczyt zapisanej własnej mapy, poprawione napisy, PPM wskaźnika bez dirty (krótki zanikający punkt). API drag nie pozwala wskazać PPM, więc ciągłego wskaźnika/smugi nie oceniono wizualnie; test pointer sprawdził jego model i timer.

Własne okno już NIE istnieje (sprawdzono procesy i Sky), nie ma otwartego dialogu. Mapa `tests/_work/patch-060-fixture-153d28f2917c4b158947eed60d07eef8/Próba 0.6.0.hexmap` zachowała1 kreskę,1 lampę,35 pól bazowej ciemności i2 Fog. Druga kreska poza siatką nie została zapisana. Ostatnią akcją UI było tylko otwarcie menu Dopasuj; wynik Dopasuj wszystko nie został sprawdzony i nie jest zaliczony.

Dalej: odtworzyć wyłącznie brakującą kreskę jako warunek krótkiej próby Dopasuj wszystko / flagi Rysik i Obiekty, domknąć własne okno z zapisem własnych danych, uzupełnić README/specyfikację/raport i commit. Nie ponawiać sześciu zaliczonych Case ani dawnej diagnostyki. Pozostałe ograniczenia UI opisać dokładnie. Zachować wszystkie zmiany; bez nowych buildów bez zmiany źródeł.

### Zakończenie prób UI i implementacji
Próba Dopasuj wszystko dokończona: kreska poza siatką rozszerza kadr; po wyłączeniu Rysika następne dopasowanie pomija kreski. Lampa poza mapą obejmowana całym prostokątem; po ukryciu Obiektów pomijana w kadrze, ale nadal oświetla G5. Polecenia menu wykonane End/Enter; sam klik zamykający menu nie był zaliczany. Własną mapę zapisano, własne okno zamknięto i sprawdzono brak okna. Bez niepewnych operacji lub oczekujących zgód. Brak powtarzania testów metod po wznowieniu.

Kod, sześć Case i końcowy EXE zatwierdzone w `3e3fb2c`. Hash głównego i zwykłej kopii nadal366A89AA311CC7A23AE0993A252B23F0433BF500AD571EAA923FA743150BEA7B. Wersja0.6.0.0, format mapy9. README, specyfikacja i raport0.6.0 opisują wykonany zakres oraz ograniczenia (zwłaszcza brak ciągłego PPM drag/klatek płynności w Sky). Pozostało wyłącznie zatwierdzenie dokumentacji i odczyt końcowego Git; bez kolejnego builda/testów lub sprzątania. Ostatnia operacja: zapis dokumentacji, zakończony sukcesem.

### Końcowy punkt wznowienia — 2026-11-12
Cały zakres funkcji ukończony. Commit główny3e3fb2c oraz90f61ed (podgląd średnicy rysika). Dodano wyłącznie Case ink-preview: PASS, bez ponawiania wcześniejszych sześciu. Końcowy EXE przebudowany po tej zmianie, FileVersion0.6.0.0; zwykła kopia identyczna SHA-256 `6E08AF16D789C9E4CD7EADF5470A596C3F0B2A119F5E55220FDD73AC218559EB`. W niej sprawdzono obrys20 jednostek przy146% (około29px), bez zmiany danych/gwiazdki. Ostatnia próba UI zamknięta, lista okien potwierdza brak własnej aplikacji/dialogu. Żadnych niepewnych operacji, zgód ani uruchomionych procesów budowy/testów.

README/specyfikacja/raport uzupełnione, wyraźnie rozdzielono metody, zwykły EXE i ograniczenia (ciągły PPM drag, brak oceny płynności klatka po klatce, przenośność świadomie odłożona). Nie ma pozostałej implementacji lub obowiązkowej próby w toku. Do zamknięcia: zatwierdzić dokumentację i sprawdzić czysty Git; potem nie uruchamiać kolejnej kampanii, buildu ani sprzątania. Ostatni zapis dokumentacji zakończył się sukcesem.
