# Plan 0.6.1

Baza: czysty master `144d5a2`, ukończona0.6.0. Sprawdzono Git/origin; zachowujemy prywatne dane i istniejącą historię. Bez pełnych kampanii, push/tagu/Release'a i sprzątania. Kolejka importu wyłącznie do backlogu.

## Etapy i decyzje

1. Odtworzenie grupowania trójki realnymi metodami tworzenia grupy, lokalna naprawa wspólnej przyczyny. Widok Obiekty świadomie wyłącza udział źródeł. Nazwy użytkowe: Ograniczenie widoczności / Pole widzenia / Promień widzenia / Widoczność. Wewnętrzne Light/Darkness pozostają.
2. Wspólny ekranowy rozmiar rysika/gumki5, zakres0,5–200 logicznych px. Przeliczenie na świat raz na gest z zoomu/DPI; zapis nadal wektorowy. Zakończenie lewego gestu przed zoomem/pan. Uchwyty poziome przy wspólnych ustawieniach rozmiaru, bez timera/przyspieszania.
3. Wskaźnik: jednokolorowy dysk o dotychczasowym zewnętrznym rozmiarze, zwężana kometa750ms, kursor ukryty tylko na płótnie podczas PPM, przywracanie wszystkimi ścieżkami zakończenia. Próbki wyłącznie chwilowe.
4. Biblioteka teł: oddzielne logiczne ID, czytelne pliki i kolejność, niezmienne pełne źródła deduplikowane w magazynie Sources, ustawienia kadru/efektów jako parametry. Wybór nie zmienia mapy przed zatwierdzeniem. Zapis nowe/nadpisz/tylko mapa/wróć; LibraryFiles i jedna transakcja pliku z dokumentem, bez half-success. Mapa nadal ma własną migawkę. MMB przesuwa cały kadr.
5. Lokalny UI: kolejność narzędzi, odróżnialne ikony masek, cztery kolumny grafik, dolne akcje paneli, rozsądna lista. Pomiar ścieżki otwarcia panelu; bez globalnego WS_EX_COMPOSITED/WPF redesignu. Pełne pokrycie mozaiki z zapasem i stabilnym udziałem pozycji.
6. Kilka jawnych Case, finalny zwykły EXE i dokumentacja/raport. Wersja0.6.1 dopiero po ukończeniu zakresu. Format mapy9 powinien wystarczyć (zmienia się użycie istniejącej grubości i chwilowych flag, biblioteka teł osobna); poszerzyć konkretną walidację własnych światowych grubości, nie limity punktów.

## Punkt wznowienia

Etap 1–3 rozpoczęty, całość NIEgotowa. Główny EXE nadal 0.6.0; nie opublikowano częściowej 0.6.1.

W kodzie roboczym: Widok Obiekty wyłącza źródła; invalidacja cache po zakończeniu transakcji; wspólny ekranowy rozmiar 5 (0,5–200), przeliczenie na świat na początku gestu; uchwyty poziome; kometa 750 ms i lokalne ukrywanie kursora. Pozostało dokończyć weryfikację tych ścieżek, nazwy/UI, cała biblioteka teł i MMB, backlog, finalny build/próby/dokumentacja.

Wykonane: `groups` PASS w `tests/_work/patch-061-groups-0938c65ecac044aeb4a23f6f406b1f0f/results.log`. Wcześniejsze dwie próby bazowe potwierdziły grupowanie 14→14 i odrzucały dopiero nową regułę Widoku. Zgłoszony błąd grupowania jeszcze NIEodtworzony; brak dowodu przyczyny, nie nazywać naprawionym. Użytkownik doprecyzował: Grupuj wybrane poza mapą, potem ruch nad mapę. Celowany `outside` (realne CreateObjectGroup/BeginObjectMove/ObjectMouseMove/FinishObjectGesture, cache pusty przed ruchem) PASS, 0→14, Undo 0, Redo14: `patch-061-outside-f30da8428ac44d0a97f49973756726ef`. Nie ponawiać bez wpływającej zmiany; konieczna próba okna i dalsza ocena.

Własne okno bazowe 0.6.0: kopia w `tests/_work/patch-061-fixture-f7858a4b14af4ce0a0b11acb3e5be41b`, mapa Trójka061.hexmap, grupa utworzona w oknie, niezapisana. Ostatni stan: menu Widok mogło pozostać otwarte; narzędzie zgłosiło ingerencję użytkownika, ostatni get_window_state miał ucięty wynik. Przed kolejnym wejściem obserwować stan; nie zakładać wyniku/zamykać cudzych okien. Brak niepewnej operacji dyskowej. Testowe procesy zakończyły się. Brak commitów 0.6.1.

### Aktualizacja po etapach 2–4

Zaimplementowano roboczą bibliotekę teł `BackgroundLibrary`/`BackgroundPicker` i decyzję zapisu po Zatwierdź; zapis parametrów, źródła, ID/kolejność, rename/delete, LibraryFiles, wspólna transakcja z mapą. MMB przesuwa cały kadr. Brak finalnej próby okna, weryfikacja dodatkowych decyzji/no-op jeszcze do dokończenia. Kompilacja do `tests/_work/compile-061/HexMapEditor.exe` PASS (nadal oznaczenia0.6.0, nie finalny artefakt).

