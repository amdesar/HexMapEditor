# Plan 0.5.4

Baza: czysty master `0e8920f`, zgodny z lokalnym origin/master. Przeczytano raport 0.5.3 i sekcję 33 specyfikacji. Bez publikacji, migracji prywatnych danych i pełnych zestawów.

1. Inwentaryzacja tests/_work, zależności generatorów i procesów; ostrożne usunięcie tylko odtwarzalnych artefaktów, zachowanie źródeł/logów/niejasnych plików.
2. Puste pola niezależne od neutralnego presetu; test nowych pól/regionów/zapisu i historii.
3. Kwadratowe kafelki i czerwone ×; lokalna historia 100 operacji terenu/mgły, odrębna od importów bibliotecznych i historii mapy.
4. Stabilna mozaika; jeden podgląd kadru z jednorazową synchronizacją nowego importu i niezależną ponowną edycją.
5. Symetria tła i lokalna poprawa nasycenia, alpha obrazu bez wyłączenia logicznego Fog.
6. Kilka celowanych Case, zwykły finalny EXE, dokumentacja/wersja/commit.

## Postęp / punkt wznowienia

Inwentaryzacja: tests/_work 164,15 MiB, reszta tests to śledzone źródła/runnery/README. Runnerzy generują dane we własnych katalogach GUID, nie czytają dawnych przebiegów jako fixtures. Brak junction/symlink w _work. Odczyt procesów CIM po wymaganej eskalacji nie wykazał HexMapEditor/testów/csc. Nie jest to samo w sobie dowód braku każdego okna; przed testami UI trzeba wybrać rzeczywiste okno przez Computer Use. Nie zamykano żadnego okna.

Ustalenia kodu: CellData domyślnie używa ID neutralnego presetu; brak niezależnego stanu niepomalowanego. Edytor puli ma 96×80 i plus60×80, bez lokalnej historii. Kadrowanie dwa panele z trwałym Linked. Fog Paint jawnie rysuje kolor przed teksturą. Tło ma nierówne powierzchnie450×320 i290×310; nasycenie ponawia cover i blur choć zmienia tylko macierz koloru.

## Etapy 1–4 — implementacja

Sprzątanie zakończone: 66 rozpoznanych plików usunięto (161013264 bajtów). _work przed172127873B, po11123542B plus mały manifest. Usunięto odtwarzalne EXE Patch042/043/050/051/052Tests oraz EXE/obrazy/mapy/JSON zakończonych patch-053 i własnej ui-053. Zachowano logi, wszystkie śledzone testy/runnery oraz niejednoznaczne migration-022, backupy i stare ręczne próbki. Brak reparse point; każdy usunięty plik zweryfikowano jako leżący pod _work i usunięto LiteralPath bez rekursywnego delete. Manifest i rozmiary: tests/_work/cleanup-054-f8ed7966e8d34f7dbf3848a13dda8317. Nie ponawiać porządków tych przebiegów.

Zaimplementowano (jeszcze bez testów): reserved ID map-unpainted dla nowych CellData i resize, niezależne od default-neutral; format8 już zapisuje wymagane ID, brak zgadywania dawnych pól. Kwadraty88 i czerwone ×, lokalne snapshoty100 z dzieleniem źródła, skróty/UndoRedo. Pojedynczy kadr608×408 z wyborem orientacji; newImport synchronizuje tylko do pierwszej zmiany, re-edit niezależny, wynikLinkedfalse. Mozaika deterministyczna,2rzędy, maks12 różnych reprezentantów, bez Random mapy. Rendering alpha mgły przeniesiony ponad rzeczywiste warstwy; logiczny hit-test/maskowanie i obrysy pozostają oparte na Fog.

Pozostało: symetria tła i pomiar/poprawa nasycenia; doprecyzowanie walidacji pustego stanu i asercji alpha; nowe celowane przypadki, zwykły finalny EXE, dokumentacja/wersja/commit. Ostatnia niepewna operacja: brak; powyższe zmiany zapisane, nie budowano jeszcze ich. Własnych procesów/dialogów brak. Główny EXE nadal0.5.3.

