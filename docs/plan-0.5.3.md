# Plan 0.5.3

Baza sprawdzona: ea067f0, master zgodny z lokalnym origin/master, czyste repozytorium. Bez prywatnej Library, migracji, pełnych testów i publikacji. Poniższe punkty wznowienia są chronologicznym zapisem; obowiązuje ostatni.

1. Odtworzenie regionów historycznych wersji i lokalne pomiary przed zmianą.
2. Wersje powierzchni + zachowanie ID/rozmieszczenia wariantów terenu i mgły, delty historii i round-trip.
3. Lokalne ograniczenie pracy podglądów/akceptacji; brak gestu z dialogu; tekst kafelków.
4. Uchwyty/proporcje/efekty tła, kolejność Widok; wspólne spójne limity i kontrola pomijanych grafik.
5. Celowane regresje/pomiary po, finalny build i reprezentatywne próby zwykłego EXE, dokumentacja/commit.

## Punkt wznowienia
Przeczytano raporty 0.5.1/0.5.2 i bieżące wymagania specyfikacji. Brak rozpoczętych testów/procesów. Stwierdzono w kodzie: RegionState zapisuje tylko TerrainId (gubi VariantId zastosowanej definicji); UI nadaje nową wersję również zmianie samej nazwy. UpdateTerrain i mgła aktualizują przez NextFill wszystkie pasujące pola. Źródło jest dekodowane raz w dialogu kadru, ale oba Paint wykonują pełnorozdzielczy HighQualityBicubic. Powtarzane referencje w Collect ponownie dekodują Base64 i porównują bajty; Rebuild puli ponownie dekoduje każdą miniaturę. Następna operacja: mały pomiar tych ścieżek i regresja regionu przed poprawką. Niepewnych operacji dyskowych brak.

## Etapy 1–4: implementacja w toku
FAIL regions przed w patch-053-regions-3471b4e5122a4295aa898b6a6cdcd38e; PASS podstawowej przegrody po w patch-053-regions-dbbcbebf9b3749019299641792163763. Nie powtarzać podstawowej próby.
Pomiary przed w patch-053-perf-64e8adbad4574bf78629915d54446597 (pierwsza próba perf miała błąd argumentu refleksji, bez pomiaru): budget150 9645.08ms, teren150 7.71ms, mgła150 10137.89ms, 8 miniatur tego samego źródła 268.58ms, po10 rysowań Pointy93.05ms/Flat91.45ms. Po (patch-053-perf-2417db52f039451ab31f47e92b550aff): 2.90/6.73/7.25/35.26/29.65/27.89ms. To lokalne pomiary metod/kontrolek, nie FPS ani dowód subiektywnej płynności.
Zaimplementowano VariantId+TerrainId regionu, zachowanie pozycji po FillVariant.Id, opcję preserve dla terenu/mgły, wersjonowanie bez zmiany nazwy/kolejności, usunięte warianty zachowują stare heksy. Zoptymalizowano powtarzane referencje immutable, cache miniatur puli, źródło podglądu1024 i bilinear. Źródła biblioteki zwalniają pełne bajty po walidacji; wybór przypina niezależny snapshot do dokumentu. Limity centralne32MiB/96MiB dokument/192MiB biblioteka; mapa256MiB/preset160MiB, pozostałe liczby bez zwiększeń.
Dodano znacznik czasu zakończenia modalnej edycji i filtr starych Down/doubleclick płótna, bez timera/DoEvents. Zaimplementowano kontrastowy obrys tekstu, uchwyty krawędzi tła, podgląd według canvas, buforowanie źródła/efektów z koalescencją najnowszego stanu, reset100%, Tło pierwsze w Widok. Poprawiono tekst historii i skan ID grafik przy pominiętych plikach. Główny EXE nadal0.5.2.
Pozostało: celowane nowe przypadki boundaries/updates/local/safety, kontrola modelu i limitów, zwykły końcowy EXE (wiadro/rozmieszczenie/kadry/tło), dokumentacja i build0.5.3 dopiero po ukończeniu. Brak własnych procesów/dialogów, niepewnych operacji dyskowych brak.