`backgrounds` PASS: `patch-061-backgrounds-7628618562234447a31ae874fe912439/results.log` — pełne źródło i wszystkie parametry, rename bez dirty, Undo/Redo pliku, usunięcie bez utraty mapy, konflikt przy Undo i ponowienie, zapis/odczyt bez pliku wejściowego, kontrolka MMB bez resize.
`size-pointer` PASS: `patch-061-size-pointer-1fd699e3186949bfbcbff0309e34d26a/results.log` — rozmiary przy1,46/0,73, grubość świata20000 round-trip, wspólne ustawienie/limit200, częściowe wymazanie dokładneUndo/Redo, pointer bez dokumentu/historii, kursor lokalny i wygaśnięcie750ms, uchwyt zależny od drogi. Nie była to próba zwykłego EXE ani wizualna ocena ciągłej komety.

Przybornik przestawiony3×4, nazwy użytkowe zaktualizowane (wewnętrzne pola Light/Darkness zachowane), przyciski usuwania na końcu tabel, mozaika rysowana z zapasem. Rozpoczęto rozmiary4kolumn selektora i szerszej listy, ikony masek. Jeszcze wymagają testu prezentacji i pomiaru panelu. Backlog kolejki zapisany w osobnym dokumencie; nic nie wdrożono.

**Wynik dodatkowej próby bazowego EXE:** rzeczywisty wybór prostokątem trzech obiektów poza mapą → Grupuj wybrane → Rączka → ruch nad mapę: oba źródła działają, środkowy członek??? zgodnie z maską. Nie odtworzono błędu. Użytkownik podał ostatnią konfigurację15×10,PointyTop, pula Ciemności kolor+grafika, ruch Kursorem i Rączką; jeśli nadal brak reprodukcji, wyraźnie zgodził się pominąć ten błąd i wrócić do niego po własnej próbie0.6.1. Pozostaje JEDNA próba tej konfiguracji, bez obiecywania naprawy nieodtworzonego błędu.

Własny bazowy EXE ponownie otwarty: `tests/_work/patch-061-fixture-f7858a4b14af4ce0a0b11acb3e5be41b/HexMapEditor.exe`, oknoID24054788. Wczytano `tests/_work/patch-061-fixture-outside-47bc8822dc654df2b5391cef297d1fa9/Trójka061.hexmap`; grupa utworzona poza mapą i przesunięta. Ostatni klik Zapisz zgłosił ingerencję użytkownika, po ponownej obserwacji nadal gwiazdka. Zapisu NIE uznano za udany. Nie powtarzać go bez obserwacji. Otwarty panel grupy, Rączka, menu zamknięte. Żadnej operacji usuwania/zamknięcia nie wykonano.

### Etap5 / przed końcową próbą EXE

Zakres kodu wprowadzony. Jeszcze NIEuznano wersji za gotową: pozostają końcowy build, zwykła kopia UI, raport/spec/README oraz końcowy Git. Zmieniono oznaczenia źródła na0.6.1 dla budowanego kandydata.

- `presentation` PASS: `patch-061-presentation-853059fa575d48ce8904b30a8e7392a5/results.log`: stabilne wyrównane przypisania mozaiki, obrazy obu orientacji, kolejność narzędzi/dolne usuwanie; kontrolka biblioteki ma cztery kolumny (pozycje0/3ten samTop,4następny). Nie był to finalny EXE. Pierwszy build testu nie przeszedł przez kolizję nazwy Screen z metodą okna; poprawiono pełną nazwą WinForms.Screen, dopiero potem PASS.
- Profilowanie panelu: `patch-061-panel-profile-24fe47c633bd461e95b620367d0af408/results.log`: UpdateCaption0,08ms, wartości/layout16,75ms, PositionOverlays0,20ms, pokazanie poddrzewa7,54ms, Show right147,91ms, PaintTree8,55ms, już otwarty UpdateUI5,60ms. Główny koszt natywnego pokazania gałęzi kompozycji, nie rekonstrukcji WPF/obliczania mapy. Nie wycofano zaakceptowanej ochrony odrysowania dla oszczędzenia~150ms; opisać ograniczenie zamiast obiecywać usunięcie pauzy. Brak nowego globalnego WS_EX_COMPOSITED/timera/DoEvents w aplikacji.
- Ostatnia konfiguracja użytkownika15×10PointyTop,pula maski kolor+grafika, grupowanie poza mapą i ruch Kursorem/Rączką: metoda`acceptance` PASS, oba źródła14pól, błąd NIEodtworzony: `patch-061-acceptance-6c089b539f1a42eca5ff334683389c7d/results.log`. Użytkownik wyraźnie zgodził się pominąć nieodtworzony błąd i ponownie sprawdzić po0.6.1; nie deklarować fikcyjnej przyczyny/naprawy. Zmiana invalidacji cache po transakcji to zachowanie spójności, nie dowiedziona przyczyna.
- `background-controls` PASS: `patch-061-background-controls-f2f7a0c2fab54948a4170cb40261e264/results.log`: odczyt ścisłych metadanych, miniatury selektora/życie Bitmap, odtworzenie suwaków, zastosowanie/no-op bez duplikatów. Poprawiono własność miniatur (zachowane do końca selektora), ukryto usuwanie z mapy w edytorze samego wpisu bibliotecznego.
- Osłona runnera bez Case sprawdzona RAZ: błąd przed buildem/uruchomieniem; nie ponawiać.