## Implementacja i celowane metody — zakończone

Tło ma równe powierzchnie365×310. W gestach suwaki korzystają z pokrytego obrazu160px i macierzy koloru/małego blur, po MouseUp/KeyUp pełny dokładny renderer podglądu310px. Jedna oczekująca invalidacja najnowszego stanu, bez timera/wątku. Krótki pomiar20 zmian nasycenia:598,29ms przed (patch-054-background-perf-21323f66c994421fb92352d83fce5936),25,74ms po (aeeab957b9b74ae7967913e05fce81f2). To metody kontrolek, nie FPS.

PASS empty 9d8887d591bf44bd9702f7a93ebc9c0e (wcześniejszy harness nie inicjował biblioteki w niewidocznym MainWindow i nie znalazł Domyślnego; poprawiono tylko harness). PASS editor ff3cc919d5a246ad8b1acb10ba2d1766, crop b0f34a37f0aa415c802fd4b08fb4797b. PASS presentation 919c6c148e2a4c2398ac67d0a6f33684 (pierwsza próba zmieniała kolejność puli bez odświeżenia aliasu pierwszego wariantu; poprawiono tylko setup testu). Wszystkie pod tests/_work/patch-054-<Case>-<ID>.

Zaktualizowano asercję alpha w historycznym Patch051Tests i wykonano wyłącznie Case fog: PASS patch-051-fog-3cbef34d9a614a91b915b4df299fd61c. Nie uruchomiono dawnej kampanii. Przezroczysta mgła pokazuje warstwę pod nią, ale logiczny Fog nadal maskuje szczegóły/listę/hit-test; ochrona odkrywcy w tym Case przeszła. Render obejmuje także obiekty pod alpha, obrysy nadal respektują logiczny Fog.

Pozostało: raz wykonać rozszerzony editor (dodana dokładna asercja Undo usunięcia grafiki z oboma kadrami), osłona runnera bezCase, końcowe oznaczenie/build0.5.4 i zwykła byte-identical kopia EXE, dokumentacja i własny commit. Fixture054.hexmap i Stripes.png z PASS presentation zachować do próby UI. Nie powtarzać innych zaliczonych Case bez zmiany. Brak własnych procesów/dialogów i niepewnych operacji.

## Końcowy EXE i UI

PASS rozszerzonego editor937c062928db4af6bc20f6fbc2c14839; runner bezCase odrzucił uruchomienie przed buildem. Zbudowano0.5.4.0. Pierwsza próba UI ujawniła słabą czytelność glifów ×/strzałek. Poprawiono lokalnie: Panel jako rodzic sąsiednich choose/remove, rysowany krzyżyk i lokalne ikony historii. Zależny editor ponowiony po tej zmianie: PASS a18d3da3bb4c47fc874a69b87cf4f368. Innych zaliczonych przypadków nie powtarzano.

Po korekcie przebudowano root i podmieniono zamkniętą własną kopię: SHA256 F5FDAC5CEE18BC495E77B4469E0F665EE9516B909615E05DE10B1DDF867250F5. UI tests/_work/ui-054-8be96cf91c704c8c93dbdcb9d6ae2562.
W finalnej kopii: kwadraty/plus/× czytelne, scroll udostępnia dolne kafelki; mozaika stabilna. Ponowna edycja grafiki: zoom, drag i obrót Pointy92°, przełączenie Flat zachowało0°; zatwierdzenie wariantu, Ctrl+Z przywrócił poziome pasy, Ctrl+Shift+Z przywrócił92°. Anuluj puli pozostawił mapę bez gwiazdki i bez globalnej historii. Tło ma równą powierzchnię obu paneli; drag nasycenia do16% i reset100%, końcowy podgląd poprawny; anulowano. Narzędzie pokazuje stan po drag, nie nagranie klatek, więc nie deklarować ciągłej płynności/FPS.