## Celowane próby zakończone
PASS boundaries patch-053-boundaries-b7766b0c8d8b44caa606d0f4bd946be3: warianty wspólnej wersji, graficzna przegroda nowszej wersji, osobne TerrainId, Fog, usunięta paleta, Undo/Redo i round-trip, nazwa/kolejność.
PASS updates patch-053-updates-7fd4f6df5fff408e9db10de36bdab430: preserve, kolejność/dodanie/usunięcie/typ, wcześniejsze wersje, bez apply, jawny reroll i dokładne Redo, mgła, osobne identyczne grafiki, zapis.
PASS local po poprawieniu sposobu wywołania przycisku w niewidocznym formularzu (PerformClick nie wywołuje Click niewidocznego przycisku): patch-053-local-53303912085f41fc982342c4dd8a79bd. Po rozszerzeniu osłony dwukliku i testu jej czasowo-przestrzennego warunku PASS local patch-053-local-32ae22bb17d148b7a9db8d9cc80d491c. Testy kontrolek obejmują 4 krawędzie, proporcje canvas2:1, reset, kontrast, kadry, brak gestu po modal i nowy gest. Nie jest to ocena płynności UI.
PASS safety patch-053-safety-b2f55d8506e646d0b6efe2d58d860388: prawdziwy import dokładnie32MiB i round-trip presetu, odrzucenie32MiB+1, budżety, pominięta grafika nieblokująca nowej i jej Undo/Redo, leniwa biblioteka/pinned dokument, smoke nowy preset/kopia Domyślnego. Fixture Proba053.hexmap jest w tym katalogu, do skopiowania do finalnej próby UI.
Doprecyzowano osłonę wejścia: oprócz starych timestampów odrzuca drugi Down tego samego kliknięcia w systemowym prostokącie/czasie dwukliku po przycisku zatwierdzenia/anulowania puli. Nowy gest w innym miejscu działa od razu; bez globalnej zwłoki, timera i DoEvents.
Pozostało tylko końcowe oznaczenie/build0.5.3, zwykły EXE i dokumentacja. Wszystkie zmiany zachowane, brak własnych okien/procesów; brak niepewnej operacji. Nie powtarzać zaliczonych metod.

## Końcowy EXE i próby okna — zakończone

Zbudowano główny EXE 0.5.3 / FileVersion 0.5.3.0. SHA-256 głównego artefaktu i użytej zwykłej kopii: `2B29C665B8C011475EE40F368FB08A6280FC2132433D39CC34EBF1137EE498B7`.
Kopia i wszystkie własne dane: `tests/_work/ui-053-146e55a1ff174b8a98897bda610a7233`.

- Wiadro w rzeczywistym oknie wypełniło tylko lewy obszar 70 pól; 10 pól przegrody nowej wersji i 70 różowych pozostało bez zmian.
- Edycja granatowego wariantu na #88BBFF z apply/preserve: zapis przed/po potwierdza 29 niezmienionych czerwonych, 41 zaktualizowanych niebieskich i 80 pozostałych bez zmian; 0 rozbieżności.
- Przy wznowieniu nie było okna. Ustalono, że ostatnia kopia After-update.hexmap istnieje i wynik powyższego porównania jest prawidłowy. Otworzono tę samą kopię EXE i własną mapę; nie ponawiano wiadra/aktualizacji.
- W kadrowaniu wykonano zoom, drag obu podglądów i suwak do 132°, następnie anulowano kadr. Oba wyniki odświeżone. Narzędzie nie rejestruje ciągłego przebiegu/FPS — płynność pozostaje do oceny użytkownika.
- Zmiana nazwy bez apply, następnie dwuklik Zatwierdź przy aktywnym Pędzlu: po zapisie 0 zmienionych payloadów heksów względem After-update. Kolejny świadomy klik naniósł jeden wybrany graficzny teren.
- Tło: prawy środkowy uchwyt zmienił tylko prawą granicę; podgląd ma proporcje płótna. Nasycenie 19→reset100, rozmycie8, krycie58, zatwierdzenie pokazało końcowy wynik. Widok ma kolejność Tło/Tereny/Ikony/Obiekty/Mgła.
- Własna mapa zapisana, okno zamknięte; lista okien procesu po zamknięciu pusta. Brak oczekujących zgód/dialogów i niepewnych operacji dyskowych.
- Runner053 bez Case zakończył się oczekiwanym błędem użycia przed kompilacją/testami.

Implementacja i wymagane reprezentatywne próby zakończone. Pozostało uzupełnienie/przegląd README, specyfikacji i raportu oraz lokalny commit. Nie budować ani nie powtarzać zaliczonych prób bez zmiany kodu. Artefakty prób pozostają ignorowane w _work, prywatna Library nietknięta.

## Finalizacja

Kod, testy i EXE zapisano w lokalnym `b14c6fa1f735828d5e289df96f4e2ce96b1b74b0`. README, specyfikacja i raport są ukończone, zawierają zasady wersji/pozycji puli, tabelę limitów, pomiary, faktyczne próby okna i jawne ograniczenia oraz dwie grupy checklisty. git diff --check przeszedł; przegląd staged wykluczył Library, mapy, logi i testowe EXE. Reguły ignore potwierdzone, brak śledzonych Library/_work.

Pozostaje wyłącznie commit tych czterech plików dokumentacji i potwierdzenie końcowego statusu Git. Brak otwartych własnych procesów/dialogów, oczekiwania na zgodę i niepewnych operacji. Nie powtarzać implementacji, builda ani zaliczonych prób. Po commicie dokumentacji i sprawdzeniu czystego statusu zadanie jest zakończone; jego hash podać w końcowej odpowiedzi. Bez publikacji.