Własne okno bazowe nie jest już obecne w aktualnej liście Sky ani Get-Process. Nie zamykano go automatycznie. Ostatniej próby zapisu nie powtarzano. Brak własnych procesów/dialogów teraz. Następna operacja: Build.ps1 głównego EXE, potem jego identyczna bajtowo kopia w nowym tests/_work. Prywatna Library/mapy nie były otwierane ani modyfikowane.

### Aktualny punkt — po wznowieniu i próbach końcowych

Implementacja zakończona z wyraźnie uzgodnionym odłożeniem nieodtworzonego błędu grup. Root EXE0.6.1.0 i kopia ui-061-bbd1b2e0099b4bd1ad7dccff6a404aaa mają SHA74C3BEBFD69CA687963B55B56727911E5D3522C58BE5B63FEA41990DC877760A. Build i kopiowanie zakończyły się sukcesem; nie ponawiać bez zmiany kodu.

W trakcie zwykłego EXE zauważono zbyt wysoki dolny przycisk: AutoSize indeksów wierszy + GrowAndShrink tabeli/przycisków dały30px; `panel-layout` PASS14fa4c25acb848629a55cd69f1fdbef8 po wcześniejszymFAIL220px. Pomiar po zmianie (`panel-profile`42f64f6237b942e9a5e6c5c1fc58c3e2) Show right139,23ms — nie deklarować usunięcia pauzy.

Po wznowieniu domknięto jedynie brakujące LostFocus płótna dla aktywnego PPM: `pointer-focus` PASSb269a482f2d44a56828146e3902d374c. Potem ostatni build/hash powyżej. `background-overwrite` PASS1d90e74753044aa38fc33412e62919dd: jeden plik/ID/źródło, dokładne Undo/Redo i Anuluj.

Rzeczywiste UI: na kandydacie362AC69E kreski40przy146%/100% (świat27,48483/40), importLPMkadru/suwaki49/156/5/zapisnowego, no-op, grupa15×10z mieszaną maską + ruch z zewnątrz i OFF/ONObiektów; działa. Po poprawce wierszy kopia3BD56CCA odczytała zapisaną grupę i pokazała poprawne dolne przyciski. Na finalnej74C3BEBF po restarcie tło z biblioteki odtworzyło kadr49/156/5; Anuluj bez mapy/gwiazdki, nagłówki pustej listy mieszczą się bez poziomego scrolla. Pełne rozróżnienie kandydatów i metod jest w raporcie. Nie powtarzać zaliczonych scenariuszy tylko z powodu wznowienia.

Własne okno finalne zamknięte, potwierdzono brak w liście Sky. Brak własnych procesów/dialogów i oczekujących zgód. Ostatnia niepewna operacja: brak. Prywatnych danych nie zmieniono; nie sprzątano źródeł testów ani bibliotek. Pozostało wyłącznie sprawdzenie dokumentacji/diff i lokalny commit, dopisanie identyfikatora/końcowego Git. Raport/README/spec zaktualizowane, backlog kolejki opisany, bez implementacji skanowania.

### Zamknięcie etapu

Commit kodu/EXE/testów: `0bcbee21af27931bd9e9a9c466997ded5936ed22`. Dokumentacja końcowa w osobnym commicie, po nim sprawdzić czysty status. Zakres0.6.1 zamknięty z opisanymi ograniczeniami i wyraźnie uzgodnionym pominięciem nieodtworzonego błędu grup. Brak implementacji kolejki folderów; tylko backlog.

Po ostatnim wznowieniu NIE powtarzano zaliczonych przypadków. Domknięto brakujące nadpisanie/anulowanie tła (`background-overwrite` PASS) i brakującą obsługę LostFocus (`pointer-focus` PASS). Finalny EXE74C3BEBF… po restarcie odczytał bibliotekę z dysku do nowej mapy: pełny kadr i49/156/5; Anuluj bez dirty, potem krótka kontrola szerokości/nagłówków pustej listy. Własne okno zamknięte, brak potwierdzony listą Sky. Kometa PPM i gest MMB w zwykłym EXE NIEzaliczone wizualnie — ograniczenie API drag; metody/kontrolki opisano osobno. Brak wstrzymanych zgód i procesów. Końcowe sprawdzenie hash/wersji/diff bez ponownego builda lub kampanii.

Ostatnia operacja niepewna: brak. Następne wznowienie nie ma zaczynać audytu ani powtarzać testów: ewentualne dalsze polecenie użytkownika otwiera nowy zakres. Raport opisuje pauzę natywnego Show (~140ms), której nie przedstawiamy jako usuniętej.