Wysłano Alt+F4 własnemu oknu bez gwiazdki. Ostatnia niepewna operacja: trzeba potwierdzić listą okien, że rzeczywiście się zakończyło, przed sprzątaniem bieżących artefaktów. Pozostało: dokumentacja/raport, wąski przegląd diff, końcowy rozmiar tests (usunąć tylko własne odtwarzalne dane bieżących prób po zamknięciu), commit i status. Nie powtarzać builda/zaliczonych prób, o ile nie zmienimy kodu.

## Ostatnia korekta i stan do finalizacji

Potwierdzono zamknięcie poprzedniego własnego okna. Drobna korekta no-op: ponowne kadrowanie starszej definicji z Linked=true i OK bez zmiany transformacji zachowuje oryginalne ID/metadane, żeby nie tworzyć niewidocznego kroku. Stary Linked nadal nie łączy ponownej edycji. Nowe importy zapisują Linked=false. Zależny crop ponowiony: PASS7df0fc614ae848c1b6cce72269531005; innych zaliczonych metod nie powtarzano.

Finalny root i kopia SHA256 D0AD9527F96E5E54451A13031D4FF5A69B12C18A78DEDF3700BF5E80A785B8BE (FileVersion0.5.4.0). W tej zwykłej kopii sprawdzono zmieniony no-op: otwarcie grafiki/kadru, OK bez edycji; UIA obie lokalne historie disabled; OK puli bez gwiazdki i bez globalnego Undo. Ruch/obrót/redo i tło sprawdzono wcześniej na poprzednim buildzie F5FD…, a ostatnia korekta dotyczy tylko bezczynnego OK — nie powtarzać niezależnych prób. Własne okno zamknięto; sprawdzić tylko niepewny wynik ostatniej listy okien, jeżeli narzędzie nie zwróciło wyniku. Po potwierdzeniu można sprzątnąć wyłącznie własne nowe patch-054/ui-054 oraz nowy patch-051-fog, pozostawiając logi i manifest.

Pozostało wyłącznie dokumentacja/raport, końcowy pomiar po sprzątaniu, review staged i lokalne commity/status. Żadnych nowych funkcji ani pełnych kampanii. Niepewnych operacji dyskowych brak.

## Finalizacja

Potwierdzono pustą listę własnych okien po ostatnim Alt+F4. Drugie sprzątanie usunęło37 własnych odtwarzalnych plików9466149B z bieżących prób; pozostawiono wyniki.log i manifest. Końcowe _work11130335B (10,61MiB); tests11487924B (10,96MiB), przed tests172466483B (164,48MiB). Wszystkie śledzone źródła/runnery zachowane, niejednoznaczne stare dane nietknięte.

README/specyfikacja/testsREADME i raport0.5.4 ukończone, z checklistą dwóch grup i jawnymi ograniczeniami UI/FPS. Implementacja kompletna, wymagane celowane metody i reprezentatywne UI zakończone. Pozostaje tylko przegląd staged, lokalne commity i potwierdzenie czystego Git. Brak własnych procesów/dialogów, oczekujących zgód i niepewnych operacji. Bez potrzeby kolejnego builda lub powtarzania prób. Końcowy hash D0AD… i FileVersion0.5.4.0 pozostają aktualne. Po commitach zadanie zakończone; hash końcowy podać w odpowiedzi.

Kod/EXE/testy zatwierdzone lokalnie w `f62314633c8a725d14a178be2b51cb34dcd18fa6`. Review staged i git diff --check przeszły; brak Library/_work w śledzonych plikach. Pozostał commit pięciu plików dokumentacji i sprawdzenie statusu. Nie wznowić ponownie zakończonych implementacji, sprzątania, testów i builda. Bez oczekującego okna lub operacji dyskowej.
